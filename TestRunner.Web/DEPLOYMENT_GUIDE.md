# TestRunner.Web - Deployment & Usage Guide

## 🚀 Quick Start

### Prerequisites

- **.NET 9.0 SDK** or later
- A modern web browser (Chrome, Firefox, Edge, Safari)
- Optional: Docker for containerized deployment

### Running Locally

1. **Navigate to the project directory:**
   ```bash
   cd TestRunner.Web
   ```

2. **Run the application:**
   ```bash
   dotnet run
   ```

3. **Open your browser:**
   - HTTP: http://localhost:5000
   - HTTPS: https://localhost:5001

The dashboard will be accessible immediately!

## 📁 Configuration

### Application Settings

Edit `appsettings.json` to configure:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.AspNetCore.SignalR": "Information"
    }
  },
  "AllowedHosts": "*",
  "ConfigDirectory": "./configs",
  "Urls": "http://localhost:5000;https://localhost:5001"
}
```

#### Key Settings:

- **ConfigDirectory**: Path to store test configuration files (default: `./configs`)
- **Urls**: Addresses the application listens on
- **Logging**: Configure logging verbosity for different components

### Environment Variables

You can override settings using environment variables:

```bash
export ConfigDirectory="/path/to/configs"
export ASPNETCORE_URLS="http://localhost:8080"
dotnet run
```

## 🌐 Production Deployment

### Option 1: Direct Hosting

1. **Publish the application:**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Run the published application:**
   ```bash
   cd publish
   ./TestRunner.Web
   ```

3. **Configure for production:**
   - Set `ASPNETCORE_ENVIRONMENT=Production`
   - Configure HTTPS certificates
   - Set up reverse proxy (nginx/Apache) if needed

### Option 2: Docker Deployment

1. **Create a Dockerfile:**
   ```dockerfile
   FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
   WORKDIR /app
   EXPOSE 80
   EXPOSE 443

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

2. **Build the Docker image:**
   ```bash
   docker build -t testrunner-web:latest .
   ```

3. **Run the container:**
   ```bash
   docker run -d -p 8080:80 \
     -v /path/to/configs:/app/configs \
     -e ConfigDirectory=/app/configs \
     testrunner-web:latest
   ```

### Option 3: Azure App Service

1. **Create Azure resources:**
   ```bash
   az group create --name TestRunnerRG --location eastus
   az appservice plan create --name TestRunnerPlan --resource-group TestRunnerRG --sku B1
   az webapp create --name testrunner-web --resource-group TestRunnerRG --plan TestRunnerPlan
   ```

2. **Deploy the application:**
   ```bash
   dotnet publish -c Release
   cd bin/Release/net9.0/publish
   zip -r publish.zip .
   az webapp deployment source config-zip --resource-group TestRunnerRG --name testrunner-web --src publish.zip
   ```

### Option 4: Linux Service (systemd)

1. **Create a service file:** `/etc/systemd/system/testrunner-web.service`
   ```ini
   [Unit]
   Description=TestRunner Web Dashboard
   After=network.target

   [Service]
   Type=notify
   WorkingDirectory=/opt/testrunner-web
   ExecStart=/usr/bin/dotnet /opt/testrunner-web/TestRunner.Web.dll
   Restart=always
   RestartSec=10
   KillSignal=SIGINT
   SyslogIdentifier=testrunner-web
   User=www-data
   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=ConfigDirectory=/var/lib/testrunner/configs

   [Install]
   WantedBy=multi-user.target
   ```

2. **Enable and start the service:**
   ```bash
   sudo systemctl enable testrunner-web
   sudo systemctl start testrunner-web
   sudo systemctl status testrunner-web
   ```

## 🔒 Security Configuration

### HTTPS Setup

1. **Development Certificate:**
   ```bash
   dotnet dev-certs https --trust
   ```

