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
builder.Services.AddSingleton<TestRunnerHub>();
builder.Services.AddScoped<TestExecutionService>();
builder.Services.AddScoped<ConfigurationService>();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Add CORS for API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
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
