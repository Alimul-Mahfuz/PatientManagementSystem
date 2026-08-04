using PatientManagementSystem.Models.Enums;

namespace PatientManagementSystem.Models.Entities
{
    public class Condition
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Icd10Code { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Per-patient link to a condition with diagnosis metadata.
    /// Many-to-many: Patient ↔ Condition.
    /// </summary>
    public class PatientCondition
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
        public int ConditionId { get; set; }
        public Condition Condition { get; set; } = null!;
        public DateTime DiagnosedDate { get; set; } = DateTime.UtcNow;
        public Severity Severity { get; set; } = Severity.Mild;
        public string? Notes { get; set; }
        public bool IsResolved { get; set; } = false;
        public DateTime? ResolvedDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}