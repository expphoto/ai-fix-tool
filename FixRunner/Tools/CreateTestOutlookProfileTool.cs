using System.Text.Json;
using NJsonSchema;

namespace FixRunner.Tools;

public class CreateTestOutlookProfileTool : ITool
{
    public string Name => "CreateTestOutlookProfile";
    public string Description => "Creates a new Outlook profile and sets it as default to isolate profile corruption";
    public bool RequiresAdmin => false;

    public JsonSchema Schema => JsonSchema.FromType<CreateTestOutlookProfileArgs>();

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        var args = JsonSerializer.Deserialize<CreateTestOutlookProfileArgs>(arguments);

        var ps = GenerateScript(args);
        var undo = GenerateUndoScript(args);

        var data = new { ProfileName = args.ProfileName };

        return new ToolResult
        {
            Success = true,
            Data = data,
            Script = ps,
            UndoScript = undo
        };
    }

    private string GenerateScript(CreateTestOutlookProfileArgs args)
    {
        var profile = args.ProfileName;
        var sb = new StringBuilder();
        sb.AppendLine("# Create Test Outlook Profile (PowerShell)");
        sb.AppendLine("$ErrorActionPreference = 'Stop'");
        sb.AppendLine("$ver = '16.0'");
        sb.AppendLine($"$outKey = \"HKCU:\\Software\\Microsoft\\Office\\$ver\\Outlook\"");
        sb.AppendLine("New-Item -Path $outKey -Force | Out-Null");
        sb.AppendLine($"Set-ItemProperty -Path $outKey -Name DefaultProfile -Value '{profile}' -Force");
        sb.AppendLine($"Write-Host \"Default Outlook profile set to '{profile}'.\" -ForegroundColor Yellow");
        sb.AppendLine("# Launch Outlook to initialize profile (user may need to complete wizard)");
        sb.AppendLine($"Start-Process -FilePath 'outlook.exe' -ArgumentList '/profiles' ");
        return sb.ToString();
    }

    private string GenerateUndoScript(CreateTestOutlookProfileArgs args)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Undo Create Test Outlook Profile (PowerShell)");
        sb.AppendLine("$ver = '16.0'");
        sb.AppendLine("$outKey = \"HKCU:\\Software\\Microsoft\\Office\\$ver\\Outlook\"");
        sb.AppendLine("# This will clear DefaultProfile so Outlook asks again on next start");
        sb.AppendLine("Remove-ItemProperty -Path $outKey -Name DefaultProfile -ErrorAction SilentlyContinue");
        return sb.ToString();
    }
}

public class CreateTestOutlookProfileArgs
{
    public string ProfileName { get; set; } = "FixRunnerTestProfile";
}

