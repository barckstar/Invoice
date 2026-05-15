namespace invoice.Application.Services;

using invoice.Application.DTOs;
using invoice.Application.Interfaces;

public class InvoiceValidationService : IInvoiceValidationService
{
    public string GetStatus(InvoiceDto invoice)
    {
        var score = 0;

        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
            score++;

        if (string.IsNullOrWhiteSpace(invoice.VendorName))
            score++;

        if (invoice.TotalAmount <= 0)
            score++;

        if (invoice.Items.Count == 0)
            score++;

        if (invoice.Confidence < 0.5)
            score++;

        return score >= 2
            ? "NeedsReview"
            : "Processed";
    }
}