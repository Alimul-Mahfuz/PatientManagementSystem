using PatientManagementSystem.Data;

namespace PatientManagementSystem.Services
{
    public class SidebarProviderService
    {
        protected readonly ApplicationDbContext _db;
        public SidebarProviderService(ApplicationDbContext db)
        {
            _db = db;
        }

        //public bool CanAccess(string permissionKey, IEnumerable<string> userPermissions)
        //{
        //    var permission = _db.Permissions.FirstOrDefault(p => p.Key == permissionKey);
        //    if (permission == null)
        //    {
        //        return false;
        //    }
        //    return userPermissions.Contains(permissionKey);
        //})

    }
}
