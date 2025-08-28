using System.Text.Json;
using NJsonSchema;

namespace FixRunner.Tools;

public class UpdateDriversTool : ITool
{
    public string Name => "UpdateDrivers";
    public string Description => "Updates system drivers to latest versions";
    public bool RequiresAdmin => true;

    public JsonSchema Schema => JsonSchema.FromType<UpdateDriversArgs>();

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            var args = JsonSerializer.Deserialize<UpdateDriversArgs>(arguments);
            
            var drivers = GetOutdatedDrivers();
            var selectedDrivers = FilterDrivers(drivers, args);
            
            var result = new
            {
                TotalDriversFound = drivers.Count,
                SelectedDrivers = selectedDrivers.Select(d => new
                {
                    d.Name,
                    d.CurrentVersion,
                    d.LatestVersion,
                    d.Category,
                    d.Size,
                    d.DownloadUrl
                }),
                EstimatedDownloadSize = selectedDrivers.Sum(d => d.Size),
                EstimatedDuration = $"{selectedDrivers.Count * 2}-{(selectedDrivers.Count * 5)} minutes",
                RequiresRestart = selectedDrivers.Any(d => d.RequiresRestart)
            };

            var updateScript = GenerateUpdateScript(selectedDrivers, args);
            var undoScript = CreateUndoScript(selectedDrivers);

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
                Error = $"Failed to prepare driver updates: {ex.Message}",
                Data = null
            };
        }
    }

    private List<DriverInfo> GetOutdatedDrivers()
    {
        // This would normally query Windows Update API or manufacturer APIs
        return new List<DriverInfo>
        {
            new DriverInfo
            {
                Name = "Intel HD Graphics",
                CurrentVersion = "27.20.100.9466",
                LatestVersion = "31.0.101.5084",
                Category = "Display",
                Size = 350 * 1024 * 1024, // 350MB
                DownloadUrl = "https://downloadmirror.intel.com/...",
                RequiresRestart = true
            },
            new DriverInfo
            {
                Name = "Realtek High Definition Audio",
                CurrentVersion = "6.0.1.8648",
                LatestVersion = "6.0.9235.1",
                Category = "Audio",
                Size = 50 * 1024 * 1024, // 50MB
                DownloadUrl = "https://www.realtek.com/...",
                RequiresRestart = false
            },
            new DriverInfo
            {
                Name = "Intel Wireless-AC 9560",
                CurrentVersion = "21.40.2.2",
                LatestVersion = "22.170.0.2",
                Category = "Network",
                Size = 25 * 1024 * 1024, // 25MB
                DownloadUrl = "https://downloadmirror.intel.com/...",
                RequiresRestart = true
            }
        };
    }

    private List<DriverInfo> FilterDrivers(List<DriverInfo> drivers, UpdateDriversArgs args)
    {
        var filtered = drivers.AsEnumerable();
        
        if (!string.IsNullOrEmpty(args.Category))
        {
            filtered = filtered.Where(d => 
                d.Category.Equals(args.Category, StringComparison.OrdinalIgnoreCase));
        }
        
        if (!string.IsNullOrEmpty(args.Manufacturer))
        {
            filtered = filtered.Where(d => 
                d.Name.Contains(args.Manufacturer, StringComparison.OrdinalIgnoreCase));
        }
        
        if (args.ExcludeCritical && !args.IncludeAll)
        {
            filtered = filtered.Where(d => !d.RequiresRestart);
        }
        
        return filtered.ToList();
    }

    private string GenerateUpdateScript(List<DriverInfo> drivers, UpdateDriversArgs args)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo Driver Update Tool");
        sb.AppendLine("echo ==================");
        sb.AppendLine("echo This will update the following drivers:");
        foreach (var driver in drivers)
        {
            sb.AppendLine($"echo - {driver.Name} ({driver.CurrentVersion} -> {driver.LatestVersion})");
        }
        sb.AppendLine("echo.");
        sb.AppendLine("pause");
        sb.AppendLine();
        
        sb.AppendLine("echo Creating system restore point...");
        sb.AppendLine("powershell -Command \"Checkpoint-Computer -Description 'Pre-Driver Update' -RestorePointType 'MODIFY_SETTINGS'\"");
        sb.AppendLine("timeout /t 5 /nobreak > nul");
        sb.AppendLine();
        
        foreach (var driver in drivers)
        {
            sb.AppendLine($"echo Updating {driver.Name}...");
            sb.AppendLine($"echo Downloading from: {driver.DownloadUrl}");
            sb.AppendLine($"powershell -Command \"Start-BitsTransfer -Source '{driver.DownloadUrl}' -Destination '%TEMP%\\{driver.Name.Replace(' ', '_')}_driver.exe'\"");
            sb.AppendLine($"%TEMP%\\{driver.Name.Replace(' ', '_')}_driver.exe /quiet /norestart");
            sb.AppendLine("timeout /t 10 /nobreak > nul");
            sb.AppendLine();
        }
        
        sb.AppendLine("echo Driver updates complete!");
        if (drivers.Any(d => d.RequiresRestart))
        {
            sb.AppendLine("echo Some drivers require a restart to complete installation.");
            sb.AppendLine("echo Please restart your computer when convenient.");
        }
        sb.AppendLine("pause");
        
        return sb.ToString();
    }

    private string CreateUndoScript(List<DriverInfo> drivers)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo Driver Update Undo Script");
        sb.AppendLine("echo =========================");
        sb.AppendLine("echo To undo driver updates:");
        sb.AppendLine("echo 1. Open Device Manager (devmgmt.msc)");
        sb.AppendLine("echo 2. Find the device with the updated driver");
        sb.AppendLine("echo 3. Right-click -> Properties -> Driver tab");
        sb.AppendLine("echo 4. Click 'Roll Back Driver' if available");
        sb.AppendLine("echo 5. Alternatively, use System Restore to revert to a previous point");
        sb.AppendLine("echo 6. Download and install the previous driver version from manufacturer");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }
}

public class UpdateDriversArgs
{
    public string? Category { get; set; }
    public string? Manufacturer { get; set; }
    public bool IncludeAll { get; set; } = false;
    public bool ExcludeCritical { get; set; } = false;
    public bool AutoRestart { get; set; } = false;
    public bool CreateRestorePoint { get; set; } = true;
}

public class DriverInfo
{
    public string Name { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public string LatestVersion { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public long Size { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public bool RequiresRestart { get; set; }
}
