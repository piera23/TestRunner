🧪 TestRunner
Un semplice ed efficace tool a riga di comando per testare applicazioni web, app mobile e script Python/JavaScript. Sviluppato in .NET 9, può essere usato sia come applicazione standalone che integrato in progetti esistenti.
✨ Caratteristiche
🔍 Auto-rilevamento progetti: Rileva automaticamente React, Vue, Angular, React Native, Python, .NET e altri progetti
🚀 Esecuzione parallela: Esegue test di più progetti contemporaneamente
📊 Report multipli: Output console colorato, JSON, HTML e JUnit XML
⚙️ Configurazione flessibile: File JSON semplice da configurare
🎯 Filtri avanzati: Filtra per nome progetto o tag
📱 Cross-platform: Funziona su Windows, macOS e Linux
🔧 CI/CD ready: Perfetto per integrazione in pipeline
🚀 Quick Start
Installazione
bash
# Clone il repository
git clone https://github.com/your-repo/testrunner.git
cd testrunner

# Build dell'applicazione
dotnet build

# Oppure crea un eseguibile standalone
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
Primo utilizzo
bash
# 1. Inizializza configurazione con auto-rilevamento
testrunner init --auto --path ./my-projects

# 2. Esegui tutti i test
testrunner run

# 3. Genera report HTML
testrunner run --report results.html --format html
📋 Comandi Disponibili
init - Inizializza configurazione
bash
# Crea configurazione di default
testrunner init

# Auto-rileva progetti nella directory corrente
testrunner init --auto

# Specifica path custom
testrunner init --auto --path ./src --config my-tests.json

# Forza sovrascrittura
testrunner init --force
detect - Rileva progetti
bash
# Rileva progetti nella directory corrente
testrunner detect

# Salva risultati in configurazione
testrunner detect --output detected-config.json

# Specifica profondità di scansione
testrunner detect --path ./src --depth 5
run - Esegui test
bash
# Esegui tutti i test
testrunner run

# Esegui progetti specifici
testrunner run --projects web-app,mobile-app

# Filtra per tag
testrunner run --tags frontend,critical

# Esecuzione parallela
testrunner run --parallel

# Genera report
testrunner run --report results.json --format json

# Dry run (mostra cosa verrebbe eseguito)
testrunner run --dry-run

# Output verboso
testrunner run --verbose
validate - Valida configurazione
bash
# Valida configurazione di default
testrunner validate

# Valida file specifico
testrunner validate --config my-config.json
⚙️ Configurazione
Esempio configurazione base
json
{
  "projects": [
    {
      "name": "web-frontend",
      "path": "./frontend",
      "type": "WebApp",
      "commands": ["npm test", "npm run build"],
      "tags": ["frontend", "critical"],
      "timeout_minutes": 10,
      "enabled": true
    },
    {
      "name": "mobile-app",
      "path": "./mobile",
      "type": "MobileApp", 
      "commands": ["npm test"],
      "tags": ["mobile"],
      "timeout_minutes": 15,
      "enabled": true
    },
    {
      "name": "python-scripts",
      "path": "./scripts",
      "type": "PythonScript",
      "commands": ["python -m pytest", "flake8 ."],
      "tags": ["backend", "scripts"],
      "timeout_minutes": 5,
      "enabled": true
    }
  ],
  "parallel_execution": true,
  "max_parallel_projects": 4,
  "stop_on_first_failure": false,
  "global_timeout_minutes": 60,
  "output_format": "Console"
}
Configurazione avanzata
json
{
  "projects": [
    {
      "name": "api-server",
      "path": "./api",
      "type": "JavaScriptApp",
      "commands": ["npm test", "npm run test:integration"],
      "pre_commands": ["npm install", "docker-compose up -d db"],
      "post_commands": ["docker-compose down"],
      "environment": {
        "NODE_ENV": "test",
        "DB_URL": "postgresql://test:test@localhost:5432/testdb"
      },
      "working_directory": "./api",
      "tags": ["api", "integration"],
      "timeout_minutes": 20,
      "enabled": true
    }
  ]
}
🎯 Tipi di Progetto Supportati
Tipo	Rilevamento Automatico	Comandi Default
WebApp	package.json + React/Vue/Angular	npm test, npm run build
MobileApp	package.json + React Native	npm test, npx tsc --noEmit
PythonScript	requirements.txt, *.py	pytest, flake8
JavaScriptApp	package.json generico	npm test
DotNetApp	*.csproj, *.sln	dotnet test, dotnet build
Custom	Configurazione manuale	Comandi personalizzati
📊 Formati di Report
Console (Default)
Output colorato e user-friendly direttamente nel terminale.
JSON
bash
testrunner run --report results.json --format json
Perfetto per integrazione CI/CD e analisi automatica.
HTML
bash
testrunner run --report results.html --format html
Report interattivo con grafici e dettagli espandibili.
JUnit XML
bash
testrunner run --report results.xml --format xml
Compatibile con Jenkins, Azure DevOps, GitHub Actions.
🔧 Integrazione CI/CD
GitHub Actions
yaml
name: Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .
