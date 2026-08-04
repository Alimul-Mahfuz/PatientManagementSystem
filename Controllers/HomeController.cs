using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatientManagementSystem.Data;
using PatientManagementSystem.Models;
using PatientManagementSystem.Models.Entities;
using PatientManagementSystem.Models.Enums;
using System.Diagnostics;

namespace PatientManagementSystem.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        public HomeController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index()
        {
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var totalPatients = await _db.Patients.CountAsync(p => p.IsActive);
            var admittedPatients = await _db.Patients.CountAsync(p => p.IsActive && p.Status == PatientStatus.Admitted);
            var occupiedBeds = await _db.Beds.CountAsync(b => b.IsActive && b.Status == BedStatus.Occupied);
            var totalBeds = await _db.Beds.CountAsync(b => b.IsActive);

            var pendingInvoices = await _db.Invoices.CountAsync(i => i.Status == InvoiceStatus.Pending);
            var monthRevenue = await _db.Invoices
                .Where(i => i.Status == InvoiceStatus.Paid && i.InvoiceDate >= monthStart)
                .SumAsync(i => (decimal?)i.TotalAmount) ?? 0m;

            var recentAdmissions = await _db.BedAssignments
                .Where(a => a.IsActive)
                .Include(a => a.Patient)
                .Include(a => a.Bed).ThenInclude(b => b!.Ward)
                .OrderByDescending(a => a.AdmissionDate)
                .Take(5)
                .Select(a => new RecentAdmissionDto
                {
                    PatientName = a.Patient.FullName,
                    PatientMrn = a.Patient.Mrn,
                    Ward = a.Bed.Ward != null ? a.Bed.Ward.Name : "",
                    BedNumber = a.Bed.Number,
                    AdmittedOn = a.AdmissionDate
                })
                .ToListAsync();

            var vm = new DashboardViewModel
            {
                TotalPatients = totalPatients,
                AdmittedPatients = admittedPatients,
                OccupiedBeds = occupiedBeds,
                TotalBeds = totalBeds,
                PendingInvoices = pendingInvoices,
                MonthRevenue = monthRevenue,
                RecentAdmissions = recentAdmissions
            };
            return View(vm);
        }

        public IActionResult Privacy() { return View(); }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class DashboardViewModel
    {
        public int TotalPatients { get; set; }
        public int AdmittedPatients { get; set; }
        public int OccupiedBeds { get; set; }
        public int TotalBeds { get; set; }
        public int PendingInvoices { get; set; }
        public decimal MonthRevenue { get; set; }
        public List<RecentAdmissionDto> RecentAdmissions { get; set; } = new();
    }

    public class RecentAdmissionDto
    {
        public string PatientName { get; set; } = string.Empty;
        public string PatientMrn { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string BedNumber { get; set; } = string.Empty;
        public DateTime AdmittedOn { get; set; }
    }
}