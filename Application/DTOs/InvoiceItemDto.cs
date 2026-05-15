namespace invoice.Application.DTOs;

public record InvoiceItemDto(
    string ServiceProduct,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRate,
    decimal TaxAmount,
    decimal LineTotal
);