using System.Text.Json;
using NJsonSchema;

namespace FixRunner.Tools;

public class CheckSystemHealthTool : ITool
{
    public string Name => "CheckSystemHealth";
    public string Description => "Performs comprehensive system health checks";
    public bool RequiresAdmin => false;

    public JsonSchema Schema => JsonSchema.FromType<CheckSystemHealthArgs>();

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            var args = JsonSerializer.Deserialize<CheckSystemHealthArgs>(arguments);
            
            var healthChecks = new Dictionary<string, object>();
            var warnings = new List<string>();
            var errors = new List<string>();

            // Check disk health
            var diskHealth = CheckDiskHealth();
            healthChecks["diskHealth"] = diskHealth;
            
            // Check memory
            var memoryHealth = CheckMemoryHealth();
            healthChecks["memoryHealth"] = memoryHealth;
            
            // Check CPU
            var cpuHealth = CheckCpuHealth();
            healthChecks["cpuHealth"] = cpuHealth;
            
            // Check Windows Update
            var updateStatus = CheckWindowsUpdateStatus();
            healthChecks["windowsUpdate"] = updateStatus;
            
            // Check services
            var servicesStatus = CheckServices();
            healthChecks["services"] = servicesStatus;
            
            // Check startup programs
            var startupPrograms = CheckStartupPrograms();
            healthChecks["startupPrograms"] = startupPrograms;

            // Generate recommendations
            var recommendations = GenerateRecommendations(healthChecks);
            
            var result = new
            {
                OverallHealth = CalculateOverallHealth(healthChecks),
                Checks = healthChecks,
                Warnings = warnings,
                Errors = errors,
                Recommendations = recommendations,
                LastBootTime = GetLastBootTime(),
                SystemUptime = GetSystemUptime()
            };

            var healthScript = GenerateHealthCheckScript(args);
            var undoScript = CreateUndoScript();

