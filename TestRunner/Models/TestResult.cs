namespace TestRunner.Models;

/// <summary>
/// Risultato dell'esecuzione di un singolo progetto
/// </summary>
public class TestResult
{
    public string ProjectName { get; set; } = "";
    public string ProjectPath { get; set; } = "";
    public ProjectType ProjectType { get; set; }
    public TestStatus Status { get; set; } = TestStatus.NotRun;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public List<CommandResult> CommandResults { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// Indica se il test è passato con successo
    /// </summary>
    public bool IsSuccess => Status == TestStatus.Passed;
    
    /// <summary>
    /// Ottiene tutti gli output dei comandi
    /// </summary>
    public string GetAllOutput()
    {
        return string.Join("\n", CommandResults.Select(r => r.Output));
    }
    
    /// <summary>
    /// Ottiene tutti gli errori dei comandi
    /// </summary>
    public string GetAllErrors()
    {
        return string.Join("\n", CommandResults.Where(r => !string.IsNullOrEmpty(r.Error)).Select(r => r.Error));
    }
}

/// <summary>
/// Risultato dell'esecuzione di un singolo comando
/// </summary>
public class CommandResult
{
    public string Command { get; set; } = "";
    public int ExitCode { get; set; }
    public string Output { get; set; } = "";
    public string Error { get; set; } = "";
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public bool IsSuccess => ExitCode == 0;
    public string? WorkingDirectory { get; set; }
}

/// <summary>
/// Risultato complessivo dell'esecuzione di tutti i test
/// </summary>
public class TestExecutionResult
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan TotalDuration => EndTime - StartTime;
    public List<TestResult> ProjectResults { get; set; } = new();
    public TestExecutionSummary Summary { get; set; } = new();
    
    /// <summary>
    /// Indica se tutti i test sono passati
    /// </summary>
    public bool IsSuccess => ProjectResults.All(r => r.IsSuccess);
    
    /// <summary>
    /// Calcola il riepilogo dei risultati
    /// </summary>
    public void CalculateSummary()
    {
        Summary = new TestExecutionSummary
        {
            TotalProjects = ProjectResults.Count,
            PassedProjects = ProjectResults.Count(r => r.Status == TestStatus.Passed),
            FailedProjects = ProjectResults.Count(r => r.Status == TestStatus.Failed),
            SkippedProjects = ProjectResults.Count(r => r.Status == TestStatus.Skipped),
            ErrorProjects = ProjectResults.Count(r => r.Status == TestStatus.Error),
            TotalDuration = TotalDuration,
            AverageDuration = ProjectResults.Any() ? 
                TimeSpan.FromTicks((long)ProjectResults.Average(r => r.Duration.Ticks)) : 
                TimeSpan.Zero
        };
    }
}

/// <summary>
/// Riepilogo dell'esecuzione dei test
/// </summary>
public class TestExecutionSummary
{
    public int TotalProjects { get; set; }
    public int PassedProjects { get; set; }
    public int FailedProjects { get; set; }
    public int SkippedProjects { get; set; }
    public int ErrorProjects { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan AverageDuration { get; set; }
    
    /// <summary>
    /// Percentuale di successo
    /// </summary>
    public double SuccessRate => TotalProjects > 0 ? 
        (double)PassedProjects / TotalProjects * 100 : 0;
    
    /// <summary>
    /// Indica se ci sono stati fallimenti
    /// </summary>
    public bool HasFailures => FailedProjects > 0 || ErrorProjects > 0;
}

/// <summary>
/// Stato di un test
/// </summary>
public enum TestStatus
{
    NotRun,     // Non ancora eseguito
    Running,    // In esecuzione
    Passed,     // Passato con successo
    Failed,     // Fallito (exit code != 0)
    Error,      // Errore durante l'esecuzione
    Skipped,    // Saltato
    Timeout     // Timeout
}