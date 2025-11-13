# TestRunner - NuGet Package Guide

## 📦 Informazioni Pacchetto

**Package ID**: `TestRunner`
**Versione**: `1.0.0`
**Tipo**: .NET Global Tool + Library
**Comando**: `testrunner`
**Target Framework**: .NET 9.0
**Licenza**: MIT

---

## 🚀 Creare il Pacchetto NuGet

### 1. Build del Pacchetto

```bash
# Naviga nella directory del progetto
cd TestRunner/TestRunner

# Crea il pacchetto NuGet
dotnet pack -c Release

# Il pacchetto verrà creato in: ./nupkg/TestRunner.1.0.0.nupkg
```

### 2. Testare il Pacchetto Localmente

```bash
# Installa il tool localmente
dotnet tool install --global --add-source ./nupkg TestRunner

# Testa il comando
testrunner --help

# Disinstalla per testare nuovamente
dotnet tool uninstall --global TestRunner
```

---

## 📤 Pubblicare su NuGet.org

### Prerequisiti

1. **Account NuGet.org**: Registrati su https://www.nuget.org
2. **API Key**: Genera una chiave API dal tuo profilo

### Pubblicazione

```bash
# Metodo 1: Usando dotnet nuget push
dotnet nuget push TestRunner/nupkg/TestRunner.1.0.0.nupkg \
    --api-key YOUR_API_KEY \
    --source https://api.nuget.org/v3/index.json

# Metodo 2: Upload manuale
# Vai su https://www.nuget.org/packages/manage/upload
# Carica il file .nupkg
```

### Verificare la Pubblicazione

Dopo 10-15 minuti, il pacchetto sarà disponibile:
```bash
dotnet tool install --global TestRunner
```

---

## 🔐 Pubblicare su Feed Privato (Azure DevOps / GitHub Packages)

### GitHub Packages

1. **Genera Personal Access Token** con scope `write:packages`

2. **Configura NuGet source**:
```bash
dotnet nuget add source \
    --name github \
    --username YOUR_GITHUB_USERNAME \
    --password YOUR_GITHUB_TOKEN \
    --store-password-in-clear-text \
    "https://nuget.pkg.github.com/piera23/index.json"
```

3. **Pubblica**:
```bash
dotnet nuget push TestRunner/nupkg/TestRunner.1.0.0.nupkg \
    --api-key YOUR_GITHUB_TOKEN \
    --source "github"
```

### Azure DevOps Artifacts

1. **Crea un Feed** in Azure DevOps → Artifacts

2. **Configura source**:
```bash
dotnet nuget add source \
    --name azure \
    --username YOUR_USERNAME \
    --password YOUR_PAT \
    "https://pkgs.dev.azure.com/YOUR_ORG/_packaging/YOUR_FEED/nuget/v3/index.json"
```

3. **Pubblica**:
```bash
dotnet nuget push TestRunner/nupkg/TestRunner.1.0.0.nupkg \
    --api-key az \
    --source "azure"
```

---

## 🎨 Aggiungere un'Icona (Opzionale ma Raccomandato)

Il pacchetto è configurato per includere un'icona `icon.png`.

### Creare l'Icona

1. **Dimensioni**: 128x128 pixels (o 64x64, 256x256)
2. **Formato**: PNG con trasparenza
3. **Posizionamento**: `TestRunner/TestRunner/icon.png`

### Esempio con ImageMagick

```bash
cd TestRunner/TestRunner

# Crea un'icona placeholder semplice (128x128 blu con testo)
convert -size 128x128 xc:#0078D4 \
    -font Arial -pointsize 32 -fill white \
    -gravity center -annotate +0+0 "TR" \
    icon.png
```

### Senza Icona

Se non vuoi un'icona, rimuovi questa riga dal `.csproj`:
```xml
<PackageIcon>icon.png</PackageIcon>
```

E rimuovi questo blocco:
```xml
<ItemGroup>
    <None Include="icon.png" Pack="true" PackagePath="\" Condition="Exists('icon.png')" />
</ItemGroup>
```

---

## 🔄 Aggiornare la Versione

### Versioning Semantico (SemVer)

Formato: `MAJOR.MINOR.PATCH`

- **MAJOR**: Cambiamenti breaking (es. 2.0.0)
- **MINOR**: Nuove funzionalità retrocompatibili (es. 1.1.0)
- **PATCH**: Bug fix retrocompatibili (es. 1.0.1)

### Aggiornare Versione nel .csproj

```xml
<Version>1.1.0</Version>
```

### Pubblicare Nuova Versione

```bash
# 1. Aggiorna Version in TestRunner.csproj
# 2. Build del nuovo pacchetto
dotnet pack -c Release

# 3. Pubblica
dotnet nuget push TestRunner/nupkg/TestRunner.1.1.0.nupkg \
    --api-key YOUR_API_KEY \
    --source https://api.nuget.org/v3/index.json
```

---

## 📝 Release Notes (Changelog)

Aggiorna `<PackageReleaseNotes>` nel `.csproj` o mantieni `CHANGELOG.md`:

```xml
<PackageReleaseNotes>
## Version 1.1.0
- Added support for parallel execution
- Fixed configuration validation bug
- Improved error reporting
</PackageReleaseNotes>
```

---

## 🎯 Utilizzare il Pacchetto

