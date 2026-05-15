using invoice.Application.DTOs;

namespace invoice.Application.Interfaces
{
    public interface IOcrService
    {
        Task<OcrResultDto?> ExtractAsync(Stream imageStream);
    }
}
