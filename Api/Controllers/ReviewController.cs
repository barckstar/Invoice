using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using invoice.Application.DTOs;
using invoice.Application.Interfaces;

namespace invoice.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _review;
    private static readonly string StorageRoot =
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Storage"));

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

    // GET /api/review?status=&from=&to=&phone=&q=&sortBy=&sortDir=&page=&pageSize=
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] InvoiceSearchQuery query)
    {
        var results = await _review.SearchAsync(query);
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

    // GET /api/review/{reviewId}/image
    [HttpGet("{reviewId}/image")]
    public async Task<IActionResult> GetImage(string reviewId)
    {
        var invoice = await _review.GetByReviewIdAsync(reviewId);
        if (invoice is null) return NotFound();

        if (!TryResolveSafePath(invoice.ImagePath, out var fullPath))
            return NotFound("Imagen no disponible");

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fullPath, out var contentType))
            contentType = "application/octet-stream";

        return PhysicalFile(fullPath, contentType);
    }

    // GET /api/review/{reviewId}/raw
    [HttpGet("{reviewId}/raw")]
    public async Task<IActionResult> GetRawJson(string reviewId)
    {
        var invoice = await _review.GetByReviewIdAsync(reviewId);
        if (invoice is null) return NotFound();

        if (!TryResolveSafePath(invoice.RawAzurePath, out var fullPath))
            return NotFound("JSON crudo no disponible");

        return PhysicalFile(fullPath, "application/json");
    }

    // Evita path traversal: el path final debe estar dentro de Storage/
    private static bool TryResolveSafePath(string? path, out string fullPath)
    {
        fullPath = "";
        if (string.IsNullOrWhiteSpace(path)) return false;

        try { fullPath = Path.GetFullPath(path); }
        catch { return false; }

        if (!fullPath.StartsWith(StorageRoot, System.StringComparison.OrdinalIgnoreCase))
            return false;

        return System.IO.File.Exists(fullPath);
    }
}