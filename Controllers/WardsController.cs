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
    public class WardsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public WardsController(ApplicationDbContext db) { _db = db; }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var wards = await _db.Wards
                .Where(w => w.IsActive)
                .OrderBy(w => w.Name)
                .Select(w => new WardViewModel
                {
                    Id = w.Id,
                    Name = w.Name,
                    Floor = w.Floor,
                    Capacity = w.Capacity,
                    Description = w.Description,
                    BedCount = w.Beds.Count(b => b.IsActive),
                    OccupiedBeds = w.Beds.Count(b => b.IsActive && b.Status == BedStatus.Occupied)
                })
                .ToListAsync();
            return View(wards);
        }

        [HttpGet]
        public IActionResult Create() => View(new WardViewModel { Capacity = 10 });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WardViewModel vm)
        {
            if (await _db.Wards.AnyAsync(w => w.Name == vm.Name))
                ModelState.AddModelError(nameof(vm.Name), "A ward with this name already exists.");
            if (!ModelState.IsValid) return View(vm);

            var w = new Ward
            {
                Name = vm.Name.Trim(),
                Floor = vm.Floor?.Trim(),
                Capacity = vm.Capacity,
                Description = vm.Description?.Trim()
            };
            _db.Wards.Add(w);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Ward '{w.Name}' created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var w = await _db.Wards.FindAsync(id);
            if (w == null) return NotFound();
            return View(new WardViewModel
            {
                Id = w.Id, Name = w.Name, Floor = w.Floor,
                Capacity = w.Capacity, Description = w.Description, IsActive = w.IsActive
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WardViewModel vm)
        {
            vm.Id = id;
            if (await _db.Wards.AnyAsync(w => w.Name == vm.Name && w.Id != id))
                ModelState.AddModelError(nameof(vm.Name), "Another ward uses this name.");
            if (!ModelState.IsValid) return View(vm);

            var w = await _db.Wards.FindAsync(id);
            if (w == null) return NotFound();
            w.Name = vm.Name.Trim();
            w.Floor = vm.Floor?.Trim();
            w.Capacity = vm.Capacity;
            w.Description = vm.Description?.Trim();
            w.IsActive = vm.IsActive;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Ward '{w.Name}' updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var w = await _db.Wards.Include(x => x.Beds).FirstOrDefaultAsync(x => x.Id == id);
            if (w == null) return NotFound();
            if (w.Beds.Any(b => b.IsActive && b.Status == BedStatus.Occupied))
            {
                TempData["Error"] = "Cannot archive a ward with occupied beds.";
                return RedirectToAction(nameof(Index));
            }
            w.IsActive = false;
            foreach (var b in w.Beds) b.IsActive = false;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Ward '{w.Name}' archived.";
            return RedirectToAction(nameof(Index));
        }

        // ---- Ward beds view ----
        [HttpGet]
        public async Task<IActionResult> Beds(int id)
        {
            var w = await _db.Wards.Include(x => x.Beds).FirstOrDefaultAsync(x => x.Id == id);
            if (w == null) return NotFound();

            var activeAssignments = await _db.BedAssignments
                .Where(a => a.Bed.WardId == id && a.IsActive)
                .Include(a => a.Patient)
                .ToDictionaryAsync(a => a.BedId);

            var beds = w.Beds.Where(b => b.IsActive).OrderBy(b => b.Number).Select(b => new BedViewModel
            {
                Id = b.Id,
                WardId = w.Id,
                WardName = w.Name,
                Number = b.Number,
                Status = b.Status,
                Notes = b.Notes,
                IsActive = b.IsActive,
                CurrentAssignmentId = activeAssignments.TryGetValue(b.Id, out var a) ? a.Id : null,
                CurrentPatientName = activeAssignments.TryGetValue(b.Id, out a) ? a.Patient.FullName : null,
                AdmittedOn = activeAssignments.TryGetValue(b.Id, out a) ? a.AdmissionDate : null
            }).ToList();

            ViewData["Ward"] = w;
            return View(beds);
        }
    }
}