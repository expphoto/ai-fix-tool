using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace FixRunner.Services;

public class PlannerClient
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string _baseUrl;
    private readonly string _model;

    public PlannerClient(ILogger logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        _baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "https://api.openai.com/v1";
        _model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";
    }

    public async Task<List<ToolCall>> PlanAsync(string issue, string? facts, List<ToolSchema> availableTools)
    {
        _logger.LogInformation("Planning repair steps for: {Issue}", issue);

        List<ToolCall> plan;
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            try
            {
                plan = await GeneratePlanWithLLM(issue, facts, availableTools);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM planning failed; falling back to rules");
                plan = await GeneratePlanWithRules(issue, facts, availableTools);
            }
        }
        else
        {
            plan = await GeneratePlanWithRules(issue, facts, availableTools);
        }
        
        _logger.LogInformation("Generated plan with {Count} steps", plan.Count);
        return plan;
    }

    private async Task<List<ToolCall>> GeneratePlanWithRules(string issue, string? facts, List<ToolSchema> availableTools)
    {
        var plan = new List<ToolCall>();
        var issueLower = issue.ToLower();
        var factsLower = facts?.ToLower() ?? string.Empty;

        // Simple rule-based planning based on keywords
        if (issueLower.Contains("outlook") || issueLower.Contains("office"))
        {
            if (issueLower.Contains("crash") || issueLower.Contains("start") || issueLower.Contains("won't open") || issueLower.Contains("wont open"))
            {
                plan.Add(new ToolCall
                {
                    ToolName = "DisableOutlookAddins",
                    Arguments = JsonDocument.Parse("{\"Scope\":\"CurrentUser\",\"BackupRegistry\":true}").RootElement
                });

                plan.Add(new ToolCall
                {
                    ToolName = "CreateTestOutlookProfile",
                    Arguments = JsonDocument.Parse("{\"ProfileName\":\"FixRunnerTestProfile\"}").RootElement
                });
            }
        }
        
        if (issueLower.Contains("proxy") || issueLower.Contains("winhttp") || issueLower.Contains("http") || issueLower.Contains("ssl"))
        {
            plan.Add(new ToolCall
            {
                ToolName = "ResetWinHTTP",
                Arguments = JsonDocument.Parse("{\"ResetProxy\":true,\"ImportIEProxy\":false}").RootElement
            });
        }
        else if (issueLower.Contains("network") || issueLower.Contains("internet") || issueLower.Contains("wifi"))
        {
            plan.Add(new ToolCall
            {
                ToolName = "ResetNetworkAdapter",
                Arguments = JsonDocument.Parse("{}").RootElement
            });
        }
        else if (issueLower.Contains("disk") || issueLower.Contains("space") || issueLower.Contains("full"))
        {
            plan.Add(new ToolCall
            {
                ToolName = "CleanDiskSpace",
                Arguments = JsonDocument.Parse("{}").RootElement
            });
        }
        else
        {
            // Default diagnostic steps
            plan.Add(new ToolCall
            {
                ToolName = "CheckSystemHealth",
                Arguments = JsonDocument.Parse("{}").RootElement
            });
        }

        return plan;
    }

    private async Task<List<ToolCall>> GeneratePlanWithLLM(string issue, string? facts, List<ToolSchema> availableTools)
    {
        var prompt = BuildPrompt(issue, facts);

        var toolsArray = BuildOpenAiToolsArray(availableTools);

        var request = new JsonObject
        {
            ["model"] = _model,
            ["temperature"] = 0.1,
            ["messages"] = new JsonArray
            {
                new JsonObject { ["role"] = "system", ["content"] = "You are a Windows repair planner. Only propose allowed tools using function calls with JSON arguments that match the provided JSON Schemas. Do not invent tools." },
                new JsonObject { ["role"] = "user", ["content"] = prompt }
            },
            ["tools"] = toolsArray,
            ["tool_choice"] = "auto"
        };

        var json = request.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        var url = _baseUrl.TrimEnd('/') + "/chat/completions";
        var response = await _httpClient.PostAsync(url, content);
        var result = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"LLM API error ({response.StatusCode}): {result}");
        }

        return ParseOpenAiToolCalls(result, availableTools);
    }

    private string BuildPrompt(string issue, string? facts)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine($"Issue: {issue}");
        
        if (!string.IsNullOrEmpty(facts))
        {
            prompt.AppendLine($"Additional facts: {facts}");
        }
        prompt.AppendLine("\nReturn function/tool calls that use only the provided tools.");
        return prompt.ToString();
    }

    private JsonArray BuildOpenAiToolsArray(List<ToolSchema> tools)
    {
        var arr = new JsonArray();
        foreach (var t in tools)
        {
            var fn = new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = t.Name,
                    ["description"] = t.Description,
                    ["parameters"] = JsonNode.Parse(t.Schema.ToJson())
                }
            };
            arr.Add(fn);
        }
        return arr;
    }

    private List<ToolCall> ParseOpenAiToolCalls(string json, List<ToolSchema> availableTools)
    {
        var plan = new List<ToolCall>();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var choices = root.GetProperty("choices");
        if (choices.GetArrayLength() == 0) return plan;
        var msg = choices[0].GetProperty("message");
        if (!msg.TryGetProperty("tool_calls", out var toolCalls)) return plan;
        var allowed = new HashSet<string>(availableTools.Select(t => t.Name), StringComparer.OrdinalIgnoreCase);
        foreach (var tc in toolCalls.EnumerateArray())
        {
            if (!tc.TryGetProperty("function", out var fn)) continue;
            var name = fn.GetProperty("name").GetString() ?? string.Empty;
            if (!allowed.Contains(name)) continue; // enforce allow-list
            var argsStr = fn.GetProperty("arguments").GetString() ?? "{}";
            JsonElement argsEl;
            try
            {
                argsEl = JsonDocument.Parse(argsStr).RootElement.Clone();
            }
            catch
            {
                // If not valid JSON, skip this tool call
                continue;
            }
            plan.Add(new ToolCall { ToolName = name, Arguments = argsEl });
        }
        return plan;
    }
}
