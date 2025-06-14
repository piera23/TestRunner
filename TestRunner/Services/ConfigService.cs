using System.Text.Json;
using TestRunner.Models;
using Microsoft.Extensions.Logging;

namespace TestRunner.Services;

/// <summary>
/// Servizio per la gestione della configurazione
/// </summary>
public class ConfigService
{
    private readonly ILogger<ConfigService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigService(ILogger<ConfigService> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
    }

    /// <summary>
    /// Carica la configurazione da file
    /// </summary>
    public async Task<TestRunnerConfig> LoadConfigAsync(string configPath)
    {
        try
        {
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException($"Configuration file not found: {configPath}");
            }

            _logger.LogInformation("Loading configuration from: {ConfigPath}", configPath);

            var json = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<TestRunnerConfig>(json, _jsonOptions);

            if (config == null)
            {
                throw new InvalidOperationException("Failed to deserialize configuration");
            }

            ValidateConfiguration(config);

            _logger.LogInformation("Configuration loaded successfully with {ProjectCount} projects", config.Projects.Count);
            return config;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in configuration file: {ConfigPath}", configPath);
            throw new InvalidOperationException($"Invalid JSON in configuration file: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration from: {ConfigPath}", configPath);
            throw;
        }
    }

    /// <summary>
    /// Salva la configurazione su file
    /// </summary>
    public async Task SaveConfigAsync(TestRunnerConfig config, string configPath)
    {
        try
        {
            _logger.LogInformation("Saving configuration to: {ConfigPath}", configPath);

            ValidateConfiguration(config);

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            await File.WriteAllTextAsync(configPath, json);

            _logger.LogInformation("Configuration saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration to: {ConfigPath}", configPath);
            throw;
        }
    }

    /// <summary>
    /// Crea una configurazione di default
    /// </summary>
    public TestRunnerConfig CreateDefaultConfig()
    {
        return new TestRunnerConfig
        {
            Projects = new List<ProjectConfig>
            {
                new()
                {
                    Name = "example-web-app",
                    Path = "./web-app",
                    Type = ProjectType.WebApp,
                    Commands = new List<string> { "npm test", "npm run build" },
                    Tags = new List<string> { "frontend", "web" },
                    TimeoutMinutes = 10,
                    Enabled = true
                },
                new()
                {
                    Name = "example-mobile-app",
                    Path = "./mobile-app",
                    Type = ProjectType.MobileApp,
                    Commands = new List<string> { "npm test" },
                    Tags = new List<string> { "mobile", "react-native" },
                    TimeoutMinutes = 15,
                    Enabled = true
                },
                new()
                {
                    Name = "example-python-script",
                    Path = "./python-scripts",
                    Type = ProjectType.PythonScript,
                    Commands = new List<string> { "python -m pytest", "python -m flake8 ." },
                    Tags = new List<string> { "backend", "python" },
                    TimeoutMinutes = 5,
                    Enabled = true
                }
            },
            GlobalTimeoutMinutes = 60,
            ParallelExecution = false,
            MaxParallelProjects = Environment.ProcessorCount,
            StopOnFirstFailure = false,
            OutputFormat = OutputFormat.Console
        };
    }

    /// <summary>
    /// Crea una configurazione automatica rilevando i progetti
    /// </summary>
    public async Task<TestRunnerConfig> CreateAutoConfigAsync(string rootPath, ProjectDetector detector)
    {
        _logger.LogInformation("Creating automatic configuration by detecting projects in: {RootPath}", rootPath);

        var detectedProjects = await detector.DetectProjectsAsync(rootPath);
        
        var config = new TestRunnerConfig
        {
            Projects = detectedProjects,
            GlobalTimeoutMinutes = 60,
            ParallelExecution = detectedProjects.Count > 1,
            MaxParallelProjects = Math.Min(Environment.ProcessorCount, detectedProjects.Count),
            StopOnFirstFailure = false,
            OutputFormat = OutputFormat.Console
        };

        _logger.LogInformation("Auto-configuration created with {ProjectCount} detected projects", detectedProjects.Count);
        
        return config;
    }

    /// <summary>
    /// Valida la configurazione
    /// </summary>
    public void ValidateConfiguration(TestRunnerConfig config)
    {
        var errors = new List<string>();

        // Validazione base
        if (config.Projects == null || !config.Projects.Any())
        {
            errors.Add("At least one project must be configured");
        }

        if (config.GlobalTimeoutMinutes <= 0)
        {
            errors.Add("Global timeout must be greater than 0");
        }

        if (config.MaxParallelProjects <= 0)
        {
            errors.Add("Max parallel projects must be greater than 0");
        }

        // Validazione progetti
        if (config.Projects != null)
        {
            var projectNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < config.Projects.Count; i++)
            {
                var project = config.Projects[i];
                var projectPrefix = $"Project {i + 1}";

                if (string.IsNullOrWhiteSpace(project.Name))
                {
                    errors.Add($"{projectPrefix}: Name is required");
                }
                else if (!projectNames.Add(project.Name))
                {
                    errors.Add($"{projectPrefix}: Duplicate project name '{project.Name}'");
                }

                if (string.IsNullOrWhiteSpace(project.Path))
                {
                    errors.Add($"{projectPrefix} ({project.Name}): Path is required");
                }

                if (project.TimeoutMinutes <= 0)
                {
                    errors.Add($"{projectPrefix} ({project.Name}): Timeout must be greater than 0");
                }

                if (!project.Commands.Any() && project.Type != ProjectType.Auto)
                {
                    errors.Add($"{projectPrefix} ({project.Name}): At least one command is required");
                }

                // Validazione comandi vuoti
                for (int j = 0; j < project.Commands.Count; j++)
                {
                    if (string.IsNullOrWhiteSpace(project.Commands[j]))
                    {
                        errors.Add($"{projectPrefix} ({project.Name}): Command {j + 1} cannot be empty");
                    }
                }
            }
        }

        if (errors.Any())
        {
            var errorMessage = "Configuration validation failed:\n" + string.Join("\n", errors.Select(e => $"- {e}"));
            throw new InvalidOperationException(errorMessage);
        }
    }

    /// <summary>
    /// Unisce due configurazioni
    /// </summary>
    public TestRunnerConfig MergeConfigurations(TestRunnerConfig primary, TestRunnerConfig secondary)
    {
        var merged = new TestRunnerConfig
        {
            // Usa i valori del primary, fallback al secondary
            GlobalTimeoutMinutes = primary.GlobalTimeoutMinutes > 0 ? primary.GlobalTimeoutMinutes : secondary.GlobalTimeoutMinutes,
            ParallelExecution = primary.ParallelExecution || secondary.ParallelExecution,
            MaxParallelProjects = Math.Max(primary.MaxParallelProjects, secondary.MaxParallelProjects),
            StopOnFirstFailure = primary.StopOnFirstFailure || secondary.StopOnFirstFailure,
            OutputFormat = primary.OutputFormat != OutputFormat.Console ? primary.OutputFormat : secondary.OutputFormat,
            ReportFile = primary.ReportFile ?? secondary.ReportFile
        };

        // Unisci i progetti (primary ha precedenza sui nomi duplicati)
        var projectsByName = new Dictionary<string, ProjectConfig>(StringComparer.OrdinalIgnoreCase);

        // Aggiungi prima i progetti secondary
        foreach (var project in secondary.Projects)
        {
            projectsByName[project.Name] = project;
        }

        // Sovrascrivi con i progetti primary
        foreach (var project in primary.Projects)
        {
            projectsByName[project.Name] = project;
        }

        merged.Projects = projectsByName.Values.ToList();

        _logger.LogInformation("Merged configurations: {PrimaryProjects} + {SecondaryProjects} = {MergedProjects} projects",
            primary.Projects.Count, secondary.Projects.Count, merged.Projects.Count);

        return merged;
    }

    /// <summary>
    /// Ottiene il percorso di configurazione di default
    /// </summary>
    public string GetDefaultConfigPath()
    {
        var possibleNames = new[] { "testrunner.json", "test-config.json", ".testrunner.json" };
        
        foreach (var name in possibleNames)
        {
            if (File.Exists(name))
            {
                return name;
            }
        }

        return "testrunner.json";
    }

    /// <summary>
    /// Verifica se esiste una configurazione di default
    /// </summary>
    public bool HasDefaultConfig()
    {
        var defaultPath = GetDefaultConfigPath();
        return File.Exists(defaultPath);
    }
}