using Dapper;
using invoice.Application.DTOs;
using invoice.Application.Interfaces;
using invoice.Infrastructure.Data;

namespace invoice.Infrastructure.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly DbConnectionFactory _factory;

    public InvoiceRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task InsertAsync(InvoiceDto invoice)
    {
        using var connection = _factory.Create();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        try
        {
            await connection.ExecuteAsync(@"
INSERT INTO Invoices (
    ReviewId,
    ImageHash,
    OwnerPhone,

    InvoiceNumber,
    VendorName,
    InvoiceDate,
    Currency,

    Subtotal,
    TaxRate,
    Discount,
    TotalIva,
    TotalAmount,

    Confidence,

    Status,

    ImagePath,
    RawAzurePath,

    CreatedAt
)
VALUES (
    @ReviewId,
    @ImageHash,
    @OwnerPhone,

    @InvoiceNumber,
    @VendorName,
    @InvoiceDate,
    @Currency,

    @Subtotal,
    @TaxRate,
    @Discount,
    @TotalIva,
    @TotalAmount,

    @Confidence,

    @Status,

    @ImagePath,
    @RawAzurePath,

    @CreatedAt
)", new
            {
                invoice.ReviewId,
                invoice.ImageHash,
                invoice.OwnerPhone,

                invoice.InvoiceNumber,
                invoice.VendorName,

                InvoiceDate = invoice.InvoiceDate.ToString("yyyy-MM-dd"),

                invoice.Currency,

                invoice.Subtotal,
                invoice.TaxRate,
                invoice.Discount,
                invoice.TotalIva,
                invoice.TotalAmount,

                invoice.Confidence,

                invoice.Status,

                invoice.ImagePath,
                invoice.RawAzurePath,

                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            }); ;

            foreach (var item in invoice.Items)
            {
                await connection.ExecuteAsync(@"
INSERT INTO InvoiceItems (
    ImageHash,
    ServiceProduct,
    Quantity,
    UnitPrice,
    LineTotal,
    TaxRate,
    TaxAmount
)
VALUES (
    @ImageHash,
    @ServiceProduct,
    @Quantity,
    @UnitPrice,
    @LineTotal,
    @TaxRate,
    @TaxAmount
)", new
                {
                    invoice.ImageHash,
                    item.ServiceProduct,
                    item.Quantity,
                    item.UnitPrice,
                    item.LineTotal,
                    item.TaxRate,
                    item.TaxAmount
                }); ;
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}