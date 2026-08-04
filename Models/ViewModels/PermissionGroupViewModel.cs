namespace PatientManagementSystem.Models.ViewModels
{
    public class PermissionGroupViewModel
    {
        public string Module { get; set; } = string.Empty;
        public List<PermissionCheckboxViewModel> Permissions { get; set; } = new();
    }

    public class PermissionCheckboxViewModel
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}