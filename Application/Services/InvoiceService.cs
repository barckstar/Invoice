using invoice.Application.DTOs;
using invoice.Application.Interfaces;

namespace invoice.Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IStorageService _storage;
    private readonly IOcrService _ocr;
    private readonly IInvoiceRepository _repo;
    private readonly IInvoiceValidationService _validator;

    public InvoiceService(
        IStorageService storage,
        IOcrService ocr,
        IInvoiceRepository repo,
        IInvoiceValidationService validator)
    {
        _storage = storage;
        _ocr = ocr;
        _repo = repo;
        _validator = validator;
    }

    public async Task<OcrResultDto?> ProcessAsync(Stream file,string ownerPhone,string extension)
    {
        var (hash, path) =
            await _storage.SaveAsync(
                file,
                ownerPhone,
                extension);

        file.Position = 0;

        var ocrResult =
            await _ocr.ExtractAsync(file);

        if (ocrResult == null)
            return null;

        var invoice = ocrResult.Invoice;

        var finalInvoice = invoice with
        {
            ImageHash = hash,
            OwnerPhone = ownerPhone,
            ImagePath = path,
            RawAzurePath = await _storage.SaveRawJsonAsync(hash,ocrResult.RawJson),
            Status = _validator.GetStatus(invoice)
        };

        await _repo.InsertAsync(finalInvoice);

        return new OcrResultDto(
            Invoice: finalInvoice,
            RawJson: ocrResult.RawJson
        );
    }
}