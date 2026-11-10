using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Middleware;
using SchoolAdmission.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure URL / port (default to 8080)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Controllers and JSON Serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Database Connection String configuration
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=school_admission;Username=postgres;Password=postgres";

// If DATABASE_URL is in postgres://user:pass@host:port/db format, convert it to Npgsql format
if (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://"))
{
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    var user = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";
    var host = uri.Host;
    var dbPort = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');
    connectionString = $"Host={host};Port={dbPort};Database={database};Username={user};Password={password}";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Register Application Services
builder.Services.AddScoped<IAdmissionService, AdmissionService>();
builder.Services.AddHostedService<ExpirationSweeper>();

// Configure CORS for Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Global Exception Middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors("AllowAll");

app.MapControllers();

// Root status endpoint for user convenience
app.MapGet("/", () => Results.Ok(new
{
    service = "School Admission System API",
    status = "Running",
    version = "1.0",
    frontendUrl = "http://localhost:3000",
    availableEndpoints = new[]
    {
        "GET /classes",
        "POST /classes",
        "GET /classes/{classId}",
        "POST /classes/{classId}/holds",
        "GET /holds/{holdId}",
        "POST /holds/{holdId}/confirm",
        "DELETE /holds/{holdId}",
        "GET /me/holds"
    }
}));

// Apply Code-First Migrations automatically on startup if using a relational database
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (dbContext.Database.IsRelational())
    {
        try
        {
            logger.LogInformation("Applying EF Core migrations...");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("EF Core migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not run migrations automatically (Database may still be starting up or not relational).");
        }
    }
}

app.Run();

// For WebApplicationFactory in integration tests
public partial class Program { }
