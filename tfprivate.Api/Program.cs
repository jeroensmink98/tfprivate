using Azure.Monitor.OpenTelemetry.AspNetCore;
using tfprivate.Api.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure base URL
builder.Configuration.SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();

// Only add Swagger in non-production environments
if (!builder.Environment.IsProduction())
{
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Terraform Private Registry API",
            Version = "v1",
            Description = "API for managing private Terraform modules"
        });

        // Add API Key security definition
        c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "X-API-Key",
            Description = "API Key authentication. Example: 'X-API-Key: your-api-key-here'"
        });

        // Add global security requirement
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "ApiKey"
                    }
                },
                Array.Empty<string>()
            }
        });

        // Configure Swagger to handle file uploads
        c.MapType<IFormFile>(() => new OpenApiSchema
        {
            Type = "string",
            Format = "binary"
        });
    });
}

builder.Services.AddControllers();

// Add Application Insights if connection string is available
var appInsightsConnectionString = builder.Configuration.GetSection("Azure:AzureMonitor:ConnectionString").Value;
var envAppInsightsKey = Environment.GetEnvironmentVariable("APP_INSIGHT_KEY");
var isAppInsightsEnabled = !string.IsNullOrEmpty(appInsightsConnectionString) || !string.IsNullOrEmpty(envAppInsightsKey);

if (isAppInsightsEnabled)
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
    {
        options.ConnectionString = !string.IsNullOrEmpty(appInsightsConnectionString)
            ? appInsightsConnectionString
            : envAppInsightsKey;
    });
}

// Configure Kestrel
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    // Listen on port 443 for HTTPS
    options.ListenAnyIP(443, listenOptions =>
    {
        listenOptions.UseHttps();
    });
    // Also listen on port 80 for HTTP
    options.ListenAnyIP(80);
});

builder.Services.AddSingleton<IStorageService, StorageService>();
builder.Services.AddSingleton<ITerraformModuleValidator, TerraformModuleValidator>();

var app = builder.Build();

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application Insights is {Status}", isAppInsightsEnabled ? "enabled" : "disabled");

var containerName = Environment.GetEnvironmentVariable("HOSTNAME");
if (!string.IsNullOrEmpty(containerName))
{
    logger.LogInformation("Running in container: {ContainerName}", containerName);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Terraform Private Registry API v1");
    });
}

app.MapControllers();

// Validate storage connection
try
{
    using var scope = app.Services.CreateScope();
    var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
    await storageService.ValidateConnectionAsync();
    logger.LogInformation("Successfully validated Azure Storage connection");
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Failed to validate Azure Storage connection. Application will now exit.");
    Environment.Exit(1);
}

app.Run();

