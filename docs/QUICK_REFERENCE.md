# TestRunner - Quick Reference

Guida rapida di riferimento per TestRunner.

## 🚀 Comandi Base

```bash
# Inizializza configurazione
testrunner init

# Auto-rileva progetti
testrunner init --auto

# Rileva progetti in directory
testrunner detect --path ./workspace

# Valida configurazione
testrunner validate

# Esegui test
testrunner run

# Help
testrunner --help
testrunner run --help
```

## 📝 Opzioni Comuni

### init
```bash
--path <path>      # Directory da scansionare (default: .)
--config <file>    # File configurazione (default: testrunner.json)
--auto             # Auto-detection
--force            # Sovrascrivi esistente
```

### detect
```bash
--path <path>      # Directory da scansionare
--depth <n>        # Profondità scansione (default: 3)
--output <file>    # Salva risultati
```

### validate
```bash
--config <file>    # File da validare
```

### run
```bash
--config <file>            # File configurazione
--projects <names...>      # Progetti specifici
--tags <tags...>           # Filtra per tag
--parallel                 # Esecuzione parallela
--report <file>           # File report
--format <format>         # Formato (Console|Json|Xml|Html|Markdown|Csv)
--verbose                  # Output dettagliato
--dry-run                  # Simula esecuzione
```

## ⚙️ Configurazione Minima

```json
{
  "projects": [
    {
      "name": "my-project",
      "path": "./",
      "type": "WebApp",
      "commands": ["npm test"]
    }
  ]
}
```

## 📊 Configurazione Completa

```json
{
  "name": "Test Suite",
  "parallel_execution": true,
  "max_parallel_projects": 4,
  "global_timeout_minutes": 60,
  "stop_on_first_failure": false,

  "global_environment": {
    "NODE_ENV": "test"
  },

  "projects": [
    {
      "name": "frontend",
      "path": "./client",
      "type": "WebApp",
      "description": "Frontend app",
      "enabled": true,
      "priority": 1,

      "commands": [
        "npm test",
        "npm run build"
      ],

      "pre_commands": [
        "npm ci"
      ],

      "post_commands": [
        "npm run cleanup"
      ],

      "environment": {
        "PORT": "3000"
      },

      "tags": ["frontend", "critical"],

      "timeout_minutes": 10,
      "working_directory": null,

      "retry_count": 2,
      "retry_delay_seconds": 5,

      "ignore_exit_codes": [],
      "expected_output_patterns": [],
      "forbidden_output_patterns": []
    }
  ]
}
```

## 🏷️ Tipi di Progetto

| Tipo | File Marker | Comandi Default |
|------|------------|----------------|
| `WebApp` | package.json | npm test, npm run build |
| `MobileApp` | package.json + metro.config.js | npm test |
| `JavaScriptApp` | package.json | npm test |
| `PythonScript` | requirements.txt, *.py | pytest, flake8 |
| `DotNetApp` | *.csproj, *.sln | dotnet test, dotnet build |
| `JavaApp` | pom.xml, build.gradle | mvn test, gradle test |
| `GoApp` | go.mod | go test ./... |
| `RustApp` | Cargo.toml | cargo test |
| `PhpApp` | composer.json | phpunit |
| `RubyApp` | Gemfile | rspec |
| `DockerApp` | Dockerfile | docker-compose build |
| `Custom` | - | Definiti dall'utente |

## 📈 Formati Output

```bash
# Console (default)
testrunner run

# JSON
testrunner run --report results.json --format Json

# JUnit XML
testrunner run --report results.xml --format Xml

# HTML
testrunner run --report results.html --format Html

# Markdown
testrunner run --report results.md --format Markdown

# CSV
testrunner run --report results.csv --format Csv
```

## 🎯 Esempi Comuni

### Test singolo progetto
```bash
testrunner run --projects frontend
```

### Test con tag specifici
```bash
testrunner run --tags critical backend
```

### Test paralleli con report HTML
```bash
testrunner run --parallel --report results.html --format Html
```

### Dry run per debug
```bash
testrunner run --dry-run --verbose
```

### Test con configurazione custom
```bash
testrunner run --config production.json
```

