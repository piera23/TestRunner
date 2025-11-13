using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestRunner.Models;
using TestRunner.Services;

namespace TestRunner;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Configura i servizi
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // Configura i comandi
        var rootCommand = new RootCommand("TestRunner - Simple test executor for web apps, mobile apps, and scripts");

        // Comando: init
        var initCommand = CreateInitCommand(serviceProvider);
        rootCommand.AddCommand(initCommand);

        // Comando: detect
        var detectCommand = CreateDetectCommand(serviceProvider);
        rootCommand.AddCommand(detectCommand);

        // Comando: run
        var runCommand = CreateRunCommand(serviceProvider);
        rootCommand.AddCommand(runCommand);

        // Comando: validate
        var validateCommand = CreateValidateCommand(serviceProvider);
        rootCommand.AddCommand(validateCommand);

        try
        {
            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Unhandled exception");
            
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            
            return 1;
        }
        finally
        {
            serviceProvider.Dispose();
        }
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole(options =>
            {
                // Usa le nuove opzioni del formatter
                options.FormatterName = "simple";
            });
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = false;
                options.TimestampFormat = "[HH:mm:ss] ";
                options.SingleLine = true;
            });
            builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
        });

        services.AddSingleton<ConfigService>();
        services.AddSingleton<ProjectDetector>();
        services.AddSingleton<TestExecutor>();
        services.AddSingleton<ReportGenerator>();
    }

    private static Command CreateInitCommand(ServiceProvider serviceProvider)
    {
        var command = new Command("init", "Initialize a new test configuration");
        
        var pathOption = new Option<string>("--path", () => ".", "Root path to scan for projects");
        var configOption = new Option<string>("--config", () => "testrunner.json", "Configuration file path");
        var autoDetectOption = new Option<bool>("--auto", () => false, "Auto-detect projects");
        var forceOption = new Option<bool>("--force", () => false, "Overwrite existing configuration");

        command.AddOption(pathOption);
        command.AddOption(configOption);
        command.AddOption(autoDetectOption);
        command.AddOption(forceOption);

        command.SetHandler(async (path, config, autoDetect, force) =>
        {
            var configService = serviceProvider.GetRequiredService<ConfigService>();
            var detector = serviceProvider.GetRequiredService<ProjectDetector>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                if (File.Exists(config) && !force)
                {
                    Console.WriteLine($"Configuration file already exists: {config}");
                    Console.WriteLine("Use --force to overwrite");
                    return;
                }

                TestRunnerConfig configuration;

                if (autoDetect)
                {
                    Console.WriteLine($"🔍 Auto-detecting projects in: {path}");
                    configuration = await configService.CreateAutoConfigAsync(path, detector);
                    Console.WriteLine($"✅ Detected {configuration.Projects.Count} projects");
                }
                else
                {
                    Console.WriteLine("📝 Creating default configuration");
                    configuration = configService.CreateDefaultConfig();
                }

                await configService.SaveConfigAsync(configuration, config);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✅ Configuration created: {config}");
                Console.ResetColor();
                
                Console.WriteLine("\n💡 Next steps:");
                Console.WriteLine($"   1. Edit {config} to customize your projects");
                Console.WriteLine("   2. Run 'testrunner run' to execute tests");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error initializing configuration");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }
        }, pathOption, configOption, autoDetectOption, forceOption);

        return command;
    }

    private static Command CreateDetectCommand(ServiceProvider serviceProvider)
    {
        var command = new Command("detect", "Detect projects in a directory");
        
        var pathOption = new Option<string>("--path", () => ".", "Root path to scan");
        var depthOption = new Option<int>("--depth", () => 3, "Maximum scan depth");
        var outputOption = new Option<string?>("--output", "Save detected projects to config file");

        command.AddOption(pathOption);
        command.AddOption(depthOption);
        command.AddOption(outputOption);

        command.SetHandler(async (path, depth, output) =>
        {
            var detector = serviceProvider.GetRequiredService<ProjectDetector>();
            var configService = serviceProvider.GetRequiredService<ConfigService>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                Console.WriteLine($"🔍 Scanning for projects in: {path}");
                var projects = await detector.DetectProjectsAsync(path, depth);

                if (!projects.Any())
                {
                    Console.WriteLine("❌ No projects detected");
                    return;
                }

                Console.WriteLine($"\n✅ Found {projects.Count} projects:");
                foreach (var project in projects)
                {
                    var typeIcon = GetProjectTypeIcon(project.Type);
                    Console.WriteLine($"  {typeIcon} {project.Name.PadRight(30)} {project.Path}");
                    if (project.Commands.Any())
                    {
                        Console.WriteLine($"      Commands: {string.Join(", ", project.Commands)}");
                    }
                }

                if (!string.IsNullOrEmpty(output))
                {
                    var config = new TestRunnerConfig { Projects = projects };
                    await configService.SaveConfigAsync(config, output);
                    Console.WriteLine($"\n💾 Configuration saved to: {output}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error detecting projects");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            }
        }, pathOption, depthOption, outputOption);

        return command;
    }

    private static Command CreateRunCommand(ServiceProvider serviceProvider)
    {
        var command = new Command("run", "Run tests for configured projects");
        
        var configOption = new Option<string>("--config", () => "testrunner.json", "Configuration file path");
        var projectsOption = new Option<string[]>("--projects", "Specific projects to run");
        var tagsOption = new Option<string[]>("--tags", "Filter projects by tags");
        var parallelOption = new Option<bool>("--parallel", "Run projects in parallel");
        var reportOption = new Option<string?>("--report", "Generate report file");
        var formatOption = new Option<OutputFormat>("--format", () => OutputFormat.Console, "Report format");
        var verboseOption = new Option<bool>("--verbose", "Verbose output");
        var dryRunOption = new Option<bool>("--dry-run", "Show what would be executed without running");

        command.AddOption(configOption);
        command.AddOption(projectsOption);
        command.AddOption(tagsOption);
        command.AddOption(parallelOption);
        command.AddOption(reportOption);
        command.AddOption(formatOption);
        command.AddOption(verboseOption);
        command.AddOption(dryRunOption);

        command.SetHandler(async (config, projects, tags, parallel, report, format, verbose, dryRun) =>
        {
            var configService = serviceProvider.GetRequiredService<ConfigService>();
            var executor = serviceProvider.GetRequiredService<TestExecutor>();
            var reportGenerator = serviceProvider.GetRequiredService<ReportGenerator>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                // Configura logging verboso se richiesto
                if (verbose)
                {
                    // Riconfigura il logging per essere più verboso
                    var newServices = new ServiceCollection();
                    newServices.AddLogging(builder =>
                    {
                        builder.AddConsole(options =>
                        {
                            options.FormatterName = "simple";
                        });
                        builder.AddSimpleConsole(options =>
                        {
                            options.IncludeScopes = true;
                            options.TimestampFormat = "[HH:mm:ss] ";
                            options.SingleLine = false;
                        });
                        builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
                    });
                }

                // Carica configurazione
                var configuration = await configService.LoadConfigAsync(config);
                
                // Override delle impostazioni da command line
                if (parallel)
                {
                    configuration.ParallelExecution = true;
                }

                Console.WriteLine($"🚀 Starting TestRunner");
                Console.WriteLine($"📁 Config: {config}");
                Console.WriteLine($"🏗️  Projects: {configuration.Projects.Count}");
                
                if (projects?.Any() == true)
                {
                    Console.WriteLine($"🎯 Filter: {string.Join(", ", projects)}");
                }
                
                if (tags?.Any() == true)
                {
                    Console.WriteLine($"🏷️  Tags: {string.Join(", ", tags)}");
                }

                if (dryRun)
                {
                    Console.WriteLine("\n🔍 DRY RUN - No tests will be executed");
                    var filteredProjects = configuration.Projects
                        .Where(p => projects == null || projects.Length == 0 || projects.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                        .Where(p => tags == null || tags.Length == 0 || p.Tags.Any(t => tags.Contains(t, StringComparer.OrdinalIgnoreCase)))
                        .ToList();

                    foreach (var project in filteredProjects)
                    {
                        Console.WriteLine($"\n📂 {project.Name} ({project.Type})");
                        Console.WriteLine($"   Path: {project.Path}");
                        Console.WriteLine($"   Commands: {string.Join(", ", project.Commands)}");
                    }
                    return;
                }

                Console.WriteLine("\n" + new string('━', 60));
                
                // Esegui test
                var result = await executor.ExecuteAllProjectsAsync(configuration, projects, tags);

                // Mostra risultati su console
                var consoleReport = reportGenerator.GenerateConsoleReport(result);
                Console.WriteLine(consoleReport);

                // Salva report se richiesto
                if (!string.IsNullOrEmpty(report))
                {
                    await reportGenerator.SaveReportAsync(result, format, report);
                    Console.WriteLine($"📄 Report saved: {report}");
                }

                // Exit code basato sui risultati
                Environment.Exit(result.IsSuccess ? 0 : 1);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error running tests");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }
        }, configOption, projectsOption, tagsOption, parallelOption, reportOption, formatOption, verboseOption, dryRunOption);

        return command;
    }

    private static Command CreateValidateCommand(ServiceProvider serviceProvider)
    {
        var command = new Command("validate", "Validate configuration file");
        
        var configOption = new Option<string>("--config", () => "testrunner.json", "Configuration file path");

        command.AddOption(configOption);

        command.SetHandler(async (config) =>
        {
            var configService = serviceProvider.GetRequiredService<ConfigService>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                Console.WriteLine($"🔍 Validating configuration: {config}");
                
                var configuration = await configService.LoadConfigAsync(config);
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ Configuration is valid!");
                Console.ResetColor();
                
                Console.WriteLine($"\n📊 Summary:");
                Console.WriteLine($"   Projects: {configuration.Projects.Count}");
                Console.WriteLine($"   Enabled: {configuration.Projects.Count(p => p.Enabled)}");
                Console.WriteLine($"   Parallel: {configuration.ParallelExecution}");
                Console.WriteLine($"   Max Parallel: {configuration.MaxParallelProjects}");

                // Mostra progetti con problemi potenziali
                var projectsWithIssues = new List<string>();
                
                foreach (var project in configuration.Projects)
                {
                    if (!Directory.Exists(project.Path))
                    {
                        projectsWithIssues.Add($"{project.Name}: Directory not found - {project.Path}");
                    }
                    
                    if (!project.Commands.Any())
                    {
                        projectsWithIssues.Add($"{project.Name}: No commands configured");
                    }
                }

                if (projectsWithIssues.Any())
                {
                    Console.WriteLine("\n⚠️  Potential issues:");
                    foreach (var issue in projectsWithIssues)
                    {
                        Console.WriteLine($"   • {issue}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error validating configuration");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Configuration is invalid: {ex.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }
        }, configOption);

        return command;
    }

    private static string GetProjectTypeIcon(ProjectType type) => type switch
    {
        ProjectType.WebApp => "🌐",
        ProjectType.MobileApp => "📱",
        ProjectType.PythonScript => "🐍",
        ProjectType.JavaScriptApp => "⚡",
        ProjectType.DotNetApp => "🔷",
        _ => "📄"
    };
}