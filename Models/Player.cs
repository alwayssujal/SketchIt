namespace SketchIt.Models
{
    public class Player
    {
        public string Name { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public bool IsHost { get; set; } = false;
        public int Score { get; set; }
        public bool HasGuessedCorrectly { get; set; }
    }
}
