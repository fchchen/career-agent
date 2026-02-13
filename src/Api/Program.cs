using CareerAgent.Api.Data;
using CareerAgent.Api.Endpoints;
using CareerAgent.Api.Middleware;
using CareerAgent.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Career Agent API", Version = "v1" });
});

// Configure storage
var databaseProvider = builder.Configuration["Database:Provider"]?.Trim();
var sqliteConnection = builder.Configuration.GetConnectionString("Sqlite");
var sqlServerConnection = builder.Configuration.GetConnectionString("SqlServer");

var useSqlServer =
    databaseProvider?.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) == true ||
    (string.IsNullOrWhiteSpace(databaseProvider) &&
     !string.IsNullOrWhiteSpace(sqlServerConnection) &&
     string.IsNullOrWhiteSpace(sqliteConnection));

var useSqlite =
    !useSqlServer &&
    !string.IsNullOrWhiteSpace(sqliteConnection) &&
    (string.IsNullOrWhiteSpace(databaseProvider) ||
     databaseProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase));

if (useSqlServer)
{
    if (string.IsNullOrWhiteSpace(sqlServerConnection))
        throw new InvalidOperationException("Database provider is set to SqlServer but ConnectionStrings:SqlServer is missing.");

    builder.Services.AddDbContext<SqlServerCareerAgentDbContext>(options =>
        options.UseSqlServer(sqlServerConnection, sql =>
        {
            sql.EnableRetryOnFailure();
        }));
    builder.Services.AddScoped<CareerAgentDbContext>(sp =>
        sp.GetRequiredService<SqlServerCareerAgentDbContext>());
    builder.Services.AddScoped<IStorageService, SqliteStorageService>();
}
else if (useSqlite)
{
    builder.Services.AddDbContext<CareerAgentDbContext>(options =>
        options.UseSqlite(sqliteConnection!));
    builder.Services.AddScoped<IStorageService, SqliteStorageService>();
}
else
{
    builder.Services.AddSingleton<IStorageService, InMemoryStorageService>();
}

// Register HttpClient for external API calls
builder.Services.AddHttpClient<JobSearchService>();
builder.Services.AddHttpClient<AdzunaJobSearchService>();
builder.Services.AddTransient<IJobSearchSource>(sp => sp.GetRequiredService<JobSearchService>());
builder.Services.AddTransient<IJobSearchSource>(sp => sp.GetRequiredService<AdzunaJobSearchService>());
builder.Services.AddTransient<IJobSearchService, CompositeJobSearchService>();
builder.Services.AddHttpClient<ILlmService, GeminiLlmService>();
builder.Services.AddHttpClient<IGeocodingService, NominatimGeocodingService>();

// Register services
builder.Services.AddSingleton<IJobScoringService, JobScoringService>();
builder.Services.AddSingleton<IPdfService, PdfService>();
builder.Services.AddScoped<IResumeService, ResumeService>();
builder.Services.AddScoped<ICareerAgentService, CareerAgentService>();

// Background service for periodic fetching (disabled — run searches manually via UI)
// builder.Services.AddHostedService<JobFetchBackgroundService>();

// Configure CORS for Angular frontend
string[]? allowedOrigins = null;
var csv = builder.Configuration["Cors:AllowedOriginsCsv"];
if (!string.IsNullOrWhiteSpace(csv))
{
    allowedOrigins = csv
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
else
{
    allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
}

allowedOrigins ??= ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
            return;
        }

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (!useSqlServer && !useSqlite)
{
    app.Logger.LogInformation("Storage mode: In-Memory");
}
else
{
    app.Logger.LogInformation("Storage mode: {Mode}", useSqlServer ? "SQL Server" : "SQLite");

    // SQLite and SQL Server maintain separate migration streams.
    using var scope = app.Services.CreateScope();
    if (useSqlServer)
    {
        var sqlServerDb = scope.ServiceProvider.GetRequiredService<SqlServerCareerAgentDbContext>();
        sqlServerDb.Database.Migrate();
    }
    else
    {
        var sqliteDb = scope.ServiceProvider.GetRequiredService<CareerAgentDbContext>();
        sqliteDb.Database.Migrate();
    }

    // Purge jobs older than 7 days on startup
    var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
    var purged = storage.PurgeOldJobsAsync(maxAgeDays: 7).GetAwaiter().GetResult();
    if (purged > 0)
        app.Logger.LogInformation("Purged {Count} jobs older than 7 days", purged);
}

// Middleware
app.UseMiddleware<GlobalExceptionMiddleware>();

// Always enable Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAngular");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Map endpoints
app.MapJobSearchEndpoints();
app.MapResumeEndpoints();
app.MapDashboardEndpoints();
app.MapAdminEndpoints();
app.MapProfileEndpoints();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithTags("Health");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
