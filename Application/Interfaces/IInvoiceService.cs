using invoice.Application.DTOs;

namespace invoice.Application.Interfaces;

public interface IInvoiceService
{
    Task<OcrResultDto?> ProcessAsync(
        Stream file,
        string ownerPhone,
        string extension
    );
}