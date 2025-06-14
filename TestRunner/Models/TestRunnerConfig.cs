using System.Text.Json.Serialization;

namespace TestRunner.Models;

/// <summary>
/// Configurazione completa del test runner
/// </summary>
public class TestRunnerConfig
{
    [JsonPropertyName("projects")]
    public List<ProjectConfig> Projects { get; set; } = new();
    
    [JsonPropertyName("global_timeout_minutes")]
    public int GlobalTimeoutMinutes { get; set; } = 60;
    
    [JsonPropertyName("parallel_execution")]
    public bool ParallelExecution { get; set; } = false;
    
    [JsonPropertyName("max_parallel_projects")]
    public int MaxParallelProjects { get; set; } = Environment.ProcessorCount;
    
    [JsonPropertyName("stop_on_first_failure")]
    public bool StopOnFirstFailure { get; set; } = false;
    
    [JsonPropertyName("output_format")]
    public OutputFormat OutputFormat { get; set; } = OutputFormat.Console;
    
    [JsonPropertyName("report_file")]
    public string? ReportFile { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = "TestRunner Suite";
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
    
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Configurazione per notifiche (future estensioni)
    /// </summary>
    [JsonPropertyName("notifications")]
    public NotificationConfig? Notifications { get; set; }
    
    /// <summary>
    /// Configurazione per storage dei risultati (future estensioni)
    /// </summary>
    [JsonPropertyName("storage")]
    public StorageConfig? Storage { get; set; }
    
    /// <summary>
    /// Variabili d'ambiente globali applicate a tutti i progetti
    /// </summary>
    [JsonPropertyName("global_environment")]
    public Dictionary<string, string> GlobalEnvironment { get; set; } = new();
    
    /// <summary>
    /// Tag da applicare a tutti i progetti
    /// </summary>
    [JsonPropertyName("global_tags")]
    public List<string> GlobalTags { get; set; } = new();
    
    /// <summary>
    /// Comandi da eseguire prima di tutti i progetti
    /// </summary>
    [JsonPropertyName("pre_execution_commands")]
    public List<string> PreExecutionCommands { get; set; } = new();
    
    /// <summary>
    /// Comandi da eseguire dopo tutti i progetti
    /// </summary>
    [JsonPropertyName("post_execution_commands")]
    public List<string> PostExecutionCommands { get; set; } = new();
    
    /// <summary>
    /// Directory di lavoro base per tutti i progetti
    /// </summary>
    [JsonPropertyName("base_directory")]
    public string? BaseDirectory { get; set; }
    
    /// <summary>
    /// Indica se continuare l'esecuzione anche in caso di errori
    /// </summary>
    [JsonPropertyName("continue_on_error")]
    public bool ContinueOnError { get; set; } = true;
    
    /// <summary>
    /// Livello di log di default
    /// </summary>
    [JsonPropertyName("log_level")]
    public Models.LogLevel LogLevel { get; set; } = Models.LogLevel.Information;
    
    /// <summary>
    /// Ottiene il numero totale di progetti abilitati
    /// </summary>
    public int EnabledProjectsCount => Projects.Count(p => p.Enabled);
    
    /// <summary>
    /// Ottiene tutti i tag unici da tutti i progetti
    /// </summary>
    public List<string> GetAllTags()
    {
        var allTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // Aggiungi tag globali
        foreach (var tag in GlobalTags)
        {
            allTags.Add(tag);
        }
        
        // Aggiungi tag dei progetti
        foreach (var project in Projects)
        {
            foreach (var tag in project.Tags)
            {
                allTags.Add(tag);
            }
        }
        
        return allTags.OrderBy(t => t).ToList();
    }
    
    /// <summary>
    /// Ottiene tutti i tipi di progetto presenti
    /// </summary>
    public List<ProjectType> GetProjectTypes()
    {
        return Projects.Select(p => p.Type).Distinct().OrderBy(t => t.ToString()).ToList();
    }
    
    /// <summary>
    /// Valida la configurazione base
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();
        
        if (!Projects.Any())
        {
            errors.Add("At least one project must be configured");
        }
        
        if (GlobalTimeoutMinutes <= 0)
        {
            errors.Add("Global timeout must be greater than 0");
        }
        
        if (MaxParallelProjects <= 0)
        {
            errors.Add("Max parallel projects must be greater than 0");
        }
        
        // Valida nomi progetti unici
        var projectNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in Projects)
        {
            if (!projectNames.Add(project.Name))
            {
                errors.Add($"Duplicate project name: {project.Name}");
            }
        }
        
        return errors;
    }
    
    /// <summary>
    /// Aggiorna il timestamp di modifica
    /// </summary>
    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Configurazione per notifiche (estensione futura)
/// </summary>
public class NotificationConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;
    
    [JsonPropertyName("on_success")]
    public bool OnSuccess { get; set; } = false;
    
    [JsonPropertyName("on_failure")]
    public bool OnFailure { get; set; } = true;
    
    [JsonPropertyName("on_start")]
    public bool OnStart { get; set; } = false;
    
    [JsonPropertyName("slack_webhook")]
    public string? SlackWebhook { get; set; }
    
    [JsonPropertyName("email_recipients")]
    public List<string> EmailRecipients { get; set; } = new();
    
    [JsonPropertyName("custom_webhooks")]
    public List<string> CustomWebhooks { get; set; } = new();
}

/// <summary>
/// Configurazione per storage dei risultati (estensione futura)
/// </summary>
public class StorageConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;
    
    [JsonPropertyName("type")]
    public StorageType Type { get; set; } = StorageType.FileSystem;
    
    [JsonPropertyName("path")]
    public string Path { get; set; } = "./testrunner-results";
    
    [JsonPropertyName("connection_string")]
    public string? ConnectionString { get; set; }
    
    [JsonPropertyName("retain_results_days")]
    public int RetainResultsDays { get; set; } = 30;
    
    [JsonPropertyName("auto_cleanup")]
    public bool AutoCleanup { get; set; } = true;
}

/// <summary>
/// Tipi di storage supportati
/// </summary>
public enum StorageType
{
    FileSystem,
    SQLite,
    PostgreSQL,
    MySQL,
    MongoDB
}

/// <summary>
/// Livelli di log
/// </summary>
public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}