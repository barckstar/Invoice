namespace invoice.Application.DTOs;

public record InvoiceReviewDto(
    string ReviewId,
    string Status,
    string InvoiceNumber,
    string VendorName,
    decimal Subtotal,
    decimal TotalIva,
    decimal TotalAmount,
    string Currency,
    IReadOnlyList<InvoiceItemDto> Items
);