using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PatientManagementSystem.Data.Permissions;
using PatientManagementSystem.Models.Entities;

namespace PatientManagementSystem.Data.Seeders
{
    public static class PermissionSeeder
    {
        /// <summary>
        /// Idempotently inserts any missing permissions from <see cref="PermissionCatalog"/>.
        /// Existing permissions are NEVER updated, deleted, or re-IDed — so existing
        /// RolePermissions / UserPermissions references remain valid across reseeds.
        /// </summary>
        public static async Task SeedAsync(ApplicationDbContext db, ILogger logger)
        {
            var catalog = PermissionCatalog.All;
            var existingKeys = await db.Permissions
                .Select(p => p.Key)
                .ToListAsync();

            var existingKeySet = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var toInsert = catalog
                .Where(p => !existingKeySet.Contains(p.Key))
                .Select(p => new Permission
                {
                    Id = p.Id,
                    Key = p.Key,
                    Name = p.Name,
                    Description = p.Description,
                    Module = p.Module,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            if (toInsert.Count == 0)
            {
                logger.LogDebug("Permission catalog already complete ({Count} entries).", catalog.Count);
                return;
            }

            // EF inserts with the explicit Id we set (because Permission.Id is ValueGeneratedNever),
            // so there's no risk of identity-shift on reseed.
            await db.Permissions.AddRangeAsync(toInsert);
            await db.SaveChangesAsync();

            logger.LogInformation("Seeded {Count} permission(s): {Keys}",
                toInsert.Count,
                string.Join(", ", toInsert.Select(p => $"{p.Id}:{p.Key}")));
        }
    }
}