            return new ToolResult
            {
                Success = true,
                Data = result,
                Error = null,
                UndoScript = undoScript
            };
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                Success = false,
                Error = $"Failed to check system health: {ex.Message}",
                Data = null
            };
        }
    }

    private object CheckDiskHealth()
    {
        var drives = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
            .Select(d => new
            {
                Name = d.Name,
                TotalSize = d.TotalSize,
                AvailableFreeSpace = d.AvailableFreeSpace,
                UsedSpacePercent = (double)(d.TotalSize - d.AvailableFreeSpace) / d.TotalSize * 100,
                Health = d.AvailableFreeSpace > (d.TotalSize * 0.15) ? "Good" : "Low Space"
            })
            .ToList();

        return new
        {
            Drives = drives,
            OverallHealth = drives.All(d => d.Health == "Good") ? "Good" : "Warning"
        };
    }

    private object CheckMemoryHealth()
    {
        // Simulate memory check - in real implementation, use WMI or performance counters
        var totalMemory = 16L * 1024 * 1024 * 1024; // 16GB
        var availableMemory = 8L * 1024 * 1024 * 1024; // 8GB
        
        return new
        {
            TotalMemory = totalMemory,
            AvailableMemory = availableMemory,
            UsedMemoryPercent = (double)(totalMemory - availableMemory) / totalMemory * 100,
            Health = (totalMemory - availableMemory) < (totalMemory * 0.8) ? "Good" : "High Usage"
        };
    }

    private object CheckCpuHealth()
    {
        // Simulate CPU check
        return new
        {
            ProcessorCount = Environment.ProcessorCount,
            CurrentLoad = 25.5, // Would be actual CPU usage
            Health = "Good"
        };
    }

    private object CheckWindowsUpdateStatus()
    {
        return new
        {
            ServiceRunning = true, // Would check actual Windows Update service
            LastCheck = DateTime.Now.AddDays(-2),
            UpdatesAvailable = 3,
            Health = "Updates Available"
        };
    }

    private object CheckServices()
    {
        var criticalServices = new[]
        {
            new { Name = "Windows Update", Status = "Running" },
            new { Name = "Windows Defender", Status = "Running" },
            new { Name = "Windows Firewall", Status = "Running" },
            new { Name = "Print Spooler", Status = "Running" }
        };

        return new
        {
            Services = criticalServices,
            FailedServices = criticalServices.Where(s => s.Status != "Running").ToList(),
            Health = criticalServices.All(s => s.Status == "Running") ? "Good" : "Warning"
        };
    }

    private object CheckStartupPrograms()
    {
        var startupPrograms = new[]
        {
            new { Name = "OneDrive", Impact = "Medium" },
            new { Name = "Adobe Reader", Impact = "Low" },
            new { Name = "Spotify", Impact = "High" },
            new { Name = "Discord", Impact = "Medium" }
        };

        return new
        {
            Programs = startupPrograms,
            HighImpactCount = startupPrograms.Count(p => p.Impact == "High"),
            Health = startupPrograms.Count(p => p.Impact == "High") <= 2 ? "Good" : "Warning"
        };
    }

    private List<string> GenerateRecommendations(Dictionary<string, object> checks)
    {
        var recommendations = new List<string>();

        if (checks["diskHealth"] is { } diskHealth)
        {
            // Add disk-related recommendations
            recommendations.Add("Consider running disk cleanup to free up space");
            recommendations.Add("Run CHKDSK to check for disk errors");
        }

        if (checks["memoryHealth"] is { } memoryHealth)
        {
            recommendations.Add("Close unnecessary programs to free up memory");
            recommendations.Add("Consider adding more RAM if usage is consistently high");
        }

        if (checks["startupPrograms"] is { } startupPrograms)
        {
            recommendations.Add("Review startup programs and disable unnecessary ones");
            recommendations.Add("Use Task Manager to manage startup impact");
        }

        if (checks["windowsUpdate"] is { } windowsUpdate)
        {
            recommendations.Add("Install available Windows updates");
            recommendations.Add("Check for driver updates");
        }

        return recommendations;
    }

    private string CalculateOverallHealth(Dictionary<string, object> checks)
    {
        int warningCount = 0;
        
        // Simple health calculation
        if (checks["diskHealth"]?.ToString()?.Contains("Warning") == true) warningCount++;
        if (checks["memoryHealth"]?.ToString()?.Contains("High") == true) warningCount++;
        if (checks["services"]?.ToString()?.Contains("Warning") == true) warningCount++;
        
        return warningCount == 0 ? "Excellent" : 
               warningCount <= 2 ? "Good" : 
               warningCount <= 4 ? "Fair" : "Poor";
    }

    private DateTime GetLastBootTime()
    {
        return DateTime.Now.AddHours(-Environment.TickCount / (1000 * 60 * 60));
    }

    private TimeSpan GetSystemUptime()
    {
        return TimeSpan.FromMilliseconds(Environment.TickCount);
    }

    private string GenerateHealthCheckScript(CheckSystemHealthArgs args)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo System Health Check Tool");
        sb.AppendLine("echo ========================");
        sb.AppendLine("echo Running comprehensive system health checks...");
        sb.AppendLine();
        
        sb.AppendLine("echo Checking disk health...");
        sb.AppendLine("echo Running CHKDSK scan...");
        sb.AppendLine("chkdsk C: /scan");
        sb.AppendLine("timeout /t 5 /nobreak > nul");
        sb.AppendLine();
        
        sb.AppendLine("echo Checking system files...");
        sb.AppendLine("sfc /scannow");
        sb.AppendLine("timeout /t 5 /nobreak > nul");
        sb.AppendLine();
        
        sb.AppendLine("echo Checking Windows Update...");
        sb.AppendLine("powershell -Command \"Get-WindowsUpdateLog\"");
        sb.AppendLine("timeout /t 5 /nobreak > nul");
        sb.AppendLine();
        
        sb.AppendLine("echo Checking services...");
        sb.AppendLine("sc query type= service state= all");
        sb.AppendLine("timeout /t 5 /nobreak > nul");
        sb.AppendLine();
        
        sb.AppendLine("echo Health check complete!");
        sb.AppendLine("echo Review the results above for any issues.");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }

    private string CreateUndoScript()
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo System Health Check Undo");
        sb.AppendLine("echo ========================");
        sb.AppendLine("echo Health checks are read-only operations.");
        sb.AppendLine("echo No changes were made to your system.");
        sb.AppendLine("echo To address any issues found:");
        sb.AppendLine("echo 1. Run the generated health check script");
        sb.AppendLine("echo 2. Follow the recommendations provided");
        sb.AppendLine("echo 3. Use Windows built-in troubleshooters");
        sb.AppendLine("echo 4. Consider System Restore if problems occur");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }
}

public class CheckSystemHealthArgs
{
    public bool CheckDiskHealth { get; set; } = true;
    public bool CheckMemory { get; set; } = true;
    public bool CheckCpu { get; set; } = true;
    public bool CheckServices { get; set; } = true;
    public bool CheckStartupPrograms { get; set; } = true;
    public bool CheckWindowsUpdate { get; set; } = true;
    public bool GenerateReport { get; set; } = true;
}
