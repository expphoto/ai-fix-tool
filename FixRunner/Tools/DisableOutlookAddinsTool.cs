using System.Text.Json;
using NJsonSchema;

namespace FixRunner.Tools;

public class DisableOutlookAddinsTool : ITool
{
    public string Name => "DisableOutlookAddins";
    public string Description => "Disables Outlook COM add-ins to isolate crashes/startup issues";
    public bool RequiresAdmin => true; // machine-wide add-ins may require admin

    public JsonSchema Schema => JsonSchema.FromType<DisableOutlookAddinsArgs>();

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        var args = JsonSerializer.Deserialize<DisableOutlookAddinsArgs>(arguments);

        var ps = GenerateScript(args);
        var undo = GenerateUndoScript(args);

        var data = new
        {
            TargetScope = args.Scope,
            Backup = args.BackupRegistry
        };

        return new ToolResult
        {
            Success = true,
            Data = data,
            Script = ps,
            UndoScript = undo
        };
    }

    private string GenerateScript(DisableOutlookAddinsArgs args)
    {
        var scopeRoot = args.Scope == "AllUsers" ? "HKLM" : "HKCU";
        var backupDir = "$env:ProgramData/FixRunner/backups";
        var sb = new StringBuilder();
        sb.AppendLine("# Disable Outlook Add-ins (PowerShell)");
        sb.AppendLine("$ErrorActionPreference = 'Stop'");
        sb.AppendLine($"$backupDir = \"{backupDir}\"");
        sb.AppendLine("New-Item -ItemType Directory -Path $backupDir -Force | Out-Null");
        if (args.BackupRegistry)
        {
            sb.AppendLine($"reg export {scopeRoot}\\Software\\Microsoft\\Office\\Outlook\\Addins \"$backupDir\\outlook_addins_{args.Scope.ToLower()}.reg\" /y 2>$null");
        }
        sb.AppendLine($"$key = \"{scopeRoot}:\\Software\\Microsoft\\Office\\Outlook\\Addins\"");
        sb.AppendLine("if (Test-Path $key) {");
        sb.AppendLine("  Get-ChildItem $key | ForEach-Object { New-ItemProperty -Path $_.PsPath -Name LoadBehavior -PropertyType DWord -Value 0 -Force | Out-Null }");
        sb.AppendLine("  Write-Host \"All add-ins under scope disabled (LoadBehavior=0).\" -ForegroundColor Yellow");
        sb.AppendLine("} else { Write-Host \"No add-ins registry key found.\" }");
        return sb.ToString();
    }

    private string GenerateUndoScript(DisableOutlookAddinsArgs args)
    {
        var backupDir = "$env:ProgramData/FixRunner/backups";
        var sb = new StringBuilder();
        sb.AppendLine("# Undo Disable Outlook Add-ins (PowerShell)");
        sb.AppendLine($"$backup = \"{backupDir}\\outlook_addins_{args.Scope.ToLower()}.reg\"");
        sb.AppendLine("if (Test-Path $backup) { Write-Host 'Restoring add-ins registry from backup...'; reg import \"$backup\" } else { Write-Host 'No registry backup found; manual re-enable may be required.' }");
        return sb.ToString();
    }
}

public class DisableOutlookAddinsArgs
{
    // Scope: CurrentUser or AllUsers
    public string Scope { get; set; } = "CurrentUser";
    public bool BackupRegistry { get; set; } = true;
}