### Come .NET Global Tool

```bash
# Installa globalmente
dotnet tool install --global TestRunner

# Usa il comando
testrunner init --auto
testrunner run --config testrunner.json

# Aggiorna
dotnet tool update --global TestRunner

# Disinstalla
dotnet tool uninstall --global TestRunner
```

### Come Libreria in Altri Progetti

```bash
# Aggiungi riferimento
dotnet add package TestRunner
```

```csharp
using TestRunner.Services;
using TestRunner.Models;

var executor = new TestExecutor(logger);
var result = await executor.ExecuteProjectAsync(project);
```

---

## 🛠️ Script di Build Automatizzati

### build-package.sh (Linux/macOS)

```bash
#!/bin/bash
set -e

VERSION=$1
if [ -z "$VERSION" ]; then
    echo "Usage: ./build-package.sh <version>"
    echo "Example: ./build-package.sh 1.0.1"
    exit 1
fi

echo "🔨 Building TestRunner v$VERSION"

# Update version in .csproj
sed -i "s/<Version>.*<\/Version>/<Version>$VERSION<\/Version>/" TestRunner/TestRunner.csproj

# Clean previous builds
rm -rf TestRunner/nupkg

# Build package
cd TestRunner
dotnet pack -c Release

echo "✅ Package created: TestRunner/nupkg/TestRunner.$VERSION.nupkg"
echo ""
echo "To test locally:"
echo "  dotnet tool install --global --add-source ./nupkg TestRunner"
echo ""
echo "To publish to NuGet.org:"
echo "  dotnet nuget push nupkg/TestRunner.$VERSION.nupkg --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json"
```

### build-package.ps1 (Windows PowerShell)

```powershell
param(
    [Parameter(Mandatory=$true)]
    [string]$Version
)

Write-Host "🔨 Building TestRunner v$Version" -ForegroundColor Green

# Update version in .csproj
$csprojPath = "TestRunner\TestRunner.csproj"
$content = Get-Content $csprojPath
$content = $content -replace '<Version>.*</Version>', "<Version>$Version</Version>"
Set-Content $csprojPath $content

# Clean previous builds
Remove-Item -Recurse -Force "TestRunner\nupkg" -ErrorAction SilentlyContinue

# Build package
Set-Location TestRunner
dotnet pack -c Release

Write-Host "✅ Package created: TestRunner\nupkg\TestRunner.$Version.nupkg" -ForegroundColor Green
Write-Host ""
Write-Host "To test locally:" -ForegroundColor Yellow
Write-Host "  dotnet tool install --global --add-source .\nupkg TestRunner"
Write-Host ""
Write-Host "To publish to NuGet.org:" -ForegroundColor Yellow
Write-Host "  dotnet nuget push nupkg\TestRunner.$Version.nupkg --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json"
```

---

## 🔍 Verificare il Contenuto del Pacchetto

```bash
# Installa NuGet Package Explorer
dotnet tool install --global NuGetPackageExplorer

# Apri il pacchetto
nuget-package-explorer TestRunner/nupkg/TestRunner.1.0.0.nupkg
```

O estrai manualmente (il .nupkg è un file ZIP):
```bash
unzip TestRunner.1.0.0.nupkg -d package-contents
ls -la package-contents
```

---

## 📊 Statistiche e Metriche

Dopo la pubblicazione su NuGet.org:

- **Statistiche download**: https://www.nuget.org/stats/packages/TestRunner
- **Badge**: Aggiungi al README.md

```markdown
[![NuGet](https://img.shields.io/nuget/v/TestRunner.svg)](https://www.nuget.org/packages/TestRunner/)
[![Downloads](https://img.shields.io/nuget/dt/TestRunner.svg)](https://www.nuget.org/packages/TestRunner/)
```

---

## 🐛 Troubleshooting

### Errore: "Package ID already exists"

Il nome `TestRunner` potrebbe essere già preso. Cambia in:
```xml
<PackageId>YourPrefix.TestRunner</PackageId>
```

### Errore: "Invalid API Key"

Verifica che:
1. La API Key sia corretta
2. La API Key abbia i permessi necessari
3. La API Key non sia scaduta

### Errore: "Icon not found"

Se non hai un'icona, rimuovi la proprietà dal .csproj:
```xml
<!-- Rimuovi questa riga -->
<PackageIcon>icon.png</PackageIcon>
```

---

## 📚 Risorse

- **NuGet.org**: https://www.nuget.org
- **Documentazione Packaging**: https://docs.microsoft.com/nuget/create-packages/creating-a-package
- **Global Tools**: https://docs.microsoft.com/dotnet/core/tools/global-tools
- **Versioning**: https://semver.org

---

## ✅ Checklist Pre-Pubblicazione

Prima di pubblicare su NuGet.org:

- [ ] Versione corretta nel .csproj
- [ ] README.md completo e aggiornato
- [ ] CHANGELOG.md aggiornato
- [ ] Licenza MIT presente
- [ ] Tests passano tutti
- [ ] Build in Release funziona
- [ ] Testato localmente con `dotnet tool install`
- [ ] Icona presente (opzionale)
- [ ] URL repository corretto
- [ ] Tags appropriati
- [ ] Descrizione chiara

---

**Buona pubblicazione!** 🚀
