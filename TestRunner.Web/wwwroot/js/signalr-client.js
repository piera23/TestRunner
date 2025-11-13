// TestRunner SignalR Client
// Real-time updates for test execution

(function () {
    'use strict';

    let connection = null;

    // Initialize SignalR connection
    function initializeSignalR() {
        connection = new signalR.HubConnectionBuilder()
            .withUrl("/testrunner-hub")
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // Connection lifecycle events
        connection.onreconnecting((error) => {
            console.warn("SignalR reconnecting:", error);
            showToast("Reconnecting to server...", "warning");
        });

        connection.onreconnected((connectionId) => {
            console.log("SignalR reconnected:", connectionId);
            showToast("Reconnected to server", "success");
        });

        connection.onclose((error) => {
            console.error("SignalR connection closed:", error);
            showToast("Connection lost. Attempting to reconnect...", "danger");

            // Attempt to reconnect after 5 seconds
            setTimeout(() => startConnection(), 5000);
        });

        // Register event handlers
        registerEventHandlers();

        // Start connection
        startConnection();
    }

    // Start SignalR connection
    function startConnection() {
        connection.start()
            .then(() => {
                console.log("SignalR connected");
                showToast("Connected to TestRunner Hub", "success", 2000);
            })
            .catch((error) => {
                console.error("SignalR connection error:", error);
                setTimeout(() => startConnection(), 5000);
            });
    }

    // Register SignalR event handlers
    function registerEventHandlers() {
        // Execution started
        connection.on("ExecutionStarted", (timestamp, projectCount) => {
            console.log(`Execution started: ${projectCount} projects`);
            showToast(`Starting execution of ${projectCount} projects...`, "info");
            updateExecutionStatus("running");
        });

        // Project started
        connection.on("ProjectStarted", (name, type) => {
            console.log(`Project started: ${name} (${type})`);
            addConsoleOutput(`[${getCurrentTime()}] 🚀 Starting project: ${name} (${type})`);
            updateCurrentProject(name);
        });

        // Command started
        connection.on("CommandStarted", (projectName, command) => {
            console.log(`Command started: ${command} for ${projectName}`);
            addConsoleOutput(`[${getCurrentTime()}]   ▶ Running: ${command}`);
        });

        // Command output (streaming)
        connection.on("CommandOutput", (projectName, command, output) => {
            console.log(`Command output: ${output}`);
            if (output && output.trim()) {
                addConsoleOutput(`     ${output.trim()}`);
            }
        });

        // Command completed
        connection.on("CommandCompleted", (projectName, command, exitCode) => {
            const success = exitCode === 0;
            console.log(`Command completed: ${command} with exit code ${exitCode}`);
            const icon = success ? "✓" : "✗";
            const status = success ? "SUCCESS" : "FAILED";
            addConsoleOutput(`[${getCurrentTime()}]   ${icon} ${status} (exit code: ${exitCode})`);
        });

        // Project completed
        connection.on("ProjectCompleted", (name, status, duration, isSuccess) => {
            console.log(`Project completed: ${name} - ${status} (${duration}s)`);
            const icon = isSuccess ? "✓" : "✗";
            addConsoleOutput(`[${getCurrentTime()}] ${icon} Project ${name} ${status} (${duration.toFixed(1)}s)\n`);

            if (isSuccess) {
                showToast(`Project ${name} passed`, "success", 3000);
            } else {
                showToast(`Project ${name} failed`, "danger", 3000);
            }
        });

        // Execution completed
        connection.on("ExecutionCompleted", (isSuccess, duration) => {
            console.log(`Execution completed: ${isSuccess} (${duration}s)`);
            addConsoleOutput(`[${getCurrentTime()}] ${'='.repeat(60)}`);
            addConsoleOutput(`[${getCurrentTime()}] Execution ${isSuccess ? 'COMPLETED' : 'FAILED'} in ${duration.toFixed(1)}s`);

            updateExecutionStatus("idle");

            if (isSuccess) {
                showToast("All tests passed! 🎉", "success", 5000);
            } else {
                showToast("Some tests failed", "danger", 5000);
            }
        });

        // Execution cancelled
        connection.on("ExecutionCancelled", () => {
            console.log("Execution cancelled");
            addConsoleOutput(`[${getCurrentTime()}] ⚠ Execution cancelled by user`);
            showToast("Execution cancelled", "warning");
            updateExecutionStatus("idle");
        });

        // Execution summary
        connection.on("ExecutionSummary", (summary) => {
            console.log("Execution summary:", summary);
            addConsoleOutput(`[${getCurrentTime()}] Total: ${summary.TotalProjects}, Passed: ${summary.PassedProjects}, Failed: ${summary.FailedProjects}`);
            addConsoleOutput(`[${getCurrentTime()}] Success Rate: ${summary.SuccessRate.toFixed(1)}%`);
        });
    }

    // Utility functions
    function getCurrentTime() {
        const now = new Date();
        return now.toLocaleTimeString('en-US', { hour12: false });
    }

    function updateExecutionStatus(status) {
        const statusElement = document.querySelector('.execution-status');
        if (statusElement) {
            if (status === "running") {
                statusElement.innerHTML = '<span class="badge bg-primary"><i class="fas fa-spinner fa-spin"></i> Running</span>';
            } else {
                statusElement.innerHTML = '<span class="badge bg-success"><i class="fas fa-check-circle"></i> Idle</span>';
            }
        }
    }

    function updateCurrentProject(projectName) {
        const projectElement = document.querySelector('.current-project');
        if (projectElement) {
            projectElement.textContent = projectName;
        }
    }

    function addConsoleOutput(line) {
        const consoleElement = document.querySelector('.console-output');
        if (consoleElement) {
            const lineElement = document.createElement('div');
            lineElement.textContent = line;
            consoleElement.appendChild(lineElement);

            // Auto-scroll to bottom
            consoleElement.scrollTop = consoleElement.scrollHeight;
        }
    }

    function showToast(message, type = "info", duration = 3000) {
        const toastContainer = document.getElementById('toast-container');
        if (!toastContainer) return;

        const icons = {
            success: 'fa-check-circle',
            danger: 'fa-exclamation-circle',
            warning: 'fa-exclamation-triangle',
            info: 'fa-info-circle'
        };

        const toast = document.createElement('div');
        toast.className = `toast show align-items-center text-white bg-${type} border-0`;
        toast.setAttribute('role', 'alert');
        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">
                    <i class="fas ${icons[type]} me-2"></i>
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        `;

        toastContainer.appendChild(toast);

        // Auto-remove after duration
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, duration);

        // Remove on close button click
        toast.querySelector('.btn-close').addEventListener('click', () => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        });
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeSignalR);
    } else {
        initializeSignalR();
    }

    // Export for use in Blazor components if needed
    window.TestRunnerSignalR = {
        connection: () => connection,
        showToast: showToast
    };
})();
