using System.ComponentModel.DataAnnotations;

namespace PatientManagementSystem.Models.ViewModels
{
    public class RoleViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Role name is required.")]
        [StringLength(60, MinimumLength = 2, ErrorMessage = "Role name must be 2-60 characters.")]
        [Display(Name = "Role name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        public List<int> SelectedPermissionIds { get; set; } = new();
        public List<PermissionGroupViewModel> PermissionGroups { get; set; } = new();

        public int UserCount { get; set; }
        public int PermissionCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}