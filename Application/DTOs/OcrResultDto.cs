namespace invoice.Application.DTOs;

public record OcrResultDto(
    InvoiceDto Invoice,
    string RawJson
);