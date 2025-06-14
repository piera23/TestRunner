using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace TestRunner.Models;

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
    Custom          // Comandi personalizzati
}