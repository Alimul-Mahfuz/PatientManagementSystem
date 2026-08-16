using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using PatientManagementSystem.Models.Enums;

namespace PatientManagementSystem.Models.ViewModels
{
    public class PatientIndexViewModel
    {
        // filter form fields
        public string? Name { get; set; }
        public string? Mrn { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public Gender? Gender { get; set; }
        public PatientStatus? Status { get; set; }
    }

    public class PatientListItemDto
    {
        public int Id { get; set; }
        public string Mrn { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public PatientStatus Status { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PatientCreateViewModel
    {
        [StringLength(20, MinimumLength = 2, ErrorMessage = "MRN must be 2-20 characters.")]
        [Display(Name = "MRN")]
        public string Mrn { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Full name must be 2-150 characters.")]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Gender")]
        public Gender Gender { get; set; } = Gender.Unknown;

        [Display(Name = "Date of birth")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(40, ErrorMessage = "Phone must be at most 40 characters.")]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(300, ErrorMessage = "Address must be at most 300 characters.")]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [StringLength(150, ErrorMessage = "Emergency contact must be at most 150 characters.")]
        [Display(Name = "Emergency contact")]
        public string? EmergencyContact { get; set; }

        [StringLength(10)]
        [Display(Name = "Blood group")]
        public string? BloodGroup { get; set; }

        [StringLength(400)]
        [Display(Name = "Allergies")]
        public string? Allergies { get; set; }

        [Display(Name = "Patient image")]
        public IFormFile? PatientImage { get; set; }

        [Display(Name = "Report")]
        public IFormFile? Report { get; set; }

        [Display(Name = "Status")]
        public PatientStatus Status { get; set; } = PatientStatus.Outpatient;
    }

    public class PatientEditViewModel : PatientCreateViewModel
    {
        public int Id { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }

    public class PatientDetailsViewModel
    {
        public int Id { get; set; }
        public string Mrn { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? EmergencyContact { get; set; }
        public string? BloodGroup { get; set; }
        public string? Allergies { get; set; }
        public PatientStatus Status { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string? ImagePath { get; set; }
        public List<PatientReportViewModel> Reports { get; set; } = new();

        public int Age => DateOfBirth.HasValue
            ? (int)((DateTime.UtcNow - DateOfBirth.Value).TotalDays / 365.25)
            : 0;
    }

    public class PatientReportViewModel
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }

    public class PatientDataTablesRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; } = 10;

        [FromForm(Name = "search[value]")]
        public string? SearchValue { get; set; }

        [FromForm(Name = "filters[name]")]
        public string? Name { get; set; }

        [FromForm(Name = "filters[mrn]")]
        public string? Mrn { get; set; }

        [FromForm(Name = "filters[phone]")]
        public string? Phone { get; set; }

        [FromForm(Name = "filters[email]")]
        public string? Email { get; set; }

        [FromForm(Name = "filters[gender]")]
        public Gender? Gender { get; set; }

        [FromForm(Name = "filters[status]")]
        public PatientStatus? Status { get; set; }
    }
}
