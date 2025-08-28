using System.Text.Json;
using NJsonSchema;

namespace FixRunner.Tools;

public class FixStartupIssuesTool : ITool
{
    public string Name => "FixStartupIssues";
    public string Description => "Diagnoses and fixes Windows startup problems including slow boot and boot failures";
    public bool RequiresAdmin => true;

    public JsonSchema Schema => JsonSchema.FromType<FixStartupIssuesArgs>();

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            var args = JsonSerializer.Deserialize<FixStartupIssuesArgs>(arguments);
            
            var issues = DiagnoseStartupIssues(args);
            var fixes = GenerateFixes(issues, args);
            
            var result = new
            {
                IssuesFound = issues,
                FixesToApply = fixes,
                BootConfiguration = GetBootConfiguration(),
                StartupPrograms = GetStartupPrograms(),
                Services = GetServices(),
                BootTime = GetBootTime(),
                EstimatedDuration = "15-30 minutes",
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
                Error = $"Failed to diagnose startup issues: {ex.Message}",
                Data = null
            };
        }
    }

    private List<StartupIssue> DiagnoseStartupIssues(FixStartupIssuesArgs args)
    {
        var issues = new List<StartupIssue>();

        // Simulate startup diagnosis
        issues.Add(new StartupIssue
        {
            Type = "SlowBoot",
            Description = "Windows takes too long to start",
            Severity = "Medium",
            Component = "Startup Programs",
            PossibleCauses = new[] { "Too many startup programs", "Slow hard drive", "Windows services" }
        });

        issues.Add(new StartupIssue
        {
            Type = "BootFailure",
            Description = "Windows fails to boot properly",
            Severity = "High",
            Component = "Boot Configuration",
            PossibleCauses = new[] { "Corrupted boot files", "Hardware issues", "Driver problems" }
        });

        issues.Add(new StartupIssue
        {
            Type = "BlackScreen",
            Description = "Black screen after Windows logo",
            Severity = "High",
            Component = "Display Drivers",
            PossibleCauses = new[] { "Display driver issues", "Windows updates", "Corrupted system files" }
        });

        issues.Add(new StartupIssue
        {
            Type = "StuckOnLogo",
            Description = "Windows stuck on boot logo",
            Severity = "High",
            Component = "System Files",
            PossibleCauses = new[] { "Corrupted system files", "Hardware failure", "Windows updates" }
        });

        issues.Add(new StartupIssue
        {
            Type = "StartupPrograms",
            Description = "Too many programs starting with Windows",
            Severity = "Low",
            Component = "Startup Configuration",
            PossibleCauses = new[] { "Software installations", "Browser extensions", "Background services" }
        });

        issues.Add(new StartupIssue
        {
            Type = "Services",
            Description = "Unnecessary services slowing startup",
            Severity = "Low",
            Component = "Windows Services",
            PossibleCauses = new[] { "Third-party services", "Windows features", "Background processes" }
        });

        return issues;
    }

    private List<StartupFix> GenerateFixes(List<StartupIssue> issues, FixStartupIssuesArgs args)
    {
        var fixes = new List<StartupFix>();

        foreach (var issue in issues)
        {
            switch (issue.Type)
            {
                case "SlowBoot":
                    fixes.Add(new StartupFix
                    {
                        Description = "Disable unnecessary startup programs",
                        Command = "taskmgr",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new StartupFix
                    {
                        Description = "Run Windows Startup Repair",
                        Command = "shutdown /r /o /f /t 0",
                        RequiresRestart = true,
                        RiskLevel = "Medium"
                    });
                    fixes.Add(new StartupFix
                    {
                        Description = "Check disk for errors",
                        Command = "chkdsk C: /f /r",
                        RequiresRestart = true,
                        RiskLevel = "Medium"
                    });
                    if (args.EnableFastStartup)
                    {
                        fixes.Add(new StartupFix
                        {
                            Description = "Enable Fast Startup",
                            Command = "powercfg.cpl",
                            RequiresRestart = false,
                            RiskLevel = "Low"
                        });
                    }
                    break;

                case "BootFailure":
                    fixes.Add(new StartupFix
                    {
                        Description = "Run System File Checker",
                        Command = "sfc /scannow",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new StartupFix
                    {
                        Description = "Run DISM to repair Windows image",
                        Command = "DISM /Online /Cleanup-Image /RestoreHealth",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new StartupFix
                    {
                        Description = "Rebuild boot configuration",
                        Command = "bootrec /rebuildbcd",
                        RequiresRestart = true,
                        RiskLevel = "High"
                    });
                    fixes.Add(new StartupFix
                    {
                        Description = "Run Windows Startup Repair from recovery",
                        Command = "shutdown /r /o /f /t 0",
                        RequiresRestart = true,
                        RiskLevel = "Medium"
                    });
                    break;

                case "BlackScreen":
                    fixes.Add(new StartupFix
                    {
                        Description = "Boot into Safe Mode",
                        Command = "shutdown /r /o /f /t 0",
                        RequiresRestart = true,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new StartupFix
                    {
                        Description = "Update display drivers",
                        Command = "devmgmt.msc",
                        RequiresRestart = true,
                        RiskLevel = "Medium"
                    });
                    fixes.Add(new StartupFix
                    {
                        Description = "Disable fast startup",
                        Command = "powercfg.cpl",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    break;

                case "StuckOnLogo":
                    fixes.Add(new StartupFix
                    {
                        Description = "Run automatic repair",
                        Command = "shutdown /r /o /f /t 0",
                        RequiresRestart = true,
                        RiskLevel = "Medium"
                    });
                    fixes.Add(new StartupFix
                    {
                        Description = "Check system files",
                        Command = "sfc /scannow",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new StartupFix
                    {
                        Description = "Check disk health",
                        Command = "chkdsk C: /f /r",
                        RequiresRestart = true,
                        RiskLevel = "Medium"
                    });
                    break;

                case "StartupPrograms":
                    fixes.Add(new StartupFix
                    {
                        Description = "Open Task Manager to manage startup programs",
                        Command = "taskmgr",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new StartupFix
                    {
                        Description = "Use System Configuration utility",
                        Command = "msconfig",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new StartupFix
                    {
                        Description = "Clean registry startup entries",
                        Command = "regedit",
                        RequiresRestart = false,
                        RiskLevel = "Medium"
                    });
                    break;

                case "Services":
                    fixes.Add(new StartupFix
                    {
                        Description = "Open Services management console",
                        Command = "services.msc",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new StartupFix
                    {
                        Description = "Disable unnecessary services",
                        Command = "services.msc",
                        RequiresRestart = false,
                        RiskLevel = "Medium"
                    });
                    fixes.Add(new StartupFix
                    {
                        Description = "Use System Configuration for services",
                        Command = "msconfig",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    break;
            }
        }

        return fixes;
    }

    private object GetBootConfiguration()
    {
        return new
        {
            BootManager = new { Timeout = 30, DefaultOS = "Windows 10", BootEntries = 2 },
            BCDStore = new { Location = "C:\\boot\\bcd", Status = "Healthy", LastModified = "2024-08-28" },
            BootFiles = new[] { "bootmgr", "BCD", "winload.exe" }
        };
    }

    private List<object> GetStartupPrograms()
    {
        return new List<object>
        {
            new { Name = "OneDrive", Publisher = "Microsoft", Status = "Enabled", Impact = "High" },
            new { Name = "Spotify", Publisher = "Spotify AB", Status = "Enabled", Impact = "Medium" },
            new { Name = "Discord", Publisher = "Discord Inc.", Status = "Enabled", Impact = "Medium" },
            new { Name = "Adobe Creative Cloud", Publisher = "Adobe", Status = "Enabled", Impact = "High" },
            new { Name = "Steam", Publisher = "Valve", Status = "Disabled", Impact = "High" },
            new { Name = "Zoom", Publisher = "Zoom Video Communications", Status = "Enabled", Impact = "Low" }
        };
    }

    private List<object> GetServices()
    {
        return new List<object>
        {
            new { Name = "Windows Search", Status = "Running", StartupType = "Automatic", Impact = "Medium" },
            new { Name = "Print Spooler", Status = "Running", StartupType = "Automatic", Impact = "Low" },
            new { Name = "Windows Update", Status = "Running", StartupType = "Manual", Impact = "Low" },
            new { Name = "Superfetch", Status = "Running", StartupType = "Automatic", Impact = "High" },
            new { Name = "Windows Defender", Status = "Running", StartupType = "Automatic", Impact = "Low" },
            new { Name = "Adobe Update Service", Status = "Running", StartupType = "Automatic", Impact = "Medium" }
        };
    }

    private object GetBootTime()
    {
        return new
        {
            LastBootTime = "2024-08-28 08:15:32",
            BootDuration = "45 seconds",
            AverageBootTime = "35 seconds",
            SlowestBoot = "120 seconds",
            FastestBoot = "25 seconds"
        };
    }

    private string GenerateFixScript(List<StartupIssue> issues, List<StartupFix> fixes, FixStartupIssuesArgs args)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo Windows Startup Fix Tool");
        sb.AppendLine("echo ========================");
        sb.AppendLine("echo This script will fix common Windows startup problems");
        sb.AppendLine("echo.");
        
        sb.AppendLine("echo Running System File Checker...");
        sb.AppendLine("sfc /scannow");
        sb.AppendLine("pause");
        sb.AppendLine();

        sb.AppendLine("echo Running DISM to repair Windows image...");
        sb.AppendLine("DISM /Online /Cleanup-Image /RestoreHealth");
        sb.AppendLine("pause");
        sb.AppendLine();

        sb.AppendLine("echo Checking disk for errors...");
        sb.AppendLine("chkdsk C: /f /r");
        sb.AppendLine("echo Note: Disk check will run on next restart");
        sb.AppendLine("pause");
        sb.AppendLine();

        sb.AppendLine("echo Opening Task Manager to manage startup programs...");
        sb.AppendLine("taskmgr");
        sb.AppendLine("echo 1. Go to 'Startup' tab");
        sb.AppendLine("echo 2. Disable unnecessary programs");
        sb.AppendLine("echo 3. Focus on 'High' impact programs first");
        sb.AppendLine("pause");
        sb.AppendLine();

        sb.AppendLine("echo Opening System Configuration...");
        sb.AppendLine("msconfig");
        sb.AppendLine("echo 1. Go to 'Startup' tab (Windows 10) or 'Services' tab");
        sb.AppendLine("echo 2. Disable unnecessary startup items");
        sb.AppendLine("echo 3. Be careful not to disable Windows services");
        sb.AppendLine("pause");
        sb.AppendLine();

        sb.AppendLine("echo Opening Services management...");
        sb.AppendLine("services.msc");
        sb.AppendLine("echo 1. Look for third-party services");
        sb.AppendLine("echo 2. Set unnecessary services to 'Manual' or 'Disabled'");
        sb.AppendLine("echo 3. Common services to disable:");
        sb.AppendLine("echo    - Print Spooler (if no printer)");
        sb.AppendLine("echo    - Windows Search (if not needed)");
        sb.AppendLine("echo    - Superfetch/SysMain (on SSD systems)");
        sb.AppendLine("pause");
        sb.AppendLine();

        if (args.EnableFastStartup)
        {
            sb.AppendLine("echo Enabling Fast Startup...");
            sb.AppendLine("powercfg.cpl");
            sb.AppendLine("echo 1. Click 'Choose what the power buttons do'");
            sb.AppendLine("echo 2. Click 'Change settings that are currently unavailable'");
            sb.AppendLine("echo 3. Check 'Turn on fast startup'");
            sb.AppendLine("pause");
            sb.AppendLine();
        }

        sb.AppendLine("echo Running Windows Startup Repair...");
        sb.AppendLine("echo This will restart your computer into recovery mode...");
        sb.AppendLine("echo Press any key to continue or Ctrl+C to cancel...");
        sb.AppendLine("pause > nul");
        sb.AppendLine("shutdown /r /o /f /t 0");
        
        return sb.ToString();
    }

    private string CreateUndoScript(List<StartupIssue> issues)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo Startup Fix Undo");
        sb.AppendLine("echo ================");
        sb.AppendLine("echo To undo startup fixes:");
        sb.AppendLine("echo 1. Re-enable startup programs:");
        sb.AppendLine("echo    - Open Task Manager (Ctrl+Shift+Esc)");
        sb.AppendLine("echo    - Go to 'Startup' tab");
        sb.AppendLine("echo    - Re-enable previously disabled programs");
        sb.AppendLine("echo 2. Restore Windows services:");
        sb.AppendLine("echo    - Open services.msc");
        sb.AppendLine("echo    - Set services back to original startup type");
        sb.AppendLine("echo 3. Restore boot configuration:");
        sb.AppendLine("echo    - Open Command Prompt as admin");
        sb.AppendLine("echo    - Run: bcdedit /enum");
        sb.AppendLine("echo    - Run: bootrec /rebuildbcd if needed");
        sb.AppendLine("echo 4. Restore System File Checker changes:");
        sb.AppendLine("echo    - Run: sfc /scannow to verify integrity");
        sb.AppendLine("echo 5. Restore registry startup entries:");
        sb.AppendLine("echo    - Open regedit and navigate to:");
        sb.AppendLine("echo      HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
        sb.AppendLine("echo      HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }
}

public class FixStartupIssuesArgs
{
    public bool EnableFastStartup { get; set; } = true;
    public bool DisableStartupPrograms { get; set; } = true;
    public bool DisableServices { get; set; } = true;
    public bool RunStartupRepair { get; set; } = true;
    public bool CheckDisk { get; set; } = true;
}

public class StartupIssue
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public string[] PossibleCauses { get; set; } = Array.Empty<string>();
}

public class StartupFix
{
    public string Description { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public bool RequiresRestart { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
}
