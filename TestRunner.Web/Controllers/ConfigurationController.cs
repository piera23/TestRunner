using Microsoft.AspNetCore.Mvc;
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
        var configs = await _configService.GetAllConfigurationsAsync();
        return Ok(configs.Select(c => new
        {
            c.Name,
            c.ProjectCount,
            c.EnabledProjects,
            c.LastModified
        }));
    }

    /// <summary>
    /// Get configuration by name
    /// </summary>
    [HttpGet("{name}")]
    public async Task<IActionResult> Get(string name)
    {
        var config = await _configService.GetConfigurationAsync(name);

        if (config == null)
        {
            return NotFound(new { error = $"Configuration not found: {name}" });
        }

        return Ok(config);
    }

    /// <summary>
    /// Create or update configuration
    /// </summary>
    [HttpPut("{name}")]
    public async Task<IActionResult> Save(string name, [FromBody] TestRunnerConfig config)
    {
        try
        {
            // Validate configuration
            var errors = await _configService.ValidateConfigurationAsync(config);
            if (errors.Any())
            {
                return BadRequest(new { errors });
            }

            await _configService.SaveConfigurationAsync(name, config);
            return Ok(new { message = "Configuration saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration");
            return StatusCode(500, new { error = ex.Message });
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
            await _configService.DeleteConfigurationAsync(name);
            return Ok(new { message = "Configuration deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration");
            return StatusCode(500, new { error = ex.Message });
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
            var config = await _configService.CreateAutoConfigurationAsync(
                request.Name,
                request.ProjectPath);

            return CreatedAtAction(nameof(Get), new { name = request.Name }, config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auto-detection");
            return StatusCode(500, new { error = ex.Message });
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
            var projects = await _configService.DetectProjectsAsync(
                request.Path,
                request.MaxDepth ?? 3);

            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting projects");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Validate configuration
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] TestRunnerConfig config)
    {
        var errors = await _configService.ValidateConfigurationAsync(config);

        if (errors.Any())
        {
            return BadRequest(new { isValid = false, errors });
        }

        return Ok(new { isValid = true, message = "Configuration is valid" });
    }
}

public class AutoDetectRequest
{
    public string Name { get; set; } = "auto-detected";
    public string ProjectPath { get; set; } = ".";
}

public class DetectProjectsRequest
{
    public string Path { get; set; } = ".";
    public int? MaxDepth { get; set; }
}
