using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using TestRunner.Models;
using Microsoft.Extensions.Logging;

namespace TestRunner.Services;

/// <summary>
/// Servizio per la generazione di report
/// </summary>
public class ReportGenerator
{
    private readonly ILogger<ReportGenerator> _logger;

    public ReportGenerator(ILogger<ReportGenerator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Genera un report in formato console
    /// </summary>
    public string GenerateConsoleReport(TestExecutionResult result)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine();
        sb.AppendLine("╔══════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                        TEST RESULTS                          ║");
        sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");
        sb.AppendLine();

        // Riepilogo generale
        sb.AppendLine($"🕐 Execution Time: {result.TotalDuration:hh\\:mm\\:ss}");
        sb.AppendLine($"📊 Total Projects: {result.Summary.TotalProjects}");
        sb.AppendLine($"✅ Passed: {result.Summary.PassedProjects}");
        sb.AppendLine($"❌ Failed: {result.Summary.FailedProjects}");
        sb.AppendLine($"⚠️  Errors: {result.Summary.ErrorProjects}");
        sb.AppendLine($"⏭️  Skipped: {result.Summary.SkippedProjects}");
        sb.AppendLine($"📈 Success Rate: {result.Summary.SuccessRate:F1}%");
        sb.AppendLine();

        // Stato generale
        var overallStatus = result.IsSuccess ? "🎉 SUCCESS" : "💥 FAILURE";
        var statusColor = result.IsSuccess ? "SUCCESS" : "FAILURE";
        sb.AppendLine($"Overall Status: {overallStatus}");
        sb.AppendLine();

        // Dettagli per progetto
        if (result.ProjectResults.Any())
        {
            sb.AppendLine("Project Details:");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            
            foreach (var projectResult in result.ProjectResults.OrderBy(r => r.ProjectName))
            {
                var statusIcon = GetStatusIcon(projectResult.Status);
                var duration = projectResult.Duration.TotalSeconds.ToString("F1");
                
                sb.AppendLine($"{statusIcon} {projectResult.ProjectName.PadRight(30)} {duration}s");
                
                if (projectResult.Status == TestStatus.Failed || projectResult.Status == TestStatus.Error)
                {
                    if (!string.IsNullOrEmpty(projectResult.ErrorMessage))
                    {
                        sb.AppendLine($"   Error: {projectResult.ErrorMessage}");
                    }
                    
                    // Mostra comandi falliti
                    var failedCommands = projectResult.CommandResults.Where(c => !c.IsSuccess).ToList();
                    if (failedCommands.Any())
                    {
                        sb.AppendLine("   Failed commands:");
                        foreach (var cmd in failedCommands)
                        {
                            sb.AppendLine($"   • {cmd.Command} (exit code: {cmd.ExitCode})");
                        }
                    }
                }
                
                sb.AppendLine();
            }
        }

        // Suggerimenti se ci sono fallimenti
        if (result.Summary.HasFailures)
        {
            sb.AppendLine("💡 Troubleshooting Tips:");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine("• Check project paths and ensure directories exist");
            sb.AppendLine("• Verify that required dependencies are installed");
            sb.AppendLine("• Run 'testrunner run --verbose' for detailed output");
            sb.AppendLine("• Use 'testrunner run --project <name>' to test individual projects");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Genera un report in formato JSON
    /// </summary>
    public async Task<string> GenerateJsonReportAsync(TestExecutionResult result, string? outputPath = null)
    {
        try
        {
            var jsonReport = new
            {
                Timestamp = DateTime.UtcNow,
                ExecutionInfo = new
                {
                    StartTime = result.StartTime,
                    EndTime = result.EndTime,
                    Duration = result.TotalDuration.TotalSeconds,
                    IsSuccess = result.IsSuccess
                },
                Summary = new
                {
                    TotalProjects = result.Summary.TotalProjects,
                    PassedProjects = result.Summary.PassedProjects,
                    FailedProjects = result.Summary.FailedProjects,
                    ErrorProjects = result.Summary.ErrorProjects,
                    SkippedProjects = result.Summary.SkippedProjects,
                    SuccessRate = Math.Round(result.Summary.SuccessRate, 2),
                    AverageDuration = result.Summary.AverageDuration.TotalSeconds
                },
                Projects = result.ProjectResults.Select(p => new
                {
                    Name = p.ProjectName,
                    Path = p.ProjectPath,
                    Type = p.ProjectType.ToString(),
                    Status = p.Status.ToString(),
                    Duration = Math.Round(p.Duration.TotalSeconds, 2),
                    StartTime = p.StartTime,
                    EndTime = p.EndTime,
                    Tags = p.Tags,
                    ErrorMessage = p.ErrorMessage,
                    Commands = p.CommandResults.Select(c => new
                    {
                        Command = c.Command,
                        ExitCode = c.ExitCode,
                        Duration = Math.Round(c.Duration.TotalSeconds, 2),
                        IsSuccess = c.IsSuccess,
                        Output = c.Output,
                        Error = c.Error
                    }).ToArray()
                }).ToArray()
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(jsonReport, options);

            if (!string.IsNullOrEmpty(outputPath))
            {
                await File.WriteAllTextAsync(outputPath, json);
                _logger.LogInformation("JSON report saved to: {OutputPath}", outputPath);
            }

            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JSON report");
            throw;
        }
    }

    /// <summary>
    /// Genera un report in formato JUnit XML
    /// </summary>
    public async Task<string> GenerateJUnitReportAsync(TestExecutionResult result, string? outputPath = null)
    {
        try
        {
            var testsuites = new XElement("testsuites",
                new XAttribute("name", "TestRunner"),
                new XAttribute("tests", result.Summary.TotalProjects),
                new XAttribute("failures", result.Summary.FailedProjects),
                new XAttribute("errors", result.Summary.ErrorProjects),
                new XAttribute("skipped", result.Summary.SkippedProjects),
                new XAttribute("time", result.TotalDuration.TotalSeconds.ToString("F3")),
                new XAttribute("timestamp", result.StartTime.ToString("O"))
            );

            foreach (var project in result.ProjectResults)
            {
                var testsuite = new XElement("testsuite",
                    new XAttribute("name", project.ProjectName),
                    new XAttribute("tests", project.CommandResults.Count),
                    new XAttribute("failures", project.CommandResults.Count(c => !c.IsSuccess)),
                    new XAttribute("errors", project.Status == TestStatus.Error ? 1 : 0),
                    new XAttribute("skipped", project.Status == TestStatus.Skipped ? 1 : 0),
                    new XAttribute("time", project.Duration.TotalSeconds.ToString("F3")),
                    new XAttribute("timestamp", project.StartTime.ToString("O"))
                );

                foreach (var command in project.CommandResults)
                {
                    var testcase = new XElement("testcase",
                        new XAttribute("name", command.Command),
                        new XAttribute("classname", project.ProjectName),
                        new XAttribute("time", command.Duration.TotalSeconds.ToString("F3"))
                    );

                    if (!command.IsSuccess)
                    {
                        var failure = new XElement("failure",
                            new XAttribute("message", $"Command failed with exit code {command.ExitCode}"),
                            new XAttribute("type", "CommandFailure"),
                            new XCData($"Command: {command.Command}\nExit Code: {command.ExitCode}\nOutput: {command.Output}\nError: {command.Error}")
                        );
                        testcase.Add(failure);
                    }

                    if (!string.IsNullOrEmpty(command.Output))
                    {
                        testcase.Add(new XElement("system-out", new XCData(command.Output)));
                    }

                    if (!string.IsNullOrEmpty(command.Error))
                    {
                        testcase.Add(new XElement("system-err", new XCData(command.Error)));
                    }

                    testsuite.Add(testcase);
                }

                testsuites.Add(testsuite);
            }

            var xmlDeclaration = new XDeclaration("1.0", "UTF-8", null);
            var document = new XDocument(xmlDeclaration, testsuites);
            
            var xml = document.ToString();

            if (!string.IsNullOrEmpty(outputPath))
            {
                await File.WriteAllTextAsync(outputPath, xml);
                _logger.LogInformation("JUnit XML report saved to: {OutputPath}", outputPath);
            }

            return xml;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JUnit XML report");
            throw;
        }
    }

    /// <summary>
    /// Genera un report in formato HTML
    /// </summary>
    public async Task<string> GenerateHtmlReportAsync(TestExecutionResult result, string? outputPath = null)
    {
        try
        {
            var html = GenerateHtmlContent(result);

            if (!string.IsNullOrEmpty(outputPath))
            {
                await File.WriteAllTextAsync(outputPath, html);
                _logger.LogInformation("HTML report saved to: {OutputPath}", outputPath);
            }

            return html;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating HTML report");
            throw;
        }
    }

    private string GenerateHtmlContent(TestExecutionResult result)
    {
        var successClass = result.IsSuccess ? "success" : "failure";
        var statusText = result.IsSuccess ? "SUCCESS" : "FAILURE";

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>TestRunner Report</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 20px; background: #f5f5f5; }}
        .container {{ max-width: 1200px; margin: 0 auto; background: white; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; border-radius: 8px 8px 0 0; }}
        .header h1 {{ margin: 0; font-size: 2.5em; }}
        .header .subtitle {{ opacity: 0.9; margin-top: 10px; }}
        .summary {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; padding: 30px; }}
        .metric {{ background: #f8f9fa; padding: 20px; border-radius: 8px; text-align: center; }}
        .metric .value {{ font-size: 2em; font-weight: bold; margin-bottom: 5px; }}
        .metric .label {{ color: #666; font-size: 0.9em; }}
        .success .value {{ color: #28a745; }}
        .failure .value {{ color: #dc3545; }}
        .warning .value {{ color: #ffc107; }}
        .info .value {{ color: #17a2b8; }}
        .status {{ padding: 20px 30px; border-bottom: 1px solid #eee; }}
        .status.success {{ background: #d4edda; color: #155724; }}
        .status.failure {{ background: #f8d7da; color: #721c24; }}
        .projects {{ padding: 30px; }}
        .project {{ border: 1px solid #eee; border-radius: 8px; margin-bottom: 20px; overflow: hidden; }}
        .project-header {{ padding: 15px 20px; background: #f8f9fa; border-bottom: 1px solid #eee; display: flex; justify-content: space-between; align-items: center; }}
        .project-title {{ font-weight: bold; display: flex; align-items: center; gap: 10px; }}
        .project-duration {{ color: #666; font-size: 0.9em; }}
        .project-content {{ padding: 20px; }}
        .command {{ background: #f8f9fa; border-radius: 4px; padding: 10px; margin-bottom: 10px; font-family: monospace; }}
        .command.success {{ border-left: 4px solid #28a745; }}
        .command.failure {{ border-left: 4px solid #dc3545; }}
        .command-output {{ background: #1e1e1e; color: #d4d4d4; padding: 15px; border-radius: 4px; margin-top: 10px; white-space: pre-wrap; font-family: monospace; font-size: 0.9em; max-height: 300px; overflow-y: auto; }}
        .status-badge {{ padding: 4px 8px; border-radius: 4px; font-size: 0.8em; font-weight: bold; }}
        .status-badge.passed {{ background: #d4edda; color: #155724; }}
        .status-badge.failed {{ background: #f8d7da; color: #721c24; }}
        .status-badge.error {{ background: #f8d7da; color: #721c24; }}
        .status-badge.skipped {{ background: #fff3cd; color: #856404; }}
        .footer {{ padding: 20px 30px; background: #f8f9fa; border-radius: 0 0 8px 8px; text-align: center; color: #666; font-size: 0.9em; }}
        .expandable {{ cursor: pointer; }}
        .expandable:hover {{ background: #f0f0f0; }}
        details {{ margin-top: 10px; }}
        summary {{ cursor: pointer; padding: 10px; background: #f8f9fa; border-radius: 4px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🧪 TestRunner Report</h1>
            <div class=""subtitle"">Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}</div>
        </div>
        
        <div class=""status {successClass}"">
            <h2>Overall Status: {statusText}</h2>
            <p>Execution completed in {result.TotalDuration:hh\\:mm\\:ss}</p>
        </div>
        
        <div class=""summary"">
            <div class=""metric info"">
                <div class=""value"">{result.Summary.TotalProjects}</div>
                <div class=""label"">Total Projects</div>
            </div>
            <div class=""metric success"">
                <div class=""value"">{result.Summary.PassedProjects}</div>
                <div class=""label"">Passed</div>
            </div>
            <div class=""metric failure"">
                <div class=""value"">{result.Summary.FailedProjects}</div>
                <div class=""label"">Failed</div>
            </div>
            <div class=""metric warning"">
                <div class=""value"">{result.Summary.SkippedProjects}</div>
                <div class=""label"">Skipped</div>
            </div>
            <div class=""metric info"">
                <div class=""value"">{result.Summary.SuccessRate:F1}%</div>
                <div class=""label"">Success Rate</div>
            </div>
            <div class=""metric info"">
                <div class=""value"">{result.Summary.AverageDuration:mm\\:ss}</div>
                <div class=""label"">Avg Duration</div>
            </div>
        </div>
        
        <div class=""projects"">
            <h2>Project Results</h2>
            {GenerateProjectsHtml(result.ProjectResults)}
        </div>
        
        <div class=""footer"">
            Report generated by TestRunner v1.0.0
        </div>
    </div>
    
    <script>
        // Add interactivity for expandable sections
        document.querySelectorAll('.expandable').forEach(el => {{
            el.addEventListener('click', () => {{
                const content = el.nextElementSibling;
                content.style.display = content.style.display === 'none' ? 'block' : 'none';
            }});
        }});
    </script>
</body>
</html>";
    }

    private string GenerateProjectsHtml(List<TestResult> projects)
    {
        var sb = new StringBuilder();
        
        foreach (var project in projects.OrderBy(p => p.ProjectName))
        {
            var statusClass = project.Status.ToString().ToLowerInvariant();
            var statusIcon = GetStatusIcon(project.Status);
            
            sb.AppendLine($@"
            <div class=""project"">
                <div class=""project-header"">
                    <div class=""project-title"">
                        {statusIcon} {project.ProjectName}
                        <span class=""status-badge {statusClass}"">{project.Status}</span>
                    </div>
                    <div class=""project-duration"">{project.Duration.TotalSeconds:F1}s</div>
                </div>
                <div class=""project-content"">
                    <p><strong>Path:</strong> {project.ProjectPath}</p>
                    <p><strong>Type:</strong> {project.ProjectType}</p>");
            
            if (project.Tags.Any())
            {
                sb.AppendLine($"<p><strong>Tags:</strong> {string.Join(", ", project.Tags)}</p>");
            }
            
            if (!string.IsNullOrEmpty(project.ErrorMessage))
            {
                sb.AppendLine($@"<div style=""background: #f8d7da; padding: 10px; border-radius: 4px; margin: 10px 0;"">
                    <strong>Error:</strong> {project.ErrorMessage}
                </div>");
            }
            
            if (project.CommandResults.Any())
            {
                sb.AppendLine("<h4>Commands:</h4>");
                foreach (var cmd in project.CommandResults)
                {
                    var cmdStatusClass = cmd.IsSuccess ? "success" : "failure";
                    var cmdIcon = cmd.IsSuccess ? "✅" : "❌";
                    
                    sb.AppendLine($@"
                    <div class=""command {cmdStatusClass}"">
                        <div style=""display: flex; justify-content: space-between; align-items: center;"">
                            <span>{cmdIcon} <code>{cmd.Command}</code></span>
                            <small>Exit: {cmd.ExitCode} | {cmd.Duration.TotalSeconds:F1}s</small>
                        </div>");
                    
                    if (!string.IsNullOrEmpty(cmd.Output) || !string.IsNullOrEmpty(cmd.Error))
                    {
                        sb.AppendLine(@"<details style=""margin-top: 10px;"">
                            <summary>Show Output</summary>");
                        
                        if (!string.IsNullOrEmpty(cmd.Output))
                        {
                            sb.AppendLine($@"<div class=""command-output"">{System.Net.WebUtility.HtmlEncode(cmd.Output)}</div>");
                        }

                        if (!string.IsNullOrEmpty(cmd.Error))
                        {
                            sb.AppendLine($@"<div class=""command-output"" style=""border-left: 4px solid #dc3545;"">
                                <strong>STDERR:</strong><br>{System.Net.WebUtility.HtmlEncode(cmd.Error)}
                            </div>");
                        }
                        
                        sb.AppendLine("</details>");
                    }
                    
                    sb.AppendLine("</div>");
                }
            }
            
            sb.AppendLine("</div></div>");
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Salva il report nel formato specificato
    /// </summary>
    public async Task SaveReportAsync(TestExecutionResult result, OutputFormat format, string outputPath)
    {
        try
        {
            _logger.LogInformation("Generating {Format} report to: {OutputPath}", format, outputPath);

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
                    
                case OutputFormat.Console:
                    var consoleReport = GenerateConsoleReport(result);
                    await File.WriteAllTextAsync(outputPath, consoleReport);
                    break;
                    
                default:
                    throw new ArgumentException($"Unsupported report format: {format}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving report");
            throw;
        }
    }

    private string GetStatusIcon(TestStatus status) => status switch
    {
        TestStatus.Passed => "✅",
        TestStatus.Failed => "❌",
        TestStatus.Error => "💥",
        TestStatus.Skipped => "⏭️",
        TestStatus.Timeout => "⏰",
        TestStatus.Running => "🔄",
        _ => "❓"
    };
}