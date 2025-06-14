using System.Text.Json.Serialization;

namespace TestRunner.Models;

/// <summary>
/// Configurazione di un singolo progetto da testare
/// </summary>
public class ProjectConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";
    
    [JsonPropertyName("type")]
    public ProjectType Type { get; set; } = ProjectType.Auto;
    
    [JsonPropertyName("commands")]
    public List<string> Commands { get; set; } = new();
    
    [JsonPropertyName("environment")]
    public Dictionary<string, string> Environment { get; set; } = new();
    
    [JsonPropertyName("timeout_minutes")]
    public int TimeoutMinutes { get; set; } = 10;
    
    [JsonPropertyName("working_directory")]
    public string? WorkingDirectory { get; set; }
    
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
    
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
    
    [JsonPropertyName("pre_commands")]
    public List<string> PreCommands { get; set; } = new();
    
    [JsonPropertyName("post_commands")]
    public List<string> PostCommands { get; set; } = new();
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
    
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 0;
    
    [JsonPropertyName("retry_count")]
    public int RetryCount { get; set; } = 0;
    
    [JsonPropertyName("retry_delay_seconds")]
    public int RetryDelaySeconds { get; set; } = 5;
    
    [JsonPropertyName("ignore_exit_codes")]
    public List<int> IgnoreExitCodes { get; set; } = new();
    
    [JsonPropertyName("expected_output_patterns")]
    public List<string> ExpectedOutputPatterns { get; set; } = new();
    
    [JsonPropertyName("forbidden_output_patterns")]
    public List<string> ForbiddenOutputPatterns { get; set; } = new();
    
    /// <summary>
    /// Ottiene la directory di lavoro effettiva
    /// </summary>
    public string GetEffectiveWorkingDirectory()
    {
        return WorkingDirectory ?? Path;
    }
    
    /// <summary>
    /// Indica se il progetto ha comandi configurati
    /// </summary>
    public bool HasCommands => Commands.Any() || PreCommands.Any() || PostCommands.Any();
    
    /// <summary>
    /// Ottiene il numero totale di comandi da eseguire
    /// </summary>
    public int TotalCommandsCount => PreCommands.Count + Commands.Count + PostCommands.Count;
    
    /// <summary>
    /// Verifica se il progetto ha un tag specifico
    /// </summary>
    public bool HasTag(string tag)
    {
        return Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Aggiunge un tag se non esiste già
    /// </summary>
    public void AddTag(string tag)
    {
        if (!HasTag(tag))
        {
            Tags.Add(tag);
        }
    }
    
    /// <summary>
    /// Rimuove un tag se esiste
    /// </summary>
    public bool RemoveTag(string tag)
    {
        return Tags.RemoveAll(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)) > 0;
    }
    
    /// <summary>
    /// Valida la configurazione del progetto
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Project name is required");
        }
        
        if (string.IsNullOrWhiteSpace(Path))
        {
            errors.Add("Project path is required");
        }
        
        if (TimeoutMinutes <= 0)
        {
            errors.Add("Timeout must be greater than 0");
        }
        
        if (RetryCount < 0)
        {
            errors.Add("Retry count cannot be negative");
        }
        
        if (RetryDelaySeconds < 0)
        {
            errors.Add("Retry delay cannot be negative");
        }
        
        // Valida comandi non vuoti
        var allCommands = PreCommands.Concat(Commands).Concat(PostCommands);
        foreach (var command in allCommands)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                errors.Add("Commands cannot be empty");
                break;
            }
        }
        
        return errors;
    }
    
    /// <summary>
    /// Clona la configurazione del progetto
    /// </summary>
    public ProjectConfig Clone()
    {
        return new ProjectConfig
        {
            Name = Name,
            Path = Path,
            Type = Type,
            Commands = new List<string>(Commands),
            Environment = new Dictionary<string, string>(Environment),
            TimeoutMinutes = TimeoutMinutes,
            WorkingDirectory = WorkingDirectory,
            Enabled = Enabled,
            Tags = new List<string>(Tags),
            PreCommands = new List<string>(PreCommands),
            PostCommands = new List<string>(PostCommands),
            Description = Description,
            Priority = Priority,
            RetryCount = RetryCount,
            RetryDelaySeconds = RetryDelaySeconds,
            IgnoreExitCodes = new List<int>(IgnoreExitCodes),
            ExpectedOutputPatterns = new List<string>(ExpectedOutputPatterns),
            ForbiddenOutputPatterns = new List<string>(ForbiddenOutputPatterns)
        };
    }
}

/// <summary>
/// Tipi di progetto supportati
/// </summary>
public enum ProjectType
{
    Auto,           // Rileva automaticamente
    WebApp,         // Applicazione web (React, Vue, Angular, etc.)
    MobileApp,      // App mobile (React Native, Flutter, etc.)
    PythonScript,   // Script Python
    JavaScriptApp,  // App Node.js/JavaScript
    DotNetApp,      // Applicazione .NET
    JavaApp,        // Applicazione Java
    GoApp,          // Applicazione Go
    RustApp,        // Applicazione Rust
    PhpApp,         // Applicazione PHP
    RubyApp,        // Applicazione Ruby
    DockerApp,      // Applicazione containerizzata
    Custom          // Comandi personalizzati
}

/// <summary>
/// Formati di output supportati
/// </summary>
public enum OutputFormat
{
    Console,        // Output colorato su console
    Json,           // JSON per integrazione CI/CD
    Xml,            // XML in formato JUnit
    Html,           // Report HTML
    Markdown,       // Report Markdown
    Csv             // Report CSV
}