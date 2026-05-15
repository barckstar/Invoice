using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using invoice.Application.DTOs;
using invoice.Application.Interfaces;

namespace invoice.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
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
            ? NotFound($"Invoice '{reviewId}' no encontrada")
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
            ? Ok("Factura actualizada y marcada como Reviewed")
            : NotFound($"Invoice '{reviewId}' no encontrada");
    }

    // POST /api/review/{reviewId}/approve
    [HttpPost("{reviewId}/approve")]
    public async Task<IActionResult> Approve(string reviewId)
    {
        var approved = await _review.ApproveAsync(reviewId);
        return approved
            ? Ok("Factura aprobada")
            : NotFound($"Invoice '{reviewId}' no encontrada");
    }

    // POST /api/review/{reviewId}/reject
    [HttpPost("{reviewId}/reject")]
    public async Task<IActionResult> Reject(string reviewId)
    {
        var rejected = await _review.RejectAsync(reviewId);
        return rejected
            ? Ok("Factura rechazada")
            : NotFound($"Invoice '{reviewId}' no encontrada");
    }
}