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
    public class BedAssignmentsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public BedAssignmentsController(ApplicationDbContext db) { _db = db; }

        [HttpGet]
        public async Task<IActionResult> Index(int? patientId)
        {
            var assignments = await _db.BedAssignments
                .Where(a => !patientId.HasValue || a.PatientId == patientId.Value)
                .Include(a => a.Patient)
                .Include(a => a.Bed).ThenInclude(b => b!.Ward)
                .OrderByDescending(a => a.AdmissionDate)
                .Take(100)
                .Select(a => new BedAssignmentViewModel
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientName = a.Patient.FullName,
                    PatientMrn = a.Patient.Mrn,
                    BedId = a.BedId,
                    BedNumber = a.Bed.Number,
                    WardName = a.Bed.Ward != null ? a.Bed.Ward.Name : "",
                    AdmissionDate = a.AdmissionDate,
                    DischargeDate = a.DischargeDate,
                    Notes = a.Notes,
                    IsActive = a.IsActive
                })
                .ToListAsync();
            ViewData["PatientId"] = patientId;
            return View(assignments);
        }

        [HttpGet]
        public async Task<IActionResult> Admit(int? patientId)
        {
            var availablePatients = await _db.Patients
                .Where(p => p.IsActive && p.Status != PatientStatus.Admitted)
                .OrderBy(p => p.FullName)
                .Select(p => new PatientOption { Id = p.Id, FullName = p.FullName, Mrn = p.Mrn })
                .ToListAsync();

            var availableBeds = await _db.Beds
                .Where(b => b.IsActive && b.Status == BedStatus.Available)
                .Include(b => b.Ward)
                .OrderBy(b => b.Ward.Name).ThenBy(b => b.Number)
                .Select(b => new BedOption
                {
                    Id = b.Id,
                    Display = $"{b.Ward.Name} / Bed {b.Number}"
                })
                .ToListAsync();

            var vm = new BedAssignmentViewModel
            {
                PatientId = patientId ?? 0,
                AdmissionDate = DateTime.UtcNow,
                AvailablePatients = availablePatients,
                AvailableBeds = availableBeds
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Admit(BedAssignmentViewModel vm)
        {
            if (vm.PatientId <= 0) ModelState.AddModelError(nameof(vm.PatientId), "Select a patient.");
            if (vm.BedId <= 0) ModelState.AddModelError(nameof(vm.BedId), "Select a bed.");

            if (vm.PatientId > 0 && await _db.BedAssignments.AnyAsync(a => a.PatientId == vm.PatientId && a.IsActive))
                ModelState.AddModelError(nameof(vm.PatientId), "Patient already has an active assignment. Discharge first.");

            if (vm.BedId > 0 && await _db.BedAssignments.AnyAsync(a => a.BedId == vm.BedId && a.IsActive))
                ModelState.AddModelError(nameof(vm.BedId), "Bed already occupied.");

            if (!ModelState.IsValid)
            {
                vm.AvailablePatients = await _db.Patients.Where(p => p.IsActive && p.Status != PatientStatus.Admitted)
                    .OrderBy(p => p.FullName).Select(p => new PatientOption { Id = p.Id, FullName = p.FullName, Mrn = p.Mrn }).ToListAsync();
                vm.AvailableBeds = await _db.Beds.Where(b => b.IsActive && b.Status == BedStatus.Available).Include(b => b.Ward)
                    .OrderBy(b => b.Ward.Name).ThenBy(b => b.Number)
                    .Select(b => new BedOption { Id = b.Id, Display = $"{b.Ward.Name} / Bed {b.Number}" }).ToListAsync();
                return View(vm);
            }

            var assignment = new BedAssignment
            {
                PatientId = vm.PatientId,
                BedId = vm.BedId,
                AdmissionDate = vm.AdmissionDate,
                Notes = vm.Notes?.Trim(),
                IsActive = true
            };
            _db.BedAssignments.Add(assignment);

            var bed = await _db.Beds.FindAsync(vm.BedId);
            if (bed != null) bed.Status = BedStatus.Occupied;

            var patient = await _db.Patients.FindAsync(vm.PatientId);
            if (patient != null) patient.Status = PatientStatus.Admitted;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Patient admitted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Discharge(int id)
        {
            var a = await _db.BedAssignments.Include(x => x.Bed).Include(x => x.Patient).FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return NotFound();

            a.IsActive = false;
            a.DischargeDate = DateTime.UtcNow;
            if (a.Bed != null) a.Bed.Status = BedStatus.Available;
            if (a.Patient != null) a.Patient.Status = PatientStatus.Discharged;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Patient discharged.";
            return RedirectToAction(nameof(Index));
        }
    }
}