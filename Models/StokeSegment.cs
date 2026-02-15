namespace SketchIt.Models
{
    public class StrokeSegment
    {
        public float StartX { get; set; }
        public float StartY { get; set; }
        public float EndX { get; set; }
        public float EndY { get; set; }

        public string Color { get; set; } = "#000";
        public float Width { get; set; }
    }
}
