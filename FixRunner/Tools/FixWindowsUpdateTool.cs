using System.Text.Json;
using NJsonSchema;

namespace FixRunner.Tools;

public class FixWindowsUpdateTool : ITool
{
    public string Name => "FixWindowsUpdate";
    public string Description => "Diagnoses and fixes Windows Update issues";
    public bool RequiresAdmin => true;

    public JsonSchema Schema => JsonSchema.FromType<FixWindowsUpdateArgs>();

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            var args = JsonSerializer.Deserialize<FixWindowsUpdateArgs>(arguments);
            
            var issues = DiagnoseWindowsUpdateIssues();
            var fixes = GenerateFixes(issues, args);
            
            var result = new
            {
                IssuesFound = issues,
                FixesToApply = fixes,
                EstimatedDuration = "5-15 minutes",
                RequiresRestart = fixes.Any(f => f.RequiresRestart),
                BackupCreated = args.CreateBackup
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
                Error = $"Failed to diagnose Windows Update issues: {ex.Message}",
                Data = null
            };
        }
    }

    private List<WindowsUpdateIssue> DiagnoseWindowsUpdateIssues()
    {
        var issues = new List<WindowsUpdateIssue>();

        // Simulate diagnosis - in real implementation, check actual Windows Update components
        issues.Add(new WindowsUpdateIssue
        {
            Type = "Service",
            Description = "Windows Update service not running",
            Severity = "High",
            Component = "wuauserv"
        });

        issues.Add(new WindowsUpdateIssue
        {
            Type = "Cache",
            Description = "Windows Update cache corrupted",
            Severity = "Medium",
            Component = "SoftwareDistribution"
        });

        issues.Add(new WindowsUpdateIssue
        {
            Type = "Database",
            Description = "Windows Update database corrupted",
            Severity = "Medium",
            Component = "DataStore"
        });

        issues.Add(new WindowsUpdateIssue
        {
            Type = "Component",
            Description = "Background Intelligent Transfer Service not running",
            Severity = "High",
            Component = "BITS"
        });

        return issues;
    }

    private List<WindowsUpdateFix> GenerateFixes(List<WindowsUpdateIssue> issues, FixWindowsUpdateArgs args)
    {
        var fixes = new List<WindowsUpdateFix>();

        foreach (var issue in issues)
        {
            switch (issue.Type)
            {
                case "Service":
                    fixes.Add(new WindowsUpdateFix
                    {
                        Description = $"Restart {issue.Component} service",
                        Command = $"net start {issue.Component}",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    break;

                case "Cache":
                    fixes.Add(new WindowsUpdateFix
                    {
                        Description = "Clear Windows Update cache",
                        Command = "Stop services, delete cache, restart services",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    break;

                case "Database":
                    fixes.Add(new WindowsUpdateFix
                    {
                        Description = "Reset Windows Update database",
                        Command = "Use Windows Update troubleshooter",
                        RequiresRestart = true,
                        RiskLevel = "Medium"
                    });
                    break;

                case "Component":
                    fixes.Add(new WindowsUpdateFix
                    {
                        Description = $"Enable and start {issue.Component} service",
                        Command = $"sc config {issue.Component} start= auto && net start {issue.Component}",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    break;
            }
        }

        return fixes;
    }

    private string GenerateFixScript(List<WindowsUpdateIssue> issues, List<WindowsUpdateFix> fixes, FixWindowsUpdateArgs args)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo Windows Update Fix Tool");
        sb.AppendLine("echo =======================");
        sb.AppendLine("echo This script will fix common Windows Update issues");
        sb.AppendLine("echo.");
        
        if (args.CreateBackup)
        {
            sb.AppendLine("echo Creating system restore point...");
            sb.AppendLine("powershell -Command \"Checkpoint-Computer -Description 'Pre-Windows Update Fix' -RestorePointType 'MODIFY_SETTINGS'\"");
            sb.AppendLine("timeout /t 10 /nobreak > nul");
            sb.AppendLine();
        }

        sb.AppendLine("echo Stopping Windows Update services...");
        sb.AppendLine("net stop wuauserv");
        sb.AppendLine("net stop bits");
        sb.AppendLine("net stop cryptsvc");
        sb.AppendLine("net stop msiserver");
        sb.AppendLine("timeout /t 5 /nobreak > nul");
        sb.AppendLine();

        sb.AppendLine("echo Renaming Windows Update folders...");
        sb.AppendLine("ren C:\\Windows\\SoftwareDistribution SoftwareDistribution.old");
        sb.AppendLine("ren C:\\Windows\\System32\\catroot2 catroot2.old");
        sb.AppendLine("timeout /t 3 /nobreak > nul");
        sb.AppendLine();

        sb.AppendLine("echo Resetting Windows Update components...");
        sb.AppendLine("sc config wuauserv start= auto");
        sb.AppendLine("sc config bits start= delayed-auto");
        sb.AppendLine("sc config cryptsvc start= auto");
        sb.AppendLine("sc config msiserver start= demand");
        sb.AppendLine();

        sb.AppendLine("echo Starting Windows Update services...");
        sb.AppendLine("net start msiserver");
        sb.AppendLine("net start cryptsvc");
        sb.AppendLine("net start bits");
        sb.AppendLine("net start wuauserv");
        sb.AppendLine("timeout /t 5 /nobreak > nul");
        sb.AppendLine();

        if (args.RunTroubleshooter)
        {
            sb.AppendLine("echo Running Windows Update troubleshooter...");
            sb.AppendLine("msdt.exe /id WindowsUpdateDiagnostic");
            sb.AppendLine("timeout /t 10 /nobreak > nul");
            sb.AppendLine();
        }

        sb.AppendLine("echo Windows Update fix complete!");
        sb.AppendLine("echo Please restart your computer to complete the fixes.");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }

    private string CreateUndoScript(List<WindowsUpdateIssue> issues)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo Windows Update Fix Undo");
        sb.AppendLine("echo =======================");
        sb.AppendLine("echo To undo Windows Update fixes:");
        sb.AppendLine("echo 1. Use System Restore to revert to a previous point");
        sb.AppendLine("echo 2. Restore the renamed folders if needed:");
        sb.AppendLine("echo    - Rename SoftwareDistribution.old back to SoftwareDistribution");
        sb.AppendLine("echo    - Rename catroot2.old back to catroot2");
        sb.AppendLine("echo 3. Check Windows Update service status:");
        sb.AppendLine("echo    - Open services.msc");
        sb.AppendLine("echo    - Ensure Windows Update service is running");
        sb.AppendLine("echo 4. Run Windows Update troubleshooter if issues persist");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }
}

public class FixWindowsUpdateArgs
{
    public bool CreateBackup { get; set; } = true;
    public bool RunTroubleshooter { get; set; } = true;
    public bool ResetComponents { get; set; } = true;
    public bool ClearCache { get; set; } = true;
}

public class WindowsUpdateIssue
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
}

public class WindowsUpdateFix
{
    public string Description { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public bool RequiresRestart { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
}
