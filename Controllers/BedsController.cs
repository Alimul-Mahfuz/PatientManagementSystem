using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatientManagementSystem.Data;
using PatientManagementSystem.Models.Entities;
using PatientManagementSystem.Models.Enums;
using PatientManagementSystem.Models.ViewModels;

namespace PatientManagementSystem.Controllers
{
    [Authorize]
    public class BedsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public BedsController(ApplicationDbContext db) { _db = db; }

        [HttpGet]
        public IActionResult Create(int wardId)
        {
            return View(new BedViewModel { WardId = wardId, Status = BedStatus.Available });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BedViewModel vm)
        {
            if (await _db.Beds.AnyAsync(b => b.WardId == vm.WardId && b.Number == vm.Number))
                ModelState.AddModelError(nameof(vm.Number), "Another bed in this ward uses this number.");
            if (!ModelState.IsValid) return View(vm);

            _db.Beds.Add(new Bed
            {
                WardId = vm.WardId,
                Number = vm.Number.Trim(),
                Status = vm.Status,
                Notes = vm.Notes?.Trim()
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Bed '{vm.Number}' added.";
            return RedirectToAction("Beds", "Wards", new { id = vm.WardId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var b = await _db.Beds.Include(x => x.Ward).FirstOrDefaultAsync(x => x.Id == id);
            if (b == null) return NotFound();
            return View(new BedViewModel
            {
                Id = b.Id, WardId = b.WardId, WardName = b.Ward.Name,
                Number = b.Number, Status = b.Status, Notes = b.Notes, IsActive = b.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BedViewModel vm)
        {
            vm.Id = id;
            if (await _db.Beds.AnyAsync(b => b.WardId == vm.WardId && b.Number == vm.Number && b.Id != id))
                ModelState.AddModelError(nameof(vm.Number), "Another bed in this ward uses this number.");
            if (!ModelState.IsValid) return View(vm);
            var b = await _db.Beds.FindAsync(id);
            if (b == null) return NotFound();
            b.Number = vm.Number.Trim();
            b.Status = vm.Status;
            b.Notes = vm.Notes?.Trim();
            b.IsActive = vm.IsActive;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Bed '{b.Number}' updated.";
            return RedirectToAction("Beds", "Wards", new { id = b.WardId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var b = await _db.Beds.FindAsync(id);
            if (b == null) return NotFound();
            if (b.Status == BedStatus.Occupied)
            {
                TempData["Error"] = "Cannot delete an occupied bed. Discharge or transfer first.";
                return RedirectToAction("Beds", "Wards", new { id = b.WardId });
            }
            b.IsActive = false;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Bed '{b.Number}' archived.";
            return RedirectToAction("Beds", "Wards", new { id = b.WardId });
        }
    }
}