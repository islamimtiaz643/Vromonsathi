namespace Vromonsathi.Models
{
    public class EmergencyContact
    {
        public int Id { get; set; }

        public int? DestinationId { get; set; }
        public Destination? Destination { get; set; }

        public EmergencyContactType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}