using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatientManagementSystem.Data;
using PatientManagementSystem.Data.Permissions;
using PatientManagementSystem.Models.Entities;
using PatientManagementSystem.Models.ViewModels;

namespace PatientManagementSystem.Controllers
{
    [Authorize]
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public RolesController(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            var ids = roles.Select(r => r.Id).ToList();

            var permCounts = await _db.RolePermissions
                .Where(rp => ids.Contains(rp.RoleId))
                .GroupBy(rp => rp.RoleId)
                .Select(g => new { RoleId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RoleId, x => x.Count);

            var userCounts = new Dictionary<string, int>();
            foreach (var role in roles)
            {
                userCounts[role.Id] = await _userManager.GetUsersInRoleAsync(role.Name!) is { } users
                    ? users.Count
                    : 0;
            }

            // For display of when each role was created — IdentityRole has no CreatedAt;
            // we approximate using the minimum GrantedAt of its role_permissions, or DateTime.UtcNow.
            var createdMap = await _db.RolePermissions
                .Where(rp => ids.Contains(rp.RoleId))
                .GroupBy(rp => rp.RoleId)
                .Select(g => new { RoleId = g.Key, First = g.Min(x => x.GrantedAt) })
                .ToDictionaryAsync(x => x.RoleId, x => x.First);

            var vm = roles.Select(r => new RoleViewModel
            {
                Id = r.Id,
                Name = r.Name ?? string.Empty,
                UserCount = userCounts.GetValueOrDefault(r.Id, 0),
                PermissionCount = permCounts.GetValueOrDefault(r.Id, 0),
                CreatedAt = createdMap.GetValueOrDefault(r.Id, DateTime.UtcNow)
            }).ToList();

            return View(vm);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var vm = new RoleViewModel
            {
                PermissionGroups = BuildPermissionGroups(selected: null)
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleViewModel vm)
        {
            if (await _roleManager.RoleExistsAsync(vm.Name))
            {
                ModelState.AddModelError(nameof(vm.Name), "A role with this name already exists.");
            }

            if (!ModelState.IsValid)
            {
                vm.PermissionGroups = BuildPermissionGroups(vm.SelectedPermissionIds);
                return View(vm);
            }

            var role = new IdentityRole(vm.Name.Trim());
            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                vm.PermissionGroups = BuildPermissionGroups(vm.SelectedPermissionIds);
                return View(vm);
            }

            await SyncRolePermissions(role.Id, vm.SelectedPermissionIds);

            TempData["Success"] = $"Role '{role.Name}' created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role is null) return NotFound();

            var selectedIds = await _db.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var vm = new RoleViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                SelectedPermissionIds = selectedIds,
                PermissionGroups = BuildPermissionGroups(selectedIds)
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, RoleViewModel vm)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role is null) return NotFound();

            vm.Id = role.Id;

            if (!string.Equals(role.Name, vm.Name, StringComparison.OrdinalIgnoreCase) &&
                await _roleManager.RoleExistsAsync(vm.Name))
            {
                ModelState.AddModelError(nameof(vm.Name), "Another role already uses this name.");
            }

            if (!ModelState.IsValid)
            {
                vm.PermissionGroups = BuildPermissionGroups(vm.SelectedPermissionIds);
                return View(vm);
            }

            role.Name = vm.Name.Trim();
            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                vm.PermissionGroups = BuildPermissionGroups(vm.SelectedPermissionIds);
                return View(vm);
            }

            await SyncRolePermissions(role.Id, vm.SelectedPermissionIds);

            TempData["Success"] = $"Role '{role.Name}' updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role is null) return NotFound();

            var selectedIds = await _db.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var selectedSet = selectedIds.ToHashSet();
            var groups = BuildPermissionGroups(selectedIds)
                .Where(g => g.Permissions.Any(p => p.IsSelected))
                .ToList();

            var users = await _userManager.GetUsersInRoleAsync(role.Name!);

            var vm = new RoleViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                PermissionGroups = groups,
                UserCount = users.Count,
                PermissionCount = selectedIds.Count
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role is null) return NotFound();

            if (string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "The built-in Admin role cannot be deleted.";
                return RedirectToAction(nameof(Index));
            }

            var users = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (users.Count > 0)
            {
                TempData["Error"] = $"Cannot delete '{role.Name}' — {users.Count} user(s) are still assigned.";
                return RedirectToAction(nameof(Index));
            }

            await _db.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .ExecuteDeleteAsync();

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = $"Role '{role.Name}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ---- helpers ----

        private List<PermissionGroupViewModel> BuildPermissionGroups(IEnumerable<int>? selected)
        {
            var set = (selected ?? Enumerable.Empty<int>()).ToHashSet();
            return PermissionCatalog.All
                .GroupBy(p => p.Module)
                .OrderBy(g => g.Key)
                .Select(g => new PermissionGroupViewModel
                {
                    Module = g.Key,
                    Permissions = g.OrderBy(p => p.Id)
                        .Select(p => new PermissionCheckboxViewModel
                        {
                            Id = p.Id,
                            Key = p.Key,
                            Name = p.Name,
                            Description = p.Description,
                            IsSelected = set.Contains(p.Id)
                        }).ToList()
                }).ToList();
        }

        private async Task SyncRolePermissions(string roleId, IEnumerable<int> desiredIds)
        {
            var desired = desiredIds.Distinct().ToHashSet();
            var existing = await _db.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            var existingIds = existing.Select(rp => rp.PermissionId).ToHashSet();

            // add new grants
            var toAdd = desired.Except(existingIds)
                .Select(pid => new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = pid,
                    GrantedAt = DateTime.UtcNow
                }).ToList();
            if (toAdd.Count > 0) await _db.RolePermissions.AddRangeAsync(toAdd);

            // remove revoked grants
            var toRemove = existing.Where(rp => !desired.Contains(rp.PermissionId)).ToList();
            if (toRemove.Count > 0) _db.RolePermissions.RemoveRange(toRemove);

            await _db.SaveChangesAsync();
        }
    }
}