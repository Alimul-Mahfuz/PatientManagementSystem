using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace PatientManagementSystem.Models.ViewModels
{
    public class UserIndexViewModel
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? RoleId { get; set; }
        public string? Status { get; set; } // "active" | "disabled" | null

        public List<RoleOption> Roles { get; set; } = new();
    }

    public class RoleOption
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class UserListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Full name must be 2-150 characters.")]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone")]
        [StringLength(30, ErrorMessage = "Phone must be at most 30 characters.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm your password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "Role")]
        public string? RoleId { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public List<RoleOption> Roles { get; set; } = new();
    }

    public class UserEditViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Full name must be 2-150 characters.")]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Phone")]
        [StringLength(30, ErrorMessage = "Phone must be at most 30 characters.")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Role")]
        public string? RoleId { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public List<RoleOption> Roles { get; set; } = new();
    }

    public class ResetPasswordViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm the new password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Per-user permission override page VM. Each permission row can be:
    /// "inherit" (no UserPermissions row — falls back to the role grant),
    /// "allow"   (UserPermissions.IsGranted = true,  overrides role),
    /// "deny"    (UserPermissions.IsGranted = false, overrides role).
    /// </summary>
    public class UserPermissionsViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = "(no role)";

        public List<UserPermissionGroupViewModel> Groups { get; set; } = new();
    }

    public class UserPermissionGroupViewModel
    {
        public string Module { get; set; } = string.Empty;
        public List<UserPermissionEntryViewModel> Permissions { get; set; } = new();
    }

    public class UserPermissionEntryViewModel
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>True if the user's role grants this permission.</summary>
        public bool InheritedFromRole { get; set; }

        /// <summary>"inherit" | "allow" | "deny". Supplied by the form on POST.</summary>
        public string State { get; set; } = "inherit";
    }

    /// <summary>
    /// Mirrors the server-side DataTables request payload.
    /// </summary>
    public class DataTablesRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; } = 10;

        // DataTables sends these as form fields when type:POST.
        [FromForm(Name = "search[value]")]
        public string? SearchValue { get; set; }

        // Custom filter fields appended to the AJAX request.
        [FromForm(Name = "filters[name]")]
        public string? Name { get; set; }

        [FromForm(Name = "filters[email]")]
        public string? Email { get; set; }

        [FromForm(Name = "filters[phone]")]
        public string? Phone { get; set; }

        [FromForm(Name = "filters[roleId]")]
        public string? RoleId { get; set; }

        [FromForm(Name = "filters[status]")]
        public string? Status { get; set; }
    }
}