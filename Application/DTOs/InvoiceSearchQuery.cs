namespace invoice.Application.DTOs;

public record InvoiceSearchQuery(
    string? Status = null,
    DateTime? From = null,
    DateTime? To = null,
    string? Phone = null,
    string? Q = null,
    string SortBy = "CreatedAt",
    string SortDir = "desc",
    int Page = 1,
    int PageSize = 20
);
