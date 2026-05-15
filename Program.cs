using invoice.Infrastructure.Data;
using invoice.Application.Interfaces;
using invoice.Infrastructure.Storage;
using invoice.Application.Services;
using invoice.Infrastructure.Repositories;
using invoice.Infrastructure.External;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

//
// 🔹 SERVICES
//

// DB
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddSingleton<SqliteInitializer>();

// Core
builder.Services.AddSingleton<IStorageService, FileStorageService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IOcrService, AzureOcrService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IInvoiceValidationService, InvoiceValidationService>();

// Controllers
builder.Services.AddControllers();

// Multipart limits
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50MB
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024;
});

// CORS (para futuro: frontend / n8n)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
    );
});

var app = builder.Build();

//
// 🔹 INIT DB
//
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<SqliteInitializer>();
    await initializer.InitializeAsync();
}

//
// 🔹 MIDDLEWARE
//

// Debug simple (puedes quitar luego)
app.Use(async (context, next) =>
{
    Console.WriteLine($"REQUEST: {context.Request.Method} {context.Request.Path}");
    await next();
});

app.UseCors("AllowAll");

// ⚠️ Desactivado por ahora para evitar conflictos
// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();