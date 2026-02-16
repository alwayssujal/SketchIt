using Microsoft.AspNetCore.SignalR;
using SketchIt.Models;
using System.Collections.Concurrent;

namespace SketchIt.Hubs
{
    public class GameHub : Hub
    {
        private readonly RoomStore _roomStore;
        private readonly IHubContext<GameHub> _hubContext;


        public GameHub(RoomStore roomStore, IHubContext<GameHub> hubContext)
        {
            _roomStore = roomStore;
            _hubContext = hubContext;
        }
        // HOST creates room
        public async Task CreateRoom(string playerName)
        {
            var roomCode = GenerateRoomCode();

            var room = new Room
            {
                RoomCode = roomCode,
                HostConnectionId = Context.ConnectionId,
                HostName = playerName,
                Players =
            {
                new Player
                {
                    Name = playerName,
                    ConnectionId = Context.ConnectionId,
                    IsHost = true,
                }
            }
            };

            _roomStore.CreateRoom(room);

            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

            await Clients.Caller.SendAsync("RoomCreated", roomCode, room.Players);
        }
        // PLAYER joins room
        public async Task JoinRoom(string code, string name)
        {
            if (!_roomStore.TryGetRoom(code, out var room))
            {
                await Clients.Caller.SendAsync("JoinFailed", "Room not found");
                return;
            }

            var player = new Player
            {
                Name = name,
                ConnectionId = Context.ConnectionId
            };

            var added = _roomStore.AddPlayer(code, player);
            if (!added)
            {
                await Clients.Caller.SendAsync("JoinFailed", "Unable to join room");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, code);

            await Clients.Group(code).SendAsync("PlayerJoined", code, room.Players);
            //await Clients.Caller.SendAsync("JoinedRoom",code);
            //await StartRound(code);
        }
        // ---------------- START GAME ----------------
        public async Task StartGame(string roomCode)
        {
            if (!_roomStore.TryGetRoom(roomCode, out var room)) return;
            if (room.HostConnectionId != Context.ConnectionId) return;

            room.GameStarted = true;

            var rnd = new Random();
            var drawer = room.Players[rnd.Next(room.Players.Count)];
            room.DrawerConnectionId = drawer.ConnectionId;

            room.CurrentWords = GetRandomWords(3);

            await Clients.Group(roomCode)
                .SendAsync("GameStarted", room.Players);

            await Clients.Group(roomCode)
                .SendAsync("DrawerAssigned",
                    drawer.ConnectionId,
                    room.CurrentWords);
        }
        public async Task WordSelected(string roomCode, string word)
        {
            if (!_roomStore.TryGetRoom(roomCode, out var room)) return;
            if (room.DrawerConnectionId != Context.ConnectionId) return;
            room.IsRoundActive = true;
            room.RoundEnded = false;

            foreach (var p in room.Players)
            {
                p.HasGuessedCorrectly = false;
            }

            room.CurrentWord = word;

            var drawer = room.Players.First(p => p.ConnectionId == Context.ConnectionId);

            var guessers = room.Players
                .Where(p => p.ConnectionId != room.DrawerConnectionId)
                .Select(p => p.ConnectionId)
                .ToList();

            await Clients.Clients(guessers)
                .SendAsync("DrawerAssignedToEveryone", drawer.Name, word.Length);

            await Clients.Client(drawer.ConnectionId)
                .SendAsync("SystemMessage", $"You're drawing: {word} ✏️");

            await Clients.Clients(guessers)
                .SendAsync("SystemMessage", "Guess the word!");
            // ✅ START ROUND TIMER
            room.IsRoundActive = true;
            StartRoundTimer(room);

            await Clients.Group(roomCode)
                .SendAsync("RoundStarted", room.RoundNumber, 60); // 60 seconds
        }
        public async Task GetPlayers(string code)
        {
            if (_roomStore.TryGetRoom(code, out var room))
            {
                var players = room.Players.Select(p => p.Name).ToList();
                await Clients.Caller.SendAsync("PlayerList", players);
            }
        }
        public async Task SetDrawer(string roomCode, string connectionId)
        {
            if (!_roomStore.TryGetRoom(roomCode, out var room))
                return;

            room.DrawerConnectionId = connectionId;

            await Clients.Group(roomCode)
                .SendAsync("DrawerChanged", connectionId);
        }
        // ------------ DRAWING EVENTS ----------------
        public async Task DrawLine(string code, float startX, float startY, float endX, float endY, string color, float lineWidth)
        {
            if (!_roomStore.TryGetRoom(code, out var room))
                return;

            // 🛑 BLOCK drawing if round ended
            if (!room.IsRoundActive)
                return;

            // 🛑 ONLY drawer can draw
            if (Context.ConnectionId != room.DrawerConnectionId)
                return;

            await Clients.GroupExcept(code, Context.ConnectionId)
                .SendAsync("ReceiveDrawLine", startX, startY, endX, endY, color, lineWidth);
        }
        public async Task Undo(string roomCode, List<List<StrokeSegment>> strokes)
        {
            await Clients.Group(roomCode)
                .SendAsync("ReceiveUndo", strokes);
        }
        public async Task ClearCanvas(string roomCode)
        {
            await Clients.Group(roomCode).SendAsync("ReceiveClearCanvas");
        }
        public async Task StrokeEnded(string roomCode)
        {
            await Clients.OthersInGroup(roomCode)
                .SendAsync("StrokeEnded");
        }
        public async Task SendChatMessage(string roomCode, string sender, string message)
        {
            if (!_roomStore.TryGetRoom(roomCode, out var room))
                return;

            var player = room.Players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            if (player == null)
                return;

            // ❌ Drawer cannot guess
            if (Context.ConnectionId == room.DrawerConnectionId)
            {
                await Clients.Group(roomCode)
                    .SendAsync("ReceiveChatMessage", sender, message);
                return;
            }

            // ✅ Correct guess
            if (!string.IsNullOrEmpty(room.CurrentWord) &&
            !player.HasGuessedCorrectly &&
            message.Equals(room.CurrentWord, StringComparison.OrdinalIgnoreCase))
                    {
                        player.HasGuessedCorrectly = true;

                        // ⏱ TIME-BASED BONUS
                        int basePoints = 10;
                        int bonus = Math.Min(room.RemainingSeconds, 50);
                        int totalPoints = basePoints + bonus;

                        player.Score += totalPoints;

                        var drawer = room.Players
                            .First(p => p.ConnectionId == room.DrawerConnectionId);
                        drawer.Score += 5;

                        await Clients.Group(roomCode)
                            .SendAsync("CorrectGuess",
                                player.Name,
                                totalPoints,
                                player.Score);

                        await Clients.Group(roomCode)
                            .SendAsync("ScoreUpdated",
                                room.Players.Select(p => new
                                {
                                    p.Name,
                                    p.Score
                                }));

                        // 🏁 end round if all guessers guessed
                        int totalGuessers = room.Players.Count - 1;
                        int guessedCount = room.Players.Count(p => p.HasGuessedCorrectly);

                        if (guessedCount >= totalGuessers)
                        {
                            await EndRound(roomCode, "AllGuessed");
                        }

                        return;
                    }


            // ❌ Normal chat
            await Clients.Group(roomCode)
                .SendAsync("ReceiveChatMessage", sender, message);
        }
        public async Task ChangeDrawer(string roomCode)
        {
            if (!_roomStore.TryGetRoom(roomCode, out var room))
                return;

            // 🔒 host-only
            //if (Context.ConnectionId != room.HostConnectionId)
            //    return;

            var available = room.Players
                .Where(p => p.ConnectionId != room.DrawerConnectionId)
                .ToList();

            if (!available.Any())
                return;

            var rnd = new Random();
            var newDrawer = available[rnd.Next(available.Count)];

            room.DrawerConnectionId = newDrawer.ConnectionId;

            await Clients.Group(roomCode)
                .SendAsync("DrawerChanged", newDrawer.ConnectionId);

            await Clients.Group(roomCode)
                .SendAsync("SystemMessage", $"{newDrawer.Name} is drawing now");
        }
        private void StartRoundTimer(Room room)
        {
            room.RoundCts?.Cancel();
            room.RoundCts = new CancellationTokenSource();
            var token = room.RoundCts.Token;

            _ = Task.Run(async () =>
            {
                int seconds = 60;
                room.RemainingSeconds = seconds;

                try
                {
                    while (seconds > 0 && !room.RoundEnded)
                    {
                        await Task.Delay(1000, token);
                        seconds--;
                        room.RemainingSeconds = seconds;

                        await _hubContext.Clients
                            .Group(room.RoomCode)
                            .SendAsync("RoundTimerTick", seconds);
                    }

                    if (!room.RoundEnded)
                    {
                        await EndRound(room.RoomCode, "TimeUp");
                    }
                }
                catch (TaskCanceledException) { }
            });
        }
        public async Task EndRound(string roomCode, string reason)
        {
            if (!_roomStore.TryGetRoom(roomCode, out var room))
                return;

            if (room.RoundEnded)
                return;

            room.RoundEnded = true;
            room.IsRoundActive = false;
            room.RoundCts?.Cancel();

            await _hubContext.Clients
                .Group(roomCode)
                .SendAsync("RoundEnded", new
                {
                    Word = room.CurrentWord,
                    Reason = reason
                });

            room.CurrentWord = null;
            room.RoundNumber++;

            if (room.RoundNumber > room.MaxRounds)
            {
                await _hubContext.Clients
                    .Group(roomCode)
                    .SendAsync("GameEnded",
                        room.Players
                            .OrderByDescending(p => p.Score)
                            .Select(p => new { p.Name, p.Score }));
                return;
            }

            await Task.Delay(3000);
            await StartNextRound(room);
        }
        private async Task StartNextRound(Room room)
        {
            if (room.Players.Count < 2)
                return;

            room.IsRoundActive = false;
            room.RoundEnded = false;
            room.RemainingSeconds = 0;

            foreach (var p in room.Players)
                p.HasGuessedCorrectly = false;

            await _hubContext.Clients.Group(room.RoomCode)
                .SendAsync("ReceiveClearCanvas"); // 🧹 CLEAR FIRST

            var nextDrawer = room.Players
                .OrderBy(_ => Guid.NewGuid())
                .First();

            room.DrawerConnectionId = nextDrawer.ConnectionId;
            room.CurrentWords = GetRandomWords(3);

            await _hubContext.Clients.Group(room.RoomCode)
                .SendAsync("DrawerAssigned",
                    nextDrawer.ConnectionId,
                    room.CurrentWords);
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var result = _roomStore.FindPlayer(Context.ConnectionId);
            if (result == null)
                return;

            var (room, player) = result.Value;

            // 🟥 HOST LEFT → DELETE ROOM
            if (player.IsHost)
            {
                room.RoundCts?.Cancel();

                await Clients.Group(room.RoomCode)
                    .SendAsync("SystemMessage", "🚨 Host disconnected. Room closed.");

                await Clients.Group(room.RoomCode)
                    .SendAsync("RoomClosed");

                _roomStore.RemoveRoom(room.RoomCode);
                return;
            }

            bool wasDrawer = player.ConnectionId == room.DrawerConnectionId;

            lock (room.SyncRoot)
            {
                room.Players.Remove(player);
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, room.RoomCode);

            await Clients.Group(room.RoomCode)
                .SendAsync("SystemMessage", $"👋 {player.Name} left the game.");

            await Clients.Group(room.RoomCode)
                .SendAsync("PlayerLeft", room.Players.Select(p => p.Name));

            if (wasDrawer && room.IsRoundActive)
            {
                await EndRound(room.RoomCode, "DrawerLeft");
            }

            await base.OnDisconnectedAsync(exception);
        }

