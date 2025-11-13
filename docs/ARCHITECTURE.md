# TestRunner - Architettura Tecnica

Questo documento descrive l'architettura tecnica dettagliata del progetto TestRunner.

## 📋 Indice

- [Overview](#overview)
- [Architettura ad alto livello](#architettura-ad-alto-livello)
- [Componenti](#componenti)
- [Flusso di esecuzione](#flusso-di-esecuzione)
- [Modelli dati](#modelli-dati)
- [Gestione errori](#gestione-errori)
- [Sicurezza](#sicurezza)
- [Performance](#performance)
- [Estensibilità](#estensibilità)

## Overview

TestRunner è un'applicazione CLI .NET 9.0 che permette di eseguire test su progetti multi-linguaggio in modo configurabile e automatizzato.

### Tecnologie principali

- **.NET 9.0** - Runtime e SDK
- **System.CommandLine** - Parsing argomenti CLI
- **Microsoft.Extensions.Logging** - Logging strutturato
- **Microsoft.Extensions.DependencyInjection** - IoC container
- **System.Text.Json** - Serializzazione JSON
- **System.Diagnostics.Process** - Esecuzione comandi

### Principi di design

- **Separation of Concerns** - Ogni classe ha una singola responsabilità
- **Dependency Injection** - Loose coupling tra componenti
- **Async/Await** - Operazioni I/O non bloccanti
- **Fail-fast** - Validazione early per errori rapidi
- **Configuration over Code** - Comportamento configurabile esternamente

## Architettura ad alto livello

```
┌─────────────────────────────────────────────────────────┐
│                         CLI Layer                        │
│                     (Program.cs)                         │
│  ┌─────────┐  ┌─────────┐  ┌──────────┐  ┌──────────┐ │
│  │  init   │  │ detect  │  │ validate │  │   run    │ │
│  └────┬────┘  └────┬────┘  └─────┬────┘  └─────┬────┘ │
└───────┼───────────┼──────────────┼─────────────┼───────┘
        │           │              │             │
        └───────────┴──────────────┴─────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
        ▼                  ▼                  ▼
┌──────────────┐  ┌─────────────────┐  ┌──────────────┐
│ ConfigService│  │ ProjectDetector │  │ TestExecutor │
└──────────────┘  └─────────────────┘  └──────────────┘
        │                  │                  │
        └──────────────────┴──────────────────┘
                           │
                           ▼
                  ┌─────────────────┐
                  │ ReportGenerator │
                  └─────────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
        ▼                  ▼                  ▼
   ┌────────┐         ┌──────┐          ┌──────┐
   │Console │         │ JSON │          │ HTML │
   └────────┘         └──────┘          └──────┘
```

### Layer architecture

1. **CLI Layer** - Interfaccia utente command-line
2. **Service Layer** - Business logic
3. **Model Layer** - Data structures
4. **Infrastructure Layer** - I/O, processi, file system

## Componenti

### 1. Program.cs (CLI Layer)

**Responsabilità:**
- Entry point dell'applicazione
- Configurazione Dependency Injection
- Definizione comandi CLI
- Orchestrazione flusso principale
- Error handling globale

**Architettura:**

```csharp
class Program
{
    static async Task<int> Main(string[] args)
    {
        // 1. Setup DI
        var services = ConfigureServices();

        // 2. Build command tree
        var rootCommand = new RootCommand();
        rootCommand.AddCommand(CreateInitCommand());
        rootCommand.AddCommand(CreateDetectCommand());
        rootCommand.AddCommand(CreateValidateCommand());
        rootCommand.AddCommand(CreateRunCommand());

        // 3. Execute
        return await rootCommand.InvokeAsync(args);
    }

    static void ConfigureServices(ServiceCollection services)
    {
        services.AddLogging(/* ... */);
        services.AddSingleton<ConfigService>();
        services.AddSingleton<ProjectDetector>();
        services.AddSingleton<TestExecutor>();
        services.AddSingleton<ReportGenerator>();
    }
}
```

**Pattern utilizzati:**
- Command Pattern (System.CommandLine)
- Dependency Injection
- Factory Pattern (per creazione comandi)

### 2. ConfigService (Service Layer)

**Responsabilità:**
- Caricamento configurazione da JSON
- Validazione configurazione
- Serializzazione/deserializzazione
- Merge di configurazioni multiple
- Template generazione

**API pubblica:**

```csharp
public class ConfigService
{
    Task<TestRunnerConfig> LoadConfigAsync(string path);
    Task SaveConfigAsync(TestRunnerConfig config, string path);
    TestRunnerConfig CreateDefaultConfig();
    Task<TestRunnerConfig> CreateAutoConfigAsync(string path, ProjectDetector detector);
    void ValidateConfiguration(TestRunnerConfig config);
    TestRunnerConfig MergeConfigurations(TestRunnerConfig primary, TestRunnerConfig secondary);
}
```

**Validazione:**

```csharp
public void ValidateConfiguration(TestRunnerConfig config)
{
    // 1. Validazione base
    if (!config.Projects.Any())
        throw new InvalidOperationException("At least one project required");

    // 2. Validazione progetti
    var projectNames = new HashSet<string>();
    foreach (var project in config.Projects)
    {
        // Nome univoco
        if (!projectNames.Add(project.Name))
            throw new InvalidOperationException($"Duplicate: {project.Name}");

        // Path richiesto
        if (string.IsNullOrWhiteSpace(project.Path))
            throw new InvalidOperationException("Path required");

        // Comandi non vuoti
        // ...
    }
}
```

### 3. ProjectDetector (Service Layer)

**Responsabilità:**
- Auto-detection tipo progetto
- Scansione ricorsiva directory
- Generazione comandi appropriati
- Identificazione file marker

**Algoritmo di detection:**

```
DetectProjectsAsync(rootPath, maxDepth)
│
├─> Per ogni directory (recursively fino a maxDepth)
│   │
│   ├─> AnalyzeDirectory(path)
│   │   │
│   │   ├─> Raccogli file names
│   │   ├─> DetectProjectType(fileNames)
│   │   │   │
│   │   │   ├─> package.json → WebApp/MobileApp
│   │   │   ├─> *.csproj → DotNetApp
│   │   │   ├─> pom.xml → JavaApp
│   │   │   ├─> go.mod → GoApp
│   │   │   ├─> Cargo.toml → RustApp
│   │   │   └─> ...
│   │   │
│   │   └─> GenerateCommands(projectType)
│   │       │
│   │       ├─> WebApp → ["npm test", "npm run build"]
│   │       ├─> PythonScript → ["pytest", "flake8"]
│   │       └─> ...
│   │
│   └─> Se progetto trovato, non scansionare sottodirectory
│
└─> Return List<ProjectConfig>
```

**File marker per detection:**

| Tipo | Marker Files |
|------|-------------|
| JavaScript/TypeScript | `package.json` |
| Python | `requirements.txt`, `pyproject.toml`, `setup.py`, `*.py` |
| .NET | `*.csproj`, `*.fsproj`, `*.sln` |
| Java | `pom.xml`, `build.gradle`, `*.java` |
| Go | `go.mod`, `*.go` |
| Rust | `Cargo.toml`, `*.rs` |
| PHP | `composer.json`, `*.php` |
| Ruby | `Gemfile`, `*.rb` |
| Docker | `Dockerfile`, `docker-compose.yml` |

**Directory skipping:**

```csharp
private bool ShouldSkipDirectory(string directoryName)
{
    var skipDirs = new[]
    {
        "node_modules", ".git", ".svn",
        "bin", "obj", ".vs", ".vscode",
        "dist", "build", "__pycache__",
        "coverage", "tmp", "temp"
    };

    return skipDirs.Contains(directoryName) ||
           directoryName.StartsWith(".");
}
```

### 4. TestExecutor (Service Layer)

**Responsabilità:**
- Esecuzione test per progetti
- Gestione processi
- Esecuzione parallela/sequenziale
- Timeout handling
- Cattura output/error
- Retry logic

**Architettura esecuzione:**

```
ExecuteAllProjectsAsync(config, filter, tags)
│
├─> FilterProjects(projects, filter, tags)
│
├─> If parallel:
│   └─> ExecuteProjectsInParallelAsync()
│       └─> SemaphoreSlim(maxParallel)
│           └─> Task.WhenAll(ExecuteProjectAsync per project)
│
└─> If sequential:
    └─> ExecuteProjectsSequentiallyAsync()
        └─> foreach project: ExecuteProjectAsync()
            │
            ├─> Execute pre_commands
            │   └─> If fails → status = Failed, return
            │
            ├─> Execute commands
            │   └─> For each command: ExecuteCommandAsync()
            │       │
            │       ├─> Validate working directory
            │       ├─> Parse command
            │       ├─> Create Process
            │       ├─> Set environment variables
            │       ├─> Start with output redirection
            │       ├─> Wait with timeout
            │       └─> Return CommandResult
            │
            └─> Execute post_commands (sempre)
                └─> Even if main commands failed
```

**Process execution:**

```csharp
private async Task<CommandResult> ExecuteCommandAsync(
    string command,
    ProjectConfig project,
    CancellationToken cancellationToken)
{
    // 1. Validazione
    cancellationToken.ThrowIfCancellationRequested();
    ValidateWorkingDirectory();
    ValidateExecutable();

    // 2. Setup process
    var (executable, arguments) = ParseCommand(command);
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };

    // 3. Environment variables
    foreach (var env in project.Environment)
        process.StartInfo.Environment[env.Key] = env.Value;

    // 4. Output capture
    var outputBuilder = new StringBuilder();
    var errorBuilder = new StringBuilder();
    process.OutputDataReceived += (s, e) => outputBuilder.AppendLine(e.Data);
    process.ErrorDataReceived += (s, e) => errorBuilder.AppendLine(e.Data);

    // 5. Execute with timeout
    process.Start();
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    var timeoutTask = Task.Delay(timeout, cancellationToken);
    var processTask = process.WaitForExitAsync(cancellationToken);

    if (await Task.WhenAny(processTask, timeoutTask) == timeoutTask)
    {
        process.Kill(true);
        return TimeoutResult();
    }

    // 6. Return result
    return new CommandResult
    {
        ExitCode = process.ExitCode,
        Output = outputBuilder.ToString(),
        Error = errorBuilder.ToString()
    };
}
```

**Command parsing sicuro:**

```csharp
private (string executable, string arguments) ParseCommand(string command)
{
    // Split command
    var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var executable = parts[0];
    var arguments = string.Join(" ", parts.Skip(1));

    // Platform-specific handling
    if (OperatingSystem.IsWindows())
    {
        if (IsBuiltInWindowsCommand(executable))
            return ("cmd.exe", $"/c {command}");
    }
    else
    {
        if (IsShellCommand(command))
        {
            // SECURITY: Proper escaping
            var escapedCommand = command.Replace("'", "'\\''");
            return ("/bin/bash", $"-c '{escapedCommand}'");
        }
    }

    return (executable, arguments);
}
```

### 5. ReportGenerator (Service Layer)

**Responsabilità:**
- Generazione report in formati multipli
- Formattazione output console
- Export JSON/XML/HTML/CSV
- Calcolo statistiche
- Rendering template

**Formato output supportati:**

```csharp
public enum OutputFormat
{
    Console,    // Colored terminal output
    Json,       // Machine-readable JSON
    Xml,        // JUnit XML format
    Html,       // Interactive HTML report
    Markdown,   // Markdown document
    Csv         // CSV spreadsheet
}
```

**Generazione report:**

```csharp
public async Task SaveReportAsync(
    TestExecutionResult result,
    OutputFormat format,
    string outputPath)
{
    switch (format)
    {
        case OutputFormat.Json:
            await GenerateJsonReportAsync(result, outputPath);
            break;

        case OutputFormat.Xml:
            await GenerateJUnitReportAsync(result, outputPath);
            break;

        case OutputFormat.Html:
            await GenerateHtmlReportAsync(result, outputPath);
            break;

        // ...
    }
}
```

## Flusso di esecuzione

### Comando `init --auto`

```
User: testrunner init --auto
│
├─> Main()
├─> Parse arguments
├─> CreateInitCommand handler
│
├─> ConfigService.CreateAutoConfigAsync()
│   │
│   ├─> ProjectDetector.DetectProjectsAsync()
│   │   └─> Scan directories
│   │       └─> Return List<ProjectConfig>
│   │
│   └─> Build TestRunnerConfig
│
├─> ConfigService.SaveConfigAsync()
│   └─> Serialize to JSON
│   └─> Write to testrunner.json
│
└─> Console output: "✅ Configuration created"
```

### Comando `run`

```
User: testrunner run --parallel --tags backend
│
├─> Main()
├─> Parse arguments
├─> CreateRunCommand handler
│
├─> ConfigService.LoadConfigAsync()
│   ├─> Read testrunner.json
│   ├─> Deserialize
│   └─> ValidateConfiguration()
│
├─> TestExecutor.ExecuteAllProjectsAsync()
│   │
│   ├─> FilterProjects(tags=["backend"])
│   │   └─> Return filtered List<ProjectConfig>
│   │
│   ├─> ExecuteProjectsInParallelAsync()
│   │   │
│   │   └─> For each project (parallel):
│   │       │
│   │       ├─> ExecuteProjectAsync()
│   │       │   ├─> Execute pre_commands
│   │       │   ├─> Execute commands
│   │       │   │   └─> ExecuteCommandAsync()
│   │       │   │       ├─> Create Process
│   │       │   │       ├─> Execute with timeout
│   │       │   │       └─> Capture output
│   │       │   └─> Execute post_commands
│   │       │
│   │       └─> Return TestResult
│   │
│   └─> Return TestExecutionResult
│
├─> ReportGenerator.GenerateConsoleReport()
│   └─> Format and print to console
│
└─> Optional: SaveReportAsync()
    └─> Export to file
```

## Modelli dati

### TestRunnerConfig

```csharp
public class TestRunnerConfig
{
    // Metadata
    public string Name { get; set; }
    public string Description { get; set; }
    public string Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Projects
    public List<ProjectConfig> Projects { get; set; }

    // Execution settings
    public int GlobalTimeoutMinutes { get; set; }
    public bool ParallelExecution { get; set; }
    public int MaxParallelProjects { get; set; }
    public bool StopOnFirstFailure { get; set; }
    public bool ContinueOnError { get; set; }

    // Output
    public OutputFormat OutputFormat { get; set; }
    public string? ReportFile { get; set; }
    public LogLevel LogLevel { get; set; }

    // Environment
    public Dictionary<string, string> GlobalEnvironment { get; set; }
    public List<string> GlobalTags { get; set; }
    public string? BaseDirectory { get; set; }

    // Hooks
    public List<string> PreExecutionCommands { get; set; }
    public List<string> PostExecutionCommands { get; set; }

    // Extensions
    public NotificationConfig? Notifications { get; set; }
    public StorageConfig? Storage { get; set; }
}
```

### ProjectConfig

```csharp
public class ProjectConfig
{
    // Identification
    public string Name { get; set; }
    public string Path { get; set; }
    public ProjectType Type { get; set; }
    public string Description { get; set; }
    public List<string> Tags { get; set; }

    // Execution
    public bool Enabled { get; set; }
    public int Priority { get; set; }
    public int TimeoutMinutes { get; set; }
    public string? WorkingDirectory { get; set; }

    // Commands
    public List<string> Commands { get; set; }
    public List<string> PreCommands { get; set; }
    public List<string> PostCommands { get; set; }

    // Environment
    public Dictionary<string, string> Environment { get; set; }

    // Retry
    public int RetryCount { get; set; }
    public int RetryDelaySeconds { get; set; }

    // Validation
    public List<int> IgnoreExitCodes { get; set; }
    public List<string> ExpectedOutputPatterns { get; set; }
    public List<string> ForbiddenOutputPatterns { get; set; }
}
```

### TestResult

```csharp
public class TestResult
{
    public string ProjectName { get; set; }
    public string ProjectPath { get; set; }
    public ProjectType ProjectType { get; set; }
    public TestStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; }
    public List<CommandResult> CommandResults { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Tags { get; set; }
    public bool IsSuccess { get; }
}

public class CommandResult
{
    public string Command { get; set; }
    public int ExitCode { get; set; }
    public string Output { get; set; }
    public string Error { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; }
    public bool IsSuccess { get; }
    public string? WorkingDirectory { get; set; }
}
```

## Gestione errori

### Strategy multi-livello

```
Level 1: Validazione Pre-Execution
├─> Config validation
├─> File existence
├─> Path validation
└─> Syntax check

Level 2: Execution Errors
├─> Process creation failed
├─> Working directory not found
├─> Command timeout
└─> Cancellation requested

Level 3: Post-Execution Validation
├─> Exit code check
├─> Output pattern matching
└─> Result validation

Level 4: Global Error Handler
└─> Unhandled exceptions
```

### Error handling pattern

```csharp
try
{
    // 1. Validazione pre-execution
    ValidateConfiguration(config);

    // 2. Esecuzione
    var result = await ExecuteAsync();

    // 3. Validazione post-execution
    ValidateResults(result);

    return result;
}
catch (ValidationException ex)
{
    _logger.LogError(ex, "Validation failed");
    return ErrorResult(ex.Message);
}
catch (ExecutionException ex)
{
    _logger.LogError(ex, "Execution failed");
    return ErrorResult(ex.Message);
}
catch (Exception ex)
{
    _logger.LogCritical(ex, "Unexpected error");
    throw;  // Re-throw for global handler
}
finally
{
    // Cleanup sempre eseguito
    await CleanupAsync();
}
```

## Sicurezza

### Threat model

**Threats considerate:**
1. Command injection
2. Path traversal
3. Environment variable injection
4. Resource exhaustion (DoS)
5. Information disclosure

### Mitigazioni

#### 1. Command injection

```csharp
// VULNERABLE (NON usato)
var command = $"bash -c \"{userInput}\"";  // ❌

// SECURE (implementazione attuale)
var escapedCommand = command.Replace("'", "'\\''");
var safeCommand = $"/bin/bash -c '{escapedCommand}'";  // ✅
```

#### 2. Path validation

```csharp
// Validate working directory
if (!Directory.Exists(workingDirectory))
{
    _logger.LogError("Invalid path: {Path}", workingDirectory);
    return ErrorResult();
}

// Prevent path traversal
var fullPath = Path.GetFullPath(workingDirectory);
if (!fullPath.StartsWith(allowedBasePath))
{
    _logger.LogError("Path traversal attempt");
    return ErrorResult();
}
```

#### 3. Resource limits

```csharp
// Timeout per command
var timeout = TimeSpan.FromMinutes(project.TimeoutMinutes);
var timeoutTask = Task.Delay(timeout, cancellationToken);

if (await Task.WhenAny(processTask, timeoutTask) == timeoutTask)
{
    process.Kill(true);  // Prevent resource exhaustion
    return TimeoutResult();
}

// Max parallel projects
var semaphore = new SemaphoreSlim(config.MaxParallelProjects);
```

#### 4. Information disclosure

```csharp
// Non loggare informazioni sensibili
_logger.LogInformation("Executing: {Command}",
    SanitizeCommand(command));  // Remove secrets

// Non includere stack trace in output utente
catch (Exception ex)
{
    _logger.LogError(ex, "Error occurred");  // Log dettagliato
    Console.WriteLine($"Error: {ex.Message}");  // Output sanitizzato
}
```

## Performance

### Ottimizzazioni implementate

#### 1. Parallel execution

```csharp
// Esecuzione parallela con semaforo
var semaphore = new SemaphoreSlim(maxParallel);
var tasks = projects.Select(async p =>
{
    await semaphore.WaitAsync();
    try
    {
        return await ExecuteProjectAsync(p);
    }
    finally
    {
        semaphore.Release();
    }
});

var results = await Task.WhenAll(tasks);
```

#### 2. Async I/O

```csharp
// Tutte le operazioni I/O sono async
await File.ReadAllTextAsync(path);
await File.WriteAllTextAsync(path, content);
await process.WaitForExitAsync(cancellationToken);
```

#### 3. Stream-based output

```csharp
// Output streaming invece di buffering
process.OutputDataReceived += (s, e) =>
{
    if (e.Data != null)
        outputBuilder.AppendLine(e.Data);
};
process.BeginOutputReadLine();
```

#### 4. Early exit

```csharp
// Stop-on-first-failure
if (config.StopOnFirstFailure && !result.IsSuccess)
{
    _logger.LogWarning("Stopping due to failure");
    break;  // Non esegue progetti rimanenti
}
```

### Benchmark tipici

| Scenario | Sequenziale | Parallelo (4 core) | Speedup |
|----------|-------------|-------------------|---------|
| 4 progetti x 2min | ~8 minuti | ~2 minuti | 4x |
| 8 progetti x 1min | ~8 minuti | ~2 minuti | 4x |
| 10 progetti x 5min | ~50 minuti | ~12.5 minuti | 4x |

## Estensibilità

### Punti di estensione

#### 1. Nuovi tipi di progetto

```csharp
// In ProjectDetector.cs
public enum ProjectType
{
    // ... existing types
    CustomType  // Nuovo tipo
}

private ProjectType DetectProjectType(HashSet<string> fileNames)
{
    // ... existing logic

    if (fileNames.Contains("custom-marker.file"))
        return ProjectType.CustomType;

    return ProjectType.Auto;
}

private List<string> GenerateCustomTypeCommands(...)
{
    return new List<string> { "custom-command" };
}
```

#### 2. Nuovi formati report

```csharp
// In ReportGenerator.cs
public enum OutputFormat
{
    // ... existing formats
    Custom  // Nuovo formato
}

public async Task<string> GenerateCustomReportAsync(
    TestExecutionResult result,
    string? outputPath = null)
{
    // Implementazione custom
}
```

#### 3. Notifiche

```csharp
public interface INotificationService
{
    Task NotifyAsync(TestExecutionResult result);
}

public class SlackNotificationService : INotificationService
{
    public async Task NotifyAsync(TestExecutionResult result)
    {
        // Send to Slack webhook
    }
}

// Register in DI
services.AddSingleton<INotificationService, SlackNotificationService>();
```

#### 4. Storage personalizzato

```csharp
public interface IResultStorage
{
    Task SaveAsync(TestExecutionResult result);
    Task<TestExecutionResult> LoadAsync(string id);
}

public class DatabaseStorage : IResultStorage
{
    // Implementazione DB
}
```

### Plugin system (futuro)

```csharp
public interface ITestRunnerPlugin
{
    string Name { get; }
    Version Version { get; }

    Task OnInitializeAsync(PluginContext context);
    Task OnBeforeExecutionAsync(ExecutionContext context);
    Task OnAfterExecutionAsync(ExecutionResult result);
}

// Loading plugins
var plugins = LoadPlugins("./plugins");
foreach (var plugin in plugins)
{
    await plugin.OnInitializeAsync(context);
}
```

---

Questo documento è vivo e verrà aggiornato con l'evoluzione del progetto.
