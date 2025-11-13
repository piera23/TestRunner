using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using TestRunner.Services;
using TestRunner.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();

// Add SignalR
builder.Services.AddSignalR();

// Add TestRunner services
builder.Services.AddSingleton<ConfigService>();
builder.Services.AddSingleton<ProjectDetector>();
builder.Services.AddSingleton<TestExecutor>();
builder.Services.AddSingleton<ReportGenerator>();

// Add Web-specific services
// Note: SignalR Hubs should NOT be registered - they are managed by the framework
builder.Services.AddSingleton<TestExecutionService>();
builder.Services.AddSingleton<ConfigurationService>();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Add CORS for API - Configured for security
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // In development, allow any origin for testing
        // In production, this should be configured with specific allowed origins
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // In production, restrict to specific origins
            // Configure via appsettings.json or environment variables
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? new[] { "https://yourdomain.com" };

            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll");

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapControllers();

// Map SignalR hub
app.MapHub<TestRunnerHub>("/testrunner-hub");

app.Run();
