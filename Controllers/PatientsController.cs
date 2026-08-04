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
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public PatientsController(ApplicationDbContext db) { _db = db; }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Search([FromForm] PatientDataTablesRequest req)
        {
            var q = _db.Patients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(req.Mrn))
            {
                var v = req.Mrn.Trim();
                q = q.Where(p => p.Mrn.Contains(v));
            }
            if (!string.IsNullOrWhiteSpace(req.Name))
            {
                var v = req.Name.Trim();
                q = q.Where(p => p.FullName.Contains(v));
            }
            if (!string.IsNullOrWhiteSpace(req.Phone))
            {
                var v = req.Phone.Trim();
                q = q.Where(p => p.Phone != null && p.Phone.Contains(v));
            }
            if (!string.IsNullOrWhiteSpace(req.Email))
            {
                var v = req.Email.Trim();
                q = q.Where(p => p.Email != null && p.Email.Contains(v));
            }
            if (req.Gender.HasValue) q = q.Where(p => p.Gender == req.Gender.Value);
            if (req.Status.HasValue) q = q.Where(p => p.Status == req.Status.Value);

            if (!string.IsNullOrWhiteSpace(req.SearchValue))
            {
                var s = req.SearchValue.Trim();
                q = q.Where(p =>
                    p.FullName.Contains(s) ||
                    p.Mrn.Contains(s) ||
                    (p.Email != null && p.Email.Contains(s)) ||
                    (p.Phone != null && p.Phone.Contains(s)));
            }

            // Soft-deleted patients excluded by default.
            q = q.Where(p => p.IsActive);

            var total = await _db.Patients.CountAsync(p => p.IsActive);
            var filtered = await q.CountAsync();

            var rows = await q
                .OrderByDescending(p => p.CreatedAt)
                .Skip(req.Start)
                .Take(req.Length <= 0 ? 10 : req.Length)
                .Select(p => new PatientListItemDto
                {
                    Id = p.Id,
                    Mrn = p.Mrn,
                    FullName = p.FullName,
                    Gender = p.Gender,
                    DateOfBirth = p.DateOfBirth,
                    Phone = p.Phone,
                    Email = p.Email,
                    Status = p.Status,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                draw = req.Draw,
                recordsTotal = total,
                recordsFiltered = filtered,
                data = rows
            });
        }

        [HttpGet]
        public IActionResult Create()
        {
            var vm = new PatientCreateViewModel { Mrn = GenerateMrn() };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PatientCreateViewModel vm)
        {
            if (!string.IsNullOrWhiteSpace(vm.Mrn) &&
                await _db.Patients.AnyAsync(p => p.Mrn == vm.Mrn))
            {
                ModelState.AddModelError(nameof(vm.Mrn), "A patient with this MRN already exists.");
            }

            if (!ModelState.IsValid) return View(vm);

            var patient = new Patient
            {
                Mrn = vm.Mrn.Trim(),
                FullName = vm.FullName.Trim(),
                Gender = vm.Gender,
                DateOfBirth = vm.DateOfBirth,
                Phone = vm.Phone?.Trim(),
                Email = vm.Email?.Trim(),
                Address = vm.Address?.Trim(),
                EmergencyContact = vm.EmergencyContact?.Trim(),
                BloodGroup = vm.BloodGroup?.Trim(),
                Allergies = vm.Allergies?.Trim(),
                Status = vm.Status,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Patient '{patient.FullName}' created (MRN {patient.Mrn}).";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var p = await _db.Patients.FindAsync(id);
            if (p == null) return NotFound();

            var vm = new PatientEditViewModel
            {
                Id = p.Id,
                Mrn = p.Mrn,
                FullName = p.FullName,
                Gender = p.Gender,
                DateOfBirth = p.DateOfBirth,
                Phone = p.Phone,
                Email = p.Email,
                Address = p.Address,
                EmergencyContact = p.EmergencyContact,
                BloodGroup = p.BloodGroup,
                Allergies = p.Allergies,
                Status = p.Status,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PatientEditViewModel vm)
        {
            vm.Id = id;
            if (!string.IsNullOrWhiteSpace(vm.Mrn) &&
                await _db.Patients.AnyAsync(p => p.Mrn == vm.Mrn && p.Id != id))
            {
                ModelState.AddModelError(nameof(vm.Mrn), "Another patient uses this MRN.");
            }
            if (!ModelState.IsValid) return View(vm);

            var p = await _db.Patients.FindAsync(id);
            if (p == null) return NotFound();

            p.Mrn = vm.Mrn.Trim();
            p.FullName = vm.FullName.Trim();
            p.Gender = vm.Gender;
            p.DateOfBirth = vm.DateOfBirth;
            p.Phone = vm.Phone?.Trim();
            p.Email = vm.Email?.Trim();
            p.Address = vm.Address?.Trim();
            p.EmergencyContact = vm.EmergencyContact?.Trim();
            p.BloodGroup = vm.BloodGroup?.Trim();
            p.Allergies = vm.Allergies?.Trim();
            p.Status = vm.Status;
            p.IsActive = vm.IsActive;
            p.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Patient '{p.FullName}' updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var p = await _db.Patients.FindAsync(id);
            if (p == null) return NotFound();

            var vm = new PatientDetailsViewModel
            {
                Id = p.Id,
                Mrn = p.Mrn,
                FullName = p.FullName,
                Gender = p.Gender,
                DateOfBirth = p.DateOfBirth,
                Phone = p.Phone,
                Email = p.Email,
                Address = p.Address,
                EmergencyContact = p.EmergencyContact,
                BloodGroup = p.BloodGroup,
                Allergies = p.Allergies,
                Status = p.Status,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _db.Patients.FindAsync(id);
            if (p == null) return NotFound();

            p.IsActive = false;
            p.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Patient '{p.FullName}' archived.";
            return RedirectToAction(nameof(Index));
        }

        // ---- helpers ----
        private string GenerateMrn()
        {
            // PMS-YYYY-XXXX (random) — uniqueness still checked at POST.
            var year = DateTime.UtcNow.Year;
            var rand = Random.Shared.Next(1000, 9999);
            return $"PMS-{year}-{rand}";
        }
    }
}