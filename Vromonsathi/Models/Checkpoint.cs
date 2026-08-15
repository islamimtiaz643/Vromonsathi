namespace Vromonsathi.Models
{
    public class Checkpoint
    {
        public int Id { get; set; }

        public int DestinationId { get; set; }
        public Destination? Destination { get; set; }

        public CheckpointType Type { get; set; }
        public string Name { get; set; } = string.Empty;

        public int SequenceOrder { get; set; }
        public double DistanceFromStartKm { get; set; }
        public string? Notes { get; set; }
    }
}