using System.Collections.Concurrent;

namespace SketchIt.Models
{
    public class RoomStore
    {
        private readonly ConcurrentDictionary<string, Room> _rooms = new();
        public IEnumerable<KeyValuePair<string, Room>> GetAllRooms() => _rooms;
        public RoomStore()
        {
            Console.WriteLine("RoomStore created: " + GetHashCode());
        }
        public bool CreateRoom(Room room)
            => _rooms.TryAdd(room.RoomCode, room);

        public bool TryGetRoom(string code, out Room room)
            => _rooms.TryGetValue(code, out room);

        public bool RemoveRoom(string code)
            => _rooms.TryRemove(code, out _);

        public bool AddPlayer(string code, Player player)
        {
            if (!_rooms.TryGetValue(code, out var room))
                return false;

            lock (room.SyncRoot)
            {
                if (room.Players.Any(p => p.ConnectionId == player.ConnectionId))
                    return false;

                room.Players.Add(player);
                return true;
            }
        }

        public void RemovePlayer(string code, string connectionId)
        {
            if (!_rooms.TryGetValue(code, out var room))
                return;

            lock (room.SyncRoot)
            {
                room.Players.RemoveAll(p => p.ConnectionId == connectionId);
            }
        }
        public (Room room, Player player)? FindPlayer(string connectionId)
        {
            foreach (var room in _rooms.Values)
            {
                lock (room.SyncRoot)
                {
                    var player = room.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
                    if (player != null)
                        return (room, player);
                }
            }
            return null;
        }


    }

}
