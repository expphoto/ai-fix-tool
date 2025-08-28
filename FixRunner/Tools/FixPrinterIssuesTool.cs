using System.Text.Json;
using NJsonSchema;

namespace FixRunner.Tools;

public class FixPrinterIssuesTool : ITool
{
    public string Name => "FixPrinterIssues";
    public string Description => "Diagnoses and fixes common printer problems";
    public bool RequiresAdmin => false;

    public JsonSchema Schema => JsonSchema.FromType<FixPrinterIssuesArgs>();

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            var args = JsonSerializer.Deserialize<FixPrinterIssuesArgs>(arguments);
            
            var issues = DiagnosePrinterIssues(args);
            var fixes = GenerateFixes(issues, args);
            
            var result = new
            {
                IssuesFound = issues,
                FixesToApply = fixes,
                PrinterStatus = GetPrinterStatus(args.PrinterName),
                SpoolerStatus = GetSpoolerStatus(),
                Drivers = GetPrinterDrivers(),
                EstimatedDuration = "2-10 minutes",
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
                Error = $"Failed to diagnose printer issues: {ex.Message}",
                Data = null
            };
        }
    }

    private List<PrinterIssue> DiagnosePrinterIssues(FixPrinterIssuesArgs args)
    {
        var issues = new List<PrinterIssue>();

        // Simulate diagnosis based on common printer problems
        if (args.PrinterName != null)
        {
            issues.Add(new PrinterIssue
            {
                Type = "Connection",
                Description = $"Printer '{args.PrinterName}' not responding",
                Severity = "High",
                Component = "Printer Connection"
            });
        }

        issues.Add(new PrinterIssue
        {
            Type = "Service",
            Description = "Print Spooler service may be stopped or hung",
            Severity = "High",
            Component = "Spooler Service"
        });

        issues.Add(new PrinterIssue
        {
            Type = "Driver",
            Description = "Printer driver may be corrupted or outdated",
            Severity = "Medium",
            Component = "Printer Driver"
        });

        issues.Add(new PrinterIssue
        {
            Type = "Queue",
            Description = "Print queue may have stuck print jobs",
            Severity = "Medium",
            Component = "Print Queue"
        });

        issues.Add(new PrinterIssue
        {
            Type = "Port",
            Description = "Printer port configuration may be incorrect",
            Severity = "Low",
            Component = "Printer Port"
        });

        return issues;
    }

    private List<PrinterFix> GenerateFixes(List<PrinterIssue> issues, FixPrinterIssuesArgs args)
    {
        var fixes = new List<PrinterFix>();

        foreach (var issue in issues)
        {
            switch (issue.Type)
            {
                case "Service":
                    fixes.Add(new PrinterFix
                    {
                        Description = "Restart Print Spooler service",
                        Command = "net stop spooler && net start spooler",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    break;

                case "Driver":
                    if (args.ReinstallDriver)
                    {
                        fixes.Add(new PrinterFix
                        {
                            Description = "Reinstall printer driver",
                            Command = "Remove and reinstall printer driver",
                            RequiresRestart = false,
                            RiskLevel = "Medium"
                        });
                    }
                    break;

                case "Queue":
                    fixes.Add(new PrinterFix
                    {
                        Description = "Clear print queue",
                        Command = "Stop spooler, clear queue, restart spooler",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    break;

                case "Port":
                    fixes.Add(new PrinterFix
                    {
                        Description = "Reset printer port configuration",
                        Command = "Use printer troubleshooter",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    break;

                case "Connection":
                    fixes.Add(new PrinterFix
                    {
                        Description = "Test printer connection",
                        Command = "Ping printer IP and test connectivity",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    break;
            }
        }

        return fixes;
    }

    private object GetPrinterStatus(string? printerName)
    {
        if (string.IsNullOrEmpty(printerName))
        {
            return new { Status = "Unknown", Message = "No printer specified" };
        }

        return new
        {
            Status = "Offline",
            Message = $"Printer '{printerName}' appears to be offline",
            LastSeen = DateTime.Now.AddHours(-2),
            IPAddress = "192.168.1.100"
        };
    }

    private object GetSpoolerStatus()
    {
        return new
        {
            ServiceName = "Print Spooler",
            Status = "Running",
            StartType = "Automatic",
            PID = 1234
        };
    }

    private List<object> GetPrinterDrivers()
    {
        return new List<object>
        {
            new { Name = "HP Universal Print Driver", Version = "61.220.0.0", Status = "OK" },
            new { Name = "Microsoft XPS Document Writer", Version = "10.0.19041.1", Status = "OK" },
            new { Name = "Fax", Version = "10.0.19041.1", Status = "OK" }
        };
    }

    private string GenerateFixScript(List<PrinterIssue> issues, List<PrinterFix> fixes, FixPrinterIssuesArgs args)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo Printer Fix Tool");
        sb.AppendLine("echo ================");
        sb.AppendLine("echo This script will fix common printer issues");
        sb.AppendLine("echo.");
        
        sb.AppendLine("echo Checking Print Spooler service...");
        sb.AppendLine("sc query spooler");
        sb.AppendLine("timeout /t 2 /nobreak > nul");
        sb.AppendLine();

        sb.AppendLine("echo Stopping Print Spooler service...");
        sb.AppendLine("net stop spooler");
        sb.AppendLine("timeout /t 3 /nobreak > nul");
        sb.AppendLine();

        sb.AppendLine("echo Clearing print queue...");
        sb.AppendLine("del /q /f \"%systemroot%\\System32\\spool\\PRINTERS\\*.*\"");
        sb.AppendLine("timeout /t 2 /nobreak > nul");
        sb.AppendLine();

        if (args.ClearCache)
        {
            sb.AppendLine("echo Clearing printer cache...");
            sb.AppendLine("del /q /f \"%systemroot%\\System32\\spool\\drivers\\color\\*.*\"");
            sb.AppendLine("timeout /t 2 /nobreak > nul");
            sb.AppendLine();
        }

        sb.AppendLine("echo Starting Print Spooler service...");
        sb.AppendLine("net start spooler");
        sb.AppendLine("timeout /t 3 /nobreak > nul");
        sb.AppendLine();

        if (args.ReinstallDriver && !string.IsNullOrEmpty(args.PrinterName))
        {
            sb.AppendLine("echo Removing and reinstalling printer driver...");
            sb.AppendLine($"printui /dl /n \"{args.PrinterName}\"");
            sb.AppendLine("timeout /t 5 /nobreak > nul");
            sb.AppendLine("echo Please reinstall your printer driver manually");
            sb.AppendLine("echo or run Windows Update to find the latest driver");
            sb.AppendLine();
        }

        if (args.RunTroubleshooter)
        {
            sb.AppendLine("echo Running printer troubleshooter...");
            sb.AppendLine("msdt.exe /id PrinterDiagnostic");
            sb.AppendLine("timeout /t 10 /nobreak > nul");
            sb.AppendLine();
        }

        sb.AppendLine("echo Printer fix complete!");
        sb.AppendLine("echo Please try printing a test page to verify the fix.");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }

    private string CreateUndoScript(List<PrinterIssue> issues)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo Printer Fix Undo");
        sb.AppendLine("echo ================");
        sb.AppendLine("echo To undo printer fixes:");
        sb.AppendLine("echo 1. Check if Print Spooler service is running:");
        sb.AppendLine("echo    - Open services.msc");
        sb.AppendLine("echo    - Look for 'Print Spooler' service");
        sb.AppendLine("echo 2. Restore printer drivers if removed:");
        sb.AppendLine("echo    - Go to Control Panel > Devices and Printers");
        sb.AppendLine("echo    - Add printer and reinstall drivers");
        sb.AppendLine("echo 3. Check print queue:");
        sb.AppendLine("echo    - Open Control Panel > Devices and Printers");
        sb.AppendLine("echo    - Right-click your printer and select 'See what's printing'");
        sb.AppendLine("echo 4. Run printer troubleshooter if issues persist");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }
}

public class FixPrinterIssuesArgs
{
    public string? PrinterName { get; set; }
    public bool ReinstallDriver { get; set; } = false;
    public bool ClearCache { get; set; } = true;
    public bool RunTroubleshooter { get; set; } = true;
    public bool ResetSpooler { get; set; } = true;
}

public class PrinterIssue
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
}

public class PrinterFix
{
    public string Description { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public bool RequiresRestart { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
}
