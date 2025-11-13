using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TestRunner.Models;
using TestRunner.Web.Services;

namespace TestRunner.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly ConfigurationService _configService;
    private readonly ILogger<ConfigurationController> _logger;

    public ConfigurationController(
        ConfigurationService configService,
        ILogger<ConfigurationController> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    /// <summary>
    /// Get all configurations
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var configs = await _configService.GetAllConfigurationsAsync();
            return Ok(configs.Select(c => new
            {
                c.Name,
                c.ProjectCount,
                c.EnabledProjects,
                c.LastModified
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all configurations");
            return StatusCode(500, new { error = "Failed to retrieve configurations" });
        }
    }

    /// <summary>
    /// Get configuration by name
    /// </summary>
    [HttpGet("{name}")]
    public async Task<IActionResult> Get(string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { error = "Configuration name is required" });
            }

            // Validate name to prevent path traversal
            if (name.Contains("..") || name.Contains("/") || name.Contains("\\"))
            {
                return BadRequest(new { error = "Invalid configuration name" });
            }

            var config = await _configService.GetConfigurationAsync(name);

            if (config == null)
            {
                return NotFound(new { error = $"Configuration not found: {name}" });
            }

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration: {Name}", name);
            return StatusCode(500, new { error = "Failed to retrieve configuration" });
        }
    }

    /// <summary>
    /// Create or update configuration
    /// </summary>
    [HttpPut("{name}")]
    public async Task<IActionResult> Save(string name, [FromBody] TestRunnerConfig config)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { error = "Configuration name is required" });
            }

            // Validate name to prevent path traversal
            if (name.Contains("..") || name.Contains("/") || name.Contains("\\"))
            {
                return BadRequest(new { error = "Invalid configuration name" });
            }

            if (config == null)
            {
                return BadRequest(new { error = "Configuration data is required" });
            }

            // Validate configuration
            var errors = await _configService.ValidateConfigurationAsync(config);
            if (errors.Any())
            {
                return BadRequest(new { errors });
            }

            await _configService.SaveConfigurationAsync(name, config);
            _logger.LogInformation("Configuration saved: {Name}", name);

            return Ok(new
            {
                message = "Configuration saved successfully",
                name,
                projectCount = config.Projects?.Count ?? 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration: {Name}", name);
            return StatusCode(500, new { error = "Failed to save configuration", details = ex.Message });
        }
    }

    /// <summary>
    /// Delete configuration
    /// </summary>
    [HttpDelete("{name}")]
    public async Task<IActionResult> Delete(string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(new { error = "Configuration name is required" });
            }

            // Validate name to prevent path traversal
            if (name.Contains("..") || name.Contains("/") || name.Contains("\\"))
            {
                return BadRequest(new { error = "Invalid configuration name" });
            }

            await _configService.DeleteConfigurationAsync(name);
            _logger.LogInformation("Configuration deleted: {Name}", name);

            return Ok(new { message = "Configuration deleted successfully" });
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = $"Configuration not found: {name}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration: {Name}", name);
            return StatusCode(500, new { error = "Failed to delete configuration" });
        }
    }

    /// <summary>
    /// Create configuration with auto-detection
    /// </summary>
    [HttpPost("auto-detect")]
    public async Task<IActionResult> AutoDetect([FromBody] AutoDetectRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid request", details = ModelState });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "Configuration name is required" });
            }

            if (string.IsNullOrWhiteSpace(request.ProjectPath))
            {
                return BadRequest(new { error = "Project path is required" });
            }

            // Validate name to prevent path traversal
            if (request.Name.Contains("..") || request.Name.Contains("/") || request.Name.Contains("\\"))
            {
                return BadRequest(new { error = "Invalid configuration name" });
            }

            // Validate path exists
            if (!Directory.Exists(request.ProjectPath))
            {
                return BadRequest(new { error = $"Directory not found: {request.ProjectPath}" });
            }

            var config = await _configService.CreateAutoConfigurationAsync(
                request.Name,
                request.ProjectPath);

            _logger.LogInformation("Auto-detected configuration created: {Name} with {Count} projects",
                request.Name, config.Projects?.Count ?? 0);

            return CreatedAtAction(nameof(Get), new { name = request.Name }, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auto-detection");
            return StatusCode(500, new { error = "Failed to auto-detect projects", details = ex.Message });
        }
    }

    /// <summary>
    /// Detect projects in directory
    /// </summary>
    [HttpPost("detect-projects")]
    public async Task<IActionResult> DetectProjects([FromBody] DetectProjectsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { error = "Invalid request", details = ModelState });
            }

            if (string.IsNullOrWhiteSpace(request.Path))
            {
                return BadRequest(new { error = "Path is required" });
            }

            // Validate path exists
            if (!Directory.Exists(request.Path))
            {
                return BadRequest(new { error = $"Directory not found: {request.Path}" });
            }

            // Validate max depth
            var maxDepth = request.MaxDepth ?? 3;
            if (maxDepth < 1 || maxDepth > 10)
            {
                return BadRequest(new { error = "Max depth must be between 1 and 10" });
            }

            var projects = await _configService.DetectProjectsAsync(request.Path, maxDepth);

            _logger.LogInformation("Detected {Count} projects in {Path}", projects.Count, request.Path);

            return Ok(new
            {
                path = request.Path,
                projectCount = projects.Count,
                projects
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied to path: {Path}", request.Path);
            return StatusCode(403, new { error = "Access denied to the specified path" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting projects in {Path}", request.Path);
            return StatusCode(500, new { error = "Failed to detect projects", details = ex.Message });
        }
    }

    /// <summary>
    /// Validate configuration
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] TestRunnerConfig config)
    {
        try
        {
            if (config == null)
            {
                return BadRequest(new { error = "Configuration data is required" });
            }

            var errors = await _configService.ValidateConfigurationAsync(config);

            if (errors.Any())
            {
                return BadRequest(new
                {
                    isValid = false,
                    errors,
                    message = "Configuration validation failed"
                });
            }

            return Ok(new
            {
                isValid = true,
                message = "Configuration is valid",
                projectCount = config.Projects?.Count ?? 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration");
            return StatusCode(500, new { error = "Failed to validate configuration" });
        }
    }
}

/// <summary>
/// Request for auto-detecting projects
/// </summary>
public class AutoDetectRequest
{
    /// <summary>
    /// Name for the configuration
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\-_]+$", ErrorMessage = "Name can only contain letters, numbers, hyphens, and underscores")]
    public string Name { get; set; } = "auto-detected";

    /// <summary>
    /// Path to scan for projects
    /// </summary>
    [Required(ErrorMessage = "Project path is required")]
    public string ProjectPath { get; set; } = ".";
}

/// <summary>
/// Request for detecting projects
/// </summary>
public class DetectProjectsRequest
{
    /// <summary>
    /// Path to scan
    /// </summary>
    [Required(ErrorMessage = "Path is required")]
    public string Path { get; set; } = ".";

    /// <summary>
    /// Maximum depth to scan (1-10)
    /// </summary>
    [Range(1, 10, ErrorMessage = "Max depth must be between 1 and 10")]
    public int? MaxDepth { get; set; }
}
