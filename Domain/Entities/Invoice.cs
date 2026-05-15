namespace Invoice.Domain.Entities
{
    public class Invoice
    {
        public string? ImageHash { get; set; }
        public string? VendorName { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
