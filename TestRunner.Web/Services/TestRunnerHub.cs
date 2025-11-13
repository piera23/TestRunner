using Microsoft.AspNetCore.SignalR;
using TestRunner.Models;

namespace TestRunner.Web.Services;

/// <summary>
/// SignalR Hub for real-time test execution updates
/// </summary>
public class TestRunnerHub : Hub
{
    private readonly ILogger<TestRunnerHub> _logger;

    public TestRunnerHub(ILogger<TestRunnerHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Send test execution started notification
    /// </summary>
    public async Task NotifyTestStarted(string projectName)
    {
        await Clients.All.SendAsync("TestStarted", projectName, DateTime.Now);
    }

    /// <summary>
    /// Send test execution progress update
    /// </summary>
    public async Task NotifyTestProgress(string projectName, string status, int percentage)
    {
        await Clients.All.SendAsync("TestProgress", projectName, status, percentage);
    }

    /// <summary>
    /// Send test execution completed notification
    /// </summary>
    public async Task NotifyTestCompleted(string projectName, TestStatus status, TimeSpan duration)
    {
        await Clients.All.SendAsync("TestCompleted", projectName, status.ToString(), duration.TotalSeconds);
    }

    /// <summary>
    /// Send command output in real-time
    /// </summary>
    public async Task NotifyCommandOutput(string projectName, string command, string output)
    {
        await Clients.All.SendAsync("CommandOutput", projectName, command, output);
    }

    /// <summary>
    /// Send overall execution summary
    /// </summary>
    public async Task NotifyExecutionSummary(TestExecutionResult result)
    {
        await Clients.All.SendAsync("ExecutionSummary", new
        {
            TotalProjects = result.Summary.TotalProjects,
            PassedProjects = result.Summary.PassedProjects,
            FailedProjects = result.Summary.FailedProjects,
            SuccessRate = result.Summary.SuccessRate,
            Duration = result.TotalDuration.TotalSeconds
        });
    }
}
