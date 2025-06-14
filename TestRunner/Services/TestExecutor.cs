using System.Diagnostics;
using System.Text;
using TestRunner.Models;
using Microsoft.Extensions.Logging;
namespace TestRunner.Services;

/// <summary>
/// Servizio per l'esecuzione dei test
/// </summary>
public class TestExecutor
{
    private readonly ILogger<TestExecutor> _logger;

    public TestExecutor(ILogger<TestExecutor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Esegue i test per un singolo progetto
    /// </summary>
    public async Task<TestResult> ExecuteProjectAsync(ProjectConfig project, CancellationToken cancellationToken = default)
    {
        var result = new TestResult
        {
            ProjectName = project.Name,
            ProjectPath = project.Path,
            ProjectType = project.Type,
            StartTime = DateTime.Now,
            Status = TestStatus.Running,
            Tags = project.Tags
        };

        _logger.LogInformation("Starting tests for project: {ProjectName}", project.Name);

        try
        {
            if (!project.Enabled)
            {
                result.Status = TestStatus.Skipped;
                result.EndTime = DateTime.Now;
                _logger.LogInformation("Project {ProjectName} is disabled, skipping", project.Name);
                return result;
            }

            if (!Directory.Exists(project.Path))
            {
                result.Status = TestStatus.Error;
                result.ErrorMessage = $"Project directory not found: {project.Path}";
                result.EndTime = DateTime.Now;
                _logger.LogError("Project directory not found: {ProjectPath}", project.Path);
                return result;
            }

            // Esegui pre-comandi
            if (project.PreCommands.Any())
            {
                _logger.LogInformation("Executing {Count} pre-commands for {ProjectName}", project.PreCommands.Count, project.Name);
                foreach (var preCommand in project.PreCommands)
                {
                    var preResult = await ExecuteCommandAsync(preCommand, project, cancellationToken);
                    result.CommandResults.Add(preResult);
                    
                    if (!preResult.IsSuccess)
                    {
                        result.Status = TestStatus.Failed;
                        result.ErrorMessage = $"Pre-command failed: {preCommand}";
                        result.EndTime = DateTime.Now;
                        return result;
                    }
                }
            }

            // Esegui comandi principali
            if (!project.Commands.Any())
            {
                result.Status = TestStatus.Skipped;
                result.ErrorMessage = "No commands configured";
                result.EndTime = DateTime.Now;
                _logger.LogWarning("No commands configured for project {ProjectName}", project.Name);
                return result;
            }

            _logger.LogInformation("Executing {Count} main commands for {ProjectName}", project.Commands.Count, project.Name);
            bool allCommandsSuccessful = true;

            foreach (var command in project.Commands)
            {
                var commandResult = await ExecuteCommandAsync(command, project, cancellationToken);
                result.CommandResults.Add(commandResult);

                if (!commandResult.IsSuccess)
                {
                    allCommandsSuccessful = false;
                    _logger.LogWarning("Command failed for {ProjectName}: {Command} (Exit code: {ExitCode})", 
                        project.Name, command, commandResult.ExitCode);
                }
            }

            // Esegui post-comandi (sempre, anche se i test principali falliscono)
            if (project.PostCommands.Any())
            {
                _logger.LogInformation("Executing {Count} post-commands for {ProjectName}", project.PostCommands.Count, project.Name);
                foreach (var postCommand in project.PostCommands)
                {
                    var postResult = await ExecuteCommandAsync(postCommand, project, cancellationToken);
                    result.CommandResults.Add(postResult);
                }
            }

            result.Status = allCommandsSuccessful ? TestStatus.Passed : TestStatus.Failed;
            result.EndTime = DateTime.Now;

            _logger.LogInformation("Project {ProjectName} completed with status: {Status} in {Duration}", 
                project.Name, result.Status, result.Duration);

            return result;
        }
        catch (OperationCanceledException)
        {
            result.Status = TestStatus.Error;
            result.ErrorMessage = "Operation was cancelled";
            result.EndTime = DateTime.Now;
            _logger.LogWarning("Tests for project {ProjectName} were cancelled", project.Name);
            return result;
        }
        catch (Exception ex)
        {
            result.Status = TestStatus.Error;
            result.ErrorMessage = ex.Message;
            result.EndTime = DateTime.Now;
            _logger.LogError(ex, "Error executing tests for project {ProjectName}", project.Name);
            return result;
        }
    }

    /// <summary>
    /// Esegue tutti i progetti nella configurazione
    /// </summary>
    public async Task<TestExecutionResult> ExecuteAllProjectsAsync(
        TestRunnerConfig config, 
        string[]? projectFilter = null,
        string[]? tagFilter = null,
        CancellationToken cancellationToken = default)
    {
        var executionResult = new TestExecutionResult
        {
            StartTime = DateTime.Now
        };

        _logger.LogInformation("Starting test execution for {ProjectCount} projects", config.Projects.Count);

        try
        {
            // Filtra i progetti
            var projectsToRun = FilterProjects(config.Projects, projectFilter, tagFilter);

            if (!projectsToRun.Any())
            {
                _logger.LogWarning("No projects matched the filter criteria");
                executionResult.EndTime = DateTime.Now;
                executionResult.CalculateSummary();
                return executionResult;
            }

            _logger.LogInformation("Executing {FilteredCount} projects after filtering", projectsToRun.Count);

            if (config.ParallelExecution)
            {
                await ExecuteProjectsInParallelAsync(projectsToRun, config, executionResult, cancellationToken);
            }
            else
            {
                await ExecuteProjectsSequentiallyAsync(projectsToRun, config, executionResult, cancellationToken);
            }

            executionResult.EndTime = DateTime.Now;
            executionResult.CalculateSummary();

            _logger.LogInformation("Test execution completed. Success: {IsSuccess}, Duration: {Duration}", 
                executionResult.IsSuccess, executionResult.TotalDuration);

            return executionResult;
        }
        catch (Exception ex)
        {
            executionResult.EndTime = DateTime.Now;
            executionResult.CalculateSummary();
            _logger.LogError(ex, "Error during test execution");
            throw;
        }
    }

    private async Task ExecuteProjectsSequentiallyAsync(
        List<ProjectConfig> projects, 
        TestRunnerConfig config, 
        TestExecutionResult executionResult,
        CancellationToken cancellationToken)
    {
        foreach (var project in projects)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var result = await ExecuteProjectAsync(project, cancellationToken);
            executionResult.ProjectResults.Add(result);

            if (config.StopOnFirstFailure && !result.IsSuccess)
            {
                _logger.LogWarning("Stopping execution due to failure in project {ProjectName}", project.Name);
                break;
            }
        }
    }

