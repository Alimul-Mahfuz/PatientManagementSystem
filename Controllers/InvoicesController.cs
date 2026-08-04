using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PatientManagementSystem.Data;
using PatientManagementSystem.Models.Entities;
using PatientManagementSystem.Models.Enums;
using PatientManagementSystem.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PatientManagementSystem.Controllers
{
    [Authorize]
    public class InvoicesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public InvoicesController(ApplicationDbContext db) { _db = db; }

        [HttpGet]
        public IActionResult Index(int? patientId)
        {
            return View(new InvoiceIndexViewModel { PatientId = patientId, Status = null });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Search([FromForm] InvoiceDataTablesRequest req)
        {
            var q = _db.Invoices.Include(i => i.Patient).AsQueryable();
            if (req.PatientId.HasValue) q = q.Where(i => i.PatientId == req.PatientId.Value);

            if (!string.IsNullOrWhiteSpace(req.Status) &&
                Enum.TryParse<InvoiceStatus>(req.Status, true, out var st))
            {
                q = q.Where(i => i.Status == st);
            }

            if (!string.IsNullOrWhiteSpace(req.SearchValue))
            {
                var s = req.SearchValue.Trim();
                q = q.Where(i =>
                    i.Number.Contains(s) ||
                    (i.Patient.FullName != null && i.Patient.FullName.Contains(s)) ||
                    (i.Patient.Mrn != null && i.Patient.Mrn.Contains(s)));
            }

            var total = await _db.Invoices.CountAsync();
            var filtered = await q.CountAsync();

            var rows = await q
                .OrderByDescending(i => i.InvoiceDate)
                .Skip(req.Start)
                .Take(req.Length <= 0 ? 10 : req.Length)
                .Select(i => new InvoiceListItemDto
                {
                    Id = i.Id,
                    Number = i.Number,
                    PatientId = i.PatientId,
                    PatientName = i.Patient.FullName,
                    InvoiceDate = i.InvoiceDate,
                    Status = i.Status,
                    TotalAmount = i.TotalAmount
                })
                .ToListAsync();

            return Ok(new { draw = req.Draw, recordsTotal = total, recordsFiltered = filtered, data = rows });
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? patientId)
        {
            var vm = new InvoiceViewModel
            {
                Number = await GenerateInvoiceNumberAsync(),
                InvoiceDate = DateTime.UtcNow,
                Items = new List<InvoiceItemViewModel> { new() }
            };
            if (patientId.HasValue) vm.PatientId = patientId.Value;
            ViewData["Patients"] = await GetPatientOptionsAsync();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InvoiceViewModel vm)
        {
            vm.Items = vm.Items.Where(it => !string.IsNullOrWhiteSpace(it.Description)).ToList();
            if (vm.PatientId <= 0) ModelState.AddModelError(nameof(vm.PatientId), "Select a patient.");
            if (vm.Items.Count == 0) ModelState.AddModelError("Items", "Add at least one line item.");

            // Re-validate totals: ensure Total per row = Quantity * UnitPrice
            for (int i = 0; i < vm.Items.Count; i++)
            {
                var it = vm.Items[i];
                if (it.Quantity < 1) ModelState.AddModelError($"Items[{i}].Quantity", "Quantity must be ≥ 1.");
                if (it.UnitPrice < 0) ModelState.AddModelError($"Items[{i}].UnitPrice", "Unit price cannot be negative.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["Patients"] = await GetPatientOptionsAsync();
                return View(vm);
            }

            var invoice = new Invoice
            {
                Number = vm.Number,
                PatientId = vm.PatientId,
                InvoiceDate = vm.InvoiceDate == default ? DateTime.UtcNow : vm.InvoiceDate,
                Status = InvoiceStatus.Pending,
                TaxPercent = vm.TaxPercent,
                Notes = vm.Notes?.Trim(),
                CreatedAt = DateTime.UtcNow,
                Items = vm.Items.Select(it => new InvoiceItem
                {
                    Description = it.Description.Trim(),
                    Quantity = it.Quantity,
                    UnitPrice = it.UnitPrice,
                    Total = it.Quantity * it.UnitPrice
                }).ToList()
            };

            Recalculate(invoice);
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Invoice {invoice.Number} created.";
            return RedirectToAction("Details", new { id = invoice.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var inv = await _db.Invoices
                .Include(i => i.Patient)
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (inv == null) return NotFound();

            var vm = MapToViewModel(inv);
            ViewData["PatientName"] = inv.Patient?.FullName ?? "—";
            ViewData["PatientMrn"] = inv.Patient?.Mrn ?? "—";
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Print(int id)
        {
            var inv = await _db.Invoices
                .Include(i => i.Patient)
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (inv == null) return NotFound();

            var vm = MapToViewModel(inv);
            ViewData["PatientName"] = inv.Patient?.FullName ?? "—";
            ViewData["PatientMrn"] = inv.Patient?.Mrn ?? "—";
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Pdf(int id)
        {
            var inv = await _db.Invoices
                .Include(i => i.Patient)
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (inv == null) return NotFound();

            var pdf = BuildInvoiceDocument(inv).GeneratePdf();
            return File(pdf, "application/pdf"); // inline preview
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var inv = await _db.Invoices.FindAsync(id);
            if (inv == null) return NotFound();
            if (inv.Status == InvoiceStatus.Cancelled)
            {
                TempData["Error"] = "Cannot pay a cancelled invoice.";
                return RedirectToAction("Details", new { id });
            }
            inv.Status = InvoiceStatus.Paid;
            inv.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Invoice {inv.Number} marked paid.";
            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var inv = await _db.Invoices.FindAsync(id);
            if (inv == null) return NotFound();
            if (inv.Status == InvoiceStatus.Paid)
            {
                TempData["Error"] = "Cannot cancel a paid invoice.";
                return RedirectToAction("Details", new { id });
            }
            inv.Status = InvoiceStatus.Cancelled;
            inv.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Invoice {inv.Number} cancelled.";
            return RedirectToAction("Details", new { id });
        }

        // ---- helpers ----
        private async Task<List<SelectListItem>> GetPatientOptionsAsync()
        {
            return await _db.Patients
                .Where(p => p.IsActive)
                .OrderBy(p => p.FullName)
                .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = $"{p.FullName} ({p.Mrn})" })
                .ToListAsync();
        }

        private async Task<string> GenerateInvoiceNumberAsync()
        {
            var year = DateTime.UtcNow.Year;
            var count = await _db.Invoices.CountAsync(i => i.InvoiceDate.Year == year) + 1;
            return $"INV-{year}-{count.ToString("D4")}";
        }

        private void Recalculate(Invoice inv)
        {
            inv.SubTotal = inv.Items.Sum(it => it.Total);
            inv.TaxAmount = Math.Round(inv.SubTotal * (inv.TaxPercent / 100m), 2);
            inv.TotalAmount = inv.SubTotal + inv.TaxAmount;
        }

        private static InvoiceViewModel MapToViewModel(Invoice inv)
        {
            var vm = new InvoiceViewModel
            {
                Id = inv.Id,
                Number = inv.Number,
                PatientId = inv.PatientId,
                InvoiceDate = inv.InvoiceDate,
                Status = inv.Status,
                TaxPercent = inv.TaxPercent,
                TaxAmount = inv.TaxAmount,
                Total = inv.TotalAmount,
                Notes = inv.Notes,
                CreatedAt = inv.CreatedAt,
                Items = inv.Items.OrderBy(it => it.Id).Select(it => new InvoiceItemViewModel
                {
                    Id = it.Id,
                    Description = it.Description,
                    Quantity = it.Quantity,
                    UnitPrice = it.UnitPrice,
                    Total = it.Total
                }).ToList()
            };
            vm.SubTotal.SubTotal = inv.SubTotal;
            return vm;
        }

        private static IDocument BuildInvoiceDocument(Invoice inv)
        {
            var patient = inv.Patient;
            var items = inv.Items.OrderBy(it => it.Id).ToList();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    page.Header().Column(column =>
                    {
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Patient Management System")
                                .FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                            row.ConstantItem(120).AlignRight().Text("INVOICE")
                                .FontSize(24).Bold().FontColor(Colors.Blue.Medium);
                        });
                        column.Item().PaddingTop(8).LineHorizontal(2).LineColor(Colors.Blue.Medium);
                    });

                    page.Content().PaddingVertical(20).Column(column =>
                    {
                        // Invoice metadata
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Invoice #: {inv.Number}").SemiBold();
                                c.Item().Text($"Status: {inv.Status}");
                            });
                            row.ConstantItem(180).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Date: {inv.InvoiceDate:dd MMM yyyy}").AlignRight();
                            });
                        });

                        // Patient info
                        column.Item().PaddingTop(16).Column(c =>
                        {
                            c.Item().Text("Bill To").SemiBold().FontColor(Colors.Grey.Darken2);
                            c.Item().Text(patient?.FullName ?? "—").Bold();
                            c.Item().Text($"MRN: {patient?.Mrn ?? "—"}");
                            if (!string.IsNullOrWhiteSpace(patient?.Phone))
                                c.Item().Text($"Phone: {patient.Phone}");
                            if (!string.IsNullOrWhiteSpace(patient?.Address))
                                c.Item().Text(patient.Address);
                        });

                        // Line items table
                        column.Item().PaddingTop(20).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(90);
                                columns.ConstantColumn(90);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Lighten3).Padding(5).Text("Description").Bold();
                                header.Cell().Background(Colors.Blue.Lighten3).Padding(5).AlignCenter().Text("Qty").Bold();
                                header.Cell().Background(Colors.Blue.Lighten3).Padding(5).AlignRight().Text("Unit Price").Bold();
                                header.Cell().Background(Colors.Blue.Lighten3).Padding(5).AlignRight().Text("Total").Bold();
                            });

                            foreach (var item in items)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Description);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text(item.Quantity.ToString());
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{item.UnitPrice:N2}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{item.Total:N2}");
                            }
                        });

                        // Totals
                        column.Item().PaddingTop(20).AlignRight().Width(220).Column(c =>
                        {
                            c.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Subtotal").FontColor(Colors.Grey.Darken1);
                                row.ConstantItem(90).AlignRight().Text($"{inv.SubTotal:N2}");
                            });
                            c.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Tax ({inv.TaxPercent:0.##}%)").FontColor(Colors.Grey.Darken1);
                                row.ConstantItem(90).AlignRight().Text($"{inv.TaxAmount:N2}");
                            });
                            c.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Blue.Medium);
                            c.Item().Row(row =>
                            {
                                row.RelativeItem().AlignBottom().Text("TOTAL").Bold().FontSize(12);
                                row.ConstantItem(90).AlignRight().Text($"{inv.TotalAmount:N2}").Bold().FontSize(14).FontColor(Colors.Blue.Medium);
                            });
                            c.Item().AlignRight().Text("NPR").FontSize(9).FontColor(Colors.Grey.Medium);
                        });

                        // Notes
                        if (!string.IsNullOrWhiteSpace(inv.Notes))
                        {
                            column.Item().PaddingTop(30).Width(250).Text(text =>
                            {
                                text.Span("Notes: ").SemiBold().FontColor(Colors.Grey.Darken2);
                                text.Span(inv.Notes);
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ").FontSize(9).FontColor(Colors.Grey.Medium);
                        text.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                        text.Span(" of ").FontSize(9).FontColor(Colors.Grey.Medium);
                        text.TotalPages().FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            });
        }
    }
}