using invoice.Application.DTOs;

namespace invoice.Application.Interfaces;

public interface IInvoiceValidationService
{
    string GetStatus(InvoiceDto invoice);
}