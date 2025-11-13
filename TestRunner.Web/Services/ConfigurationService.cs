using System.Text.Json;
using TestRunner.Models;
using TestRunner.Services;

namespace TestRunner.Web.Services;

/// <summary>
/// Service for managing configurations in the web interface
/// </summary>
public class ConfigurationService
{
    private readonly ConfigService _configService;
    private readonly ProjectDetector _projectDetector;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly string _configDirectory;

    public ConfigurationService(
        ConfigService configService,
        ProjectDetector projectDetector,
        ILogger<ConfigurationService> logger,
        IConfiguration configuration)
    {
        _configService = configService;
        _projectDetector = projectDetector;
        _logger = logger;
        _configDirectory = configuration.GetValue<string>("ConfigDirectory") ?? "./configs";

        // Ensure config directory exists
        Directory.CreateDirectory(_configDirectory);
    }

    /// <summary>
    /// Get all available configurations
    /// </summary>
    public async Task<List<ConfigInfo>> GetAllConfigurationsAsync()
    {
        var configs = new List<ConfigInfo>();

        if (!Directory.Exists(_configDirectory))
            return configs;

        var jsonFiles = Directory.GetFiles(_configDirectory, "*.json");

        foreach (var file in jsonFiles)
        {
            try
            {
                var config = await _configService.LoadConfigAsync(file);
                configs.Add(new ConfigInfo
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    FilePath = file,
                    ProjectCount = config.Projects.Count,
                    EnabledProjects = config.EnabledProjectsCount,
                    LastModified = File.GetLastWriteTime(file),
                    Config = config
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load config: {File}", file);
            }
        }

        return configs.OrderByDescending(c => c.LastModified).ToList();
    }

    /// <summary>
    /// Get configuration by name
    /// </summary>
    public async Task<TestRunnerConfig?> GetConfigurationAsync(string name)
    {
        var filePath = Path.Combine(_configDirectory, $"{name}.json");

        if (!File.Exists(filePath))
            return null;

        try
        {
            return await _configService.LoadConfigAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration: {Name}", name);
            return null;
        }
    }

    /// <summary>
    /// Save configuration
    /// </summary>
    public async Task SaveConfigurationAsync(string name, TestRunnerConfig config)
    {
        var filePath = Path.Combine(_configDirectory, $"{name}.json");

        config.UpdatedAt = DateTime.UtcNow;
        await _configService.SaveConfigAsync(config, filePath);

        _logger.LogInformation("Configuration saved: {Name}", name);
    }

    /// <summary>
    /// Create new configuration with auto-detection
    /// </summary>
    public async Task<TestRunnerConfig> CreateAutoConfigurationAsync(string name, string projectPath)
    {
        _logger.LogInformation("Creating auto configuration for: {Path}", projectPath);

        var config = await _configService.CreateAutoConfigAsync(projectPath, _projectDetector);
        config.Name = name;

        await SaveConfigurationAsync(name, config);

        return config;
    }

    /// <summary>
    /// Delete configuration
    /// </summary>
    public Task DeleteConfigurationAsync(string name)
    {
        var filePath = Path.Combine(_configDirectory, $"{name}.json");

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("Configuration deleted: {Name}", name);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validate configuration
    /// </summary>
    public Task<List<string>> ValidateConfigurationAsync(TestRunnerConfig config)
    {
        try
        {
            _configService.ValidateConfiguration(config);
            return Task.FromResult(new List<string>());
        }
        catch (InvalidOperationException ex)
        {
            var errors = ex.Message.Split('\n')
                .Where(line => line.StartsWith("- "))
                .Select(line => line.Substring(2))
                .ToList();
            return Task.FromResult(errors);
        }
    }

    /// <summary>
    /// Detect projects in directory
    /// </summary>
    public async Task<List<ProjectConfig>> DetectProjectsAsync(string path, int maxDepth = 3)
    {
        return await _projectDetector.DetectProjectsAsync(path, maxDepth);
    }
}

/// <summary>
/// Configuration information
/// </summary>
public class ConfigInfo
{
    public string Name { get; set; } = "";
    public string FilePath { get; set; } = "";
    public int ProjectCount { get; set; }
    public int EnabledProjects { get; set; }
    public DateTime LastModified { get; set; }
    public TestRunnerConfig? Config { get; set; }
}
