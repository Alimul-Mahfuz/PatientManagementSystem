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
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var vm = new UserIndexViewModel
            {
                Roles = _roleManager.Roles
                    .OrderBy(r => r.Name)
                    .Select(r => new RoleOption { Id = r.Id, Name = r.Name ?? string.Empty })
                    .ToList()
            };
            return View(vm);
        }

        /// <summary>
        /// Server-side DataTables endpoint. Returns paged, filtered, sortable user rows
        /// in the DataTables AJAX response shape.
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Search([FromForm] DataTablesRequest req)
        {
            var query = _db.Users.AsQueryable();

            // ---- Individual filters ----
            if (!string.IsNullOrWhiteSpace(req.Name))
            {
                var n = req.Name.Trim();
                query = query.Where(u => (u.FullName != null && u.FullName.Contains(n)) ||
                                         (u.UserName != null && u.UserName.Contains(n)));
            }

            if (!string.IsNullOrWhiteSpace(req.Email))
            {
                var e = req.Email.Trim();
                query = query.Where(u => u.Email != null && u.Email.Contains(e));
            }

            if (!string.IsNullOrWhiteSpace(req.Phone))
            {
                var p = req.Phone.Trim();
                query = query.Where(u => u.PhoneNumber != null && u.PhoneNumber.Contains(p));
            }

            if (string.Equals(req.Status, "active", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(u => u.IsActive);
            }
            else if (string.Equals(req.Status, "disabled", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(u => !u.IsActive);
            }

            // Global DataTables search box — falls back across name/email/phone.
            if (!string.IsNullOrWhiteSpace(req.SearchValue))
            {
                var s = req.SearchValue.Trim();
                query = query.Where(u =>
                    (u.FullName != null && u.FullName.Contains(s)) ||
                    (u.UserName != null && u.UserName.Contains(s)) ||
                    (u.Email != null && u.Email.Contains(s)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(s)));
            }

            // Role filter — needs user-role join.
            List<string>? userIdsInRole = null;
            if (!string.IsNullOrWhiteSpace(req.RoleId))
            {
                userIdsInRole = await _db.UserRoles
                    .Where(ur => ur.RoleId == req.RoleId)
                    .Select(ur => ur.UserId!)
                    .ToListAsync();

                if (userIdsInRole.Count == 0)
                {
                    // Role has no members — short-circuit.
                    return Ok(new
                    {
                        draw = req.Draw,
                        recordsTotal = 0,
                        recordsFiltered = 0,
                        data = Array.Empty<object>()
                    });
                }

                query = query.Where(u => userIdsInRole.Contains(u.Id));
            }

            var totalRecords = await _db.Users.CountAsync();
            var filteredCount = await query.CountAsync();

            // Server-side paging.
            var pageUsers = await query
                .OrderBy(u => u.FullName)
                .Skip(req.Start)
                .Take(req.Length <= 0 ? 10 : req.Length)
                .ToListAsync();

            // ---- Roles for the page (batched) ----
            var pageUserIds = pageUsers.Select(u => u.Id).ToList();
            var rolePairs = await (
                from ur in _db.UserRoles
                join r in _db.Roles on ur.RoleId equals r.Id
                where pageUserIds.Contains(ur.UserId)
                select new { ur.UserId, RoleName = r.Name ?? "" }
            ).ToListAsync();

            var roleMap = rolePairs
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).ToList());

            var rows = pageUsers.Select(u => new UserListItemDto
            {
                Id = u.Id,
                FullName = string.IsNullOrWhiteSpace(u.FullName) ? (u.UserName ?? "(no name)") : u.FullName,
                Email = u.Email ?? string.Empty,
                PhoneNumber = u.PhoneNumber ?? string.Empty,
                Role = roleMap.ContainsKey(u.Id) ? string.Join(", ", roleMap[u.Id].OrderBy(r => r)) : "—",
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            }).ToList();

            return Ok(new
            {
                draw = req.Draw,
                recordsTotal = totalRecords,
                recordsFiltered = filteredCount,
                data = rows
            });
        }

        // ============================================================
        // CREATE
        // ============================================================

        [HttpGet]
        public IActionResult Create()
        {
            var vm = new UserCreateViewModel
            {
                Roles = BuildRoleOptions()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel vm)
        {
            vm.Roles = BuildRoleOptions();

            // Email uniqueness (Identity enforces this too, but we surface a friendly error).
            var existing = await _userManager.FindByEmailAsync(vm.Email);
            if (existing != null)
            {
                ModelState.AddModelError(nameof(vm.Email), "A user with this email already exists.");
            }

            // Role existence (selected role id must match a real role row).
            IdentityRole? selectedRole = null;
            if (!string.IsNullOrWhiteSpace(vm.RoleId))
            {
                selectedRole = await _roleManager.FindByIdAsync(vm.RoleId);
                if (selectedRole == null)
                {
                    ModelState.AddModelError(nameof(vm.RoleId), "Selected role does not exist.");
                }
            }

            if (!ModelState.IsValid) return View(vm);

            var user = new ApplicationUser
            {
                UserName = vm.Email.Trim(),
                Email = vm.Email.Trim(),
                FullName = vm.FullName.Trim(),
                PhoneNumber = vm.PhoneNumber?.Trim(),
                IsActive = vm.IsActive,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, vm.Password);
            if (!createResult.Succeeded)
            {
                foreach (var e in createResult.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }

            if (selectedRole != null)
            {
                await _userManager.AddToRoleAsync(user, selectedRole.Name!);
            }

            TempData["Success"] = $"User '{user.FullName}' created.";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // EDIT
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roleNames = await _userManager.GetRolesAsync(user);
            var role = roleNames.FirstOrDefault();
            var roleId = string.Empty;
            if (!string.IsNullOrEmpty(role))
            {
                roleId = (await _roleManager.FindByNameAsync(role))?.Id ?? string.Empty;
            }

            var vm = new UserEditViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                RoleId = roleId,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = BuildRoleOptions()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserEditViewModel vm)
        {
            vm.Id = id;
            vm.Roles = BuildRoleOptions();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Email uniqueness (only if changed).
            if (!string.Equals(user.Email, vm.Email, StringComparison.OrdinalIgnoreCase))
            {
                var existing = await _userManager.FindByEmailAsync(vm.Email);
                if (existing != null && existing.Id != user.Id)
                {
                    ModelState.AddModelError(nameof(vm.Email), "Another user already uses this email.");
                }
            }

            // Role existence.
            IdentityRole? selectedRole = null;
            if (!string.IsNullOrWhiteSpace(vm.RoleId))
            {
                selectedRole = await _roleManager.FindByIdAsync(vm.RoleId);
                if (selectedRole == null)
                {
                    ModelState.AddModelError(nameof(vm.RoleId), "Selected role does not exist.");
                }
            }

            if (!ModelState.IsValid) return View(vm);

            user.FullName = vm.FullName.Trim();
            user.PhoneNumber = vm.PhoneNumber?.Trim();
            user.IsActive = vm.IsActive;

            // Email is the canonical sign-in handle — update UserName + Email together.
            if (!string.Equals(user.Email, vm.Email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = vm.Email.Trim();
                user.UserName = vm.Email.Trim();
                user.NormalizedEmail = vm.Email.Trim().ToUpperInvariant();
                user.NormalizedUserName = vm.Email.Trim().ToUpperInvariant();
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var e in updateResult.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }

            // Re-sync role membership.
            var currentRoles = (await _userManager.GetRolesAsync(user)).ToList();
            if (currentRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (selectedRole != null)
                await _userManager.AddToRoleAsync(user, selectedRole.Name!);

            TempData["Success"] = $"User '{user.FullName}' updated.";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // RESET PASSWORD
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var vm = new ResetPasswordViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, ResetPasswordViewModel vm)
        {
            vm.Id = id;
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Strip the token (Identity admin reset doesn't need one here — we're acting
            // as the administrator). RemoveVerificationToken optional.
            if (!ModelState.IsValid) return View(vm);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, vm.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(vm);
            }

            TempData["Success"] = $"Password reset for '{user.Email}'.";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // USER-SPECIFIC PERMISSIONS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Permissions(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Effective role (single role per user in this app).
            var roleNames = await _userManager.GetRolesAsync(user);
            var roleName = roleNames.FirstOrDefault() ?? "(no role)";

            // Permission IDs granted by the user's role (so we can mark "inherited").
            var rolePermissionIds = new HashSet<int>();
            if (roleNames.Any())
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    rolePermissionIds = await _db.RolePermissions
                        .Where(rp => rp.RoleId == role.Id)
                        .Select(rp => rp.PermissionId)
                        .ToHashSetAsync();
                }
            }

            // Existing per-user overrides.
            var userOverrides = await _db.UserPermissions
                .Where(up => up.UserId == user.Id)
                .ToDictionaryAsync(up => up.PermissionId, up => up.IsGranted);

            var groups = PermissionCatalog.All
                .GroupBy(p => p.Module)
                .OrderBy(g => g.Key)
                .Select(g => new UserPermissionGroupViewModel
                {
                    Module = g.Key,
                    Permissions = g.OrderBy(p => p.Id).Select(p =>
                    {
                        var state = "inherit";
                        if (userOverrides.TryGetValue(p.Id, out var granted))
                        {
                            state = granted ? "allow" : "deny";
                        }
                        return new UserPermissionEntryViewModel
                        {
                            Id = p.Id,
                            Key = p.Key,
                            Name = p.Name,
                            Description = p.Description,
                            InheritedFromRole = rolePermissionIds.Contains(p.Id),
                            State = state
                        };
                    }).ToList()
                }).ToList();

            var vm = new UserPermissionsViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                RoleName = roleName,
                Groups = groups
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Permissions(string id, UserPermissionsViewModel vm)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            vm.Id = user.Id;
            vm.FullName = user.FullName;
            vm.Email = user.Email ?? string.Empty;

            // Re-rebuild group metadata (inherited flags) since the form only POSTs State.
            var roleNames = await _userManager.GetRolesAsync(user);
            var roleName = roleNames.FirstOrDefault() ?? "(no role)";
            vm.RoleName = roleName;

            var rolePermissionIds = new HashSet<int>();
            if (roleNames.Any())
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    rolePermissionIds = await _db.RolePermissions
                        .Where(rp => rp.RoleId == role.Id)
                        .Select(rp => rp.PermissionId)
                        .ToHashSetAsync();
                }
            }

            // Re-attach inherited flag per permission for display.
            var submittedStates = vm.Groups
                .SelectMany(g => g.Permissions)
                .Where(p => p != null && p.Id > 0)
                .ToDictionary(p => p.Id, p => p.State ?? "inherit");

            // Rebuild the groups with current inherited flags + submitted states (so form
            // preserves what was just chosen on validation failures or stale pages).
            vm.Groups = PermissionCatalog.All
                .GroupBy(p => p.Module)
                .OrderBy(g => g.Key)
                .Select(g => new UserPermissionGroupViewModel
                {
                    Module = g.Key,
                    Permissions = g.OrderBy(p => p.Id).Select(p =>
                    {
                        var st = submittedStates.TryGetValue(p.Id, out var s) ? s : "inherit";
                        return new UserPermissionEntryViewModel
                        {
                            Id = p.Id,
                            Key = p.Key,
                            Name = p.Name,
                            Description = p.Description,
                            InheritedFromRole = rolePermissionIds.Contains(p.Id),
                            State = st
                        };
                    }).ToList()
                }).ToList();

            // Sync UserPermissions table with submitted states.
            var existing = await _db.UserPermissions
                .Where(up => up.UserId == user.Id)
                .ToListAsync();
            var existingByPerm = existing.ToDictionary(up => up.PermissionId);

            var validPermissionIds = PermissionCatalog.All.Select(p => p.Id).ToHashSet();
            var now = DateTime.UtcNow;

            foreach (var (permId, state) in submittedStates)
            {
                if (!validPermissionIds.Contains(permId)) continue; // guard against tampering

                if (state == "inherit")
                {
                    if (existingByPerm.TryGetValue(permId, out var row))
                    {
                        _db.UserPermissions.Remove(row);
                    }
                    continue;
                }

                var granted = string.Equals(state, "allow", StringComparison.OrdinalIgnoreCase);
                if (existingByPerm.TryGetValue(permId, out var existingRow))
                {
                    if (existingRow.IsGranted != granted)
                    {
                        existingRow.IsGranted = granted;
                        existingRow.UpdatedAt = now;
                    }
                }
                else
                {
                    _db.UserPermissions.Add(new UserPermission
                    {
                        UserId = user.Id,
                        PermissionId = permId,
                        IsGranted = granted,
                        UpdatedAt = now
                    });
                }
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Permissions updated for '{user.FullName}'.";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // TOGGLE ACTIVE (AJAX)
        // ============================================================

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound(new { ok = false, error = "User not found." });

            user.IsActive = !user.IsActive;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    ok = false,
                    error = string.Join("; ", result.Errors.Select(e => e.Description))
                });
            }

            return Ok(new { ok = true, isActive = user.IsActive });
        }

        // ============================================================
        // HELPERS
        // ============================================================

        private List<RoleOption> BuildRoleOptions()
            => _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => new RoleOption { Id = r.Id, Name = r.Name ?? string.Empty })
                .ToList();
    }
}