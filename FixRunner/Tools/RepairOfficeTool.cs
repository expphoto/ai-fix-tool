using System.Text.Json;
using NJsonSchema;

namespace FixRunner.Tools;

public class RepairOfficeTool : ITool
{
    public string Name => "RepairOffice";
    public string Description => "Performs a repair operation on Microsoft Office installation";
    public bool RequiresAdmin => true;

    public JsonSchema Schema => JsonSchema.FromType<RepairOfficeArgs>();

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            var args = JsonSerializer.Deserialize<RepairOfficeArgs>(arguments);
            
            var result = new
            {
                RepairType = args.RepairType,
                QuickRepair = args.RepairType == "Quick",
                OnlineRepair = args.RepairType == "Online",
                OfficeApps = args.OfficeApps,
                Started = DateTime.UtcNow,
                EstimatedDuration = args.RepairType == "Quick" ? "5-10 minutes" : "30-60 minutes"
            };

            // Generate the actual repair command
            var repairCommand = GenerateRepairScriptPS(args);
            
            // Create undo script
            var undoScript = CreateUndoScript(args);

            return new ToolResult
            {
                Success = true,
                Data = result,
                Error = null,
                UndoScript = undoScript,
                Script = repairCommand
            };
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                Success = false,
                Error = $"Failed to prepare Office repair: {ex.Message}",
                Data = null
            };
        }
    }

    private string GenerateRepairScriptPS(RepairOfficeArgs args)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Office Repair (PowerShell)");
        sb.AppendLine("Write-Host \"Starting Office repair...\" -ForegroundColor Cyan");
        sb.AppendLine("$exe = \"C:\\Program Files\\Common Files\\Microsoft Shared\\ClickToRun\\OfficeClickToRun.exe\"");
        sb.AppendLine("if (-Not (Test-Path $exe)) { $exe = \"C:\\Program Files (x86)\\Common Files\\Microsoft Shared\\ClickToRun\\OfficeClickToRun.exe\" }");
        sb.AppendLine("if (-Not (Test-Path $exe)) { throw \"OfficeClickToRun.exe not found.\" }");
        if (args.ForceCloseApps)
        {
            sb.AppendLine("Write-Host \"Closing running Office apps...\"");
            sb.AppendLine("Get-Process winword, excel, powerpnt, outlook -ErrorAction SilentlyContinue | Stop-Process -Force");
        }
        if (args.RepairType == "Quick")
        {
            sb.AppendLine("Write-Host \"Performing Quick Repair...\"");
            sb.AppendLine("Start-Process -FilePath $exe -ArgumentList 'scenario=Repair platform=x86 culture=en-us RepairType=QuickRepair DisplayLevel=True' -Wait");
        }
        else
        {
            sb.AppendLine("Write-Host \"Performing Online Repair...\"");
            sb.AppendLine("Start-Process -FilePath $exe -ArgumentList 'scenario=Repair platform=x86 culture=en-us RepairType=FullRepair DisplayLevel=True' -Wait");
        }
        sb.AppendLine("Write-Host \"Office repair initiated. Follow on-screen instructions.\" -ForegroundColor Green");
        return sb.ToString();
    }

    private string CreateUndoScript(RepairOfficeArgs args)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo Office repair undo script");
        sb.AppendLine("echo Note: Office repair operations cannot be easily undone.");
        sb.AppendLine("echo If issues persist, consider:");
        sb.AppendLine("echo 1. Running System Restore to a previous point");
        sb.AppendLine("echo 2. Reinstalling Office completely");
        sb.AppendLine("echo 3. Contacting Microsoft Support");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }
}

public class RepairOfficeArgs
{
    public string RepairType { get; set; } = "Quick"; // "Quick" or "Online"
    public List<string> OfficeApps { get; set; } = new() { "Word", "Excel", "PowerPoint", "Outlook" };
    public bool ForceCloseApps { get; set; } = true;
}
