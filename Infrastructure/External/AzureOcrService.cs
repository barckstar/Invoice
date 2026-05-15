using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using invoice.Application.DTOs;
using invoice.Application.Interfaces;

namespace invoice.Infrastructure.External;

public class AzureOcrService : IOcrService
{
    private readonly DocumentAnalysisClient _client;

    public AzureOcrService(IConfiguration config)
    {
        var endpoint = config["AzureConfig:Endpoint"]
            ?? throw new ArgumentNullException("Azure Endpoint missing");

        var apiKey = config["AzureConfig:ApiKey"]
            ?? throw new ArgumentNullException("Azure ApiKey missing");

        _client = new DocumentAnalysisClient(
            new Uri(endpoint),
            new AzureKeyCredential(apiKey));
    }

    public async Task<OcrResultDto?> ExtractAsync(Stream imageStream)
    {
        var operation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-invoice",
            imageStream);

        var result = operation.Value;

        var document = result.Documents.FirstOrDefault();

        if (document == null)
            return null;

        var items = ExtractItems(document);

        var totalAmount = CleanAmount(GetVal(document, "InvoiceTotal"));

        var totalIva = items.Sum(x => x.TaxAmount);

        var discount = ExtractGlobalDiscount(result, document);

        var subtotal = totalAmount - totalIva + discount;

        var taxRate = subtotal > 0
            ? Math.Round((totalIva / subtotal) * 100, 2)
            : 0;

        var invoice = new InvoiceDto(
            ReviewId: Guid.NewGuid().ToString(),

            ImageHash: "",

            OwnerPhone: "",

            InvoiceNumber: GetVal(document, "InvoiceId"),

            VendorName: GetVal(document, "VendorName"),

            Currency: GetCurrency(document, result),

            Subtotal: subtotal,

            TaxRate: taxRate,

            Discount: discount,

            TotalIva: totalIva,

            TotalAmount: totalAmount,

            InvoiceDate: ParseDate(GetVal(document, "InvoiceDate")),

            Confidence: (double)document.Confidence,

            Status: "PendingReview",

            RawAzurePath: result.Content,

            ImagePath: "",

            CreatedAt: DateTime.UtcNow,

            Items: items
        );

        return new OcrResultDto(
            Invoice: invoice,
            RawJson: result.Content
        );
    }

    private static string GetVal(AnalyzedDocument d, string k) =>
        d.Fields.TryGetValue(k, out var f)
            ? f.Content
            : "";

    private static decimal CleanAmount(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        var clean = new string(
            content.Where(c =>
                char.IsDigit(c) ||
                c == '.' ||
                c == ',')
            .ToArray())
            .Replace(',', '.');

        return decimal.TryParse(
            clean,
            System.Globalization.CultureInfo.InvariantCulture,
            out var result)
            ? result
            : 0;
    }

    private decimal ExtractGlobalDiscount(
        AnalyzeResult result,
        AnalyzedDocument document)
    {
        if (document.Fields.TryGetValue(
            "InvoiceTotalDiscount",
            out var discountField))
        {
            return CleanAmount(discountField.Content);
        }

        return 0;
    }

    private List<InvoiceItemDto> ExtractItems(
        AnalyzedDocument document)
    {
        var list = new List<InvoiceItemDto>();

        if (!document.Fields.TryGetValue("Items", out var itemsField) ||
            itemsField.FieldType != DocumentFieldType.List)
        {
            return list;
        }

        foreach (var itemField in itemsField.Value.AsList())
        {
            var fields = itemField.Value.AsDictionary();

            var rawAmount = fields.TryGetValue("Amount", out var amount)
                ? amount.Content
                : "0";

            var lineTotal = CleanAmount(rawAmount);

            var taxRate = ExtractCrTax(rawAmount);

            var taxAmount = lineTotal * (taxRate / 100);

            list.Add(new InvoiceItemDto(
                ServiceProduct: GetSubVal(fields, "Description"),

                Quantity: GetSubDec(fields, "Quantity"),

                UnitPrice: CleanAmount(
                    fields.TryGetValue("UnitPrice", out var p)
                        ? p.Content
                        : "0"),

                LineTotal: lineTotal,

                TaxRate: taxRate,

                TaxAmount: taxAmount
            ));
        }

        return list;
    }

    private decimal ExtractCrTax(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return 0;

        int lastSep = Math.Max(
            content.LastIndexOf('.'),
            content.LastIndexOf(','));

        if (lastSep != -1 &&
            content.Length > lastSep + 3)
        {
            string suffix = content.Substring(lastSep + 3).Trim();

            if (string.IsNullOrEmpty(suffix))
                return 0;

            char taxCode = suffix[0];

            return taxCode switch
            {
                'G' or 'g' => 13m,
                '1' => 1m,
                '2' => 2m,
                '4' => 4m,
                '8' => 8m,
                '0' => 0m,
                _ => 13m
            };
        }

        return 0;
    }

    private string GetSubVal(
        IReadOnlyDictionary<string, DocumentField> d,
        string k)
    {
        return d.TryGetValue(k, out var f)
            ? f.Content
            : "Item";
    }

    private decimal GetSubDec(
        IReadOnlyDictionary<string, DocumentField> d,
        string k)
    {
        if (!d.TryGetValue(k, out var f))
            return 0m;

        if (k == "Quantity" &&
            f.FieldType == DocumentFieldType.Double)
        {
            return (decimal)f.Value.AsDouble();
        }

        return CleanAmount(f.Content);
    }

    private static DateTime ParseDate(string raw)
    {
        return DateTime.TryParse(raw, out var d)
            ? d
            : DateTime.UtcNow;
    }

    private string GetCurrency(
        AnalyzedDocument doc,
        AnalyzeResult result)
    {
        if (doc.Fields.TryGetValue("InvoiceTotal", out var totalField) &&
            totalField.FieldType == DocumentFieldType.Currency)
        {
            var code = totalField.Value.AsCurrency().Code;

            if (!string.IsNullOrWhiteSpace(code))
                return code;
        }

        var content = result.Content;

        if (content.Contains("€"))
            return "EUR";

        if (content.Contains("$"))
            return "USD";

        if (content.Contains("¢") ||
            content.Contains("CRC"))
        {
            return "CRC";
        }

        return "UNKNOWN";
    }
}