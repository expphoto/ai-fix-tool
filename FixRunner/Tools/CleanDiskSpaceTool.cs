using System.Text.Json;
using NJsonSchema;

namespace FixRunner.Tools;

public class CleanDiskSpaceTool : ITool
{
    public string Name => "CleanDiskSpace";
    public string Description => "Cleans temporary files and frees up disk space";
    public bool RequiresAdmin => false;

    public JsonSchema Schema => JsonSchema.FromType<CleanDiskSpaceArgs>();

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            var args = JsonSerializer.Deserialize<CleanDiskSpaceArgs>(arguments);
            
            var drives = DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
                .ToList();

            var cleanupTargets = new List<string>();
            var estimatedSpace = 0L;

            if (args.CleanTempFiles)
            {
                cleanupTargets.Add("Temporary files");
                estimatedSpace += 500 * 1024 * 1024; // ~500MB estimate
            }

            if (args.CleanRecycleBin)
            {
                cleanupTargets.Add("Recycle Bin");
                estimatedSpace += 100 * 1024 * 1024; // ~100MB estimate
            }

            if (args.CleanBrowserCache)
            {
                cleanupTargets.Add("Browser cache");
                estimatedSpace += 200 * 1024 * 1024; // ~200MB estimate
            }

            if (args.CleanSystemCache)
            {
                cleanupTargets.Add("System cache");
                estimatedSpace += 100 * 1024 * 1024; // ~100MB estimate
            }

            var result = new
            {
                Drives = drives.Select(d => new
                {
                    Name = d.Name,
                    AvailableSpace = d.AvailableFreeSpace,
                    TotalSize = d.TotalSize,
                    FreeSpacePercent = (double)d.AvailableFreeSpace / d.TotalSize * 100
                }),
                CleanupTargets = cleanupTargets,
                EstimatedSpaceToFree = estimatedSpace,
                EstimatedSpaceFormatted = FormatBytes(estimatedSpace),
                RequiresRestart = false
            };

            var cleanupScript = GenerateCleanupScript(args);
            var undoScript = CreateUndoScript(args);

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
                Error = $"Failed to prepare disk cleanup: {ex.Message}",
                Data = null
            };
        }
    }

    private string GenerateCleanupScript(CleanDiskSpaceArgs args)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo Disk Space Cleanup Tool");
        sb.AppendLine("echo =======================");
        sb.AppendLine("echo Starting cleanup process...");
        sb.AppendLine();
        
        if (args.CleanTempFiles)
        {
            sb.AppendLine("echo Cleaning temporary files...");
            sb.AppendLine("del /q /f \"%TEMP%\\*.*\"");
            sb.AppendLine("del /q /f /s \"%WINDIR%\\Temp\\*.*\"");
            sb.AppendLine("del /q /f /s \"%LOCALAPPDATA%\\Temp\\*.*\"");
            sb.AppendLine("timeout /t 2 /nobreak > nul");
            sb.AppendLine();
        }
        
        if (args.CleanRecycleBin)
        {
            sb.AppendLine("echo Emptying Recycle Bin...");
            sb.AppendLine("powershell -Command \"Clear-RecycleBin -Force -ErrorAction SilentlyContinue\"");
            sb.AppendLine("timeout /t 2 /nobreak > nul");
            sb.AppendLine();
        }
        
        if (args.CleanBrowserCache)
        {
            sb.AppendLine("echo Cleaning browser cache...");
            sb.AppendLine("echo Chrome cache...");
            sb.AppendLine("del /q /f /s \"%LOCALAPPDATA%\\Google\\Chrome\\User Data\\Default\\Cache\\*.*\"");
            sb.AppendLine("echo Edge cache...");
            sb.AppendLine("del /q /f /s \"%LOCALAPPDATA%\\Microsoft\\Edge\\User Data\\Default\\Cache\\*.*\"");
            sb.AppendLine("echo Firefox cache...");
            sb.AppendLine("del /q /f /s \"%LOCALAPPDATA%\\Mozilla\\Firefox\\Profiles\\*\\cache2\\*.*\"");
            sb.AppendLine("timeout /t 2 /nobreak > nul");
            sb.AppendLine();
        }
        
        if (args.CleanSystemCache)
        {
            sb.AppendLine("echo Cleaning system cache...");
            sb.AppendLine("echo Running Disk Cleanup utility...");
            sb.AppendLine("cleanmgr /sagerun:1");
            sb.AppendLine("timeout /t 5 /nobreak > nul");
            sb.AppendLine();
        }
        
        sb.AppendLine("echo Cleanup complete!");
        sb.AppendLine("echo You may want to restart your computer to complete the cleanup.");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }

    private string CreateUndoScript(CleanDiskSpaceArgs args)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo Disk Cleanup Undo Script");
        sb.AppendLine("echo ========================");
        sb.AppendLine("echo Note: Deleted files cannot be recovered from standard cleanup.");
        sb.AppendLine("echo To recover deleted files:");
        sb.AppendLine("echo 1. Check Recycle Bin (if not emptied)");
        sb.AppendLine("echo 2. Use file recovery software like Recuva");
        sb.AppendLine("echo 3. Restore from backup if available");
        sb.AppendLine("echo 4. Use System Restore if system files were affected");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

public class CleanDiskSpaceArgs
{
    public bool CleanTempFiles { get; set; } = true;
    public bool CleanRecycleBin { get; set; } = true;
    public bool CleanBrowserCache { get; set; } = false;
    public bool CleanSystemCache { get; set; } = true;
    public long MinFreeSpaceGB { get; set; } = 1;
    public bool SilentMode { get; set; } = false;
}
