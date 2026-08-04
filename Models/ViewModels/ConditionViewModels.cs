using System.ComponentModel.DataAnnotations;
using PatientManagementSystem.Models.Entities;
using PatientManagementSystem.Models.Enums;

namespace PatientManagementSystem.Models.ViewModels
{
    public class ConditionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "ICD-10 must be at most 20 chars.")]
        [Display(Name = "ICD-10 code")]
        public string? Icd10Code { get; set; }

        [StringLength(400)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }

    public class PatientConditionViewModel
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientMrn { get; set; } = string.Empty;
        public int ConditionId { get; set; }
        public string ConditionName { get; set; } = string.Empty;
        public string? Icd10Code { get; set; }
        public DateTime DiagnosedDate { get; set; }
        public Severity Severity { get; set; } = Severity.Mild;
        public string? Notes { get; set; }
        public bool IsResolved { get; set; }
        public DateTime? ResolvedDate { get; set; }

        public List<ConditionOption> AvailableConditions { get; set; } = new();
    }

    public class ConditionOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Icd10Code { get; set; }
    }
}