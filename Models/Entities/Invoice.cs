namespace PatientManagementSystem.Models.Entities
{
    public class Invoice
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
        public string Number { get; set; } = string.Empty; // INV-YYYY-XXXX
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
        public Models.Enums.InvoiceStatus Status { get; set; } = Models.Enums.InvoiceStatus.Pending;
        public decimal SubTotal { get; set; }
        public decimal TaxPercent { get; set; } = 0;
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    }

    public class InvoiceItem
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; } // Quantity * UnitPrice (computed and stored for query simplicity)
    }
}