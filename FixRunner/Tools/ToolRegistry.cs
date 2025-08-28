using System.Text.Json;
using FixRunner.Tools;

namespace FixRunner;

public class ToolRegistry
{
    private readonly Dictionary<string, ITool> _tools;

    public ToolRegistry()
    {
        _tools = new Dictionary<string, ITool>(StringComparer.OrdinalIgnoreCase)
        {
            { "CheckOfficeInstallation", new CheckOfficeInstallationTool() },
            { "RepairOffice", new RepairOfficeTool() },
            { "ResetNetworkAdapter", new ResetNetworkAdapterTool() },
            { "DisableOutlookAddins", new DisableOutlookAddinsTool() },
            { "CreateTestOutlookProfile", new CreateTestOutlookProfileTool() },
            { "ResetWinHTTP", new ResetWinHTTPTool() },
            { "CleanDiskSpace", new CleanDiskSpaceTool() },
            { "CheckSystemHealth", new CheckSystemHealthTool() }
        };
    }

    public ITool GetTool(string name)
    {
        if (_tools.TryGetValue(name, out var tool))
        {
            return tool;
        }

        throw new KeyNotFoundException($"Tool '{name}' not found");
    }

    public List<ToolSchema> GetToolSchemas()
    {
        return _tools.Values.Select(tool => new ToolSchema
        {
            Name = tool.Name,
            Description = tool.Description,
            RequiresAdmin = tool.RequiresAdmin,
            Schema = tool.Schema
        }).ToList();
    }

    public bool HasTool(string name)
    {
        return _tools.ContainsKey(name);
    }
}
