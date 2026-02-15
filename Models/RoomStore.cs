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

            lock (room)
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

            lock (room)
            {
                room.Players.RemoveAll(p => p.ConnectionId == connectionId);
            }
        }

    }

}
