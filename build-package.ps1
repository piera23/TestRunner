param(
    [Parameter(Mandatory=$true)]
    [string]$Version
)

Write-Host "🔨 Building TestRunner v$Version" -ForegroundColor Green

# Update version in .csproj
$csprojPath = "TestRunner\TestRunner.csproj"
$content = Get-Content $csprojPath -Raw
$content = $content -replace '<Version>.*</Version>', "<Version>$Version</Version>"
Set-Content $csprojPath $content

# Clean previous builds
if (Test-Path "TestRunner\nupkg") {
    Remove-Item -Recurse -Force "TestRunner\nupkg"
}

# Build package
Push-Location TestRunner
dotnet pack -c Release
Pop-Location

Write-Host ""
Write-Host "✅ Package created: TestRunner\nupkg\TestRunner.$Version.nupkg" -ForegroundColor Green
Write-Host "✅ Symbols package: TestRunner\nupkg\TestRunner.$Version.snupkg" -ForegroundColor Green
Write-Host ""
Write-Host "📦 Package contents:" -ForegroundColor Cyan
Write-Host "   - testrunner CLI tool"
Write-Host "   - TestRunner libraries"
Write-Host "   - README.md"
Write-Host "   - Symbol files for debugging"
Write-Host ""
Write-Host "🧪 To test locally:" -ForegroundColor Yellow
Write-Host "   dotnet tool install --global --add-source .\TestRunner\nupkg TestRunner"
Write-Host "   testrunner --help"
Write-Host ""
Write-Host "📤 To publish to NuGet.org:" -ForegroundColor Yellow
Write-Host "   dotnet nuget push TestRunner\nupkg\TestRunner.$Version.nupkg ``"
Write-Host "       --api-key YOUR_API_KEY ``"
Write-Host "       --source https://api.nuget.org/v3/index.json"
Write-Host ""
Write-Host "🔐 To publish to GitHub Packages:" -ForegroundColor Yellow
Write-Host "   dotnet nuget push TestRunner\nupkg\TestRunner.$Version.nupkg ``"
Write-Host "       --api-key YOUR_GITHUB_TOKEN ``"
Write-Host "       --source https://nuget.pkg.github.com/piera23/index.json"
