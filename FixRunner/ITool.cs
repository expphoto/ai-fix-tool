using System.Text.Json;
using NJsonSchema;

namespace FixRunner;

public interface ITool
{
    string Name { get; }
    string Description { get; }
    bool RequiresAdmin { get; }
    
    JsonSchema Schema { get; }
    
    Task<ToolResult> ExecuteAsync(JsonElement arguments);
}

public class ToolResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public object? Data { get; set; }
    public string? UndoScript { get; set; }
    public string? Script { get; set; }
}

public class ToolSchema
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresAdmin { get; set; }
    public JsonSchema Schema { get; set; } = JsonSchema.CreateAnySchema();
}
