using PatientManagementSystem.Models.Enums;

namespace PatientManagementSystem.Models.Entities
{
    public class Patient
    {
        public int Id { get; set; }

        // MRN — short, human-readable unique identifier.
        public string Mrn { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;
        public Gender Gender { get; set; } = Gender.Unknown;
        public DateTime? DateOfBirth { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? EmergencyContact { get; set; }
        public string? BloodGroup { get; set; }
        public string? Allergies { get; set; }
        public string? ImagePath { get; set; }
        public PatientStatus Status { get; set; } = PatientStatus.Outpatient;

        public ICollection<PatientReport> Reports { get; set; } = new List<PatientReport>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true; // soft-delete flag
    }
}
