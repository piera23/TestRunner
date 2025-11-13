using Microsoft.AspNetCore.SignalR;
using TestRunner.Models;
using TestRunner.Services;

namespace TestRunner.Web.Services;

/// <summary>
/// Service for executing tests with real-time notifications
/// </summary>
public class TestExecutionService
{
    private readonly TestExecutor _testExecutor;
    private readonly IHubContext<TestRunnerHub> _hubContext;
    private readonly ILogger<TestExecutionService> _logger;
    private readonly SemaphoreSlim _executionLock = new(1, 1);
    private TestExecutionResult? _currentExecution;
    private bool _isRunning;

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
        await _executionLock.WaitAsync(cancellationToken);
        try
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("Test execution is already running");
            }

            _isRunning = true;
            _currentExecution = null;

            _logger.LogInformation("Starting test execution");

            // Notify start
            await _hubContext.Clients.All.SendAsync("ExecutionStarted",
                DateTime.Now, config.Projects.Count, cancellationToken);

            // Execute tests with monitoring
            var result = await ExecuteWithMonitoringAsync(config, projectFilter, tagFilter, cancellationToken);

            _currentExecution = result;

            // Notify completion
            await _hubContext.Clients.All.SendAsync("ExecutionCompleted",
                result.IsSuccess, result.TotalDuration.TotalSeconds, cancellationToken);

            _logger.LogInformation("Test execution completed. Success: {IsSuccess}", result.IsSuccess);

            return result;
        }
        finally
        {
            _isRunning = false;
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

                // Execute project
                var projectResult = await ExecuteProjectWithNotificationsAsync(project, cancellationToken);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during test execution");
            executionResult.EndTime = DateTime.Now;
            executionResult.CalculateSummary();
            throw;
        }
    }

    private async Task<TestResult> ExecuteProjectWithNotificationsAsync(
        ProjectConfig project,
        CancellationToken cancellationToken)
    {
        var result = new TestResult
        {
            ProjectName = project.Name,
            ProjectPath = project.Path,
            ProjectType = project.Type,
            StartTime = DateTime.Now,
            Status = TestStatus.Running,
            Tags = project.Tags
        };

        try
        {
            if (!project.Enabled)
            {
                result.Status = TestStatus.Skipped;
                result.EndTime = DateTime.Now;
                return result;
            }

            // Execute commands
            foreach (var command in project.Commands)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Notify command start
                await _hubContext.Clients.All.SendAsync("CommandStarted",
                    project.Name, command, cancellationToken);

                // For real execution, you would use TestExecutor here
                // This is a simplified version
                await Task.Delay(1000, cancellationToken); // Simulate execution

                // Notify command completion
                await _hubContext.Clients.All.SendAsync("CommandCompleted",
                    project.Name, command, 0, cancellationToken);
            }

            result.Status = TestStatus.Passed;
            result.EndTime = DateTime.Now;

            return result;
        }
        catch (OperationCanceledException)
        {
            result.Status = TestStatus.Error;
            result.ErrorMessage = "Operation was cancelled";
            result.EndTime = DateTime.Now;
            return result;
        }
        catch (Exception ex)
        {
            result.Status = TestStatus.Error;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.Now;
            _logger.LogError(ex, "Error executing project {ProjectName}", project.Name);
            return result;
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
    public async Task CancelExecutionAsync()
    {
        if (_isRunning)
        {
            _logger.LogWarning("Cancelling test execution");
            await _hubContext.Clients.All.SendAsync("ExecutionCancelled");
            // Actual cancellation would use CancellationTokenSource
        }
    }
}
