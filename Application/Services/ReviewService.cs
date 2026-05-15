using invoice.Application.Constants;
using invoice.Application.DTOs;
using invoice.Application.Interfaces;

namespace invoice.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IInvoiceRepository _repo;

    public ReviewService(IInvoiceRepository repo)
    {
        _repo = repo;
    }

    public Task<InvoiceDto?> GetByReviewIdAsync(string reviewId)
        => _repo.GetByReviewIdAsync(reviewId);

    public Task<IReadOnlyList<InvoiceDto>> GetPendingAsync(int page, int pageSize)
        => _repo.GetPendingAsync(
            page: page < 1 ? 1 : page,
            pageSize: pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize);

    public async Task<bool> UpdateAsync(string reviewId, UpdateInvoiceReviewRequest request)
    {
        var exists = await _repo.GetByReviewIdAsync(reviewId);
        if (exists is null) return false;

        await _repo.UpdateReviewAsync(reviewId, request);
        return true;
    }

    public async Task<bool> ApproveAsync(string reviewId)
    {
        var invoice = await _repo.GetByReviewIdAsync(reviewId);
        if (invoice is null) return false;

        await _repo.UpdateStatusAsync(reviewId, InvoiceStatus.Reviewed);
        return true;
    }

    public async Task<bool> RejectAsync(string reviewId)
    {
        var invoice = await _repo.GetByReviewIdAsync(reviewId);
        if (invoice is null) return false;

        await _repo.UpdateStatusAsync(reviewId, InvoiceStatus.Failed);
        return true;
    }
}