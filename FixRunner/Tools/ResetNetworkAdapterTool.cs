using System.Text.Json;
using NJsonSchema;

namespace FixRunner.Tools;

public class ResetNetworkAdapterTool : ITool
{
    public string Name => "ResetNetworkAdapter";
    public string Description => "Resets network adapter settings to resolve connectivity issues";
    public bool RequiresAdmin => true;

    public JsonSchema Schema => JsonSchema.FromType<ResetNetworkAdapterArgs>();

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            var args = JsonSerializer.Deserialize<ResetNetworkAdapterArgs>(arguments);
            
            var adapters = GetNetworkAdapters();
            var targetAdapter = args.AdapterName ?? GetPrimaryAdapter(adapters);
            
            var result = new
            {
                TargetAdapter = targetAdapter,
                AdaptersFound = adapters,
                Actions = new List<string>
                {
                    "Release IP address",
                    "Renew IP address",
                    "Flush DNS cache",
                    "Reset Winsock",
                    "Reset TCP/IP stack"
                },
                EstimatedDuration = "2-3 minutes"
            };

            var resetScript = GenerateResetScriptPS(targetAdapter, args);
            var undoScript = CreateUndoScript(targetAdapter);

            return new ToolResult
            {
                Success = true,
                Data = result,
                Error = null,
                UndoScript = undoScript,
                Script = resetScript
            };
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                Success = false,
                Error = $"Failed to prepare network reset: {ex.Message}",
                Data = null
            };
        }
    }

    private List<string> GetNetworkAdapters()
    {
        // This would normally use WMI or PowerShell to get actual adapters
        return new List<string>
        {
            "Ethernet",
            "Wi-Fi",
            "Ethernet 2",
            "Bluetooth Network Connection"
        };
    }

    private string GetPrimaryAdapter(List<string> adapters)
    {
        return adapters.FirstOrDefault(a => a.Contains("Ethernet") || a.Contains("Wi-Fi")) ?? "Ethernet";
    }

    private string GenerateResetScriptPS(string adapterName, ResetNetworkAdapterArgs args)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Network Adapter Reset Tool (PowerShell)");
        sb.AppendLine($"Write-Host \"Resetting network settings for: {adapterName}\" -ForegroundColor Cyan");
        sb.AppendLine("Start-Sleep -Seconds 1");
        sb.AppendLine();
        sb.AppendLine("Write-Host \"Releasing IP address...\"");
        sb.AppendLine("ipconfig /release");
        sb.AppendLine("Start-Sleep -Seconds 2");
        sb.AppendLine();
        sb.AppendLine("Write-Host \"Flushing DNS cache...\"");
        sb.AppendLine("ipconfig /flushdns");
        sb.AppendLine("Start-Sleep -Seconds 2");
        sb.AppendLine();
        sb.AppendLine("Write-Host \"Renewing IP address...\"");
        sb.AppendLine("ipconfig /renew");
        sb.AppendLine("Start-Sleep -Seconds 5");
        sb.AppendLine();
        if (args.IncludeWinsockReset)
        {
            sb.AppendLine("Write-Host \"Resetting Winsock catalog...\"");
            sb.AppendLine("netsh winsock reset");
            sb.AppendLine("Start-Sleep -Seconds 2");
            sb.AppendLine();
        }
        if (args.IncludeTcpIpReset)
        {
            sb.AppendLine("Write-Host \"Resetting TCP/IP stack...\"");
            sb.AppendLine("netsh int ip reset");
            sb.AppendLine("Start-Sleep -Seconds 2");
            sb.AppendLine();
        }
        sb.AppendLine("Write-Host \"Network reset complete!\" -ForegroundColor Green");
        if (args.RestartRequired)
        {
            sb.AppendLine("Write-Host \"A restart is recommended to apply all changes.\"");
        }
        return sb.ToString();
    }

    private string CreateUndoScript(string adapterName)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo Network Reset Undo Script");
        sb.AppendLine("echo =========================");
        sb.AppendLine("echo Network settings have been reset to defaults.");
        sb.AppendLine("echo To undo this action:");
        sb.AppendLine("echo 1. Check if you have any custom network configurations backed up");
        sb.AppendLine("echo 2. Restore from System Restore point if available");
        sb.AppendLine("echo 3. Manually reconfigure any static IP settings");
        sb.AppendLine("echo 4. Reinstall network adapter drivers if needed");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }
}

public class ResetNetworkAdapterArgs
{
    public string? AdapterName { get; set; }
    public bool IncludeWinsockReset { get; set; } = true;
    public bool IncludeTcpIpReset { get; set; } = true;
    public bool RestartRequired { get; set; } = true;
}
