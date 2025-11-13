using Microsoft.AspNetCore.SignalR;
using TestRunner.Models;
using TestRunner.Services;

namespace TestRunner.Web.Services;

/// <summary>
/// Service for executing tests with real-time notifications
/// </summary>
public class TestExecutionService : IDisposable
{
    private readonly TestExecutor _testExecutor;
    private readonly IHubContext<TestRunnerHub> _hubContext;
    private readonly ILogger<TestExecutionService> _logger;
    private readonly SemaphoreSlim _executionLock = new(1, 1);
    private CancellationTokenSource? _currentCancellationTokenSource;
    private TestExecutionResult? _currentExecution;
    private bool _isRunning;
    private bool _disposed;

    public TestExecutionService(
        TestExecutor testExecutor,
        IHubContext<TestRunnerHub> hubContext,
        ILogger<TestExecutionService> logger)
    {
        _testExecutor = testExecutor;
        _hubContext = hubContext;
        _logger = logger;
    }

    public bool IsRunning => _isRunning;
    public TestExecutionResult? CurrentExecution => _currentExecution;

    /// <summary>
    /// Execute tests with real-time notifications
    /// </summary>
    public async Task<TestExecutionResult> ExecuteTestsAsync(
        TestRunnerConfig config,
        string[]? projectFilter = null,
        string[]? tagFilter = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _executionLock.WaitAsync(cancellationToken);
        try
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("Test execution is already running");
            }

            _isRunning = true;
            _currentExecution = null;

            // Create linked cancellation token
            _currentCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _logger.LogInformation("Starting test execution");

            // Notify start
            await _hubContext.Clients.All.SendAsync("ExecutionStarted",
                DateTime.Now, config.Projects.Count, cancellationToken);

            // Execute tests with monitoring
            var result = await ExecuteWithMonitoringAsync(
                config,
                projectFilter,
                tagFilter,
                _currentCancellationTokenSource.Token);

            _currentExecution = result;

            // Notify completion
            await _hubContext.Clients.All.SendAsync("ExecutionCompleted",
                result.IsSuccess, result.TotalDuration.TotalSeconds, cancellationToken);

            _logger.LogInformation("Test execution completed. Success: {IsSuccess}", result.IsSuccess);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Test execution was cancelled");
            await _hubContext.Clients.All.SendAsync("ExecutionCancelled", cancellationToken);
            throw;
        }
        finally
        {
            _isRunning = false;
            _currentCancellationTokenSource?.Dispose();
            _currentCancellationTokenSource = null;
            _executionLock.Release();
        }
    }

    private async Task<TestExecutionResult> ExecuteWithMonitoringAsync(
        TestRunnerConfig config,
        string[]? projectFilter,
        string[]? tagFilter,
        CancellationToken cancellationToken)
    {
        var executionResult = new TestExecutionResult
        {
            StartTime = DateTime.Now
        };

        try
        {
            // Filter projects
            var projectsToRun = FilterProjects(config.Projects, projectFilter, tagFilter);

            _logger.LogInformation("Executing {Count} projects", projectsToRun.Count);

            // Execute each project
            foreach (var project in projectsToRun)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Notify project start
                await _hubContext.Clients.All.SendAsync("ProjectStarted",
                    project.Name, project.Type.ToString(), cancellationToken);

                // Execute project using the real TestExecutor
                var projectResult = await _testExecutor.ExecuteProjectAsync(project, cancellationToken);

                // Send command results via SignalR
                foreach (var commandResult in projectResult.CommandResults)
                {
                    await _hubContext.Clients.All.SendAsync("CommandCompleted",
                        project.Name,
                        commandResult.Command,
                        commandResult.ExitCode,
                        commandResult.Output ?? string.Empty,
                        cancellationToken);
                }

                executionResult.ProjectResults.Add(projectResult);

                // Notify project completion
                await _hubContext.Clients.All.SendAsync("ProjectCompleted",
                    project.Name,
                    projectResult.Status.ToString(),
                    projectResult.Duration.TotalSeconds,
                    projectResult.IsSuccess,
                    cancellationToken);

                if (config.StopOnFirstFailure && !projectResult.IsSuccess)
                {
                    _logger.LogWarning("Stopping execution due to failure in {ProjectName}", project.Name);
                    break;
                }
            }

            executionResult.EndTime = DateTime.Now;
            executionResult.CalculateSummary();

            return executionResult;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Test execution was cancelled during monitoring");
            executionResult.EndTime = DateTime.Now;
            executionResult.CalculateSummary();
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during test execution");
            executionResult.EndTime = DateTime.Now;
            executionResult.CalculateSummary();
            throw;
        }
    }

    private List<ProjectConfig> FilterProjects(
        List<ProjectConfig> projects,
        string[]? projectFilter,
        string[]? tagFilter)
    {
        var filtered = projects.AsEnumerable();

        if (projectFilter?.Any() == true)
        {
            filtered = filtered.Where(p =>
                projectFilter.Contains(p.Name, StringComparer.OrdinalIgnoreCase));
        }

        if (tagFilter?.Any() == true)
        {
            filtered = filtered.Where(p =>
                p.Tags.Any(tag => tagFilter.Contains(tag, StringComparer.OrdinalIgnoreCase)));
        }

        return filtered.Where(p => p.Enabled).ToList();
    }

    /// <summary>
    /// Cancel current execution
    /// </summary>
    public Task CancelExecutionAsync()
    {
        if (_isRunning && _currentCancellationTokenSource != null)
        {
            _logger.LogWarning("Cancelling test execution");
            _currentCancellationTokenSource.Cancel();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _currentCancellationTokenSource?.Dispose();
        _executionLock.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