2. **Production Certificate (Let's Encrypt):**
   ```bash
   sudo apt-get install certbot
   sudo certbot certonly --standalone -d yourdomain.com
   ```

3. **Configure HTTPS in appsettings.json:**
   ```json
   {
     "Kestrel": {
       "Endpoints": {
         "Https": {
           "Url": "https://*:443",
           "Certificate": {
             "Path": "/etc/letsencrypt/live/yourdomain.com/fullchain.pem",
             "KeyPath": "/etc/letsencrypt/live/yourdomain.com/privkey.pem"
           }
         }
       }
     }
   }
   ```

### Authentication (Optional)

To add authentication to the dashboard:

1. **Install authentication packages:**
   ```bash
   dotnet add package Microsoft.AspNetCore.Authentication.OpenIdConnect
   ```

2. **Configure in Program.cs:**
   ```csharp
   builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
       .AddOpenIdConnect(options => {
           // Configure your identity provider
       });
   ```

3. **Add authorization attributes:**
   ```csharp
   [Authorize]
   public class ConfigurationController : ControllerBase
   {
       // ...
   }
   ```

## 📊 Monitoring & Logging

### Application Insights (Azure)

1. **Add Application Insights:**
   ```bash
   dotnet add package Microsoft.ApplicationInsights.AspNetCore
   ```

2. **Configure in Program.cs:**
   ```csharp
   builder.Services.AddApplicationInsightsTelemetry();
   ```

### Logging to File

1. **Add Serilog:**
   ```bash
   dotnet add package Serilog.AspNetCore
   dotnet add package Serilog.Sinks.File
   ```

2. **Configure in Program.cs:**
   ```csharp
   builder.Host.UseSerilog((context, config) =>
   {
       config.WriteTo.Console()
             .WriteTo.File("logs/testrunner-.log", rollingInterval: RollingInterval.Day);
   });
   ```

### Health Checks

The application includes built-in health check endpoints:

- **Health Status**: `/health`
- **Ready Status**: `/health/ready`

Configure monitoring tools to poll these endpoints.

## 🌍 Reverse Proxy Configuration

### Nginx

```nginx
server {
    listen 80;
    server_name yourdomain.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### Apache

```apache
<VirtualHost *:80>
    ServerName yourdomain.com
    ProxyPreserveHost On
    ProxyPass / http://localhost:5000/
    ProxyPassReverse / http://localhost:5000/

    RewriteEngine on
    RewriteCond %{HTTP:Upgrade} websocket [NC]
    RewriteCond %{HTTP:Connection} upgrade [NC]
    RewriteRule ^/?(.*) "ws://localhost:5000/$1" [P,L]
</VirtualHost>
```

## 📱 Usage Guide

### Dashboard Pages

#### 1. Home Dashboard (`/`)
- Overview of test execution status
- Quick statistics (configurations, recent runs)
- Recent executions list
- Quick actions (run tests, create config)

#### 2. Configurations (`/configurations`)
- View all test configurations
- Create new configurations
- Edit existing configurations
- Delete configurations
- Auto-detect projects in directories

#### 3. Run Tests (`/run`)
- Select configuration to run
- Filter by projects or tags
- Real-time execution progress
- Live console output
- Execution results summary

#### 4. Projects (`/projects`)
- View all detected projects
- Filter by configuration or type
- Enable/disable projects
- Search projects
- Quick project execution

#### 5. History (`/history`)
- View past test executions
- Filter by configuration, status, or date
- Execution statistics
- Success rate trends
- Detailed execution results

### API Usage

#### Run Tests via API

```bash
curl -X POST http://localhost:5000/api/testrunner/run \
  -H "Content-Type: application/json" \
  -d '{
    "configName": "production",
    "projects": ["ProjectA", "ProjectB"],
    "tags": ["critical"]
  }'
```

#### Get Execution Status

```bash
curl http://localhost:5000/api/testrunner/status
```

#### Create Configuration

```bash
curl -X PUT http://localhost:5000/api/configuration/my-config \
  -H "Content-Type: application/json" \
  -d @config.json
```

#### Auto-Detect Projects

```bash
curl -X POST http://localhost:5000/api/configuration/auto-detect \
  -H "Content-Type: application/json" \
  -d '{
    "name": "auto-config",
    "projectPath": "/path/to/projects"
  }'
```

### SignalR Real-Time Updates

Connect to the SignalR hub for real-time notifications:

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/testrunner-hub")
    .withAutomaticReconnect()
    .build();

// Listen for events
connection.on("ExecutionStarted", (timestamp, projectCount) => {
    console.log(`Execution started: ${projectCount} projects`);
});

connection.on("ProjectCompleted", (name, status, duration) => {
    console.log(`Project ${name} completed: ${status} (${duration}s)`);
});

// Start connection
await connection.start();
```

## 🔧 Troubleshooting

### Port Already in Use

If ports 5000/5001 are already in use:

```bash
export ASPNETCORE_URLS="http://localhost:8080;https://localhost:8081"
dotnet run
```

### SignalR Connection Fails

1. Check firewall settings
2. Verify WebSocket support is enabled
3. Check reverse proxy WebSocket configuration
4. Review browser console for errors

### Configuration Not Loading

1. Verify `ConfigDirectory` path exists
2. Check file permissions
3. Validate JSON syntax in config files
4. Review application logs

### Performance Issues

1. **Enable Response Compression:**
   ```csharp
   builder.Services.AddResponseCompression();
   ```

2. **Configure Response Caching:**
   ```csharp
   builder.Services.AddResponseCaching();
   ```

3. **Optimize SignalR:**
   ```csharp
   builder.Services.AddSignalR(options => {
       options.EnableDetailedErrors = false; // Production
       options.MaximumReceiveMessageSize = 102400;
   });
   ```

## 🧪 Testing the Deployment

### Health Check

```bash
curl http://localhost:5000/health
```

Expected response: `Healthy`

### API Test

```bash
curl http://localhost:5000/api/configuration
```

Expected: List of configurations (may be empty initially)

### SignalR Test

Open browser developer console and run:

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/testrunner-hub")
    .build();
connection.start().then(() => console.log("Connected!"));
```

Expected: "Connected!" message

## 📚 Additional Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Blazor Documentation](https://docs.microsoft.com/aspnet/core/blazor)
- [SignalR Documentation](https://docs.microsoft.com/aspnet/core/signalr)
- [.NET Deployment Guide](https://docs.microsoft.com/dotnet/core/deploying)

## 🆘 Support

For issues and questions:
- Check the logs in the application directory
- Review the main README.md
- Check the ARCHITECTURE.md for technical details
- Open an issue on the project repository

## 🎯 Performance Tips

1. **Use Production Build:**
   ```bash
   dotnet run -c Release
   ```

2. **Enable Caching:** Add response caching for API endpoints

3. **Optimize SignalR:** Configure message size limits and compression

4. **Monitor Resources:** Use tools like `dotnet-counters` and `dotnet-trace`

5. **Database for History:** Implement persistent storage for execution history (currently in-memory)
