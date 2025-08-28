using System.Text.Json;
using NJsonSchema;

namespace FixRunner.Tools;

public class CheckOfficeInstallationTool : ITool
{
    public string Name => "CheckOfficeInstallation";
    public string Description => "Checks the health and status of Microsoft Office installation";
    public bool RequiresAdmin => false;

    public JsonSchema Schema => JsonSchema.FromType<CheckOfficeInstallationArgs>();

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            // Parse arguments
            var args = JsonSerializer.Deserialize<CheckOfficeInstallationArgs>(arguments);
            
            // Check Office installation status
            var officePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Microsoft Office"
            );

            var officeExists = Directory.Exists(officePath);
            var officeVersion = GetOfficeVersion();
            var activationStatus = GetOfficeActivationStatus();
            
            var result = new
            {
                OfficeInstalled = officeExists,
                Version = officeVersion,
                ActivationStatus = activationStatus,
                OfficePath = officeExists ? officePath : null,
                Issues = new List<string>()
            };

            // Check for common issues
            if (!officeExists)
            {
                result.Issues.Add("Microsoft Office is not installed or not found in standard location");
            }
            else if (string.IsNullOrEmpty(officeVersion))
            {
                result.Issues.Add("Unable to determine Office version");
            }
            else if (!activationStatus.Contains("Licensed"))
            {
                result.Issues.Add("Office activation issue detected");
            }

            return new ToolResult
            {
                Success = true,
                Data = result,
                Error = null
            };
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                Success = false,
                Error = $"Failed to check Office installation: {ex.Message}",
                Data = null
            };
        }
    }

    private string GetOfficeVersion()
    {
        try
        {
            // Check registry for Office version
            var officeKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Office\ClickToRun\Configuration");
            
            if (officeKey != null)
            {
                var version = officeKey.GetValue("VersionToReport")?.ToString();
                if (!string.IsNullOrEmpty(version))
                {
                    return version;
                }
            }

            // Fallback to checking Office 16.0
            officeKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Office\16.0\Common\InstallRoot");
            
            if (officeKey != null)
            {
                return "16.0";
            }

            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private string GetOfficeActivationStatus()
    {
        try
        {
            // This is a simplified check - in production, you'd use Office COM APIs
            return "License Status Unknown (requires Office COM API)";
        }
        catch
        {
            return "Unable to check activation status";
        }
    }
}

public class CheckOfficeInstallationArgs
{
    public bool IncludeActivationCheck { get; set; } = true;
}
