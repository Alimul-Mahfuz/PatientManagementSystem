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
        private readonly IWebHostEnvironment _environment;

        public PatientsController(ApplicationDbContext db, IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

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

            if (!await SaveUploadsAsync(patient, vm.PatientImage, vm.Report))
            {
                _db.Patients.Remove(patient);
                await _db.SaveChangesAsync();
                return View(vm);
            }
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

            if (!await SaveUploadsAsync(p, vm.PatientImage, vm.Report)) return View(vm);

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Patient '{p.FullName}' updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var p = await _db.Patients
                .Include(patient => patient.Reports)
                .FirstOrDefaultAsync(patient => patient.Id == id);
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
                UpdatedAt = p.UpdatedAt,
                ImagePath = p.ImagePath,
                Reports = p.Reports.OrderByDescending(report => report.UploadedAt).Select(report => new PatientReportViewModel
                {
                    Id = report.Id,
                    OriginalFileName = report.OriginalFileName,
                    ContentType = report.ContentType,
                    UploadedAt = report.UploadedAt
                }).ToList()
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadReport(int id)
        {
            var report = await _db.PatientReports.FindAsync(id);
            if (report == null) return NotFound();

            var path = Path.Combine(GetUploadRoot(), report.FilePath.Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(path)) return NotFound();
            return PhysicalFile(path, report.ContentType, report.OriginalFileName);
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

        private async Task<bool> SaveUploadsAsync(Patient patient, IFormFile? image, IFormFile? report)
        {
            if (image != null && !ValidateUpload(image, new[] { ".jpg", ".jpeg", ".png", ".webp" }, 5 * 1024 * 1024, "Patient image"))
                return false;
            if (report != null && !ValidateUpload(report, new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx" }, 10 * 1024 * 1024, "Report"))
                return false;

            var directory = Path.Combine(GetUploadRoot(), "uploads", "patients", patient.Id.ToString());
            Directory.CreateDirectory(directory);

            if (image != null)
            {
                DeleteStoredFile(patient.ImagePath);
                var fileName = $"image-{Guid.NewGuid():N}{Path.GetExtension(image.FileName).ToLowerInvariant()}";
                var relativePath = $"uploads/patients/{patient.Id}/{fileName}";
                await using var stream = System.IO.File.Create(Path.Combine(directory, fileName));
                await image.CopyToAsync(stream);
                patient.ImagePath = relativePath;
            }

            if (report != null)
            {
                var fileName = $"report-{Guid.NewGuid():N}{Path.GetExtension(report.FileName).ToLowerInvariant()}";
                var relativePath = $"uploads/patients/{patient.Id}/{fileName}";
                await using var stream = System.IO.File.Create(Path.Combine(directory, fileName));
                await report.CopyToAsync(stream);
                _db.PatientReports.Add(new PatientReport
                {
                    PatientId = patient.Id,
                    OriginalFileName = Path.GetFileName(report.FileName),
                    StoredFileName = fileName,
                    FilePath = relativePath,
                    ContentType = string.IsNullOrWhiteSpace(report.ContentType) ? "application/octet-stream" : report.ContentType,
                    UploadedAt = DateTime.UtcNow
                });
            }

            return true;
        }

        private bool ValidateUpload(IFormFile file, string[] extensions, long maxLength, string label)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (file.Length == 0 || file.Length > maxLength || !extensions.Contains(extension))
            {
                ModelState.AddModelError(string.Empty, $"{label} must be one of {string.Join(", ", extensions)} and no larger than {maxLength / (1024 * 1024)} MB.");
                return false;
            }
            return true;
        }

        private string GetUploadRoot() => _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

        private void DeleteStoredFile(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;
            var path = Path.Combine(GetUploadRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }
}