### Pipeline CI/CD
```bash
testrunner run \
  --parallel \
  --tags ci \
  --report test-results.xml \
  --format Xml \
  --stop-on-first-failure
```

## 🔧 Variabili d'Ambiente

### Globali (applicate a tutti)
```json
{
  "global_environment": {
    "CI": "true",
    "NODE_ENV": "test",
    "LOG_LEVEL": "info"
  }
}
```

### Per Progetto (override globali)
```json
{
  "projects": [{
    "environment": {
      "PORT": "3000",
      "API_URL": "http://localhost:3001"
    }
  }]
}
```

## 🏃 Pre/Post Commands

```json
{
  "projects": [{
    "pre_commands": [
      "npm ci",
      "docker-compose up -d"
    ],
    "commands": [
      "npm test"
    ],
    "post_commands": [
      "docker-compose down",
      "rm -rf temp/"
    ]
  }]
}
```

## 🔄 Retry Logic

```json
{
  "projects": [{
    "retry_count": 2,
    "retry_delay_seconds": 5
  }]
}
```

## ⏱️ Timeout

```json
{
  "global_timeout_minutes": 60,
  "projects": [{
    "timeout_minutes": 10
  }]
}
```

## 🔍 Filtri

### Per nome progetto
```bash
testrunner run --projects frontend backend api
```

### Per tag
```bash
testrunner run --tags critical unit-test
```

### Combinati
```bash
testrunner run --projects frontend --tags critical
```

## 🎨 Output Console

```
╔══════════════════════════════════════════════════════════════╗
║                        TEST RESULTS                          ║
╚══════════════════════════════════════════════════════════════╝

🕐 Execution Time: 00:02:34
📊 Total Projects: 5
✅ Passed: 4
❌ Failed: 1
⏭️  Skipped: 0
📈 Success Rate: 80.0%

Overall Status: 💥 FAILURE
```

## 🐛 Debug

```bash
# Verbose logging
testrunner run --verbose

# Dry run (no execution)
testrunner run --dry-run

# Validate config
testrunner validate

# Test single project
testrunner run --projects problematic-project --verbose
```

## 📁 Struttura File

```
project/
├── testrunner.json         # Configurazione
├── frontend/               # Progetto 1
│   ├── package.json
│   └── ...
├── backend/                # Progetto 2
│   ├── package.json
│   └── ...
└── results/               # Report (opzionale)
    ├── results.html
    └── results.json
```

## 🌐 Variabili Shell

```json
{
  "environment": {
    "PATH": "/usr/local/bin:$PATH",
    "HOME": "$HOME",
    "USER": "${USER}"
  }
}
```

## 🔐 Sicurezza

- ✅ Comandi da configurazione trusted
- ✅ No input utente non validato
- ✅ Timeout per prevenire hang
- ✅ Validazione path
- ✅ Proper escaping shell commands

## 💡 Best Practices

1. **Tag meaningful**: `critical`, `unit`, `integration`, `e2e`
2. **Timeout realistici**: Unit 5-10min, Integration 10-20min
3. **Pre-commands per setup**: Install deps, start services
4. **Post-commands per cleanup**: Stop services, remove temp files
5. **Parallel per velocità**: Su progetti indipendenti
6. **Sequential per ordine**: Quando c'è dipendenza
7. **Retry per test flaky**: E2E, network tests
8. **Environment variables**: Per configurazioni differenti

## 🚨 Troubleshooting

| Problema | Soluzione |
|----------|-----------|
| Config non trovata | `testrunner init --auto` |
| Directory non esiste | Controlla `path` in config |
| Timeout | Aumenta `timeout_minutes` |
| Permission denied | `chmod +x script.sh` |
| Test falliscono | `--verbose` per debug |
| Output mancante | `--verbose` e controlla redirect |

## 📚 Links Utili

- [README completo](../README.md)
- [Architettura](ARCHITECTURE.md)
- [Contribuire](../CONTRIBUTING.md)
- [Changelog](../CHANGELOG.md)

## 🆘 Help

```bash
testrunner --help
testrunner init --help
testrunner detect --help
testrunner validate --help
testrunner run --help
```

---

**Tip**: Usa tab completion se abilitato! 🎯
