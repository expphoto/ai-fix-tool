using System.Text.Json;
using NJsonSchema;

namespace FixRunner.Tools;

public class ResetWinHTTPTool : ITool
{
    public string Name => "ResetWinHTTP";
    public string Description => "Resets WinHTTP proxy settings and optionally imports IE/WinINET proxy";
    public bool RequiresAdmin => true;

    public JsonSchema Schema => JsonSchema.FromType<ResetWinHTTPArgs>();

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        var args = JsonSerializer.Deserialize<ResetWinHTTPArgs>(arguments);

        var ps = GenerateScript(args);
        var undo = GenerateUndoScript();

        return new ToolResult
        {
            Success = true,
            Data = new { args.ResetProxy, args.ImportIEProxy },
            Script = ps,
            UndoScript = undo
        };
    }

    private string GenerateScript(ResetWinHTTPArgs args)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Reset WinHTTP Proxy (PowerShell)");
        sb.AppendLine("$ErrorActionPreference = 'Stop'");
        sb.AppendLine("$backupDir = Join-Path $env:ProgramData 'FixRunner\\backups'");
        sb.AppendLine("New-Item -ItemType Directory -Path $backupDir -Force | Out-Null");
        sb.AppendLine("$backupFile = Join-Path $backupDir 'winhttp-proxy.txt'");
        sb.AppendLine("Write-Host 'Saving current WinHTTP proxy to backup...'" );
        sb.AppendLine("netsh winhttp show proxy > \"$backupFile\"");
        if (args.ResetProxy)
        {
            sb.AppendLine("Write-Host 'Resetting WinHTTP proxy to direct access...' -ForegroundColor Yellow");
            sb.AppendLine("netsh winhttp reset proxy");
        }
        if (args.ImportIEProxy)
        {
            sb.AppendLine("Write-Host 'Importing proxy settings from WinINET/IE...' -ForegroundColor Yellow");
            sb.AppendLine("netsh winhttp import proxy source=ie");
        }
        sb.AppendLine("Write-Host 'WinHTTP proxy reset complete.' -ForegroundColor Green");
        return sb.ToString();
    }

    private string GenerateUndoScript()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Undo WinHTTP Proxy Reset (PowerShell)");
        sb.AppendLine("$backupFile = Join-Path $env:ProgramData 'FixRunner\\backups\\winhttp-proxy.txt'");
        sb.AppendLine("if (Test-Path $backupFile) {");
        sb.AppendLine("  $content = Get-Content $backupFile -Raw");
        sb.AppendLine("  if ($content -match 'Proxy Server\(s\):\s*(.+)') {");
        sb.AppendLine("    $proxy = $Matches[1].Trim()");
        sb.AppendLine("    if ($proxy -and $proxy -ne 'none') { Write-Host \"Restoring previous proxy: $proxy\"; netsh winhttp set proxy $proxy } else { Write-Host 'Previous state was direct access; resetting proxy to direct.'; netsh winhttp reset proxy }");
        sb.AppendLine("  } else { Write-Host 'Backup did not contain a parsable proxy; resetting to direct.'; netsh winhttp reset proxy }");
        sb.AppendLine("} else { Write-Host 'No backup file found; resetting to direct.'; netsh winhttp reset proxy }");
        return sb.ToString();
    }
}

public class ResetWinHTTPArgs
{
    public bool ResetProxy { get; set; } = true;
    public bool ImportIEProxy { get; set; } = false;
}

