using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using PatientManagementSystem.Models.Entities;
using PatientManagementSystem.Models.Enums;

namespace PatientManagementSystem.Models.ViewModels
{
    public class InvoiceViewModel
    {
        public int Id { get; set; }

        public string Number { get; set; } = string.Empty;

        [Required(ErrorMessage = "Patient is required.")]
        [Display(Name = "Patient")]
        public int PatientId { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Invoice date")]
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;

        [Range(0, 100)]
        [Display(Name = "Tax %")]
        public decimal TaxPercent { get; set; } = 0;

        [StringLength(400)]
        public string? Notes { get; set; }

        public List<InvoiceItemViewModel> Items { get; set; } = new();

        public SubTotalDisplay SubTotal { get; set; } = new();
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SubTotalDisplay
    {
        public decimal SubTotal { get; set; }
    }

    public class InvoiceItemViewModel
    {
        public int Id { get; set; }
        [Required] [StringLength(200)] public string Description { get; set; } = string.Empty;
        [Range(1, 999)] public int Quantity { get; set; } = 1;
        [Range(0, 1000000)] public decimal UnitPrice { get; set; } = 0;
        public decimal Total { get; set; }
    }

    public class InvoiceListItemDto
    {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty;
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public InvoiceStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class InvoiceIndexViewModel
    {
        public int? PatientId { get; set; }
        public string? Status { get; set; }
    }

    public class InvoiceDataTablesRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; } = 10;

        [FromForm(Name = "search[value]")]
        public string? SearchValue { get; set; }

        [FromForm(Name = "filters[status]")]
        public string? Status { get; set; }

        [FromForm(Name = "filters[patientId]")]
        public int? PatientId { get; set; }
    }
}