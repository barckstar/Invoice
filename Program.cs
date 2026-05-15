using invoice.Infrastructure.Data;
using invoice.Application.Interfaces;
using invoice.Infrastructure.Storage;
using invoice.Application.Services;
using invoice.Infrastructure.Repositories;
using invoice.Infrastructure.External;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── DB ───────────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddSingleton<SqliteInitializer>();

// ─── SERVICES ─────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IStorageService, FileStorageService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IOcrService, AzureOcrService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IInvoiceValidationService, InvoiceValidationService>();

// ─── JWT ──────────────────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret missing in config");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "invoice-api",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "invoice-client",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ─── CONTROLLERS ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ─── MULTIPART LIMITS ─────────────────────────────────────────────────────────
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 50 * 1024 * 1024);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 50 * 1024 * 1024);

// ─── CORS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ─── BUILD ────────────────────────────────────────────────────────────────────
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider
        .GetRequiredService<SqliteInitializer>()
        .InitializeAsync();
}

// ─── MIDDLEWARE ───────────────────────────────────────────────────────────────
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();