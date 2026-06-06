namespace invoice.Application.Services;

using invoice.Application.Constants;
using invoice.Application.DTOs;
using invoice.Application.Interfaces;

public class InvoiceValidationService : IInvoiceValidationService
{
    // Pesos por criticidad: un campo crítico solo basta para NeedsReview.
    private const int CRITICAL = 2;
    private const int MINOR    = 1;
    private const int THRESHOLD = 2;

    public string GetStatus(InvoiceDto invoice)
    {
        var score = 0;

        // Críticos — un solo fallo dispara NeedsReview
        if (invoice.TotalAmount <= 0)                    score += CRITICAL;
        if (string.IsNullOrWhiteSpace(invoice.VendorName))    score += CRITICAL;
        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber)) score += CRITICAL;

        // Currency desconocida/vacía indica que el parsing falló
        if (string.IsNullOrWhiteSpace(invoice.Currency) ||
            invoice.Currency.Equals("UNKNOWN", System.StringComparison.OrdinalIgnoreCase))
            score += CRITICAL;

        // Fecha inválida (default DateTime o anterior a 2000)
        if (invoice.InvoiceDate.Year < 2000) score += CRITICAL;

        // Coherencia: subtotal + IVA - descuento debe acercarse al total
        if (invoice.TotalAmount > 0 && invoice.Subtotal > 0)
        {
            var expected = invoice.Subtotal + invoice.TotalIva - invoice.Discount;
            var diff = System.Math.Abs(expected - invoice.TotalAmount);
            var tolerance = invoice.TotalAmount * 0.05m; // 5%
            if (diff > tolerance) score += CRITICAL;
        }

        // Menores — necesitan al menos 2 para disparar
        if (invoice.Subtotal <= 0)        score += MINOR;
        if (invoice.TotalIva < 0)         score += CRITICAL; // negativo es bug
        if (invoice.Items.Count == 0)     score += MINOR;
        if (invoice.Confidence < 0.7)     score += MINOR;

        return score >= THRESHOLD
            ? InvoiceStatus.NeedsReview
            : InvoiceStatus.Processed;
    }
}