        private List<string> GetRandomWords(int count)
        {
            var words = new[]
            {
                "Cat", "Dog", "Banana", "Pizza", "Robot",
                "Rocket", "Cupcake", "Rainbow", "Dinosaur", "Castle",
                "Dragon", "Elephant", "Sushi", "Giraffe", "Penguin",
                "Zombie", "Unicorn", "Mermaid", "Octopus", "Toothbrush",
                "Cactus", "Icecream", "Ninja", "Wizard", "Ghost",
                "Spider", "Monkey", "Lion", "Tiger", "Shark",
                "Crown", "Sword", "Moon", "Star", "Cloud",
                "Sun", "Balloon", "Car", "Train", "Bicycle",
                "Skateboard", "Rocketship", "Pumpkin", "Snowman", "Butterfly",
                "Flower", "Tree", "Pineapple", "Apple", "Bread",
                "Cheese", "Cake", "Cookie", "Donut", "Pie",
                "Watermelon", "Orange", "Strawberry", "Lemon", "Cherry",
                "Bear", "Frog", "Snake", "Bat", "Horse",
                "Sheep", "Cow", "Pig", "Fox", "Rabbit",
                "Bee", "Ant", "Fish", "Whale", "Dolphin",
                "Turtle", "Crab", "Octopus", "Lobster", "Parrot",
                "Owl", "Eagle", "Peacock", "Crow", "Flamingo",
                "Chair", "Table", "Lamp", "Book", "Phone",
                "Computer", "Glasses", "Hat", "Shoes", "Watch",
                "Key", "Door", "Window", "Bottle", "Pen",
                "Pencil", "Clock", "Bag", "Ring", "Necklace"
            };

            return words.OrderBy(_ => Guid.NewGuid())
                        .Take(count)
                        .ToList();
        }
        private Player GetRandomPlayer(Room room)
        {
            var rnd = new Random();
            return room.Players[rnd.Next(room.Players.Count)];
        }
        private string GenerateRoomCode()
            => Guid.NewGuid().ToString("N")[..4].ToUpper();
    }
}