    private async Task ExecuteProjectsInParallelAsync(
        List<ProjectConfig> projects, 
        TestRunnerConfig config, 
        TestExecutionResult executionResult,
        CancellationToken cancellationToken)
    {
        var semaphore = new SemaphoreSlim(config.MaxParallelProjects, config.MaxParallelProjects);
        var tasks = new List<Task<TestResult>>();

        foreach (var project in projects)
        {
            tasks.Add(ExecuteProjectWithSemaphoreAsync(project, semaphore, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);
        executionResult.ProjectResults.AddRange(results);
    }

    private async Task<TestResult> ExecuteProjectWithSemaphoreAsync(
        ProjectConfig project, 
        SemaphoreSlim semaphore, 
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            return await ExecuteProjectAsync(project, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<CommandResult> ExecuteCommandAsync(
        string command, 
        ProjectConfig project, 
        CancellationToken cancellationToken)
    {
        var result = new CommandResult
        {
            Command = command,
            StartTime = DateTime.Now,
            WorkingDirectory = project.WorkingDirectory ?? project.Path
        };

        try
        {
            using var process = new Process();
            
            var (executable, arguments) = ParseCommand(command);
            
            process.StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                WorkingDirectory = result.WorkingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Aggiungi variabili d'ambiente
            foreach (var env in project.Environment)
            {
                process.StartInfo.Environment[env.Key] = env.Value;
            }

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            _logger.LogDebug("Executing command: {Command} in {WorkingDirectory}", command, result.WorkingDirectory);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Aspetta il completamento con timeout
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(project.TimeoutMinutes), cancellationToken);
            var processTask = process.WaitForExitAsync(cancellationToken);

            var completedTask = await Task.WhenAny(processTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                // Timeout raggiunto
                try
                {
                    process.Kill(true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to kill process after timeout");
                }

                result.ExitCode = -1;
                result.Error = $"Command timed out after {project.TimeoutMinutes} minutes";
                result.EndTime = DateTime.Now;
                return result;
            }

            await processTask;

            result.ExitCode = process.ExitCode;
            result.Output = outputBuilder.ToString().Trim();
            result.Error = errorBuilder.ToString().Trim();
            result.EndTime = DateTime.Now;

            _logger.LogDebug("Command completed with exit code {ExitCode}: {Command}", result.ExitCode, command);

            return result;
        }
        catch (Exception ex)
        {
            result.ExitCode = -1;
            result.Error = $"Failed to execute command: {ex.Message}";
            result.EndTime = DateTime.Now;
            _logger.LogError(ex, "Error executing command: {Command}", command);
            return result;
        }
    }

    private (string executable, string arguments) ParseCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return ("", "");

        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return ("", "");

        var executable = parts[0];
        var arguments = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : "";

        // Gestisci comandi speciali per diverse piattaforme
        if (OperatingSystem.IsWindows())
        {
            // Su Windows, alcuni comandi potrebbero necessitare cmd.exe
            if (IsBuiltInWindowsCommand(executable))
            {
                return ("cmd.exe", $"/c {command}");
            }
        }
        else
        {
            // Su Linux/Mac, alcuni comandi potrebbero necessitare bash
            if (IsShellCommand(executable))
            {
                return ("/bin/bash", $"-c \"{command}\"");
            }
        }

        return (executable, arguments);
    }

    private bool IsBuiltInWindowsCommand(string command)
    {
        var builtInCommands = new[] { "dir", "copy", "move", "del", "echo", "type", "cd", "md", "rd" };
        return builtInCommands.Contains(command.ToLowerInvariant());
    }

    private bool IsShellCommand(string command)
    {
        // Comandi che potrebbero richiedere shell su Unix
        return command.Contains("&&") || command.Contains("||") || command.Contains("|") || command.Contains(">");
    }

    private List<ProjectConfig> FilterProjects(
        List<ProjectConfig> projects, 
        string[]? projectFilter, 
        string[]? tagFilter)
    {
        var filtered = projects.AsEnumerable();

        // Filtra per nome progetto
        if (projectFilter?.Any() == true)
        {
            filtered = filtered.Where(p => 
                projectFilter.Contains(p.Name, StringComparer.OrdinalIgnoreCase));
        }

        // Filtra per tag
        if (tagFilter?.Any() == true)
        {
            filtered = filtered.Where(p => 
                p.Tags.Any(tag => tagFilter.Contains(tag, StringComparer.OrdinalIgnoreCase)));
        }

        return filtered.ToList();
    }
}