using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatientManagementSystem.Data;
using PatientManagementSystem.Models.Entities;
using PatientManagementSystem.Models.ViewModels;

namespace PatientManagementSystem.Controllers
{
    [Authorize]
    public class ConditionsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ConditionsController(ApplicationDbContext db) { _db = db; }

        [HttpGet]
        public async Task<IActionResult> Index(string? q)
        {
            var query = _db.Conditions.Where(c => c.IsActive);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var s = q.Trim();
                query = query.Where(c => c.Name.Contains(s) || (c.Icd10Code != null && c.Icd10Code.Contains(s)));
            }
            var items = await query.OrderBy(c => c.Name)
                .Select(c => new ConditionViewModel
                {
                    Id = c.Id, Name = c.Name, Icd10Code = c.Icd10Code,
                    Description = c.Description, IsActive = c.IsActive, CreatedAt = c.CreatedAt
                }).ToListAsync();
            ViewData["q"] = q;
            return View(items);
        }

        [HttpGet]
        public IActionResult Create() => View(new ConditionViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ConditionViewModel vm)
        {
            if (!string.IsNullOrWhiteSpace(vm.Name) &&
                await _db.Conditions.AnyAsync(c => c.Name == vm.Name))
            {
                ModelState.AddModelError(nameof(vm.Name), "A condition with this name already exists.");
            }
            if (!ModelState.IsValid) return View(vm);
            var e = new Condition
            {
                Name = vm.Name.Trim(), Icd10Code = vm.Icd10Code?.Trim(),
                Description = vm.Description?.Trim(), IsActive = true
            };
            _db.Conditions.Add(e);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Condition '{e.Name}' created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var c = await _db.Conditions.FindAsync(id);
            if (c == null) return NotFound();
            return View(new ConditionViewModel
            {
                Id = c.Id, Name = c.Name, Icd10Code = c.Icd10Code,
                Description = c.Description, IsActive = c.IsActive, CreatedAt = c.CreatedAt
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ConditionViewModel vm)
        {
            vm.Id = id;
            if (!string.IsNullOrWhiteSpace(vm.Name) &&
                await _db.Conditions.AnyAsync(c => c.Name == vm.Name && c.Id != id))
            {
                ModelState.AddModelError(nameof(vm.Name), "Another condition uses this name.");
            }
            if (!ModelState.IsValid) return View(vm);
            var c = await _db.Conditions.FindAsync(id);
            if (c == null) return NotFound();
            c.Name = vm.Name.Trim();
            c.Icd10Code = vm.Icd10Code?.Trim();
            c.Description = vm.Description?.Trim();
            c.IsActive = vm.IsActive;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Condition '{c.Name}' updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _db.Conditions.FindAsync(id);
            if (c == null) return NotFound();
            c.IsActive = false;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Condition '{c.Name}' archived.";
            return RedirectToAction(nameof(Index));
        }
    }
}