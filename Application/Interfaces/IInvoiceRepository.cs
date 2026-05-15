using invoice.Application.DTOs;

namespace invoice.Application.Interfaces;

public interface IInvoiceRepository
{
    Task InsertAsync(InvoiceDto invoice);
}