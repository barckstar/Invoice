using invoice.Application.DTOs;

namespace invoice.Application.Interfaces;

public interface IInvoiceRepository
{
    Task InsertAsync(InvoiceDto invoice);

    Task<InvoiceDto?> GetByReviewIdAsync(string reviewId);

    Task<IReadOnlyList<InvoiceDto>> GetPendingAsync(int page, int pageSize);

    Task<PagedResult<InvoiceDto>> SearchAsync(InvoiceSearchQuery query);

    Task UpdateReviewAsync(string reviewId, UpdateInvoiceReviewRequest request);

    Task UpdateStatusAsync(string reviewId, string status);
}