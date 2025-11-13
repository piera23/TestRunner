# Contributing to TestRunner

Prima di tutto, grazie per considerare di contribuire a TestRunner! 🎉

Seguendo queste linee guida aiuterai a mantenere il progetto organizzato e faciliterai la revisione delle tue contribuzioni.

## 📋 Indice

- [Code of Conduct](#code-of-conduct)
- [Come posso contribuire?](#come-posso-contribuire)
- [Setup ambiente di sviluppo](#setup-ambiente-di-sviluppo)
- [Processo di sviluppo](#processo-di-sviluppo)
- [Standard di codice](#standard-di-codice)
- [Commit messages](#commit-messages)
- [Pull Request process](#pull-request-process)
- [Testing](#testing)

## Code of Conduct

Questo progetto e tutti i partecipanti devono aderire al nostro [Code of Conduct](CODE_OF_CONDUCT.md). Partecipando, ti aspettiamo che tu rispetti questo codice.

## Come posso contribuire?

### 🐛 Reporting Bugs

Prima di creare una issue per un bug:

1. **Controlla** se la issue esiste già
2. **Verifica** di usare l'ultima versione
3. **Raccogli** informazioni rilevanti:
   - Versione TestRunner
   - Sistema operativo
   - .NET SDK version
   - File di configurazione (senza dati sensibili)
   - Passi per riprodurre
   - Comportamento atteso vs reale
   - Log di errore completi

**Template per bug report:**

```markdown
### Descrizione
Descrizione chiara e concisa del bug.

### Passi per riprodurre
1. Vai a '...'
2. Esegui comando '...'
3. Vedi errore

### Comportamento atteso
Cosa ti aspettavi che succedesse.

### Comportamento reale
Cosa è successo invece.

### Ambiente
- OS: [es. Ubuntu 22.04]
- .NET SDK: [es. 9.0.100]
- TestRunner version: [es. 1.0.0]

### Configurazione
```json
{
  "projects": [...]
}
```

### Log
```
Incolla qui i log di errore
```

### Screenshot
Se applicabile, aggiungi screenshot.
```

### 💡 Suggesting Enhancements

Per suggerire nuove funzionalità:

1. **Cerca** se qualcuno l'ha già proposta
2. **Descrivi chiaramente**:
   - Problema che risolve
   - Comportamento proposto
   - Possibili alternative
   - Esempi d'uso

### 📝 Improving Documentation

La documentazione è cruciale! Puoi contribuire:

- Correggendo typo o errori
- Aggiungendo esempi
- Migliorando spiegazioni
- Traducendo in altre lingue
- Aggiungendo diagrammi

### 🔧 Code Contributions

Contribuzioni di codice sono benvenute per:

- Fix di bug
- Nuove funzionalità
- Miglioramenti performance
- Refactoring
- Test aggiuntivi

## Setup ambiente di sviluppo

### Prerequisiti

```bash
# .NET SDK 9.0 o superiore
dotnet --version

# Git
git --version

# Editor (consigliati)
# - Visual Studio 2022
# - Visual Studio Code con C# extension
# - JetBrains Rider
```

### Clone e build

```bash
# 1. Fork del repository su GitHub

# 2. Clone del tuo fork
git clone https://github.com/YOUR_USERNAME/TestRunner.git
cd TestRunner

# 3. Aggiungi upstream remote
git remote add upstream https://github.com/ORIGINAL_OWNER/TestRunner.git

# 4. Verifica remotes
git remote -v

# 5. Build del progetto
dotnet build TestRunner.sln

# 6. Esegui test (quando implementati)
dotnet test TestRunner.sln

# 7. Esegui l'applicazione
dotnet run --project TestRunner/TestRunner.csproj -- --help
```

### Struttura del progetto

```
TestRunner/
├── TestRunner.sln              # Solution
├── TestRunner/
│   ├── TestRunner.csproj      # Project file
│   ├── Program.cs             # Entry point, CLI
│   ├── Models/                # Data models
│   │   ├── TestRunnerConfig.cs
│   │   ├── ProjectConfig.cs
│   │   └── TestResult.cs
│   └── Services/              # Business logic
│       ├── ConfigService.cs
│       ├── ProjectDetector.cs
│       ├── TestExecutor.cs
│       └── ReportGenerator.cs
├── TestRunner.Tests/          # Unit tests (TODO)
├── docs/                      # Documentazione extra
└── example/                   # Esempi
```

## Processo di sviluppo

### 1. Crea un branch

```bash
# Assicurati di essere updated
git checkout main
git pull upstream main

# Crea branch per la feature
git checkout -b feature/amazing-feature

# O per un bugfix
git checkout -b fix/nasty-bug
```

### 2. Sviluppo

- Scrivi codice pulito e leggibile
- Segui gli standard di codice (vedi sotto)
- Aggiungi commenti dove necessario
- Scrivi/aggiorna test
- Testa localmente

### 3. Test

```bash
# Build
dotnet build

# Test
dotnet test

# Esegui manualmente
dotnet run --project TestRunner/TestRunner.csproj -- init --auto
dotnet run --project TestRunner/TestRunner.csproj -- detect
dotnet run --project TestRunner/TestRunner.csproj -- run --dry-run
```

### 4. Commit

```bash
# Stage dei cambiamenti
git add .

# Commit con messaggio descrittivo
git commit -m "feat: add support for Ruby projects"

# Push al tuo fork
git push origin feature/amazing-feature
```

### 5. Pull Request

1. Vai al tuo fork su GitHub
2. Clicca "Compare & pull request"
3. Compila il template della PR
4. Attendi review

## Standard di codice

### C# Coding Conventions

Seguiamo le [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) di Microsoft.

#### Naming

```csharp
// PascalCase per classi, metodi, properties
public class TestExecutor { }
public void ExecuteTests() { }
public string ProjectName { get; set; }

// camelCase per parametri e variabili locali
private void Execute(string projectPath)
{
    var testResult = new TestResult();
}

// _camelCase per private fields
private readonly ILogger _logger;

// SCREAMING_CASE per costanti
private const int MAX_RETRY_COUNT = 3;
```

#### Formatting

```csharp
// Usa 4 spazi per indentazione (no tabs)
public class Example
{
    // Spazio dopo keywords
    if (condition)
    {
        // Code here
    }

    // Braces su nuova linea (Allman style)
    public void Method()
    {
        // Code
    }

    // Una dichiarazione per riga
    var name = "test";
    var age = 42;
}
```

#### Commenti

```csharp
/// <summary>
/// XML documentation per metodi pubblici
/// </summary>
/// <param name="config">Configurazione da validare</param>
/// <returns>True se valida</returns>
public bool ValidateConfig(TestRunnerConfig config)
{
    // Commenti inline per logica complessa
    // Ma preferisci codice auto-documentante

    // GOOD
    var isConfigValid = config.Projects.Any();

    // AVOID
    var x = config.Projects.Any();  // check if has projects
}
```

#### Principi

- **SOLID principles**
- **DRY** (Don't Repeat Yourself)
- **YAGNI** (You Aren't Gonna Need It)
- **Single Responsibility** per classi e metodi
- **Dependency Injection** dove appropriato
- **Async/await** per operazioni I/O

### Code review checklist

Prima di creare la PR, verifica:

- [ ] Il codice compila senza warning
- [ ] Tutti i test passano
- [ ] Hai aggiunto test per nuovo codice
- [ ] La documentazione è aggiornata
- [ ] Hai seguito gli standard di codice
- [ ] Non ci sono secret/password nel codice
- [ ] Le performance sono accettabili
- [ ] Il codice gestisce gli errori appropriatamente
- [ ] Hai testato su più piattaforme (se possibile)

## Commit messages

Seguiamo [Conventional Commits](https://www.conventionalcommits.org/).

### Formato

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- **feat**: Nuova funzionalità
- **fix**: Bug fix
- **docs**: Solo documentazione
- **style**: Formatting, missing semicolons, etc
- **refactor**: Code refactoring
- **perf**: Performance improvement
- **test**: Aggiunta test
- **chore**: Build, tooling, dependencies

### Esempi

```bash
# Feature
git commit -m "feat(detector): add Ruby project detection"

# Bug fix
git commit -m "fix(executor): handle null working directory"

# Documentation
git commit -m "docs: add examples for monorepo setup"

# Multi-line con body
git commit -m "feat(report): add CSV export format

- Implement CSV generator
- Add column headers
- Escape special characters
- Add tests for CSV export

Closes #123"
```

## Pull Request process

### 1. Prima di creare la PR

- Sincronizza con upstream main
- Risolvi conflitti
- Esegui test
- Review del tuo codice

```bash
# Sync con upstream
git fetch upstream
git rebase upstream/main

# Se ci sono conflitti, risolvili
git add .
git rebase --continue

# Force push al tuo branch
git push -f origin feature/amazing-feature
```

### 2. Crea la PR

Titolo e descrizione chiari:

```markdown
## Descrizione
Breve descrizione delle modifiche.

## Motivazione e contesto
Perché questa modifica è necessaria? Quale problema risolve?

## Tipo di modifica
- [ ] Bug fix (non-breaking change)
- [ ] Nuova funzionalità (non-breaking change)
- [ ] Breaking change (fix o feature che causa incompatibilità)
- [ ] Documentazione

## Come è stato testato?
Descrivi i test eseguiti.

## Checklist
- [ ] Il mio codice segue gli standard del progetto
- [ ] Ho eseguito self-review
- [ ] Ho commentato codice complesso
- [ ] Ho aggiornato la documentazione
- [ ] Le mie modifiche non generano warning
- [ ] Ho aggiunto test
- [ ] Tutti i test passano

## Screenshot (se applicabile)
```

### 3. Durante la review

- Rispondi ai commenti
- Fai le modifiche richieste
- Sii aperto al feedback
- Mantieni la discussione professionale

### 4. Dopo il merge

```bash
# Pulisci il branch locale
git checkout main
git pull upstream main
git branch -d feature/amazing-feature

# Pulisci il branch remoto
git push origin --delete feature/amazing-feature
```

## Testing

### Unit tests

```csharp
// TestRunner.Tests/Services/ConfigServiceTests.cs
public class ConfigServiceTests
{
    [Fact]
    public void LoadConfig_ValidFile_ReturnsConfig()
    {
        // Arrange
        var logger = Mock.Of<ILogger<ConfigService>>();
        var service = new ConfigService(logger);

        // Act
        var result = await service.LoadConfigAsync("valid.json");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Projects);
    }
}
```

### Integration tests

```csharp
[Fact]
public async Task ExecuteProject_RealCommand_ReturnsSuccess()
{
    // Test con comandi reali in ambiente isolato
}
```

### Test manuale

```bash
# Crea configurazione test
cat > test-config.json <<EOF
{
  "projects": [{
    "name": "test",
    "path": ".",
    "type": "Custom",
    "commands": ["echo 'Hello'"]
  }]
}
EOF

# Esegui
dotnet run --project TestRunner/TestRunner.csproj -- run --config test-config.json
```

## Domande?

Se hai domande:

- 💬 Apri una [Discussion](https://github.com/OWNER/TestRunner/discussions)
- 📧 Contatta i maintainer
- 📖 Consulta la [documentazione](README.md)

Grazie per contribuire! 🚀
