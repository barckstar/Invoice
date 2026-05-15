namespace invoice.Application.DTOs;

public record InvoiceDto(
    string ReviewId,
    string ImageHash,
    string OwnerPhone,
    string InvoiceNumber,
    string VendorName,
    string Currency,
    decimal Subtotal,
    decimal TaxRate,
    decimal Discount,
    decimal TotalIva,
    decimal TotalAmount,
    DateTime InvoiceDate,
    double Confidence,
    string Status,
    string RawAzurePath,
    string ImagePath,
    DateTime CreatedAt,
    IReadOnlyList<InvoiceItemDto> Items
);