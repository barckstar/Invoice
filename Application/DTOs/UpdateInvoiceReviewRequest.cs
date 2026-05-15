namespace invoice.Application.DTOs;

public record UpdateInvoiceReviewRequest(
    string InvoiceNumber,
    string VendorName,

    string Currency,

    decimal Subtotal,
    decimal TaxRate,
    decimal Discount,
    decimal TotalIva,
    decimal TotalAmount,

    List<InvoiceItemDto> Items
);