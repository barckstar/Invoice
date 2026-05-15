namespace invoice.Application.DTOs;

public class ProcessInvoiceRequest
{
    public IFormFile? File { get; set; }
    public string? OwnerPhone { get; set; }
}