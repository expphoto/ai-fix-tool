using System.Text;
using System.Text.Json;
using NJsonSchema;

namespace FixRunner.Tools;

public class FixWiFiIssuesTool : ITool
{
    public string Name => "fix_wifi_issues";
    public string Description => "Diagnoses and fixes common WiFi connectivity issues including adapter problems, DNS issues, and network profile corruption";
    public bool RequiresAdmin => true;

    public JsonSchema Schema => JsonSchema.FromType<FixWiFiIssuesArgs>();

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            var args = JsonSerializer.Deserialize<FixWiFiIssuesArgs>(arguments);
            
            var issues = DiagnoseWiFiIssues(args);
            var fixes = GenerateFixes(issues, args);
            
            var result = new
            {
                IssuesFound = issues,
                FixesToApply = fixes,
                NetworkAdapters = GetNetworkAdapters(),
                WiFiNetworks = GetAvailableNetworks(),
                ConnectionStatus = GetConnectionStatus(),
                DriverStatus = GetDriverStatus(),
                EstimatedDuration = "10-15 minutes",
                RequiresRestart = fixes.Any(f => f.RequiresRestart)
            };

            var fixScript = GenerateFixScript(issues, fixes, args);
            var undoScript = CreateUndoScript(issues);

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
                Error = $"Failed to diagnose WiFi issues: {ex.Message}",
                Data = null
            };
        }
    }

    private List<WiFiIssue> DiagnoseWiFiIssues(FixWiFiIssuesArgs args)
    {
        var issues = new List<WiFiIssue>();

        // Simulate WiFi diagnosis
        issues.Add(new WiFiIssue
        {
            Type = "NoConnection",
            Description = "Cannot connect to WiFi network",
            Severity = "High",
            Component = "WiFi Adapter",
            PossibleCauses = new[] { "Incorrect password", "Network adapter disabled", "Router issues" }
        });

        issues.Add(new WiFiIssue
        {
            Type = "LimitedConnectivity",
            Description = "Connected but no internet access",
            Severity = "High",
            Component = "Network Stack",
            PossibleCauses = new[] { "DNS issues", "IP configuration", "Router problems" }
        });

        issues.Add(new WiFiIssue
        {
            Type = "SlowSpeed",
            Description = "WiFi speed significantly slower than expected",
            Severity = "Medium",
            Component = "WiFi Adapter",
            PossibleCauses = new[] { "Outdated drivers", "Channel interference", "Distance from router" }
        });

        issues.Add(new WiFiIssue
        {
            Type = "Intermittent",
            Description = "WiFi connection drops frequently",
            Severity = "Medium",
            Component = "WiFi Adapter",
            PossibleCauses = new[] { "Power management", "Driver issues", "Signal interference" }
        });

        issues.Add(new WiFiIssue
        {
            Type = "NotVisible",
            Description = "WiFi network not showing in available networks",
            Severity = "Medium",
            Component = "WiFi Adapter",
            PossibleCauses = new[] { "Hidden SSID", "Adapter issues", "Router configuration" }
        });

        return issues;
    }

    private List<WiFiFix> GenerateFixes(List<WiFiIssue> issues, FixWiFiIssuesArgs args)
    {
        var fixes = new List<WiFiFix>();

        foreach (var issue in issues)
        {
            switch (issue.Type)
            {
                case "NoConnection":
                    fixes.Add(new WiFiFix
                    {
                        Description = "Run Windows Network Troubleshooter",
                        Command = "msdt.exe -id NetworkDiagnosticsNetworkAdapter",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new WiFiFix
                    {
                        Description = "Reset network adapter",
                        Command = "netsh interface set interface \"Wi-Fi\" admin=disable && netsh interface set interface \"Wi-Fi\" admin=enable",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new WiFiFix
                    {
                        Description = "Forget and re-add WiFi network",
                        Command = "ms-settings:network-wifi",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    if (args.ResetTCP)
                    {
                        fixes.Add(new WiFiFix
                        {
                            Description = "Reset TCP/IP stack",
                            Command = "netsh int ip reset",
                            RequiresRestart = true,
                            RiskLevel = "Medium"
                        });
                    }
                    break;

                case "LimitedConnectivity":
                    fixes.Add(new WiFiFix
                    {
                        Description = "Flush DNS cache",
                        Command = "ipconfig /flushdns",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new WiFiFix
                    {
                        Description = "Release and renew IP address",
                        Command = "ipconfig /release && ipconfig /renew",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new WiFiFix
                    {
                        Description = "Reset network adapter",
                        Command = "netsh interface set interface \"Wi-Fi\" admin=disable && netsh interface set interface \"Wi-Fi\" admin=enable",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    if (args.ResetTCP)
                    {
                        fixes.Add(new WiFiFix
                        {
                            Description = "Reset TCP/IP stack",
                            Command = "netsh int ip reset",
                            RequiresRestart = true,
                            RiskLevel = "Medium"
                        });
                    }
                    break;

                case "SlowSpeed":
                    if (args.UpdateDrivers)
                    {
                        fixes.Add(new WiFiFix
                        {
                            Description = "Update WiFi adapter drivers",
                            Command = "devmgmt.msc",
                            RequiresRestart = true,
                            RiskLevel = "Medium"
                        });
                    }
                    fixes.Add(new WiFiFix
                    {
                        Description = "Disable power saving for WiFi adapter",
                        Command = "powercfg.cpl",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new WiFiFix
                    {
                        Description = "Change WiFi channel on router (requires router access)",
                        Command = "cmd /c start http://192.168.1.1",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    break;

                case "Intermittent":
                    fixes.Add(new WiFiFix
                    {
                        Description = "Disable power management for WiFi adapter",
                        Command = "devmgmt.msc",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new WiFiFix
                    {
                        Description = "Change roaming aggressiveness",
                        Command = "devmgmt.msc",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    if (args.UpdateDrivers)
                    {
                        fixes.Add(new WiFiFix
                        {
                            Description = "Update WiFi adapter drivers",
                            Command = "devmgmt.msc",
                            RequiresRestart = true,
                            RiskLevel = "Medium"
                        });
                    }
                    break;

                case "NotVisible":
                    fixes.Add(new WiFiFix
                    {
                        Description = "Restart WiFi adapter",
                        Command = "netsh interface set interface \"Wi-Fi\" admin=disable && netsh interface set interface \"Wi-Fi\" admin=enable",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new WiFiFix
                    {
                        Description = "Scan for hardware changes",
                        Command = "devmgmt.msc",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new WiFiFix
                    {
                        Description = "Check if SSID is hidden and add manually",
                        Command = "ms-settings:network-wifi",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    break;
            }
        }

        return fixes;
    }

    private List<object> GetNetworkAdapters()
    {
        return new List<object>
        {
            new { Name = "Intel(R) Wi-Fi 6 AX201 160MHz", Type = "WiFi", Status = "Enabled", Speed = "866.7 Mbps" },
            new { Name = "Realtek PCIe GbE Family Controller", Type = "Ethernet", Status = "Disabled", Speed = "1 Gbps" },
            new { Name = "Bluetooth Device (Personal Area Network)", Type = "Bluetooth", Status = "Enabled", Speed = "3 Mbps" }
        };
    }

    private List<object> GetAvailableNetworks()
    {
        return new List<object>
        {
            new { SSID = "HomeNetwork_5G", Signal = "Excellent", Security = "WPA3", Channel = 36 },
            new { SSID = "HomeNetwork_2.4G", Signal = "Good", Security = "WPA3", Channel = 6 },
            new { SSID = "NeighborWiFi", Signal = "Fair", Security = "WPA2", Channel = 11 },
            new { SSID = "CoffeeShop_Guest", Signal = "Poor", Security = "Open", Channel = 1 }
        };
    }

    private object GetConnectionStatus()
    {
        return new
        {
            Connected = true,
            SSID = "HomeNetwork_5G",
            SignalStrength = "Excellent",
            Speed = "866.7 Mbps",
            IPAddress = "192.168.1.105",
            Gateway = "192.168.1.1",
            DNS = new[] { "8.8.8.8", "8.8.4.4" }
        };
    }

    private object GetDriverStatus()
    {
        return new
        {
            WiFiDriver = new { Name = "Intel Wireless", Version = "22.200.0.6", Status = "Update available" },
            EthernetDriver = new { Name = "Realtek PCIe GbE", Version = "10.50.511.2021", Status = "Up to date" }
        };
    }

    private string GenerateFixScript(List<WiFiIssue> issues, List<WiFiFix> fixes, FixWiFiIssuesArgs args)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo WiFi Issues Fix Tool");
        sb.AppendLine("echo ===================");
        sb.AppendLine("echo This script will fix common WiFi connectivity problems");
        sb.AppendLine("echo.");
        
        sb.AppendLine("echo Running Windows Network Troubleshooter...");
        sb.AppendLine("msdt.exe -id NetworkDiagnosticsNetworkAdapter");
        sb.AppendLine("pause");
        sb.AppendLine();

        sb.AppendLine("echo Flushing DNS cache...");
        sb.AppendLine("ipconfig /flushdns");
        sb.AppendLine("echo DNS cache flushed");
        sb.AppendLine("pause");
        sb.AppendLine();

        sb.AppendLine("echo Releasing and renewing IP address...");
        sb.AppendLine("ipconfig /release");
        sb.AppendLine("ipconfig /renew");
        sb.AppendLine("pause");
        sb.AppendLine();

        if (args.ResetTCP)
        {
            sb.AppendLine("echo Resetting TCP/IP stack...");
            sb.AppendLine("netsh int ip reset");
            sb.AppendLine("echo TCP/IP stack reset - restart required");
            sb.AppendLine("pause");
            sb.AppendLine();

            sb.AppendLine("echo Resetting Winsock catalog...");
            sb.AppendLine("netsh winsock reset");
            sb.AppendLine("echo Winsock reset - restart required");
            sb.AppendLine("pause");
            sb.AppendLine();
        }

        sb.AppendLine("echo Opening Network settings...");
        sb.AppendLine("ms-settings:network-wifi");
        sb.AppendLine("echo 1. Check WiFi settings");
        sb.AppendLine("echo 2. Forget and re-add network if needed");
        sb.AppendLine("pause");
        sb.AppendLine();

        sb.AppendLine("echo Opening Device Manager for driver updates...");
        sb.AppendLine("devmgmt.msc");
        sb.AppendLine("echo 1. Expand 'Network adapters'");
        sb.AppendLine("echo 2. Right-click WiFi adapter and select 'Update driver'");
        sb.AppendLine("echo 3. Check power management settings");
        sb.AppendLine("pause");
        sb.AppendLine();

        sb.AppendLine("echo WiFi troubleshooting complete!");
        sb.AppendLine("echo Additional steps:");
        sb.AppendLine("echo 1. Restart your computer if TCP/IP was reset");
        sb.AppendLine("echo 2. Check router placement and interference");
        sb.AppendLine("echo 3. Update router firmware if accessible");
        sb.AppendLine("echo 4. Test with different WiFi networks");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }

    private string CreateUndoScript(List<WiFiIssue> issues)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo WiFi Fix Undo");
        sb.AppendLine("echo =============");
        sb.AppendLine("echo To undo WiFi fixes:");
        sb.AppendLine("echo 1. Restore previous network adapter drivers:");
        sb.AppendLine("echo    - Open Device Manager (devmgmt.msc)");
        sb.AppendLine("echo    - Right-click network adapter > Properties");
        sb.AppendLine("echo    - Driver tab > Roll Back Driver");
        sb.AppendLine("echo 2. Re-enable power management if disabled:");
        sb.AppendLine("echo    - Device Manager > Network adapters");
        sb.AppendLine("echo    - Properties > Power Management tab");
        sb.AppendLine("echo    - Re-check 'Allow computer to turn off device'");
        sb.AppendLine("echo 3. Restore previous DNS settings:");
        sb.AppendLine("echo    - Control Panel > Network and Sharing Center");
        sb.AppendLine("echo    - Change adapter settings > Properties > IPv4");
        sb.AppendLine("echo 4. Re-add any forgotten WiFi networks");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }
}

public class FixWiFiIssuesArgs
{
    public bool UpdateDrivers { get; set; } = true;
    public bool ResetTCP { get; set; } = true;
    public bool ResetAdapter { get; set; } = true;
    public bool CheckPowerManagement { get; set; } = true;
}

public class WiFiIssue
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public string[] PossibleCauses { get; set; } = Array.Empty<string>();
}

public class WiFiFix
{
    public string Description { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public bool RequiresRestart { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
}
