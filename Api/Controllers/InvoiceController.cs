using Microsoft.AspNetCore.Mvc;
using invoice.Application.Interfaces;
using invoice.Application.DTOs;

namespace invoice.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _service;

    public InvoiceController(IInvoiceService service)
    {
        _service = service;
    }

    [HttpPost("process")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Process([FromForm] ProcessInvoiceRequest request)
    {
        Console.WriteLine("ENTRO AL ENDPOINT"); // ← AQUÍ

        if (request.File == null || request.File.Length == 0)
            return BadRequest("File is required");

        var extension = Path.GetExtension(request.File.FileName)
            .Replace(".", "")
            .ToLower();

        using var stream = request.File.OpenReadStream();

        var result = await _service.ProcessAsync(
            stream,
            request.OwnerPhone,
            extension
        );

        return Ok(result);
    }

    [HttpGet("test")]
    public IActionResult Test() => Ok("OK");
}