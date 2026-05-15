using Microsoft.AspNetCore.Mvc;
using invoice.Application.DTOs;
using invoice.Application.Interfaces;

namespace invoice.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _review;

    public ReviewController(IReviewService review)
    {
        _review = review;
    }

    // GET /api/review/pending?page=1&pageSize=20
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var results = await _review.GetPendingAsync(page, pageSize);
        return Ok(results);
    }

    // GET /api/review/{reviewId}
    [HttpGet("{reviewId}")]
    public async Task<IActionResult> GetById(string reviewId)
    {
        var invoice = await _review.GetByReviewIdAsync(reviewId);
        return invoice is null
            ? NotFound($"Invoice with ReviewId '{reviewId}' not found")
            : Ok(invoice);
    }

    // PUT /api/review/{reviewId}
    [HttpPut("{reviewId}")]
    public async Task<IActionResult> Update(
        string reviewId,
        [FromBody] UpdateInvoiceReviewRequest request)
    {
        var updated = await _review.UpdateAsync(reviewId, request);
        return updated
            ? Ok("Invoice updated and marked as Reviewed")
            : NotFound($"Invoice with ReviewId '{reviewId}' not found");
    }

    // POST /api/review/{reviewId}/approve
    [HttpPost("{reviewId}/approve")]
    public async Task<IActionResult> Approve(string reviewId)
    {
        var approved = await _review.ApproveAsync(reviewId);
        return approved
            ? Ok("Invoice approved")
            : NotFound($"Invoice with ReviewId '{reviewId}' not found");
    }

    // POST /api/review/{reviewId}/reject
    [HttpPost("{reviewId}/reject")]
    public async Task<IActionResult> Reject(string reviewId)
    {
        var rejected = await _review.RejectAsync(reviewId);
        return rejected
            ? Ok("Invoice rejected")
            : NotFound($"Invoice with ReviewId '{reviewId}' not found");
    }
}