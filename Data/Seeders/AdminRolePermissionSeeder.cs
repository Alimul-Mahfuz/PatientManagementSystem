using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PatientManagementSystem.Data.Permissions;
using PatientManagementSystem.Models.Entities;

namespace PatientManagementSystem.Data.Seeders
{
    public static class AdminRolePermissionSeeder
    {
        private const string AdminRole = "Admin";

        /// <summary>
        /// Grants every permission in <see cref="PermissionCatalog"/> to the Admin role.
        /// Idempotent — only adds permissions the role does not already have.
        /// Never removes permissions, so manual tweaks survive reseed.
        /// </summary>
        public static async Task SeedAsync(
            ApplicationDbContext db,
            RoleManager<IdentityRole> roleManager,
            ILogger logger)
        {
            var admin = await roleManager.FindByNameAsync(AdminRole);
            if (admin is null)
            {
                logger.LogWarning("Admin role not found — skipping permission grant.");
                return;
            }

            var grantedIds = await db.RolePermissions
                .Where(rp => rp.RoleId == admin.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();
            var grantedSet = grantedIds.ToHashSet();

            var allPermissionIds = PermissionCatalog.All.Select(p => p.Id).ToList();
            var toAdd = allPermissionIds
                .Where(id => !grantedSet.Contains(id))
                .Select(id => new RolePermission
                {
                    RoleId = admin.Id,
                    PermissionId = id,
                    GrantedAt = DateTime.UtcNow
                })
                .ToList();

            if (toAdd.Count == 0)
            {
                logger.LogDebug("Admin role already has all {Count} permissions.", allPermissionIds.Count);
                return;
            }

            await db.RolePermissions.AddRangeAsync(toAdd);
            await db.SaveChangesAsync();
            logger.LogInformation("Granted {Count} permission(s) to {Role}.", toAdd.Count, AdminRole);
        }
    }
}