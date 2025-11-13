# TestRunner.Web - Dashboard Blazor

Dashboard web interattiva per TestRunner con API REST e aggiornamenti real-time.

## 🌟 Caratteristiche

### Dashboard Web
- **Real-time Updates**: Visualizza l'esecuzione dei test in tempo reale con SignalR
- **Gestione Configurazioni**: Crea, modifica ed elimina configurazioni
- **Auto-Detection**: Rileva automaticamente progetti nella directory
- **Esecuzione Test**: Avvia test dalla UI con feedback live
- **Storico**: Visualizza esecuzioni precedenti
- **Report Interattivi**: Grafici e statistiche

### API REST
- **RESTful API**: Integrazione con sistemi esterni
- **Swagger/OpenAPI**: Documentazione API automatica
- **CORS**: Configurabile per chiamate cross-origin

### SignalR Hub
- **Real-time Notifications**: Aggiornamenti istantanei
- **Progress Tracking**: Percentuale completamento
- **Command Output**: Stream output comandi in tempo reale

## 🏗️ Architettura

```
TestRunner.Web/
├── Program.cs                          # Entry point, DI setup
├── TestRunner.Web.csproj              # Project file
│
├── Controllers/                        # REST API Controllers
│   ├── TestRunnerController.cs        # Test execution API
│   └── ConfigurationController.cs     # Configuration management API
│
├── Services/                           # Business logic
│   ├── TestRunnerHub.cs               # SignalR hub for real-time
│   ├── TestExecutionService.cs        # Test execution with notifications
│   └── ConfigurationService.cs        # Configuration management
│
├── Pages/                              # Blazor pages
│   ├── _Host.cshtml                   # Host page
│   ├── Index.razor                    # Dashboard home
│   ├── Configurations.razor           # Config management
│   ├── RunTests.razor                 # Execute tests
│   ├── History.razor                  # Execution history
│   └── Reports.razor                  # View reports
│
├── Components/                         # Reusable Blazor components
│   ├── ProjectCard.razor              # Project display card
│   ├── TestProgress.razor             # Progress indicator
│   ├── ExecutionSummary.razor         # Results summary
│   └── ConfigEditor.razor             # JSON config editor
│
└── wwwroot/                           # Static files
    ├── css/
    │   └── site.css                   # Custom styles
    └── js/
        └── signalr-client.js          # SignalR client