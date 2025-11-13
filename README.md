# TestRunner

Un potente ed estensibile test runner multipiattaforma per gestire ed eseguire test su progetti web, mobile e script di vari linguaggi.

[![.NET Version](https://img.shields.io/badge/.NET-9.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

## 📋 Indice

- [Caratteristiche](#-caratteristiche)
- [Requisiti](#-requisiti)
- [Installazione](#-installazione)
- [Quick Start](#-quick-start)
- [Comandi](#-comandi)
- [Configurazione](#-configurazione)
- [Esempi](#-esempi)
- [Architettura](#-architettura)
- [Formati di Output](#-formati-di-output)
- [Best Practices](#-best-practices)
- [Troubleshooting](#-troubleshooting)
- [Contribuire](#-contribuire)

## ✨ Caratteristiche

- **🌍 Multi-linguaggio**: Supporta JavaScript/TypeScript, Python, .NET, Java, Go, Rust, PHP, Ruby
- **🔄 Esecuzione parallela**: Esegui test su più progetti contemporaneamente
- **🔍 Auto-detection**: Rileva automaticamente il tipo di progetto e i comandi di test
- **📊 Report multipli**: Console, JSON, XML (JUnit), HTML, Markdown, CSV
- **🏷️ Tag-based filtering**: Filtra progetti per nome o tag
- **⚙️ Configurabile**: Timeout personalizzabili, variabili d'ambiente, pre/post comandi
- **🔒 Sicuro**: Validazione robusta e gestione sicura dei comandi
- **📈 CI/CD ready**: Output compatibile con Jenkins, GitLab CI, GitHub Actions

## 📦 Requisiti

- **.NET 9.0 SDK** o superiore
- Sistema operativo: Linux, macOS, Windows

### Dipendenze opzionali (in base ai progetti da testare)

- **Node.js/npm** - per progetti JavaScript/TypeScript
- **Python 3.x** - per script Python
- **JDK** - per progetti Java
- **Go** - per progetti Go
- **Rust/Cargo** - per progetti Rust
- **PHP** - per progetti PHP
- **Ruby** - per progetti Ruby

## 🚀 Installazione

### Da sorgente

```bash
# Clona la repository
git clone https://github.com/yourusername/TestRunner.git
cd TestRunner

# Build del progetto
dotnet build TestRunner.sln

# Pubblica come eseguibile standalone
dotnet publish TestRunner/TestRunner.csproj -c Release -o ./publish

# Aggiungi al PATH (opzionale)
export PATH=$PATH:$(pwd)/publish
```

### Come tool globale .NET

```bash
# Installa come tool globale
dotnet tool install --global TestRunner

# Ora puoi usare 'testrunner' da qualsiasi directory
testrunner --help
```

## 🏁 Quick Start

### 1. Inizializza una configurazione

```bash
# Configurazione manuale
testrunner init

# Auto-rilevamento progetti
testrunner init --auto --path ./my-workspace
```

### 2. Rileva progetti nella directory corrente

```bash
testrunner detect --path . --depth 3
```

### 3. Valida la configurazione

```bash
testrunner validate --config testrunner.json
```

### 4. Esegui i test

```bash
# Esegui tutti i progetti
testrunner run

# Esegui progetti specifici
testrunner run --projects web-frontend mobile-app

# Esegui progetti per tag
testrunner run --tags backend critical

# Esecuzione parallela
testrunner run --parallel --max-parallel 4

# Con report HTML
testrunner run --report results.html --format Html
```

## 📝 Comandi

### `init` - Inizializza configurazione

Crea un nuovo file di configurazione `testrunner.json`.

```bash
testrunner init [options]

Options:
  --path <path>         Directory radice da scansionare (default: .)
  --config <config>     Percorso file configurazione (default: testrunner.json)
  --auto               Auto-rileva progetti
  --force              Sovrascrivi configurazione esistente
```

**Esempi:**
```bash
# Configurazione manuale con template
testrunner init

# Auto-detection con salvataggio
testrunner init --auto --path ~/projects

# Forza sovrascrittura
testrunner init --auto --force
```

### `detect` - Rileva progetti

Scansiona una directory e identifica automaticamente i progetti.

```bash
testrunner detect [options]

Options:
  --path <path>         Directory da scansionare (default: .)
  --depth <depth>       Profondità massima scansione (default: 3)
  --output <output>     Salva risultati in file config
```

**Esempi:**
```bash
# Rileva nella directory corrente
testrunner detect

# Scansione profonda con salvataggio
testrunner detect --path ~/workspace --depth 5 --output detected.json
```

### `validate` - Valida configurazione

Verifica la validità del file di configurazione.

```bash
testrunner validate [options]

Options:
  --config <config>     File configurazione (default: testrunner.json)
```

**Esempi:**
```bash
testrunner validate
testrunner validate --config production.json
```

### `run` - Esegui test

Esegue i test configurati sui progetti.

```bash
testrunner run [options]

Options:
  --config <config>          File configurazione (default: testrunner.json)
  --projects <names...>      Progetti specifici da eseguire
  --tags <tags...>           Filtra per tag
  --parallel                 Esecuzione parallela
  --report <file>           File di output report
  --format <format>         Formato report (Console|Json|Xml|Html|Markdown|Csv)
  --verbose                  Output dettagliato
  --dry-run                  Mostra cosa verrebbe eseguito senza eseguire
```

**Esempi:**
```bash
# Esecuzione base
testrunner run

# Progetti specifici
testrunner run --projects frontend backend-api

# Solo progetti con tag "critical"
testrunner run --tags critical

# Parallelo con report HTML
testrunner run --parallel --report results.html --format Html

# Dry run per vedere cosa verrebbe eseguito
testrunner run --dry-run --verbose

# Esecuzione con filtri multipli
testrunner run --tags backend python --parallel --max-parallel 2
```

## ⚙️ Configurazione

### Struttura del file `testrunner.json`

```json
{
  "name": "My Test Suite",
  "description": "Suite di test per il progetto",
  "version": "1.0",
  "global_timeout_minutes": 60,
  "parallel_execution": true,
  "max_parallel_projects": 4,
  "stop_on_first_failure": false,
  "continue_on_error": true,
  "log_level": "Information",
  "output_format": "Console",
  "report_file": null,

  "global_environment": {
    "CI": "true",
    "NODE_ENV": "test"
  },

  "global_tags": ["ci", "automated"],

  "pre_execution_commands": [
    "echo 'Starting test suite...'"
  ],

  "post_execution_commands": [
    "echo 'Test suite completed'"
  ],

  "base_directory": ".",

  "projects": [
    {
      "name": "web-frontend",
      "path": "./frontend",
      "type": "WebApp",
      "description": "Frontend web application",
      "enabled": true,
      "priority": 1,

      "commands": [
        "npm test",
        "npm run build"
      ],

      "pre_commands": [
        "npm install --production=false"
      ],

      "post_commands": [
        "npm run cleanup"
      ],

      "environment": {
        "NODE_ENV": "test",
        "PORT": "3000"
      },

      "tags": ["frontend", "critical", "web"],

      "timeout_minutes": 10,
      "working_directory": null,

      "retry_count": 2,
      "retry_delay_seconds": 5,

      "ignore_exit_codes": [],

      "expected_output_patterns": [
        "All tests passed"
      ],

      "forbidden_output_patterns": [
        "FATAL ERROR",
        "Segmentation fault"
      ]
    }
  ],

  "notifications": {
    "enabled": false,
    "on_success": false,
    "on_failure": true,
    "on_start": false,
    "slack_webhook": null,
    "email_recipients": [],
    "custom_webhooks": []
  },

  "storage": {
    "enabled": false,
    "type": "FileSystem",
    "path": "./testrunner-results",
    "retain_results_days": 30,
    "auto_cleanup": true
  }
}
```

### Tipi di progetto supportati

| Tipo | Descrizione | File di rilevamento |
|------|-------------|---------------------|
| `WebApp` | Applicazioni web (React, Vue, Angular) | `package.json` |
| `MobileApp` | App mobile (React Native, Flutter) | `package.json` + `metro.config.js` |
| `JavaScriptApp` | Applicazioni Node.js/JavaScript | `package.json` |
| `PythonScript` | Script e applicazioni Python | `requirements.txt`, `*.py` |
| `DotNetApp` | Applicazioni .NET (C#, F#) | `*.csproj`, `*.sln` |
| `JavaApp` | Applicazioni Java | `pom.xml`, `build.gradle` |
| `GoApp` | Applicazioni Go | `go.mod`, `*.go` |
| `RustApp` | Applicazioni Rust | `Cargo.toml` |
| `PhpApp` | Applicazioni PHP | `composer.json`, `*.php` |
| `RubyApp` | Applicazioni Ruby | `Gemfile` |
| `DockerApp` | Applicazioni containerizzate | `Dockerfile`, `docker-compose.yml` |
| `Custom` | Comandi personalizzati | Definiti manualmente |

### Opzioni globali

#### Timeout e esecuzione

- **`global_timeout_minutes`** (int): Timeout globale in minuti per tutta l'esecuzione
- **`parallel_execution`** (bool): Abilita esecuzione parallela
- **`max_parallel_projects`** (int): Numero massimo di progetti paralleli
- **`stop_on_first_failure`** (bool): Ferma l'esecuzione al primo fallimento
- **`continue_on_error`** (bool): Continua anche in caso di errori

#### Logging e output

- **`log_level`** (enum): `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`
- **`output_format`** (enum): `Console`, `Json`, `Xml`, `Html`, `Markdown`, `Csv`
- **`report_file`** (string): Percorso file di output del report

#### Ambiente

- **`global_environment`** (dict): Variabili d'ambiente applicate a tutti i progetti
- **`global_tags`** (array): Tag applicati a tutti i progetti
- **`base_directory`** (string): Directory base per percorsi relativi

#### Hooks

- **`pre_execution_commands`** (array): Comandi eseguiti prima di tutti i progetti
- **`post_execution_commands`** (array): Comandi eseguiti dopo tutti i progetti

### Opzioni per progetto

#### Base

- **`name`** (string, required): Nome univoco del progetto
- **`path`** (string, required): Percorso al progetto
- **`type`** (enum, required): Tipo di progetto
- **`description`** (string): Descrizione del progetto
- **`enabled`** (bool): Abilita/disabilita il progetto
- **`priority`** (int): Priorità di esecuzione (più alto = prima)

#### Comandi

- **`commands`** (array): Comandi principali da eseguire
- **`pre_commands`** (array): Comandi di setup
- **`post_commands`** (array): Comandi di cleanup (sempre eseguiti)

#### Esecuzione

- **`timeout_minutes`** (int): Timeout in minuti
- **`working_directory`** (string): Directory di lavoro (default: `path`)
- **`environment`** (dict): Variabili d'ambiente specifiche
- **`retry_count`** (int): Numero di retry in caso di fallimento
- **`retry_delay_seconds`** (int): Delay tra retry

#### Validazione

- **`ignore_exit_codes`** (array): Exit code da ignorare
- **`expected_output_patterns`** (array): Pattern regex attesi nell'output
- **`forbidden_output_patterns`** (array): Pattern che indicano fallimento

#### Organizzazione

- **`tags`** (array): Tag per filtraggio e organizzazione

## 📚 Esempi

### Esempio 1: Progetto Full-Stack

```json
{
  "projects": [
    {
      "name": "frontend",
      "path": "./client",
      "type": "WebApp",
      "commands": ["npm test", "npm run lint", "npm run build"],
      "pre_commands": ["npm ci"],
      "tags": ["frontend", "critical"],
      "timeout_minutes": 15,
      "environment": {
        "NODE_ENV": "test",
        "API_URL": "http://localhost:3001"
      }
    },
    {
      "name": "backend-api",
      "path": "./server",
      "type": "JavaScriptApp",
      "commands": ["npm test", "npm run test:integration"],
      "pre_commands": ["npm ci", "docker-compose up -d postgres"],
      "post_commands": ["docker-compose down"],
      "tags": ["backend", "api", "critical"],
      "timeout_minutes": 20,
      "environment": {
        "NODE_ENV": "test",
        "DB_HOST": "localhost"
      }
    },
    {
      "name": "database-migrations",
      "path": "./database",
      "type": "PythonScript",
      "commands": ["python -m pytest tests/"],
      "pre_commands": ["pip install -r requirements-test.txt"],
      "tags": ["database", "backend"],
      "timeout_minutes": 5
    }
  ],
  "parallel_execution": true,
  "max_parallel_projects": 2
}
```

### Esempio 2: Monorepo con microservizi

```json
{
  "name": "Microservices Suite",
  "projects": [
    {
      "name": "user-service",
      "path": "./services/user",
      "type": "DotNetApp",
      "commands": ["dotnet test", "dotnet build -c Release"],
      "tags": ["microservice", "dotnet", "critical"],
      "timeout_minutes": 8
    },
    {
      "name": "order-service",
      "path": "./services/order",
      "type": "JavaApp",
      "commands": ["mvn test", "mvn package"],
      "tags": ["microservice", "java"],
      "timeout_minutes": 12
    },
    {
      "name": "notification-service",
      "path": "./services/notification",
      "type": "GoApp",
      "commands": ["go test ./...", "go build"],
      "tags": ["microservice", "go"],
      "timeout_minutes": 5
    },
    {
      "name": "analytics-service",
      "path": "./services/analytics",
      "type": "PythonScript",
      "commands": ["pytest", "mypy ."],
      "tags": ["microservice", "python"],
      "timeout_minutes": 10
    }
  ],
  "parallel_execution": true,
  "max_parallel_projects": 4,
  "stop_on_first_failure": false
}
```

### Esempio 3: Pipeline CI/CD

```json
{
  "name": "CI Pipeline",
  "pre_execution_commands": [
    "echo 'Starting CI pipeline...'",
    "git fetch origin",
    "docker-compose -f docker-compose.test.yml up -d"
  ],
  "post_execution_commands": [
    "docker-compose -f docker-compose.test.yml down",
    "echo 'Pipeline completed'"
  ],
  "projects": [
    {
      "name": "unit-tests",
      "path": "./",
      "type": "Custom",
      "commands": ["npm run test:unit"],
      "tags": ["unit", "fast"],
      "timeout_minutes": 5,
      "priority": 3
    },
    {
      "name": "integration-tests",
      "path": "./",
      "type": "Custom",
      "commands": ["npm run test:integration"],
      "tags": ["integration"],
      "timeout_minutes": 15,
      "priority": 2
    },
    {
      "name": "e2e-tests",
      "path": "./",
      "type": "Custom",
      "commands": ["npm run test:e2e"],
      "tags": ["e2e", "slow"],
      "timeout_minutes": 30,
      "priority": 1,
      "retry_count": 2,
      "retry_delay_seconds": 10
    }
  ],
  "parallel_execution": false,
  "stop_on_first_failure": true,
  "output_format": "Xml",
  "report_file": "test-results.xml"
}
```

## 🏗️ Architettura

### Struttura del progetto

```
TestRunner/
├── TestRunner.sln                  # Solution file
├── README.md                       # Questa documentazione
├── LICENSE                         # Licenza del progetto
└── TestRunner/
    ├── TestRunner.csproj          # Project file
    ├── Program.cs                 # Entry point, CLI commands
    ├── Models/                    # Data models
    │   ├── TestRunnerConfig.cs   # Configurazione principale
    │   ├── ProjectConfig.cs      # Configurazione progetto
    │   └── TestResult.cs         # Risultati esecuzione
    ├── Services/                  # Business logic
    │   ├── ConfigService.cs      # Gestione configurazione
    │   ├── ProjectDetector.cs    # Auto-detection progetti
    │   ├── TestExecutor.cs       # Esecuzione test
    │   └── ReportGenerator.cs    # Generazione report
    └── example/
        └── simple-config.json    # Esempio configurazione
```

### Componenti principali

#### 1. **Program.cs**
Entry point dell'applicazione. Gestisce:
- Parsing argomenti CLI con System.CommandLine
- Dependency Injection
- Routing comandi
- Error handling globale

#### 2. **ConfigService**
Responsabile per:
- Caricamento e validazione configurazione
- Serializzazione/deserializzazione JSON
- Merge di configurazioni
- Creazione template default

#### 3. **ProjectDetector**
Auto-rileva progetti analizzando:
- File di configurazione (package.json, pom.xml, *.csproj, etc.)
- Struttura directory
- Tipo di file presenti
- Genera comandi appropriati per tipo progetto

#### 4. **TestExecutor**
Esegue i test:
- Gestisce esecuzione sequenziale e parallela
- Crea e gestisce processi
- Cattura stdout/stderr
- Gestisce timeout e cancellazione
- Applica variabili d'ambiente

#### 5. **ReportGenerator**
Genera report in formati multipli:
- Console (colorato e formattato)
- JSON (per integrazione)
- XML/JUnit (per CI/CD)
- HTML (interattivo)
- Markdown
- CSV

### Flusso di esecuzione

```
┌─────────────┐
│   CLI Args  │
└──────┬──────┘
       │
       v
┌─────────────────┐
│ Command Router  │
└──────┬──────────┘
       │
       v
┌─────────────────┐       ┌──────────────────┐
│ ConfigService   │──────▶│ Load & Validate  │
└──────┬──────────┘       └──────────────────┘
       │
       v
┌─────────────────┐       ┌──────────────────┐
│ TestExecutor    │──────▶│ Execute Projects │
└──────┬──────────┘       └──────────────────┘
       │
       v
┌─────────────────┐       ┌──────────────────┐
│ ReportGenerator │──────▶│ Generate Output  │
└─────────────────┘       └──────────────────┘
```

### Gestione errori

Il sistema implementa gestione errori multi-livello:

1. **Validazione configurazione**: Prima dell'esecuzione
2. **Validazione directory**: Prima di ogni progetto
3. **Timeout comandi**: Durante l'esecuzione
4. **Exception handling**: Try-catch a tutti i livelli
5. **Logging strutturato**: Microsoft.Extensions.Logging

### Sicurezza

- ✅ Nessun comando arbitrario da input utente
- ✅ Comandi provengono da configurazione trusted
- ✅ Escaping corretto per shell commands
- ✅ Validazione percorsi file
- ✅ Timeout per prevenire hang
- ✅ Nessuna valutazione dinamica di codice

## 📊 Formati di Output

### Console (Default)

Output colorato con emoji e formattazione:

```
╔══════════════════════════════════════════════════════════════╗
║                        TEST RESULTS                          ║
╚══════════════════════════════════════════════════════════════╝

🕐 Execution Time: 00:02:34
📊 Total Projects: 5
✅ Passed: 4
❌ Failed: 1
⚠️  Errors: 0
⏭️  Skipped: 0
📈 Success Rate: 80.0%

Overall Status: 💥 FAILURE

Project Details:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ frontend                        12.3s
✅ backend-api                     18.5s
❌ mobile-app                      5.2s
   Error: Command failed: npm test
   Failed commands:
   • npm test (exit code: 1)

✅ python-scripts                  3.1s
✅ dotnet-service                  9.8s
```

### JSON

Formato strutturato per integrazione:

```json
{
  "timestamp": "2024-11-13T14:30:00.000Z",
  "executionInfo": {
    "startTime": "2024-11-13T14:27:26.000Z",
    "endTime": "2024-11-13T14:30:00.000Z",
    "duration": 154.2,
    "isSuccess": false
  },
  "summary": {
    "totalProjects": 5,
    "passedProjects": 4,
    "failedProjects": 1,
    "errorProjects": 0,
    "skippedProjects": 0,
    "successRate": 80.0,
    "averageDuration": 30.84
  },
  "projects": [
    {
      "name": "frontend",
      "path": "./client",
      "type": "WebApp",
      "status": "Passed",
      "duration": 12.3,
      "commands": [...]
    }
  ]
}
```

### XML (JUnit)

Compatibile con CI/CD tools:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<testsuites name="TestRunner" tests="5" failures="1" errors="0"
            skipped="0" time="154.200" timestamp="2024-11-13T14:27:26Z">
  <testsuite name="frontend" tests="2" failures="0" errors="0"
             skipped="0" time="12.300" timestamp="2024-11-13T14:27:26Z">
    <testcase name="npm test" classname="frontend" time="8.100" />
    <testcase name="npm run build" classname="frontend" time="4.200" />
  </testsuite>
  <testsuite name="mobile-app" tests="1" failures="1" errors="0"
             skipped="0" time="5.200" timestamp="2024-11-13T14:27:38Z">
    <testcase name="npm test" classname="mobile-app" time="5.200">
      <failure message="Command failed with exit code 1" type="CommandFailure">
        <![CDATA[Command: npm test
Exit Code: 1
Output: ...
Error: ...]]>
      </failure>
    </testcase>
  </testsuite>
</testsuites>
```

### HTML

Report interattivo con dettagli espandibili:

- Riepilogo visuale con grafici
- Lista progetti con stato colorato
- Output comandi espandibile
- Responsive design
- Esportabile e condivisibile

## 💡 Best Practices

### 1. Organizzazione progetti

```json
{
  "projects": [
    // Raggruppa per tipo
    {
      "name": "frontend-web",
      "tags": ["frontend", "web", "critical"]
    },
    {
      "name": "frontend-mobile",
      "tags": ["frontend", "mobile"]
    },
    {
      "name": "backend-api",
      "tags": ["backend", "api", "critical"]
    }
  ]
}
```

### 2. Timeout appropriati

- **Unit tests**: 5-10 minuti
- **Integration tests**: 10-20 minuti
- **E2E tests**: 20-30 minuti
- **Build pesanti**: 15-30 minuti

### 3. Pre/Post commands

```json
{
  "pre_commands": [
    "npm ci",                          // Installa dipendenze
    "docker-compose up -d database"    // Avvia servizi
  ],
  "commands": [
    "npm test"                         // Esegui test
  ],
  "post_commands": [
    "docker-compose down",             // Cleanup (sempre eseguito)
    "rm -rf coverage/"                 // Rimuovi file temporanei
  ]
}
```

### 4. Variabili d'ambiente

```json
{
  "global_environment": {
    "CI": "true",
    "NODE_ENV": "test"
  },
  "projects": [
    {
      "environment": {
        "PORT": "3000",               // Override per progetto
        "API_KEY": "${TEST_API_KEY}"  // Da environment reale
      }
    }
  ]
}
```

### 5. Retry strategico

```json
{
  "retry_count": 2,        // Solo per test flaky (e2e, network)
  "retry_delay_seconds": 5 // Delay tra retry
}
```

### 6. Esecuzione parallela intelligente

```json
{
  "parallel_execution": true,
  "max_parallel_projects": 4,  // Basato su CPU disponibili
  // Progetti CPU-intensive non paralleli
  "stop_on_first_failure": true  // Per CI veloci
}
```

## 🐛 Troubleshooting

### Problema: "Configuration file not found"

**Causa**: File `testrunner.json` non presente o percorso errato

**Soluzione**:
```bash
# Verifica file esiste
ls -la testrunner.json

# Specifica percorso esplicito
testrunner run --config /path/to/testrunner.json

# O crea nuova configurazione
testrunner init --auto
```

### Problema: "Working directory does not exist"

**Causa**: Path del progetto non valido

**Soluzione**:
```bash
# Valida configurazione
testrunner validate

# Controlla path dei progetti
cat testrunner.json | grep "path"

# Usa path assoluti o relativi corretti
{
  "path": "./frontend",      // Relativo
  "path": "/home/user/app"   // Assoluto
}
```

### Problema: "Command timed out"

**Causa**: Comando supera timeout configurato

**Soluzione**:
```json
{
  "timeout_minutes": 30,  // Aumenta timeout
  "global_timeout_minutes": 120  // Timeout globale
}
```

### Problema: "Permission denied" su Linux/Mac

**Causa**: Script o binari non eseguibili

**Soluzione**:
```bash
# Rendi eseguibile
chmod +x ./gradlew
chmod +x ./scripts/*.sh

# Oppure usa bash esplicitamente
{
  "commands": ["bash ./run-tests.sh"]
}
```

### Problema: Test falliscono solo in TestRunner

**Causa**: Variabili d'ambiente mancanti o directory di lavoro errata

**Soluzione**:
```json
{
  "environment": {
    "NODE_ENV": "test",
    "PATH": "/usr/local/bin:$PATH"
  },
  "working_directory": "./src",  // Cambia se necessario
  "pre_commands": [
    "export DEBUG=*"  // Debug variabili
  ]
}
```

### Problema: Output non mostrato

**Causa**: Output buffering o redirect

**Soluzione**:
```bash
# Usa verbose mode
testrunner run --verbose

# Genera report dettagliato
testrunner run --report debug.html --format Html
```

### Debug generale

```bash
# 1. Dry run per vedere comandi
testrunner run --dry-run

# 2. Verbose logging
testrunner run --verbose

# 3. Test singolo progetto
testrunner run --projects problematic-project --verbose

# 4. Validazione configurazione
testrunner validate --config testrunner.json

# 5. Verifica detection
testrunner detect --path . --depth 3
```

## 🤝 Contribuire

Contributi sono benvenuti! Per favore:

1. Fork del repository
2. Crea branch feature (`git checkout -b feature/AmazingFeature`)
3. Commit delle modifiche (`git commit -m 'Add AmazingFeature'`)
4. Push al branch (`git push origin feature/AmazingFeature`)
5. Apri una Pull Request

### Linee guida

- Segui le convenzioni di codice C# esistenti
- Aggiungi unit test per nuove funzionalità
- Aggiorna la documentazione
- Testa su Windows, Linux e macOS se possibile

## 📄 Licenza

Questo progetto è distribuito sotto licenza MIT. Vedi file `LICENSE` per dettagli.

## 🙏 Ringraziamenti

- [System.CommandLine](https://github.com/dotnet/command-line-api) - CLI framework
- [Microsoft.Extensions.Logging](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging) - Logging
- Tutti i contributori del progetto

## 📞 Supporto

- 📧 Email: support@testrunner.dev
- 🐛 Issues: [GitHub Issues](https://github.com/yourusername/TestRunner/issues)
- 💬 Discussions: [GitHub Discussions](https://github.com/yourusername/TestRunner/discussions)
- 📖 Wiki: [Project Wiki](https://github.com/yourusername/TestRunner/wiki)

---

**Made with ❤️ by the TestRunner Team**
