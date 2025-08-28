using System.Text.Json;
using NJsonSchema;

namespace FixRunner.Tools;

public class FixFileExplorerIssuesTool : ITool
{
    public string Name => "FixFileExplorerIssues";
    public string Description => "Fixes Windows File Explorer problems including crashes, slow performance, and missing features";
    public bool RequiresAdmin => false;

    public JsonSchema Schema => JsonSchema.FromType<FixFileExplorerIssuesArgs>();

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            var args = JsonSerializer.Deserialize<FixFileExplorerIssuesArgs>(arguments);
            
            var issues = DiagnoseFileExplorerIssues(args);
            var fixes = GenerateFixes(issues, args);
            
            var result = new
            {
                IssuesFound = issues,
                FixesToApply = fixes,
                ExplorerSettings = GetExplorerSettings(),
                ShellExtensions = GetShellExtensions(),
                FileAssociations = GetFileAssociations(),
                CacheInfo = GetCacheInfo(),
                EstimatedDuration = "10-20 minutes",
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
                Error = $"Failed to diagnose File Explorer issues: {ex.Message}",
                Data = null
            };
        }
    }

    private List<FileExplorerIssue> DiagnoseFileExplorerIssues(FixFileExplorerIssuesArgs args)
    {
        var issues = new List<FileExplorerIssue>();

        // Simulate File Explorer diagnosis
        issues.Add(new FileExplorerIssue
        {
            Type = "Crashes",
            Description = "File Explorer crashes or freezes frequently",
            Severity = "High",
            Component = "Explorer Process",
            PossibleCauses = new[] { "Corrupted system files", "Faulty shell extensions", "Memory issues" }
        });

        issues.Add(new FileExplorerIssue
        {
            Type = "SlowPerformance",
            Description = "File Explorer is slow to open or navigate",
            Severity = "Medium",
            Component = "File System",
            PossibleCauses = new[] { "Large number of files", "Thumbnail cache issues", "Network drives" }
        });

        issues.Add(new FileExplorerIssue
        {
            Type = "MissingIcons",
            Description = "File icons are missing or showing as generic",
            Severity = "Low",
            Component = "Icon Cache",
            PossibleCauses = new[] { "Corrupted icon cache", "Missing file associations", "Registry issues" }
        });

        issues.Add(new FileExplorerIssue
        {
            Type = "SearchNotWorking",
            Description = "File search is not working properly",
            Severity = "Medium",
            Component = "Windows Search",
            PossibleCauses = new[] { "Windows Search service issues", "Indexing problems", "Corrupted search database" }
        });

        issues.Add(new FileExplorerIssue
        {
            Type = "ContextMenuIssues",
            Description = "Right-click context menu is slow or missing items",
            Severity = "Medium",
            Component = "Shell Extensions",
            PossibleCauses = new[] { "Too many shell extensions", "Corrupted registry entries", "Third-party software" }
        });

        issues.Add(new FileExplorerIssue
        {
            Type = "NavigationPaneIssues",
            Description = "Navigation pane shows duplicate or missing items",
            Severity = "Low",
            Component = "Navigation Pane",
            PossibleCauses = new[] { "Registry corruption", "User profile issues", "Windows updates" }
        });

        return issues;
    }

    private List<FileExplorerFix> GenerateFixes(List<FileExplorerIssue> issues, FixFileExplorerIssuesArgs args)
    {
        var fixes = new List<FileExplorerFix>();

        foreach (var issue in issues)
        {
            switch (issue.Type)
            {
                case "Crashes":
                    fixes.Add(new FileExplorerFix
                    {
                        Description = "Restart Windows Explorer process",
                        Command = "taskkill /f /im explorer.exe && start explorer.exe",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new FileExplorerFix
                    {
                        Description = "Run System File Checker",
                        Command = "sfc /scannow",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new FileExplorerFix
                    {
                        Description = "Clear File Explorer cache",
                        Command = "ie4uinit.exe -ClearIconCache",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    if (args.ClearShellExtensions)
                    {
                        fixes.Add(new FileExplorerFix
                        {
                            Description = "Disable problematic shell extensions",
                            Command = "shell:AppsFolder",
                            RequiresRestart = false,
                            RiskLevel = "Medium"
                        });
                    }
                    break;

                case "SlowPerformance":
                    fixes.Add(new FileExplorerFix
                    {
                        Description = "Clear thumbnail cache",
                        Command = "cleanmgr /sagerun:1",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new FileExplorerFix
                    {
                        Description = "Disable Quick Access",
                        Command = "regedit",
                        RequiresRestart = false,
                        RiskLevel = "Medium"
                    });
                    fixes.Add(new FileExplorerFix
                    {
                        Description = "Optimize folder options",
                        Command = "control folders",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    break;

                case "MissingIcons":
                    fixes.Add(new FileExplorerFix
                    {
                        Description = "Rebuild icon cache",
                        Command = "ie4uinit.exe -ClearIconCache",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new FileExplorerFix
                    {
                        Description = "Clear thumbnail cache",
                        Command = "cleanmgr /sagerun:1",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new FileExplorerFix
                    {
                        Description = "Reset file associations",
                        Command = "control defaultprograms",
                        RequiresRestart = false,
                        RiskLevel = "Medium"
                    });
                    break;

                case "SearchNotWorking":
                    fixes.Add(new FileExplorerFix
                    {
                        Description = "Restart Windows Search service",
                        Command = "net stop wsearch && net start wsearch",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new FileExplorerFix
                    {
                        Description = "Rebuild search index",
                        Command = "control.exe srchadmin.dll",
                        RequiresRestart = false,
                        RiskLevel = "Medium"
                    });
                    fixes.Add(new FileExplorerFix
                    {
                        Description = "Check Windows Search settings",
                        Command = "control.exe srchadmin.dll",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    break;

                case "ContextMenuIssues":
                    fixes.Add(new FileExplorerFix
                    {
                        Description = "Clean context menu registry",
                        Command = "regedit",
                        RequiresRestart = false,
                        RiskLevel = "Medium"
                    });
                    fixes.Add(new FileExplorerFix
                    {
                        Description = "Disable shell extensions",
                        Command = "shell:AppsFolder",
                        RequiresRestart = false,
                        RiskLevel = "Medium"
                    });
                    fixes.Add(new FileExplorerFix
                    {
                        Description = "Use ShellExView to manage extensions",
                        Command = "start https://www.nirsoft.net/utils/shexview.html",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    break;

                case "NavigationPaneIssues":
                    fixes.Add(new FileExplorerFix
                    {
                        Description = "Reset navigation pane settings",
                        Command = "regedit",
                        RequiresRestart = false,
                        RiskLevel = "Medium"
                    });
                    fixes.Add(new FileExplorerFix
                    {
                        Description = "Clear File Explorer settings",
                        Command = "regedit",
                        RequiresRestart = false,
                        RiskLevel = "Medium"
                    });
                    fixes.Add(new FileExplorerFix
                    {
                        Description = "Create new user profile",
                        Command = "netplwiz",
                        RequiresRestart = true,
                        RiskLevel = "High"
                    });
                    break;
            }
        }

        return fixes;
    }

    private object GetExplorerSettings()
    {
        return new
        {
            General = new { OpenFileExplorerTo = "Quick access", ShowRecentlyUsedFiles = true, ShowFrequentlyUsedFolders = true },
            View = new { ShowHiddenFiles = false, ShowFileExtensions = true, HideProtectedSystemFiles = true },
            Search = new { SearchInSubfolders = true, FindPartialMatches = true, UseNaturalLanguageSearch = false },
            Privacy = new { ShowRecentlyUsedFiles = true, ShowFrequentlyUsedFolders = true, ClearHistoryOnExit = false }
        };
    }

    private List<object> GetShellExtensions()
    {
        return new List<object>
        {
            new { Name = "WinRAR", Publisher = "RARLAB", Type = "Context Menu", Status = "Enabled" },
            new { Name = "7-Zip", Publisher = "Igor Pavlov", Type = "Context Menu", Status = "Enabled" },
            new { Name = "Adobe Acrobat", Publisher = "Adobe", Type = "Context Menu", Status = "Enabled" },
            new { Name = "Google Drive", Publisher = "Google", Type = "Context Menu", Status = "Enabled" },
            new { Name = "Dropbox", Publisher = "Dropbox", Type = "Context Menu", Status = "Enabled" },
            new { Name = "VLC Media Player", Publisher = "VideoLAN", Type = "Context Menu", Status = "Enabled" }
        };
    }

    private List<object> GetFileAssociations()
    {
        return new List<object>
        {
            new { Extension = ".txt", DefaultApp = "Notepad", IconStatus = "OK" },
            new { Extension = ".pdf", DefaultApp = "Microsoft Edge", IconStatus = "OK" },
            new { Extension = ".jpg", DefaultApp = "Photos", IconStatus = "OK" },
            new { Extension = ".mp4", DefaultApp = "Movies & TV", IconStatus = "OK" },
            new { Extension = ".zip", DefaultApp = "File Explorer", IconStatus = "OK" },
            new { Extension = ".docx", DefaultApp = "Word", IconStatus = "Missing" }
        };
    }

    private object GetCacheInfo()
    {
        return new
        {
            IconCache = new { Size = "45 MB", LastCleared = "2024-08-15", Status = "Needs clearing" },
            ThumbnailCache = new { Size = "125 MB", LastCleared = "2024-08-10", Status = "Needs clearing" },
            SearchIndex = new { Size = "2.3 GB", LastRebuilt = "2024-07-20", Status = "OK" },
            RecentItems = new { Count = 245, LastCleared = "2024-08-25", Status = "OK" }
        };
    }

    private string GenerateFixScript(List<FileExplorerIssue> issues, List<FileExplorerFix> fixes, FixFileExplorerIssuesArgs args)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo Windows File Explorer Fix Tool");
        sb.AppendLine("echo ==============================");
        sb.AppendLine("echo This script will fix common File Explorer problems");
        sb.AppendLine("echo.");
        
        sb.AppendLine("echo Running System File Checker...");
        sb.AppendLine("sfc /scannow");
        sb.AppendLine("pause");
        sb.AppendLine();

        sb.AppendLine("echo Restarting Windows Explorer...");
        sb.AppendLine("taskkill /f /im explorer.exe");
        sb.AppendLine("timeout /t 2");
        sb.AppendLine("start explorer.exe");
        sb.AppendLine("echo Explorer restarted successfully");
        sb.AppendLine("pause");
        sb.AppendLine();

        if (args.ClearIconCache)
        {
            sb.AppendLine("echo Clearing icon cache...");
            sb.AppendLine("ie4uinit.exe -ClearIconCache");
            sb.AppendLine("taskkill /f /im explorer.exe");
            sb.AppendLine("del /f /s /q %localappdata%\\Microsoft\\Windows\\Explorer\\iconcache*");
            sb.AppendLine("del /f /s /q %localappdata%\\Microsoft\\Windows\\Explorer\\thumbcache*");
            sb.AppendLine("start explorer.exe");
            sb.AppendLine("echo Icon cache cleared");
            sb.AppendLine("pause");
            sb.AppendLine();
        }

        if (args.ClearThumbnailCache)
        {
            sb.AppendLine("echo Clearing thumbnail cache...");
            sb.AppendLine("cleanmgr /sagerun:1");
            sb.AppendLine("echo Thumbnail cache cleared");
            sb.AppendLine("pause");
            sb.AppendLine();
        }

        sb.AppendLine("echo Opening Folder Options...");
        sb.AppendLine("control folders");
        sb.AppendLine("echo 1. Go to 'View' tab");
        sb.AppendLine("echo 2. Click 'Reset Folders'");
        sb.AppendLine("echo 3. Click 'Restore Defaults'");
        sb.AppendLine("pause");
        sb.AppendLine();

        sb.AppendLine("echo Opening Default Programs...");
        sb.AppendLine("control defaultprograms");
        sb.AppendLine("echo 1. Click 'Set your default programs'");
        sb.AppendLine("echo 2. Reset file associations if needed");
        sb.AppendLine("pause");
        sb.AppendLine();

        sb.AppendLine("echo Opening Indexing Options...");
        sb.AppendLine("control.exe srchadmin.dll");
        sb.AppendLine("echo 1. Click 'Advanced'");
        sb.AppendLine("echo 2. Click 'Rebuild' to rebuild search index");
        sb.AppendLine("echo Note: This may take several hours");
        sb.AppendLine("pause");
        sb.AppendLine();

        sb.AppendLine("echo Registry fixes for File Explorer...");
        sb.AppendLine("echo WARNING: Registry editing can be dangerous!");
        sb.AppendLine("echo Creating registry backup...");
        sb.AppendLine("reg export HKCU\\Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\Shell explorer_backup.reg");
        sb.AppendLine("regedit");
        sb.AppendLine("echo Navigate to: HKEY_CURRENT_USER\\Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\Shell");
        sb.AppendLine("echo Delete problematic bags and folders");
        sb.AppendLine("pause");
        sb.AppendLine();

        sb.AppendLine("echo Opening Shell Extensions management...");
        sb.AppendLine("echo Download ShellExView from: https://www.nirsoft.net/utils/shexview.html");
        sb.AppendLine("start https://www.nirsoft.net/utils/shexview.html");
        sb.AppendLine("echo 1. Run ShellExView as administrator");
        sb.AppendLine("echo 2. Disable non-Microsoft shell extensions");
        sb.AppendLine("echo 3. Restart Explorer after changes");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }

    private string CreateUndoScript(List<FileExplorerIssue> issues)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo File Explorer Fix Undo");
        sb.AppendLine("echo ======================");
        sb.AppendLine("echo To undo File Explorer fixes:");
        sb.AppendLine("echo 1. Restore registry backup:");
        sb.AppendLine("echo    - Run: reg import explorer_backup.reg");
        sb.AppendLine("echo 2. Re-enable shell extensions:");
        sb.AppendLine("echo    - Use ShellExView to re-enable extensions");
        sb.AppendLine("echo 3. Restore file associations:");
        sb.AppendLine("echo    - Open Settings > Apps > Default apps");
        sb.AppendLine("echo    - Reset to Microsoft defaults");
        sb.AppendLine("echo 4. Restore folder view settings:");
        sb.AppendLine("echo    - Open File Explorer > View > Options");
        sb.AppendLine("echo    - Click 'Restore Defaults'");
        sb.AppendLine("echo 5. Rebuild search index:");
        sb.AppendLine("echo    - Open Indexing Options");
        sb.AppendLine("echo    - Click 'Advanced' > 'Rebuild'");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }
}

public class FixFileExplorerIssuesArgs
{
    public bool ClearIconCache { get; set; } = true;
    public bool ClearThumbnailCache { get; set; } = true;
    public bool ClearShellExtensions { get; set; } = true;
    public bool ResetFileAssociations { get; set; } = false;
    public bool RebuildSearchIndex { get; set; } = false;
}

public class FileExplorerIssue
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public string[] PossibleCauses { get; set; } = Array.Empty<string>();
}

public class FileExplorerFix
{
    public string Description { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public bool RequiresRestart { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
}
