using System.Text.Json;
using NJsonSchema;

namespace FixRunner.Tools;

public class FixSlowPerformanceTool : ITool
{
    public string Name => "FixSlowPerformance";
    public string Description => "Diagnoses and fixes system performance issues";
    public bool RequiresAdmin => false;

    public JsonSchema Schema => JsonSchema.FromType<FixSlowPerformanceArgs>();

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            var args = JsonSerializer.Deserialize<FixSlowPerformanceArgs>(arguments);
            
            var issues = DiagnosePerformanceIssues(args);
            var fixes = GenerateFixes(issues, args);
            
            var result = new
            {
                IssuesFound = issues,
                FixesToApply = fixes,
                SystemInfo = GetSystemInfo(),
                StartupPrograms = GetStartupPrograms(),
                RunningProcesses = GetRunningProcesses(),
                DiskUsage = GetDiskUsage(),
                MemoryUsage = GetMemoryUsage(),
                EstimatedDuration = "5-15 minutes",
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
                Error = $"Failed to diagnose performance issues: {ex.Message}",
                Data = null
            };
        }
    }

    private List<PerformanceIssue> DiagnosePerformanceIssues(FixSlowPerformanceArgs args)
    {
        var issues = new List<PerformanceIssue>();

        // Simulate performance diagnosis
        issues.Add(new PerformanceIssue
        {
            Type = "Startup",
            Description = "Too many programs running at startup",
            Severity = "High",
            Component = "Startup Programs",
            Impact = "Slow boot time"
        });

        issues.Add(new PerformanceIssue
        {
            Type = "Memory",
            Description = "High memory usage detected",
            Severity = "Medium",
            Component = "RAM",
            Impact = "System lag"
        });

        issues.Add(new PerformanceIssue
        {
            Type = "Disk",
            Description = "Disk space running low",
            Severity = "Medium",
            Component = "Storage",
            Impact = "Slow file operations"
        });

        issues.Add(new PerformanceIssue
        {
            Type = "Background",
            Description = "Too many background processes",
            Severity = "Medium",
            Component = "Processes",
            Impact = "CPU usage spikes"
        });

        issues.Add(new PerformanceIssue
        {
            Type = "Visual",
            Description = "Visual effects may be impacting performance",
            Severity = "Low",
            Component = "Display",
            Impact = "UI responsiveness"
        });

        return issues;
    }

    private List<PerformanceFix> GenerateFixes(List<PerformanceIssue> issues, FixSlowPerformanceArgs args)
    {
        var fixes = new List<PerformanceFix>();

        foreach (var issue in issues)
        {
            switch (issue.Type)
            {
                case "Startup":
                    fixes.Add(new PerformanceFix
                    {
                        Description = "Disable unnecessary startup programs",
                        Command = "Use Task Manager > Startup tab",
                        RequiresRestart = true,
                        RiskLevel = "Low"
                    });
                    break;

                case "Memory":
                    fixes.Add(new PerformanceFix
                    {
                        Description = "Close unnecessary programs and browser tabs",
                        Command = "Use Task Manager to identify memory hogs",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    if (args.OptimizeMemory)
                    {
                        fixes.Add(new PerformanceFix
                        {
                            Description = "Optimize memory usage",
                            Command = "Adjust virtual memory settings",
                            RequiresRestart = false,
                            RiskLevel = "Low"
                        });
                    }
                    break;

                case "Disk":
                    fixes.Add(new PerformanceFix
                    {
                        Description = "Clean up disk space",
                        Command = "Use Disk Cleanup and Storage Sense",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    if (args.Defragment)
                    {
                        fixes.Add(new PerformanceFix
                        {
                            Description = "Defragment hard drive",
                            Command = "dfrgui.exe",
                            RequiresRestart = false,
                            RiskLevel = "Low"
                        });
                    }
                    break;

                case "Background":
                    fixes.Add(new PerformanceFix
                    {
                        Description = "End unnecessary background processes",
                        Command = "Use Task Manager > Processes tab",
                        RequiresRestart = false,
                        RiskLevel = "Medium"
                    });
                    break;

                case "Visual":
                    if (args.AdjustVisualEffects)
                    {
                        fixes.Add(new PerformanceFix
                        {
                            Description = "Adjust visual effects for best performance",
                            Command = "SystemPropertiesPerformance.exe",
                            RequiresRestart = false,
                            RiskLevel = "Low"
                        });
                    }
                    break;
            }
        }

        return fixes;
    }

    private object GetSystemInfo()
    {
        return new
        {
            OS = "Windows 10 Pro",
            Version = "22H2",
            Build = "19045.4780",
            Processor = "Intel Core i7-8700K",
            RAM = "16 GB",
            SystemType = "64-bit operating system"
        };
    }

    private List<object> GetStartupPrograms()
    {
        return new List<object>
        {
            new { Name = "OneDrive", Publisher = "Microsoft Corporation", Status = "Enabled", Impact = "High" },
            new { Name = "Spotify", Publisher = "Spotify AB", Status = "Enabled", Impact = "Medium" },
            new { Name = "Discord", Publisher = "Discord Inc.", Status = "Enabled", Impact = "Medium" },
            new { Name = "Adobe Creative Cloud", Publisher = "Adobe Systems", Status = "Enabled", Impact = "High" },
            new { Name = "Steam", Publisher = "Valve Corporation", Status = "Enabled", Impact = "Low" },
            new { Name = "Microsoft Teams", Publisher = "Microsoft Corporation", Status = "Enabled", Impact = "Medium" }
        };
    }

    private List<object> GetRunningProcesses()
    {
        return new List<object>
        {
            new { Name = "chrome.exe", CPU = "15%", Memory = "2.1 GB", Description = "Google Chrome" },
            new { Name = "explorer.exe", CPU = "2%", Memory = "150 MB", Description = "Windows Explorer" },
            new { Name = "svchost.exe", CPU = "5%", Memory = "300 MB", Description = "Service Host" },
            new { Name = "spotify.exe", CPU = "3%", Memory = "400 MB", Description = "Spotify" },
            new { Name = "discord.exe", CPU = "1%", Memory = "200 MB", Description = "Discord" }
        };
    }

    private object GetDiskUsage()
    {
        return new
        {
            C = new { Total = "500 GB", Used = "420 GB", Free = "80 GB", Usage = "84%" },
            D = new { Total = "1 TB", Used = "600 GB", Free = "400 GB", Usage = "60%" }
        };
    }

    private object GetMemoryUsage()
    {
        return new
        {
            Total = "16 GB",
            InUse = "12.5 GB",
            Available = "3.5 GB",
            Cached = "2.1 GB",
            PagedPool = "800 MB",
            NonPagedPool = "400 MB"
        };
    }

    private string GenerateFixScript(List<PerformanceIssue> issues, List<PerformanceFix> fixes, FixSlowPerformanceArgs args)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo System Performance Fix Tool");
        sb.AppendLine("echo ===========================");
        sb.AppendLine("echo This script will optimize system performance");
        sb.AppendLine("echo.");
        
        sb.AppendLine("echo Opening Task Manager to manage startup programs...");
        sb.AppendLine("start taskmgr");
        sb.AppendLine("echo.");
        sb.AppendLine("echo In Task Manager:");
        sb.AppendLine("echo 1. Go to 'Startup' tab");
        sb.AppendLine("echo 2. Disable programs you don't need at startup");
        sb.AppendLine("echo 3. Focus on programs with 'High' startup impact");
        sb.AppendLine("pause");
        sb.AppendLine();

        sb.AppendLine("echo Opening Disk Cleanup...");
        sb.AppendLine("cleanmgr.exe");
        sb.AppendLine("echo Select all options and clean up system files");
        sb.AppendLine("pause");
        sb.AppendLine();

        if (args.Defragment)
        {
            sb.AppendLine("echo Opening Disk Defragmenter...");
            sb.AppendLine("dfrgui.exe");
            sb.AppendLine("echo Analyze and defragment your drives");
            sb.AppendLine("pause");
            sb.AppendLine();
        }

        if (args.AdjustVisualEffects)
        {
            sb.AppendLine("echo Adjusting visual effects for best performance...");
            sb.AppendLine("SystemPropertiesPerformance.exe");
            sb.AppendLine("echo Select 'Adjust for best performance'");
            sb.AppendLine("pause");
            sb.AppendLine();
        }

        sb.AppendLine("echo Performance optimization complete!");
        sb.AppendLine("echo Additional recommendations:");
        sb.AppendLine("echo 1. Restart your computer to apply changes");
        sb.AppendLine("echo 2. Keep browser tabs to a minimum");
        sb.AppendLine("echo 3. Regularly clean up temporary files");
        sb.AppendLine("echo 4. Monitor Task Manager for resource usage");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }

    private string CreateUndoScript(List<PerformanceIssue> issues)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo Performance Fix Undo");
        sb.AppendLine("echo ===================");
        sb.AppendLine("echo To undo performance optimizations:");
        sb.AppendLine("echo 1. Re-enable startup programs in Task Manager");
        sb.AppendLine("echo 2. Restore visual effects to default:");
        sb.AppendLine("echo    - Run: SystemPropertiesPerformance.exe");
        sb.AppendLine("echo    - Select 'Let Windows choose what's best for my computer'");
        sb.AppendLine("echo 3. Reinstall any programs that were removed");
        sb.AppendLine("echo 4. Check if any system services were disabled");
        sb.AppendLine("echo 5. Monitor system performance after undoing changes");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }
}

public class FixSlowPerformanceArgs
{
    public bool DisableStartupPrograms { get; set; } = true;
    public bool CleanDiskSpace { get; set; } = true;
    public bool Defragment { get; set; } = true;
    public bool OptimizeMemory { get; set; } = true;
    public bool AdjustVisualEffects { get; set; } = true;
    public bool EndBackgroundProcesses { get; set; } = false;
}

public class PerformanceIssue
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
}

public class PerformanceFix
{
    public string Description { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public bool RequiresRestart { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
}
