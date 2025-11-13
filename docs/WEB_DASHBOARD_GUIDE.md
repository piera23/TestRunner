# TestRunner Web Dashboard - Guida Completa

Guida completa alla dashboard web Blazor per TestRunner con API REST integrate.

## 📋 Indice

- [Panoramica](#panoramica)
- [Architettura](#architettura)
- [Setup e Installazione](#setup-e-installazione)
- [API REST](#api-rest)
- [Dashboard UI](#dashboard-ui)
- [Real-time Updates](#real-time-updates)
- [Completamento Progetto](#completamento-progetto)
- [Deployment](#deployment)

## 🌟 Panoramica

Il progetto **TestRunner.Web** fornisce:

### ✅ Funzionalità Implementate

1. **API REST completa**
   - `/api/testrunner/*` - Gestione esecuzioni
   - `/api/configuration/*` - Gestione configurazioni

2. **SignalR Hub**
   - Real-time notifications
   - Progress tracking
   - Command output streaming

3. **Servizi Backend**
   - `TestExecutionService` - Esecuzione con notifiche
   - `ConfigurationService` - Gestione configurazioni
   - `TestRunnerHub` - Comunicazione real-time

4. **Infrastruttura**
   - Dependency Injection configurato
   - Logging strutturato
   - CORS policy

### 📝 Da Completare

Le pagine Blazor UI devono essere create. Di seguito le istruzioni complete.

## 🏗️ Architettura

```
┌──────────────────────────────────────────────────────┐
│                   Browser Client                      │
│  ┌────────────┐  ┌──────────────┐  ┌─────────────┐ │
│  │ Blazor UI  │  │ SignalR.js   │  │   REST API  │ │
│  └─────┬──────┘  └──────┬───────┘  └──────┬──────┘ │
└────────┼─────────────────┼──────────────────┼────────┘
         │                 │                  │
         │ WebSocket      │ WS               │ HTTP
         │                 │                  │
┌────────┼─────────────────┼──────────────────┼────────┐
│        │                 │                  │         │
│  ┌─────▼──────┐    ┌────▼─────────┐  ┌────▼──────┐ │
│  │ Blazor Hub │    │TestRunnerHub │  │Controllers│ │
│  └─────┬──────┘    └────┬─────────┘  └────┬──────┘ │
│        │                │                  │         │
│  ┌─────▼───────────────▼──────────────────▼──────┐ │
│  │          Services (Business Logic)            │ │
│  │  - TestExecutionService                       │ │
│  │  - ConfigurationService                       │ │
│  │  - TestRunnerHub                              │ │
│  └───────────────────┬───────────────────────────┘ │
│                      │                              │
│  ┌───────────────────▼───────────────────────────┐ │
│  │     TestRunner Core (CLI Services)            │ │
│  │  - ConfigService                              │ │
│  │  - ProjectDetector                            │ │
│  │  - TestExecutor                               │ │
│  │  - ReportGenerator                            │ │
│  └───────────────────────────────────────────────┘ │
│                  TestRunner.Web                     │
└─────────────────────────────────────────────────────┘
```

## 🚀 Setup e Installazione

### 1. Prerequisiti

```bash
# .NET 9.0 SDK
dotnet --version  # >= 9.0.0

# Node.js (per client JavaScript, opzionale)
node --version
```

### 2. Build del Progetto

```bash
cd /home/user/TestRunner

# Restore dependencies
dotnet restore TestRunner.Web/TestRunner.Web.csproj

# Build
dotnet build TestRunner.Web/TestRunner.Web.csproj

# Run
dotnet run --project TestRunner.Web/TestRunner.Web.csproj
```

### 3. Accesso

```
http://localhost:5000
https://localhost:5001
```

## 📡 API REST

### Endpoints TestRunner

#### GET `/api/testrunner/status`
Ottieni stato esecuzione corrente.

**Response:**
```json
{
  "isRunning": false,
  "currentExecution": null
}
```

#### POST `/api/testrunner/run`
Avvia esecuzione test.

**Request:**
```json
{
  "configName": "production",
  "projects": ["frontend", "backend"],
  "tags": ["critical"]
}
```

**Response:**
```json
{
  "message": "Test execution started"
}
```

#### POST `/api/testrunner/cancel`
Annulla esecuzione corrente.

#### GET `/api/testrunner/history`
Ottieni storico esecuzioni.

### Endpoints Configuration

#### GET `/api/configuration`
Lista tutte le configurazioni.

#### GET `/api/configuration/{name}`
Ottieni configurazione specifica.

#### PUT `/api/configuration/{name}`
Salva/aggiorna configurazione.

**Request Body:** TestRunnerConfig JSON

#### DELETE `/api/configuration/{name}`
Elimina configurazione.

#### POST `/api/configuration/auto-detect`
Crea configurazione con auto-detection.

**Request:**
```json
{
  "name": "my-project",
  "projectPath": "/path/to/projects"
}
```

#### POST `/api/configuration/detect-projects`
Rileva progetti in directory.

**Request:**
```json
{
  "path": "/path/to/scan",
  "maxDepth": 3
}
```

#### POST `/api/configuration/validate`
Valida configurazione.

**Request Body:** TestRunnerConfig JSON

**Response:**
```json
{
  "isValid": true,
  "message": "Configuration is valid"
}
```

## 🔄 Real-time Updates con SignalR

### Connessione JavaScript

```javascript
// Connect to SignalR hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/testrunner-hub")
    .build();

// Handle events
connection.on("ExecutionStarted", (timestamp, projectCount) => {
    console.log(`Execution started: ${projectCount} projects`);
});

connection.on("ProjectStarted", (name, type) => {
    console.log(`Project started: ${name} (${type})`);
});

connection.on("ProjectCompleted", (name, status, duration, isSuccess) => {
    console.log(`Project completed: ${name} - ${status} (${duration}s)`);
});

connection.on("ExecutionCompleted", (isSuccess, duration) => {
    console.log(`Execution completed: ${isSuccess} (${duration}s)`);
});

// Start connection
await connection.start();
```

### Eventi SignalR

| Evento | Parametri | Descrizione |
|--------|-----------|-------------|
| `ExecutionStarted` | timestamp, projectCount | Esecuzione iniziata |
| `ProjectStarted` | name, type | Progetto iniziato |
| `CommandStarted` | projectName, command | Comando iniziato |
| `CommandOutput` | projectName, command, output | Output comando |
| `CommandCompleted` | projectName, command, exitCode | Comando completato |
| `ProjectCompleted` | name, status, duration, isSuccess | Progetto completato |
| `ExecutionCompleted` | isSuccess, duration | Esecuzione completata |
| `ExecutionCancelled` | - | Esecuzione annullata |

## 🎨 Dashboard UI - Completamento

### Pagine da Creare

#### 1. `_Host.cshtml` - Pagina Host

```html
@page "/"
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@{
    Layout = null;
}

<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>TestRunner Dashboard</title>
    <base href="~/" />
    <link rel="stylesheet" href="css/bootstrap/bootstrap.min.css" />
    <link href="css/site.css" rel="stylesheet" />
    <component type="typeof(HeadOutlet)" render-mode="ServerPrerendered" />
</head>
<body>
    <component type="typeof(App)" render-mode="ServerPrerendered" />

    <div id="blazor-error-ui">
        An unhandled error has occurred.
        <a href="" class="reload">Reload</a>
        <a class="dismiss">🗙</a>
    </div>

    <script src="_framework/blazor.server.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/signalr@latest/dist/browser/signalr.min.js"></script>
</body>
</html>
```

#### 2. `App.razor` - Root Component

```razor
<Router AppAssembly="@typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
        <FocusOnNavigate RouteData="@routeData" Selector="h1" />
    </Found>
    <NotFound>
        <PageTitle>Not found</PageTitle>
        <LayoutView Layout="@typeof(MainLayout)">
            <p role="alert">Sorry, there's nothing at this address.</p>
        </LayoutView>
    </NotFound>
</Router>
```

#### 3. `MainLayout.razor` - Layout Principale

```razor
@inherits LayoutComponentBase

<div class="page">
    <div class="sidebar">
        <NavMenu />
    </div>

    <main>
        <div class="top-row px-4">
            <a href="https://docs.microsoft.com/aspnet/" target="_blank">About</a>
        </div>

        <article class="content px-4">
            @Body
        </article>
    </main>
</div>
```

#### 4. `NavMenu.razor` - Menu Navigazione

```razor
<div class="top-row ps-3 navbar navbar-dark">
    <div class="container-fluid">
        <a class="navbar-brand" href="">TestRunner</a>
        <button title="Navigation menu" class="navbar-toggler" @onclick="ToggleNavMenu">
            <span class="navbar-toggler-icon"></span>
        </button>
    </div>
</div>

<div class="@NavMenuCssClass nav-scrollable" @onclick="ToggleNavMenu">
    <nav class="flex-column">
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="" Match="NavLinkMatch.All">
                <span class="oi oi-home" aria-hidden="true"></span> Dashboard
            </NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="configurations">
                <span class="oi oi-list-rich" aria-hidden="true"></span> Configurations
            </NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="run">
                <span class="oi oi-play-circle" aria-hidden="true"></span> Run Tests
            </NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="history">
                <span class="oi oi-clock" aria-hidden="true"></span> History
            </NavLink>
        </div>
        <div class="nav-item px-3">
            <NavLink class="nav-link" href="reports">
                <span class="oi oi-bar-chart" aria-hidden="true"></span> Reports
            </NavLink>
        </div>
    </nav>
</div>

@code {
    private bool collapseNavMenu = true;

    private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

    private void ToggleNavMenu()
    {
        collapseNavMenu = !collapseNavMenu;
    }
}
```

#### 5. `Index.razor` - Dashboard Home

```razor
@page "/"
@using TestRunner.Web.Services
@inject TestExecutionService ExecutionService
@inject ConfigurationService ConfigService

<PageTitle>Dashboard</PageTitle>

<h1>TestRunner Dashboard</h1>

<div class="row">
    <div class="col-md-3">
        <div class="card">
            <div class="card-body">
                <h5 class="card-title">Total Configurations</h5>
                <p class="card-text display-4">@configCount</p>
            </div>
        </div>
    </div>
    <div class="col-md-3">
        <div class="card">
            <div class="card-body">
                <h5 class="card-title">Execution Status</h5>
                <p class="card-text display-4">
                    @if (ExecutionService.IsRunning)
                    {
                        <span class="badge bg-primary">Running</span>
                    }
                    else
                    {
                        <span class="badge bg-success">Idle</span>
                    }
                </p>
            </div>
        </div>
    </div>
</div>

@code {
    private int configCount = 0;

    protected override async Task OnInitializedAsync()
    {
        var configs = await ConfigService.GetAllConfigurationsAsync();
        configCount = configs.Count;
    }
}
```

#### 6. `RunTests.razor` - Esegui Test

```razor
@page "/run"
@using TestRunner.Web.Services
@using TestRunner.Models
@inject TestExecutionService ExecutionService
@inject ConfigurationService ConfigService
@inject NavigationManager Navigation

<PageTitle>Run Tests</PageTitle>

<h1>Run Tests</h1>

@if (ExecutionService.IsRunning)
{
    <div class="alert alert-info">
        <h4>Execution in Progress...</h4>
        <div class="progress">
            <div class="progress-bar progress-bar-striped progress-bar-animated"
                 role="progressbar"
                 style="width: @progress%">
                @progress%
            </div>
        </div>
    </div>
}
else
{
    <div class="card">
        <div class="card-body">
            <h5 class="card-title">Configuration</h5>

            <div class="mb-3">
                <label class="form-label">Select Configuration</label>
                <select class="form-select" @bind="selectedConfig">
                    @foreach (var config in configurations)
                    {
                        <option value="@config.Name">@config.Name (@config.ProjectCount projects)</option>
                    }
                </select>
            </div>

            <button class="btn btn-primary" @onclick="RunTests">
                <span class="oi oi-play-circle"></span> Run Tests
            </button>
        </div>
    </div>
}

@code {
    private List<ConfigInfo> configurations = new();
    private string selectedConfig = "";
    private int progress = 0;

    protected override async Task OnInitializedAsync()
    {
        configurations = await ConfigService.GetAllConfigurationsAsync();
        if (configurations.Any())
        {
            selectedConfig = configurations.First().Name;
        }
    }

    private async Task RunTests()
    {
        var config = await ConfigService.GetConfigurationAsync(selectedConfig);
        if (config != null)
        {
            await ExecutionService.ExecuteTestsAsync(config);
        }
    }
}
```

### CSS Styling

Creare `/wwwroot/css/site.css`:

```css
/* Main Layout */
.page {
    position: relative;
    display: flex;
    flex-direction: column;
}

.main {
    flex: 1;
}

.sidebar {
    background-color: #2c3e50;
    width: 250px;
}

.top-row {
    background-color: #34495e;
    border-bottom: 1px solid #d6d5d5;
    justify-content: flex-end;
    height: 3.5rem;
    display: flex;
    align-items: center;
}

/* Cards */
.card {
    margin-bottom: 1rem;
    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

/* Progress */
.progress {
    height: 30px;
    font-size: 1rem;
}

/* Badges */
.badge {
    font-size: 1.5rem;
    padding: 0.5rem 1rem;
}

/* Real-time updates */
.live-update {
    animation: pulse 2s infinite;
}

@keyframes pulse {
    0% { opacity: 1; }
    50% { opacity: 0.5; }
    100% { opacity: 1; }
}
```

## 🔧 Completamento Progetto

### Passi per Completare

1. **Creare le pagine Blazor** (.razor files) come specificato sopra
2. **Aggiungere Bootstrap CSS** in wwwroot/
3. **Implementare componenti riutilizzabili**:
   - `ProjectCard.razor` - Visualizza info progetto
   - `TestProgress.razor` - Barra progresso real-time
   - `ExecutionSummary.razor` - Riepilogo risultati
4. **Integrare SignalR client-side** per aggiornamenti real-time
5. **Aggiungere charts** con libreria tipo Chart.js o Blazor.Charts

### Librerie Consigliate

```xml
<PackageReference Include="Blazorise" Version="1.5.0" />
<PackageReference Include="Blazorise.Bootstrap5" Version="1.5.0" />
<PackageReference Include="Blazorise.Icons.FontAwesome" Version="1.5.0" />
<PackageReference Include="Blazor.Charts" Version="1.0.0" />
```

## 🚀 Deployment

### Development

```bash
dotnet run --project TestRunner.Web/TestRunner.Web.csproj
```

### Production Build

```bash
dotnet publish TestRunner.Web/TestRunner.Web.csproj \
  -c Release \
  -o ./publish

# Run published app
cd publish
dotnet TestRunner.Web.dll
```

### Docker

Creare `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["TestRunner.Web/TestRunner.Web.csproj", "TestRunner.Web/"]
COPY ["TestRunner/TestRunner.csproj", "TestRunner/"]
RUN dotnet restore "TestRunner.Web/TestRunner.Web.csproj"
COPY . .
WORKDIR "/src/TestRunner.Web"
RUN dotnet build "TestRunner.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TestRunner.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TestRunner.Web.dll"]
```

```bash
# Build image
docker build -t testrunner-web .

# Run container
docker run -d -p 5000:5000 --name testrunner testrunner-web
```

### IIS / Azure

- Pubblica con `dotnet publish`
- Configura Application Pool per .NET 9.0
- Abilita WebSocket per SignalR
- Configura CORS se necessario

## 📚 Risorse Aggiuntive

- [Blazor Documentation](https://docs.microsoft.com/en-us/aspnet/core/blazor/)
- [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/)
- [ASP.NET Core Web API](https://docs.microsoft.com/en-us/aspnet/core/web-api/)

## 🎯 Prossimi Passi

1. ✅ Backend API completo
2. ✅ SignalR Hub implementato
3. ✅ Servizi business logic
4. 🔲 Pagine Blazor UI
5. 🔲 Componenti riutilizzabili
6. 🔲 Charts e grafici
7. 🔲 Autenticazione/Autorizzazione
8. 🔲 Database per storico
9. 🔲 Notifiche (email, Slack)

---

**La struttura backend è completa e funzionante!** Basta completare le pagine Blazor UI seguendo gli esempi sopra.
