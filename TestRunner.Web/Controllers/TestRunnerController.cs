using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
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
        try
        {
            var currentExecution = _executionService.CurrentExecution;

            return Ok(new
            {
                IsRunning = _executionService.IsRunning,
                CurrentExecution = currentExecution != null ? new
                {
                    StartTime = currentExecution.StartTime,
                    TotalProjects = currentExecution.Summary?.TotalProjects ?? 0,
                    PassedProjects = currentExecution.Summary?.PassedProjects ?? 0,
                    FailedProjects = currentExecution.Summary?.FailedProjects ?? 0,
                    SkippedProjects = currentExecution.Summary?.SkippedProjects ?? 0,
                    ErrorProjects = currentExecution.Summary?.ErrorProjects ?? 0
                } : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting execution status");
            return StatusCode(500, new { error = "Error retrieving execution status" });
        }
    }

    /// <summary>
    /// Run tests with specified configuration
    /// </summary>
    [HttpPost("run")]
    public async Task<IActionResult> RunTests([FromBody] RunTestsRequest request)
    {
        try
        {
            // Validate request
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid request", details = ModelState });
            }

            if (string.IsNullOrWhiteSpace(request.ConfigName))
            {
                return BadRequest(new { error = "Configuration name is required" });
            }

            if (_executionService.IsRunning)
            {
                return Conflict(new { error = "Test execution is already running" });
            }

            var config = await _configService.GetConfigurationAsync(request.ConfigName);

            if (config == null)
            {
                return NotFound(new { error = $"Configuration not found: {request.ConfigName}" });
            }

            // Validate configuration has projects
            if (config.Projects == null || !config.Projects.Any())
            {
                return BadRequest(new { error = "Configuration has no projects defined" });
            }

            _logger.LogInformation("Starting test execution for configuration: {ConfigName}", request.ConfigName);

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
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Test execution was cancelled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during test execution");
                }
            });

            return Accepted(new
            {
                message = "Test execution started",
                configName = request.ConfigName,
                projectCount = config.Projects.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting test execution");
            return StatusCode(500, new { error = "Failed to start test execution", details = ex.Message });
        }
    }

    /// <summary>
    /// Cancel current test execution
    /// </summary>
    [HttpPost("cancel")]
    public async Task<IActionResult> CancelExecution()
    {
        try
        {
            if (!_executionService.IsRunning)
            {
                return BadRequest(new { error = "No test execution is currently running" });
            }

            await _executionService.CancelExecutionAsync();
            _logger.LogInformation("Test execution cancellation requested");

            return Ok(new { message = "Cancellation requested" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling test execution");
            return StatusCode(500, new { error = "Failed to cancel test execution" });
        }
    }

    /// <summary>
    /// Get execution history
    /// </summary>
    [HttpGet("history")]
    [HttpGet("history/{count:int}")]
    public IActionResult GetHistory(int count = 50)
    {
        try
        {
            // Validate count parameter
            if (count <= 0 || count > 1000)
            {
                return BadRequest(new { error = "Count must be between 1 and 1000" });
            }

            // In a real implementation, this would retrieve from a database
            // For now, return mock data for demonstration
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
                    Duration = 120.5,
                    Status = "Completed"
                },
                new
                {
                    Id = "2",
                    Timestamp = DateTime.Now.AddHours(-5),
                    ConfigName = "development",
                    TotalProjects = 3,
                    PassedProjects = 3,
                    FailedProjects = 0,
                    Duration = 45.2,
                    Status = "Completed"
                }
            };

            return Ok(history.Take(count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving execution history");
            return StatusCode(500, new { error = "Failed to retrieve execution history" });
        }
    }

    /// <summary>
    /// Get detailed execution result by ID
    /// </summary>
    [HttpGet("result/{id}")]
    public IActionResult GetExecutionResult(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new { error = "Execution ID is required" });
            }

            // In a real implementation, retrieve from database
            // For now, return current execution if ID matches
            if (_executionService.CurrentExecution != null)
            {
                return Ok(_executionService.CurrentExecution);
            }

            return NotFound(new { error = $"Execution not found: {id}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving execution result for ID: {Id}", id);
            return StatusCode(500, new { error = "Failed to retrieve execution result" });
        }
    }
}

/// <summary>
/// Request model for running tests
/// </summary>
public class RunTestsRequest
{
    /// <summary>
    /// Name of the configuration to run
    /// </summary>
    [Required(ErrorMessage = "Configuration name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Configuration name must be between 1 and 100 characters")]
    public string ConfigName { get; set; } = "default";

    /// <summary>
    /// Optional filter for specific projects
    /// </summary>
    [MaxLength(50, ErrorMessage = "Maximum 50 projects can be specified")]
    public string[]? Projects { get; set; }

    /// <summary>
    /// Optional filter for projects by tags
    /// </summary>
    [MaxLength(20, ErrorMessage = "Maximum 20 tags can be specified")]
    public string[]? Tags { get; set; }
}
