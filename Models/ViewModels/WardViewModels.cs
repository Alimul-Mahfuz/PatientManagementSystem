using System.ComponentModel.DataAnnotations;
using PatientManagementSystem.Models.Entities;
using PatientManagementSystem.Models.Enums;

namespace PatientManagementSystem.Models.ViewModels
{
    public class WardViewModel
    {
        public int Id { get; set; }

        [Required] [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)] public string? Floor { get; set; }
        [Range(1, 500)] public int Capacity { get; set; } = 1;
        [StringLength(300)] public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        public int BedCount { get; set; }
        public int OccupiedBeds { get; set; }
        public int AvailableBeds => BedCount - OccupiedBeds;
    }

    public class BedViewModel
    {
        public int Id { get; set; }
        public int WardId { get; set; }
        public string WardName { get; set; } = string.Empty;

        [Required] [StringLength(20, MinimumLength = 1)]
        public string Number { get; set; } = string.Empty;
        public BedStatus Status { get; set; } = BedStatus.Available;
        [StringLength(300)] public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;

        public int? CurrentAssignmentId { get; set; }
        public string? CurrentPatientName { get; set; }
        public DateTime? AdmittedOn { get; set; }
    }

    public class BedAssignmentViewModel
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientMrn { get; set; } = string.Empty;
        public int BedId { get; set; }
        public string BedNumber { get; set; } = string.Empty;
        public string WardName { get; set; } = string.Empty;
        public DateTime AdmissionDate { get; set; }
        public DateTime? DischargeDate { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }

        public List<PatientOption> AvailablePatients { get; set; } = new();
        public List<BedOption> AvailableBeds { get; set; } = new();
    }

    public class PatientOption
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Mrn { get; set; } = string.Empty;
    }
    public class BedOption
    {
        public int Id { get; set; }
        public string Display { get; set; } = string.Empty;
    }
}