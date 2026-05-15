using Dapper;
using System.Data.Common;

namespace invoice.Infrastructure.Data;

public class SqliteInitializer
{
    private readonly DbConnectionFactory _factory;

    public SqliteInitializer(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        EnsureStorageFolder();

        using var connection = _factory.Create();
        connection.Open();

        await connection.ExecuteAsync("PRAGMA foreign_keys = ON;");
        await connection.ExecuteAsync(GetSchema());
    }

    private static void EnsureStorageFolder()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "Storage");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    private static string GetSchema() => @"
CREATE TABLE IF NOT EXISTS Users (
    Phone        TEXT PRIMARY KEY,
    Name         TEXT,
    Email        TEXT UNIQUE,
    Cedula       TEXT,
    PasswordHash TEXT,
    CreatedAt    TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Invoices (
    ImageHash     TEXT PRIMARY KEY,
    ReviewId      TEXT NOT NULL,
    OwnerPhone    TEXT,

    InvoiceNumber TEXT,
    VendorName    TEXT,
    InvoiceDate   TEXT,
    Currency      TEXT,

    Subtotal      REAL,
    TaxRate       REAL,
    Discount      REAL,
    TotalIva      REAL,
    TotalAmount   REAL,

    Confidence    REAL,
    Status        TEXT NOT NULL DEFAULT 'NeedsReview',

    ImagePath     TEXT,
    RawAzurePath  TEXT,

    CreatedAt     TEXT DEFAULT CURRENT_TIMESTAMP,

    FOREIGN KEY(OwnerPhone) REFERENCES Users(Phone)
);

CREATE TABLE IF NOT EXISTS InvoiceItems (
    Id             INTEGER PRIMARY KEY AUTOINCREMENT,
    ImageHash      TEXT,
    ServiceProduct TEXT,
    Quantity       REAL,
    UnitPrice      REAL,
    LineTotal      REAL,
    TaxRate        REAL,
    TaxAmount      REAL,
    FOREIGN KEY(ImageHash) REFERENCES Invoices(ImageHash)
);

CREATE TABLE IF NOT EXISTS DownloadTokens (
    Token      TEXT PRIMARY KEY,
    OwnerPhone TEXT,
    FilePath   TEXT NOT NULL,
    ExpiresAt  DATETIME NOT NULL,
    IsUsed     INTEGER DEFAULT 0,
    FOREIGN KEY(OwnerPhone) REFERENCES Users(Phone)
);

CREATE INDEX IF NOT EXISTS idx_invoices_owner  ON Invoices(OwnerPhone);
CREATE INDEX IF NOT EXISTS idx_invoices_status ON Invoices(Status);
CREATE INDEX IF NOT EXISTS idx_invoices_date   ON Invoices(InvoiceDate);
CREATE INDEX IF NOT EXISTS idx_invoices_review ON Invoices(ReviewId);
CREATE INDEX IF NOT EXISTS idx_items_hash      ON InvoiceItems(ImageHash);
CREATE INDEX IF NOT EXISTS idx_tokens_owner    ON DownloadTokens(OwnerPhone);
";
}