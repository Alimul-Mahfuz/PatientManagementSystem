using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using PatientManagementSystem.Models.Entities;

namespace PatientManagementSystem.Data.Seeders
{
    public static class UserRoleSeeder
    {
        private const string AdminRole = "Admin";
        private const string AdminEmail = "admin@hospital.local";
        private const string AdminPassword = "Admin@123";
        private const string AdminFullName = "System Administrator";

        public static async Task SeedAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger logger)
        {
            // 1. Ensure the Admin role exists.
            if (!await roleManager.RoleExistsAsync(AdminRole))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(AdminRole));
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    logger.LogError("Failed to create {Role} role: {Errors}", AdminRole, errors);
                    return;
                }
                logger.LogInformation("Seeded role: {Role}", AdminRole);
            }

            // 2. Ensure the admin user exists.
            var admin = await userManager.FindByEmailAsync(AdminEmail);
            if (admin is null)
            {
                admin = new ApplicationUser
                {
                    UserName = AdminEmail,
                    Email = AdminEmail,
                    EmailConfirmed = true,
                    FullName = AdminFullName,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(admin, AdminPassword);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    logger.LogError("Failed to seed admin user {Email}: {Errors}", AdminEmail, errors);
                    return;
                }
                logger.LogInformation("Seeded admin user: {Email}", AdminEmail);
            }
            else
            {
                logger.LogDebug("Admin user already exists: {Email}", AdminEmail);
            }

            // 3. Ensure the admin user is in the Admin role (idempotent).
            if (!await userManager.IsInRoleAsync(admin, AdminRole))
            {
                var roleAssign = await userManager.AddToRoleAsync(admin, AdminRole);
                if (!roleAssign.Succeeded)
                {
                    var errors = string.Join(", ", roleAssign.Errors.Select(e => e.Description));
                    logger.LogWarning("Failed to assign {Role} to {Email}: {Errors}", AdminRole, AdminEmail, errors);
                }
                else
                {
                    logger.LogInformation("Assigned {Role} role to {Email}", AdminRole, AdminEmail);
                }
            }
        }
    }
}