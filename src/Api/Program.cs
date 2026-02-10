using CareerAgent.Api.BackgroundServices;
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
var sqliteConnection = builder.Configuration.GetConnectionString("Sqlite");

if (!string.IsNullOrEmpty(sqliteConnection))
{
    builder.Services.AddDbContext<CareerAgentDbContext>(options =>
        options.UseSqlite(sqliteConnection));
    builder.Services.AddScoped<IStorageService, SqliteStorageService>();
}
else
{
    builder.Services.AddSingleton<IStorageService, InMemoryStorageService>();
}

// Register HttpClient for external API calls
builder.Services.AddHttpClient<IJobSearchService, JobSearchService>();
builder.Services.AddHttpClient<IClaudeApiService, ClaudeApiService>();

// Register services
builder.Services.AddSingleton<IJobScoringService, JobScoringService>();
builder.Services.AddSingleton<IPdfService, PdfService>();
builder.Services.AddScoped<IResumeService, ResumeService>();
builder.Services.AddScoped<ICareerAgentService, CareerAgentService>();

// Background service for periodic fetching
builder.Services.AddHostedService<JobFetchBackgroundService>();

// Configure CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (string.IsNullOrEmpty(sqliteConnection))
{
    app.Logger.LogInformation("Storage mode: In-Memory (no SQLite connection string configured)");
}
else
{
    app.Logger.LogInformation("Storage mode: SQLite");

    // Auto-migrate
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CareerAgentDbContext>();
    db.Database.Migrate();
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

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithTags("Health");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
