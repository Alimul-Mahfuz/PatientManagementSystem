using PatientManagementSystem.Models.Enums;

namespace PatientManagementSystem.Models.Entities
{
    public class Ward
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Floor { get; set; }
        public int Capacity { get; set; } = 1;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public ICollection<Bed> Beds { get; set; } = new List<Bed>();
    }

    public class Bed
    {
        public int Id { get; set; }
        public int WardId { get; set; }
        public Ward Ward { get; set; } = null!;
        public string Number { get; set; } = string.Empty; // bed label e.g. "W1-12A"
        public BedStatus Status { get; set; } = BedStatus.Available;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }

    public class BedAssignment
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
        public int BedId { get; set; }
        public Bed Bed { get; set; } = null!;
        public DateTime AdmissionDate { get; set; } = DateTime.UtcNow;
        public DateTime? DischargeDate { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
    }
}