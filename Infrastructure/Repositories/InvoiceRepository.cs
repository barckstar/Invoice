using Dapper;
using invoice.Application.Constants;
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

    // ─── INSERT ──────────────────────────────────────────────────────────────

    public async Task InsertAsync(InvoiceDto invoice)
    {
        using var connection = _factory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            await connection.ExecuteAsync(@"
                INSERT INTO Invoices (
                    ImageHash, ReviewId, OwnerPhone,
                    InvoiceNumber, VendorName, InvoiceDate, Currency,
                    Subtotal, TaxRate, Discount, TotalIva, TotalAmount,
                    Confidence, Status,
                    ImagePath, RawAzurePath, CreatedAt
                ) VALUES (
                    @ImageHash, @ReviewId, @OwnerPhone,
                    @InvoiceNumber, @VendorName, @InvoiceDate, @Currency,
                    @Subtotal, @TaxRate, @Discount, @TotalIva, @TotalAmount,
                    @Confidence, @Status,
                    @ImagePath, @RawAzurePath, @CreatedAt
                )",
                new
                {
                    invoice.ImageHash,
                    invoice.ReviewId,
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
                },
                transaction);

            if (invoice.Items.Count > 0)
                await InsertItemsAsync(connection, transaction, invoice.ImageHash, invoice.Items);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // ─── GET BY REVIEW ID ─────────────────────────────────────────────────────

    public async Task<InvoiceDto?> GetByReviewIdAsync(string reviewId)
    {
        using var connection = _factory.Create();
        connection.Open();

        var invoice = await connection.QueryFirstOrDefaultAsync<InvoiceRow>(@"
            SELECT * FROM Invoices WHERE ReviewId = @ReviewId LIMIT 1",
            new { ReviewId = reviewId });

        if (invoice is null) return null;

        var items = await FetchItemsAsync(connection, invoice.ImageHash);

        return MapToDto(invoice, items);
    }

    // ─── GET PENDING ──────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<InvoiceDto>> GetPendingAsync(int page, int pageSize)
    {
        using var connection = _factory.Create();
        connection.Open();

        var rows = await connection.QueryAsync<InvoiceRow>(@"
            SELECT * FROM Invoices
            WHERE Status = @Status
            ORDER BY CreatedAt DESC
            LIMIT @PageSize OFFSET @Offset",
            new
            {
                Status = InvoiceStatus.NeedsReview,
                PageSize = pageSize,
                Offset = (page - 1) * pageSize
            });

        var invoiceRows = rows.ToList();

        if (invoiceRows.Count == 0)
            return [];

        var hashes = invoiceRows.Select(r => r.ImageHash).ToList();

        var allItems = await connection.QueryAsync<InvoiceItemRow>(@"
            SELECT * FROM InvoiceItems
            WHERE ImageHash IN @Hashes",
            new { Hashes = hashes });

        var itemsByHash = allItems
            .GroupBy(i => i.ImageHash)
            .ToDictionary(g => g.Key, g => g.ToList());

        return invoiceRows
            .Select(r => MapToDto(r,
                itemsByHash.TryGetValue(r.ImageHash, out var its)
                    ? its
                    : []))
            .ToList()
            .AsReadOnly();
    }

    // ─── SEARCH ───────────────────────────────────────────────────────────────

    private static readonly HashSet<string> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "CreatedAt", "InvoiceDate", "TotalAmount", "VendorName"
    };

    public async Task<PagedResult<InvoiceDto>> SearchAsync(InvoiceSearchQuery query)
    {
        using var connection = _factory.Create();
        connection.Open();

        var page     = query.Page     < 1 ? 1  : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize > 100 ? 100 : query.PageSize;

        var sortBy  = SortableColumns.Contains(query.SortBy) ? query.SortBy : "CreatedAt";
        var sortDir = string.Equals(query.SortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

        var where = new List<string>();
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            where.Add("Status = @Status");
            p.Add("Status", query.Status);
        }
        if (query.From.HasValue)
        {
            where.Add("InvoiceDate >= @From");
            p.Add("From", query.From.Value.ToString("yyyy-MM-dd"));
        }
        if (query.To.HasValue)
        {
            where.Add("InvoiceDate <= @To");
            p.Add("To", query.To.Value.ToString("yyyy-MM-dd"));
        }
        if (!string.IsNullOrWhiteSpace(query.Phone))
        {
            where.Add("OwnerPhone LIKE @Phone");
            p.Add("Phone", $"%{query.Phone.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            where.Add("(ReviewId LIKE @Q OR InvoiceNumber LIKE @Q OR VendorName LIKE @Q)");
            p.Add("Q", $"%{query.Q.Trim()}%");
        }

        var whereSql = where.Count == 0 ? "" : "WHERE " + string.Join(" AND ", where);

        var total = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM Invoices {whereSql}", p);

        p.Add("PageSize", pageSize);
        p.Add("Offset", (page - 1) * pageSize);

        var rows = await connection.QueryAsync<InvoiceRow>(
            $@"SELECT * FROM Invoices
               {whereSql}
               ORDER BY {sortBy} {sortDir}
               LIMIT @PageSize OFFSET @Offset", p);

        var invoiceRows = rows.ToList();

        if (invoiceRows.Count == 0)
            return new PagedResult<InvoiceDto>(total, page, pageSize, []);

        var hashes = invoiceRows.Select(r => r.ImageHash).ToList();

        var allItems = await connection.QueryAsync<InvoiceItemRow>(
            "SELECT * FROM InvoiceItems WHERE ImageHash IN @Hashes",
            new { Hashes = hashes });

        var itemsByHash = allItems
            .GroupBy(i => i.ImageHash)
            .ToDictionary(g => g.Key, g => g.ToList());

        var items = invoiceRows
            .Select(r => MapToDto(r,
                itemsByHash.TryGetValue(r.ImageHash, out var its) ? its : []))
            .ToList()
            .AsReadOnly();

        return new PagedResult<InvoiceDto>(total, page, pageSize, items);
    }

    // ─── UPDATE REVIEW ────────────────────────────────────────────────────────

    public async Task UpdateReviewAsync(string reviewId, UpdateInvoiceReviewRequest request)
    {
        using var connection = _factory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            await connection.ExecuteAsync(@"
                UPDATE Invoices SET
                    InvoiceNumber = @InvoiceNumber,
                    VendorName    = @VendorName,
                    Currency      = @Currency,
                    Subtotal      = @Subtotal,
                    TaxRate       = @TaxRate,
                    Discount      = @Discount,
                    TotalIva      = @TotalIva,
                    TotalAmount   = @TotalAmount,
                    Status        = @Status
                WHERE ReviewId = @ReviewId",
                new
                {
                    request.InvoiceNumber,
                    request.VendorName,
                    request.Currency,
                    request.Subtotal,
                    request.TaxRate,
                    request.Discount,
                    request.TotalIva,
                    request.TotalAmount,
                    Status = InvoiceStatus.Reviewed,
                    ReviewId = reviewId
                },
                transaction);

            var imageHash = await connection.ExecuteScalarAsync<string>(
                "SELECT ImageHash FROM Invoices WHERE ReviewId = @ReviewId",
                new { ReviewId = reviewId },
                transaction);

            if (!string.IsNullOrEmpty(imageHash))
            {
                await connection.ExecuteAsync(
                    "DELETE FROM InvoiceItems WHERE ImageHash = @ImageHash",
                    new { ImageHash = imageHash },
                    transaction);

                if (request.Items.Count > 0)
                    await InsertItemsAsync(connection, transaction, imageHash, request.Items);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // ─── UPDATE STATUS ────────────────────────────────────────────────────────

    public async Task UpdateStatusAsync(string reviewId, string status)
    {
        using var connection = _factory.Create();
        connection.Open();

        await connection.ExecuteAsync(@"
            UPDATE Invoices SET Status = @Status WHERE ReviewId = @ReviewId",
            new { Status = status, ReviewId = reviewId });
    }

    // ─── PRIVATE HELPERS ──────────────────────────────────────────────────────

    private static async Task InsertItemsAsync(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        string imageHash,
        IEnumerable<InvoiceItemDto> items)
    {
        foreach (var item in items)
        {
            await connection.ExecuteAsync(@"
                INSERT INTO InvoiceItems (
                    ImageHash, ServiceProduct, Quantity,
                    UnitPrice, LineTotal, TaxRate, TaxAmount
                ) VALUES (
                    @ImageHash, @ServiceProduct, @Quantity,
                    @UnitPrice, @LineTotal, @TaxRate, @TaxAmount
                )",
                new
                {
                    ImageHash = imageHash,
                    item.ServiceProduct,
                    item.Quantity,
                    item.UnitPrice,
                    item.LineTotal,
                    item.TaxRate,
                    item.TaxAmount
                },
                transaction);
        }
    }

    private static async Task<List<InvoiceItemRow>> FetchItemsAsync(
        System.Data.IDbConnection connection,
        string imageHash)
    {
        var rows = await connection.QueryAsync<InvoiceItemRow>(
            "SELECT * FROM InvoiceItems WHERE ImageHash = @ImageHash",
            new { ImageHash = imageHash });

        return rows.ToList();
    }

    private static InvoiceDto MapToDto(
        InvoiceRow row,
        IEnumerable<InvoiceItemRow> itemRows)
    {
        var items = itemRows
            .Select(i => new InvoiceItemDto(
                i.ServiceProduct,
                i.Quantity,
                i.UnitPrice,
                i.TaxRate,
                i.TaxAmount,
                i.LineTotal))
            .ToList()
            .AsReadOnly();

        return new InvoiceDto(
            ReviewId: row.ReviewId,
            ImageHash: row.ImageHash,
            OwnerPhone: row.OwnerPhone,
            InvoiceNumber: row.InvoiceNumber,
            VendorName: row.VendorName,
            Currency: row.Currency,
            Subtotal: (decimal)row.Subtotal,
            TaxRate: (decimal)row.TaxRate,
            Discount: (decimal)row.Discount,
            TotalIva: (decimal)row.TotalIva,
            TotalAmount: (decimal)row.TotalAmount,
            InvoiceDate: DateTime.TryParse(row.InvoiceDate, out var d) ? d : DateTime.UtcNow,
            Confidence: row.Confidence,
            Status: row.Status,
            RawAzurePath: row.RawAzurePath,
            ImagePath: row.ImagePath,
            CreatedAt: DateTime.TryParse(row.CreatedAt, out var c) ? c : DateTime.UtcNow,
            Items: items
        );
    }

    // ─── PRIVATE ROW MODELS ───────────────────────────────────────────────────

    private sealed class InvoiceRow
    {
        public string ImageHash { get; init; } = "";
        public string ReviewId { get; init; } = "";
        public string OwnerPhone { get; init; } = "";
        public string InvoiceNumber { get; init; } = "";
        public string VendorName { get; init; } = "";
        public string InvoiceDate { get; init; } = "";
        public string Currency { get; init; } = "";
        public double Subtotal { get; init; }
        public double TaxRate { get; init; }
        public double Discount { get; init; }
        public double TotalIva { get; init; }
        public double TotalAmount { get; init; }
        public double Confidence { get; init; }
        public string Status { get; init; } = "";
        public string ImagePath { get; init; } = "";
        public string RawAzurePath { get; init; } = "";
        public string CreatedAt { get; init; } = "";
    }

    private sealed class InvoiceItemRow
    {
        public string ImageHash { get; init; } = "";
        public string ServiceProduct { get; init; } = "";
        public decimal Quantity { get; init; }
        public decimal UnitPrice { get; init; }
        public decimal LineTotal { get; init; }
        public decimal TaxRate { get; init; }
        public decimal TaxAmount { get; init; }
    }
}