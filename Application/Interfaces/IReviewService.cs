using invoice.Application.DTOs;

namespace invoice.Application.Interfaces;

public interface IReviewService
{
    Task<InvoiceDto?> GetByReviewIdAsync(string reviewId);

    Task<IReadOnlyList<InvoiceDto>> GetPendingAsync(int page, int pageSize);

    Task<bool> UpdateAsync(string reviewId, UpdateInvoiceReviewRequest request);

    Task<bool> ApproveAsync(string reviewId);

    Task<bool> RejectAsync(string reviewId);
}