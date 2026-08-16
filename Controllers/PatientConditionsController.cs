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
    public class PatientConditionsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public PatientConditionsController(ApplicationDbContext db) { _db = db; }

        [HttpGet]
        public async Task<IActionResult> Index(int? patientId)
        {
            if (patientId is null) return RedirectToAction("Index", "Patients");

            var patient = await _db.Patients.FindAsync(patientId.Value);
            if (patient == null) return NotFound();

            var patientConditions = await _db.PatientConditions
                .Where(pc => pc.PatientId == patientId.Value)
                .Include(pc => pc.Condition)
                .OrderByDescending(pc => pc.DiagnosedDate)
                .Select(pc => new PatientConditionViewModel
                {
                    Id = pc.Id,
                    PatientId = pc.PatientId,
                    PatientName = patient.FullName,
                    PatientMrn = patient.Mrn,
                    ConditionId = pc.ConditionId,
                    ConditionName = pc.Condition.Name,
                    Icd10Code = pc.Condition.Icd10Code,
                    DiagnosedDate = pc.DiagnosedDate,
                    Severity = pc.Severity,
                    Notes = pc.Notes,
                    IsResolved = pc.IsResolved,
                    ResolvedDate = pc.ResolvedDate
                })
                .ToListAsync();

            var assignedIds = patientConditions.Select(pc => pc.ConditionId).ToHashSet();
            var available = await _db.Conditions
                .Where(c => c.IsActive && !assignedIds.Contains(c.Id))
                .OrderBy(c => c.Name)
                .Select(c => new ConditionOption { Id = c.Id, Name = c.Name, Icd10Code = c.Icd10Code })
                .ToListAsync();

            var vm = new PatientConditionViewModel
            {
                PatientId = patient.Id,
                PatientName = patient.FullName,
                PatientMrn = patient.Mrn,
                AvailableConditions = available
            };
            ViewData["PatientConditions"] = patientConditions;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(PatientConditionViewModel vm)
        {
            if (vm.PatientId <= 0 || vm.ConditionId <= 0)
            {
                TempData["Error"] = vm.PatientId <= 0
                    ? "Select a patient and a condition."
                    : "Select a condition.";
                return RedirectToAction(nameof(Index), new { patientId = vm.PatientId });
            }
            if (await _db.PatientConditions.AnyAsync(pc => pc.PatientId == vm.PatientId && pc.ConditionId == vm.ConditionId))
            {
                TempData["Error"] = "Patient already has this condition.";
                return RedirectToAction(nameof(Index), new { patientId = vm.PatientId });
            }
            var pc = new PatientCondition
            {
                PatientId = vm.PatientId,
                ConditionId = vm.ConditionId,
                DiagnosedDate = vm.DiagnosedDate == default ? DateTime.UtcNow : vm.DiagnosedDate,
                Severity = vm.Severity,
                Notes = vm.Notes?.Trim()
            };
            _db.PatientConditions.Add(pc);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Condition added.";
            return RedirectToAction(nameof(Index), new { patientId = vm.PatientId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id)
        {
            var pc = await _db.PatientConditions.FindAsync(id);
            if (pc == null) return NotFound();
            pc.IsResolved = true;
            pc.ResolvedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Condition marked resolved.";
            return RedirectToAction(nameof(Index), new { patientId = pc.PatientId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var pc = await _db.PatientConditions.FindAsync(id);
            if (pc == null) return NotFound();
            var pid = pc.PatientId;
            _db.PatientConditions.Remove(pc);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Condition removed.";
            return RedirectToAction(nameof(Index), new { patientId = pid });
        }
    }
}
