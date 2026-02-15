namespace SketchIt.Models
{
    public class Room
    {
        public string RoomCode { get; set; } = string.Empty;
        public string HostConnectionId { get; set; } = string.Empty;
        public string? DrawerConnectionId { get; set; }
        public string HostName { get; set; } = string.Empty;
        public List<Player> Players { get; set; } = new List<Player>();
        public bool IsActive { get; set; } = true;
        public bool GameStarted { get; set; } = false;
        public List<string> CurrentWords { get; set; } = new();
        public string? CurrentWord { get; set; }
        public int RoundNumber { get; set; } = 1;
        public CancellationTokenSource? RoundCts { get; set; }
        public int RemainingSeconds { get; set; }
        public int MaxRounds { get; set; } = 5;
        public bool RoundEnded { get; set; }
        public bool IsRoundActive { get; set; }

    }
}
