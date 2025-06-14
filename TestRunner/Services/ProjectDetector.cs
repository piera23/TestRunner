using System.Text.Json;
using TestRunner.Models;
namespace TestRunner.Services;

/// <summary>
/// Servizio per il rilevamento automatico dei progetti
/// </summary>
public class ProjectDetector
{
    /// <summary>
    /// Rileva automaticamente i progetti in una directory
    /// </summary>
    public async Task<List<ProjectConfig>> DetectProjectsAsync(string rootPath, int maxDepth = 3)
    {
        var projects = new List<ProjectConfig>();
        
        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {rootPath}");
        }

        await DetectProjectsRecursiveAsync(rootPath, projects, 0, maxDepth);
        
        return projects;
    }

    private async Task DetectProjectsRecursiveAsync(string currentPath, List<ProjectConfig> projects, int currentDepth, int maxDepth)
    {
        if (currentDepth > maxDepth) return;

        try
        {
            var projectConfig = await AnalyzeDirectoryAsync(currentPath);
            if (projectConfig != null)
            {
                projects.Add(projectConfig);
                return; // Se troviamo un progetto, non cerchiamo nelle sottodirectory
            }

            // Cerca nelle sottodirectory
            var subdirectories = Directory.GetDirectories(currentPath)
                .Where(dir => !ShouldSkipDirectory(Path.GetFileName(dir)))
                .ToArray();

            foreach (var subdir in subdirectories)
            {
                await DetectProjectsRecursiveAsync(subdir, projects, currentDepth + 1, maxDepth);
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Ignora directory a cui non abbiamo accesso
        }
    }

    /// <summary>
    /// Analizza una directory per determinare il tipo di progetto
    /// </summary>
    public async Task<ProjectConfig?> AnalyzeDirectoryAsync(string projectPath)
    {
        if (!Directory.Exists(projectPath))
            return null;

        var directoryName = Path.GetFileName(projectPath);
        var files = Directory.GetFiles(projectPath);
        var fileNames = files.Select(Path.GetFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Rileva tipo di progetto e genera configurazione
        var projectType = DetectProjectType(fileNames);
        if (projectType == ProjectType.Auto)
            return null;

        var config = new ProjectConfig
        {
            Name = directoryName,
            Path = projectPath,
            Type = projectType,
            Commands = await GenerateCommandsAsync(projectType, projectPath, fileNames)
        };

        return config;
    }

    private ProjectType DetectProjectType(HashSet<string> fileNames)
    {
        // React/Vue/Angular - Web Apps
        if (fileNames.Contains("package.json"))
        {
            // Potrebbe essere web app o mobile app, controlliamo meglio
            return DetectJavaScriptProjectType(fileNames);
        }

        // Python
        if (fileNames.Contains("requirements.txt") || 
            fileNames.Contains("pyproject.toml") || 
            fileNames.Contains("setup.py") ||
            fileNames.Any(f => f.EndsWith(".py", StringComparison.OrdinalIgnoreCase)))
        {
            return ProjectType.PythonScript;
        }

        // .NET
        if (fileNames.Any(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                              f.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)))
        {
            return ProjectType.DotNetApp;
        }

        // JavaScript puro (senza package.json)
        if (fileNames.Any(f => f.EndsWith(".js", StringComparison.OrdinalIgnoreCase)) &&
            !fileNames.Contains("package.json"))
        {
            return ProjectType.JavaScriptApp;
        }

        return ProjectType.Auto;
    }

    private ProjectType DetectJavaScriptProjectType(HashSet<string> fileNames)
    {
        // React Native
        if (fileNames.Contains("metro.config.js") || 
            fileNames.Contains("react-native.config.js") ||
            fileNames.Contains("app.json"))
        {
            return ProjectType.MobileApp;
        }

        // Se ha package.json, assumiamo sia una web app
        return ProjectType.WebApp;
    }

    private async Task<List<string>> GenerateCommandsAsync(ProjectType projectType, string projectPath, HashSet<string> fileNames)
    {
        var commands = new List<string>();

        switch (projectType)
        {
            case ProjectType.WebApp:
                commands.AddRange(await GenerateWebAppCommandsAsync(projectPath, fileNames));
                break;

            case ProjectType.MobileApp:
                commands.AddRange(await GenerateMobileAppCommandsAsync(projectPath, fileNames));
                break;

            case ProjectType.PythonScript:
                commands.AddRange(GeneratePythonCommandsAsync(fileNames));
                break;

            case ProjectType.JavaScriptApp:
                commands.AddRange(GenerateJavaScriptCommandsAsync(fileNames));
                break;

            case ProjectType.DotNetApp:
                commands.AddRange(GenerateDotNetCommandsAsync(fileNames));
                break;
        }

        return commands;
    }

    private async Task<List<string>> GenerateWebAppCommandsAsync(string projectPath, HashSet<string> fileNames)
    {
        var commands = new List<string>();

        if (fileNames.Contains("package.json"))
        {
            var packageJsonPath = Path.Combine(projectPath, "package.json");
            var scripts = await GetPackageJsonScriptsAsync(packageJsonPath);

            // Aggiungi comandi comuni per web app
            if (scripts.ContainsKey("test"))
                commands.Add("npm test");
            if (scripts.ContainsKey("lint"))
                commands.Add("npm run lint");
            if (scripts.ContainsKey("build"))
                commands.Add("npm run build");
            
            // Se non ci sono script di test, aggiungi check di sintassi
            if (!scripts.ContainsKey("test"))
            {
                // Verifica sintassi JavaScript/TypeScript
                if (fileNames.Any(f => f.EndsWith(".ts") || f.EndsWith(".tsx")))
                    commands.Add("npx tsc --noEmit");
                else
                    commands.Add("node -c package.json"); // Check sintassi package.json
            }
        }

        return commands;
    }

    private async Task<List<string>> GenerateMobileAppCommandsAsync(string projectPath, HashSet<string> fileNames)
    {
        var commands = new List<string>();

        if (fileNames.Contains("package.json"))
        {
            var packageJsonPath = Path.Combine(projectPath, "package.json");
            var scripts = await GetPackageJsonScriptsAsync(packageJsonPath);

            // React Native
            if (scripts.ContainsKey("test"))
                commands.Add("npm test");
            if (scripts.ContainsKey("lint"))
                commands.Add("npm run lint");
            
            // Check sintassi TypeScript/JavaScript
            if (fileNames.Any(f => f.EndsWith(".ts") || f.EndsWith(".tsx")))
                commands.Add("npx tsc --noEmit");
        }

        return commands;
    }

    private List<string> GeneratePythonCommandsAsync(HashSet<string> fileNames)
    {
        var commands = new List<string>();

        // Se c'è pytest, usalo
        if (fileNames.Any(f => f.StartsWith("test_") || f.EndsWith("_test.py")))
        {
            commands.Add("python -m pytest");
        }
        else
        {
            // Altrimenti fai un check di sintassi
            commands.Add("python -m py_compile *.py");
        }

        // Check con flake8 se disponibile
        commands.Add("python -c \"import flake8\" && flake8 . || echo 'Flake8 not available, skipping linting'");

        return commands;
    }

    private List<string> GenerateJavaScriptCommandsAsync(HashSet<string> fileNames)
    {
        var commands = new List<string>();

        // Check sintassi JavaScript
        foreach (var jsFile in fileNames.Where(f => f.EndsWith(".js", StringComparison.OrdinalIgnoreCase)))
        {
            commands.Add($"node -c \"{jsFile}\"");
        }

        return commands;
    }

    private List<string> GenerateDotNetCommandsAsync(HashSet<string> fileNames)
    {
        var commands = new List<string>();

        // Se ci sono progetti di test
        if (fileNames.Any(f => f.Contains("test", StringComparison.OrdinalIgnoreCase) && f.EndsWith(".csproj")))
        {
            commands.Add("dotnet test");
        }
        else
        {
            // Altrimenti solo build
            commands.Add("dotnet build");
        }

        return commands;
    }

    private async Task<Dictionary<string, object>> GetPackageJsonScriptsAsync(string packageJsonPath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(packageJsonPath);
            var packageJson = JsonSerializer.Deserialize<JsonElement>(json);
            
            if (packageJson.TryGetProperty("scripts", out var scriptsElement))
            {
                return scriptsElement.EnumerateObject()
                    .ToDictionary(prop => prop.Name, prop => (object)prop.Value.GetString()!);
            }
        }
        catch
        {
            // Ignora errori di parsing
        }

        return new Dictionary<string, object>();
    }

    private bool ShouldSkipDirectory(string directoryName)
    {
        var skipDirs = new[]
        {
            "node_modules", ".git", ".svn", "bin", "obj", ".vs", ".vscode",
            "dist", "build", "__pycache__", ".pytest_cache", "coverage",
            ".nyc_output", "tmp", "temp"
        };

        return skipDirs.Contains(directoryName, StringComparer.OrdinalIgnoreCase) ||
               directoryName.StartsWith(".");
    }
}