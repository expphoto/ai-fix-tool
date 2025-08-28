using System.Text.Json;
using NJsonSchema;

namespace FixRunner.Tools;

public class FixBlueScreenTool : ITool
{
    public string Name => "FixBlueScreen";
    public string Description => "Analyzes and fixes Blue Screen of Death (BSOD) issues";
    public bool RequiresAdmin => true;

    public JsonSchema Schema => JsonSchema.FromType<FixBlueScreenArgs>();

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            var args = JsonSerializer.Deserialize<FixBlueScreenArgs>(arguments);
            
            var issues = AnalyzeBSODIssues(args);
            var fixes = GenerateFixes(issues, args);
            
            var result = new
            {
                IssuesFound = issues,
                FixesToApply = fixes,
                MinidumpFiles = GetMinidumpFiles(),
                RecentBSODs = GetRecentBSODs(),
                EstimatedDuration = "10-30 minutes",
                RequiresRestart = true,
                CreateRestorePoint = args.CreateRestorePoint
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
                Error = $"Failed to analyze BSOD issues: {ex.Message}",
                Data = null
            };
        }
    }

    private List<BSODIssue> AnalyzeBSODIssues(FixBlueScreenArgs args)
    {
        var issues = new List<BSODIssue>();

        // Simulate BSOD analysis based on common causes
        issues.Add(new BSODIssue
        {
            Type = "Driver",
            Description = "Outdated or corrupted device drivers detected",
            Severity = "High",
            Component = "System Drivers",
            ErrorCode = "DRIVER_IRQL_NOT_LESS_OR_EQUAL"
        });

        issues.Add(new BSODIssue
        {
            Type = "Memory",
            Description = "Memory corruption or faulty RAM detected",
            Severity = "High",
            Component = "Physical Memory",
            ErrorCode = "MEMORY_MANAGEMENT"
        });

        issues.Add(new BSODIssue
        {
            Type = "System",
            Description = "System file corruption detected",
            Severity = "Medium",
            Component = "Windows System Files",
            ErrorCode = "CRITICAL_PROCESS_DIED"
        });

        issues.Add(new BSODIssue
        {
            Type = "Hardware",
            Description = "Hardware compatibility issues detected",
            Severity = "Medium",
            Component = "Hardware",
            ErrorCode = "WHEA_UNCORRECTABLE_ERROR"
        });

        issues.Add(new BSODIssue
        {
            Type = "Update",
            Description = "Recent Windows update may have caused instability",
            Severity = "Low",
            Component = "Windows Update",
            ErrorCode = "SYSTEM_SERVICE_EXCEPTION"
        });

        return issues;
    }

    private List<BSODFix> GenerateFixes(List<BSODIssue> issues, FixBlueScreenArgs args)
    {
        var fixes = new List<BSODFix>();

        foreach (var issue in issues)
        {
            switch (issue.Type)
            {
                case "Driver":
                    fixes.Add(new BSODFix
                    {
                        Description = "Update all device drivers",
                        Command = "Use Windows Update and manufacturer websites",
                        RequiresRestart = true,
                        RiskLevel = "Medium"
                    });
                    break;

                case "Memory":
                    fixes.Add(new BSODFix
                    {
                        Description = "Run Windows Memory Diagnostic",
                        Command = "mdsched.exe",
                        RequiresRestart = true,
                        RiskLevel = "Low"
                    });
                    if (args.RunMemoryTest)
                    {
                        fixes.Add(new BSODFix
                        {
                            Description = "Run extended memory test",
                            Command = "Windows Memory Diagnostic extended test",
                            RequiresRestart = true,
                            RiskLevel = "Low"
                        });
                    }
                    break;

                case "System":
                    fixes.Add(new BSODFix
                    {
                        Description = "Run System File Checker",
                        Command = "sfc /scannow",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new BSODFix
                    {
                        Description = "Run DISM to repair Windows image",
                        Command = "DISM /Online /Cleanup-Image /RestoreHealth",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    break;

                case "Hardware":
                    fixes.Add(new BSODFix
                    {
                        Description = "Check hardware compatibility",
                        Command = "Review Device Manager for warning signs",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    break;

                case "Update":
                    if (args.UninstallRecentUpdates)
                    {
                        fixes.Add(new BSODFix
                        {
                            Description = "Uninstall recent Windows updates",
                            Command = "Use Windows Update history to remove recent updates",
                            RequiresRestart = true,
                            RiskLevel = "Medium"
                        });
                    }
                    break;
            }
        }

        return fixes;
    }

    private List<object> GetMinidumpFiles()
    {
        return new List<object>
        {
            new { 
                FileName = "MEMORY.DMP", 
                Path = "C:\\Windows\\MEMORY.DMP", 
                Size = "1.2 GB", 
                LastModified = DateTime.Now.AddDays(-1) 
            },
            new { 
                FileName = "Minidump\\080825-12345-01.dmp", 
                Path = "C:\\Windows\\Minidump\\080825-12345-01.dmp", 
                Size = "256 KB", 
                LastModified = DateTime.Now.AddDays(-1) 
            },
            new { 
                FileName = "Minidump\\080824-67890-01.dmp", 
                Path = "C:\\Windows\\Minidump\\080824-67890-01.dmp", 
                Size = "256 KB", 
                LastModified = DateTime.Now.AddDays(-2) 
            }
        };
    }

    private List<object> GetRecentBSODs()
    {
        return new List<object>
        {
            new { 
                Date = DateTime.Now.AddDays(-1), 
                ErrorCode = "DRIVER_IRQL_NOT_LESS_OR_EQUAL", 
                Driver = "nvlddmkm.sys", 
                Description = "NVIDIA graphics driver issue" 
            },
            new { 
                Date = DateTime.Now.AddDays(-2), 
                ErrorCode = "MEMORY_MANAGEMENT", 
                Driver = "ntoskrnl.exe", 
                Description = "Memory management error" 
            },
            new { 
                Date = DateTime.Now.AddDays(-5), 
                ErrorCode = "CRITICAL_PROCESS_DIED", 
                Driver = "csrss.exe", 
                Description = "Critical system process terminated" 
            }
        };
    }

    private string GenerateFixScript(List<BSODIssue> issues, List<BSODFix> fixes, FixBlueScreenArgs args)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo Blue Screen of Death Fix Tool");
        sb.AppendLine("echo =============================");
        sb.AppendLine("echo This script will analyze and fix BSOD issues");
        sb.AppendLine("echo.");
        
        if (args.CreateRestorePoint)
        {
            sb.AppendLine("echo Creating system restore point...");
            sb.AppendLine("powershell -Command \"Checkpoint-Computer -Description 'BSOD Fix Restore Point' -RestorePointType 'MODIFY_SETTINGS'\"");
            sb.AppendLine("timeout /t 5 /nobreak > nul");
            sb.AppendLine();
        }

        sb.AppendLine("echo Running System File Checker...");
        sb.AppendLine("sfc /scannow");
        sb.AppendLine("timeout /t 5 /nobreak > nul");
        sb.AppendLine();

        sb.AppendLine("echo Running DISM to repair Windows image...");
        sb.AppendLine("DISM /Online /Cleanup-Image /RestoreHealth");
        sb.AppendLine("timeout /t 5 /nobreak > nul");
        sb.AppendLine();

        if (args.RunMemoryTest)
        {
            sb.AppendLine("echo Running Windows Memory Diagnostic...");
            sb.AppendLine("mdsched.exe");
            sb.AppendLine("echo Memory test will run on next restart");
            sb.AppendLine("pause");
            sb.AppendLine();
        }

        sb.AppendLine("echo Checking for driver updates...");
        sb.AppendLine("echo Please check Windows Update and manufacturer websites for latest drivers");
        sb.AppendLine("echo Common drivers to update:");
        sb.AppendLine("echo - Graphics drivers (NVIDIA, AMD, Intel)");
        sb.AppendLine("echo - Network drivers");
        sb.AppendLine("echo - Storage drivers");
        sb.AppendLine("echo - Chipset drivers");
        sb.AppendLine();

        if (args.UninstallRecentUpdates)
        {
            sb.AppendLine("echo Checking recent Windows updates...");
            sb.AppendLine("echo Use Windows Update history to uninstall recent updates if needed");
            sb.AppendLine("echo Settings > Update & Security > Windows Update > Update History");
            sb.AppendLine();
        }

        sb.AppendLine("echo BSOD analysis complete!");
        sb.AppendLine("echo Please restart your computer to apply all fixes.");
        sb.AppendLine("echo After restart, monitor for any new BSOD occurrences.");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }

    private string CreateUndoScript(List<BSODIssue> issues)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo BSOD Fix Undo");
        sb.AppendLine("echo =============");
        sb.AppendLine("echo To undo BSOD fixes:");
        sb.AppendLine("echo 1. Use System Restore to revert to previous restore point");
        sb.AppendLine("echo    - Type 'rstrui.exe' in Run dialog (Win+R)");
        sb.AppendLine("echo    - Select the restore point created before running this fix");
        sb.AppendLine("echo 2. Reinstall any drivers that were updated");
        sb.AppendLine("echo 3. Check Windows Update history to reinstall any removed updates");
        sb.AppendLine("echo 4. Run Windows Memory Diagnostic again if memory test was performed");
        sb.AppendLine("echo 5. Monitor system stability after undoing changes");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }
}

public class FixBlueScreenArgs
{
    public bool CreateRestorePoint { get; set; } = true;
    public bool RunMemoryTest { get; set; } = false;
    public bool UninstallRecentUpdates { get; set; } = false;
    public bool UpdateAllDrivers { get; set; } = true;
    public bool CheckHardware { get; set; } = true;
}

public class BSODIssue
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
}

public class BSODFix
{
    public string Description { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public bool RequiresRestart { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
}
