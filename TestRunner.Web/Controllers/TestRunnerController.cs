using Microsoft.AspNetCore.Mvc;
using TestRunner.Models;
using TestRunner.Web.Services;

namespace TestRunner.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestRunnerController : ControllerBase
{
    private readonly TestExecutionService _executionService;
    private readonly ConfigurationService _configService;
    private readonly ILogger<TestRunnerController> _logger;

    public TestRunnerController(
        TestExecutionService executionService,
        ConfigurationService configService,
        ILogger<TestRunnerController> logger)
    {
        _executionService = executionService;
        _configService = configService;
        _logger = logger;
    }

    /// <summary>
    /// Get execution status
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            IsRunning = _executionService.IsRunning,
            CurrentExecution = _executionService.CurrentExecution != null ? new
            {
                StartTime = _executionService.CurrentExecution.StartTime,
                TotalProjects = _executionService.CurrentExecution.Summary.TotalProjects,
                PassedProjects = _executionService.CurrentExecution.Summary.PassedProjects,
                FailedProjects = _executionService.CurrentExecution.Summary.FailedProjects
            } : null
        });
    }

    /// <summary>
    /// Run tests with specified configuration
    /// </summary>
    [HttpPost("run")]
    public async Task<IActionResult> RunTests([FromBody] RunTestsRequest request)
    {
        try
        {
            if (_executionService.IsRunning)
            {
                return Conflict(new { error = "Test execution is already running" });
            }

            var config = await _configService.GetConfigurationAsync(request.ConfigName);

            if (config == null)
            {
                return NotFound(new { error = $"Configuration not found: {request.ConfigName}" });
            }

            // Start execution in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await _executionService.ExecuteTestsAsync(
                        config,
                        request.Projects,
                        request.Tags);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during test execution");
                }
            });

            return Accepted(new { message = "Test execution started" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting test execution");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cancel current test execution
    /// </summary>
    [HttpPost("cancel")]
    public async Task<IActionResult> CancelExecution()
    {
        await _executionService.CancelExecutionAsync();
        return Ok(new { message = "Cancellation requested" });
    }

    /// <summary>
    /// Get execution history
    /// </summary>
    [HttpGet("history")]
    public IActionResult GetHistory()
    {
        // In a real implementation, this would retrieve from a database
        var history = new List<object>
        {
            new
            {
                Id = "1",
                Timestamp = DateTime.Now.AddHours(-2),
                ConfigName = "production",
                TotalProjects = 5,
                PassedProjects = 4,
                FailedProjects = 1,
                Duration = 120.5
            }
        };

        return Ok(history);
    }
}

public class RunTestsRequest
{
    public string ConfigName { get; set; } = "default";
    public string[]? Projects { get; set; }
    public string[]? Tags { get; set; }
}
