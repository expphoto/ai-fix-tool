using System.Text.Json;
using FixRunner.Services;
using FixRunner.Tools;
using Microsoft.Extensions.Logging;
using NJsonSchema;
using System.Diagnostics;

namespace FixRunner;

public class Program
{
    private static async Task<int> Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<Program>();
        
        try
        {
            var options = ParseArguments(args);
            if (options == null)
            {
                return 1;
            }

            var journal = new Journal();
            var planner = new PlannerClient(logger);
            var toolRegistry = new ToolRegistry();

            if (options.Undo)
            {
                await ExecuteUndoAsync(journal, logger);
                return 0;
            }

            if (string.IsNullOrEmpty(options.Issue))
            {
                logger.LogError("Issue description is required");
                return 1;
            }

            var plan = await planner.PlanAsync(options.Issue, options.Facts, toolRegistry.GetToolSchemas());
            
            if (options.DryRun)
            {
                logger.LogInformation("=== DRY RUN MODE ===");
                DisplayPlan(plan, logger);
                return 0;
            }

            logger.LogInformation("=== EXECUTION MODE ===");
            await ExecutePlanAsync(plan, toolRegistry, journal, logger);
            
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error occurred");
            return 1;
        }
    }

    private static CliOptions? ParseArguments(string[] args)
    {
        var options = new CliOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--issue":
                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("Error: --issue requires a value");
                        return null;
                    }
                    options.Issue = args[++i];
                    break;
                case "--facts":
                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("Error: --facts requires a value");
                        return null;
                    }
                    options.Facts = args[++i];
                    break;
                case "--dry-run":
                    options.DryRun = true;
                    break;
                case "--execute":
                    options.DryRun = false;
                    break;
                case "--undo":
                    options.Undo = true;
                    break;
                case "--help":
                case "-h":
                    PrintHelp();
                    return null;
                default:
                    Console.WriteLine($"Unknown argument: {args[i]}");
                    PrintHelp();
                    return null;
            }
        }

        return options;
    }

    private static void PrintHelp()
    {
        Console.WriteLine(@"
FixRunner - Windows-first agent runner for safe system repairs

Usage:
  FixRunner.exe --issue ""description"" [--dry-run|--execute] [--facts ""additional facts""]
  FixRunner.exe --undo

Options:
  --issue     Description of the issue to resolve
  --facts     Additional facts about the system (optional)
  --dry-run   Show plan without executing
  --execute   Execute the repair plan
  --undo      Undo the last repair operation
  --help, -h  Show this help message

Examples:
  FixRunner.exe --issue ""Outlook crashes on startup"" --dry-run
  FixRunner.exe --issue ""Outlook crashes on startup"" --execute
  FixRunner.exe --undo
");
    }

    private static void DisplayPlan(List<ToolCall> plan, ILogger logger)
    {
        logger.LogInformation("Proposed repair plan:");
        for (int i = 0; i < plan.Count; i++)
        {
            var call = plan[i];
            logger.LogInformation($"{i + 1}. {call.ToolName}");
            logger.LogInformation($"   Args: {JsonSerializer.Serialize(call.Arguments, new JsonSerializerOptions { WriteIndented = true })}");
        }
    }

    private static async Task ExecutePlanAsync(List<ToolCall> plan, ToolRegistry toolRegistry, Journal journal, ILogger logger)
    {
        for (int i = 0; i < plan.Count; i++)
        {
            var call = plan[i];
            logger.LogInformation($"Executing step {i + 1}/{plan.Count}: {call.ToolName}");

            var tool = toolRegistry.GetTool(call.ToolName);
            if (tool == null)
            {
                throw new InvalidOperationException($"Unknown tool: {call.ToolName}");
            }

            // Schema validation
            var validationErrors = tool.Schema.Validate(call.Arguments.GetRawText());
            if (validationErrors != null && validationErrors.Any())
            {
                var msg = string.Join("; ", validationErrors.Select(e => $"{e.Path}: {e.Kind}"));
                throw new InvalidOperationException($"Arguments validation failed for {call.ToolName}: {msg}");
            }

            var result = await tool.ExecuteAsync(call.Arguments);

            // Execute forward script via PowerShell if provided
            if (!string.IsNullOrWhiteSpace(result.Script))
            {
                var psResult = await ExecutePowerShellScriptAsync(result.Script!);
                if (!psResult.Success)
                {
                    logger.LogError($"PowerShell execution failed for {call.ToolName}: {psResult.Error}");
                    throw new Exception($"PowerShell failed: {psResult.Error}");
                }
            }
            await journal.LogAsync(new JournalEntry
            {
                Timestamp = DateTime.UtcNow,
                ToolName = call.ToolName,
                Arguments = call.Arguments,
                Result = result,
                UndoScript = result.UndoScript
            });

            if (!result.Success)
            {
                logger.LogError($"Tool execution failed: {result.Error}");
                throw new Exception($"Failed to execute {call.ToolName}: {result.Error}");
            }

            logger.LogInformation($"✓ {call.ToolName} completed successfully");
        }

        logger.LogInformation("All repair steps completed successfully");
    }

    private static async Task ExecuteUndoAsync(Journal journal, ILogger logger)
    {
        logger.LogInformation("=== UNDO MODE ===");
        
        var entries = await journal.GetRecentEntriesAsync();
        if (entries.Count == 0)
        {
            logger.LogInformation("No recent entries found to undo");
            return;
        }

        logger.LogInformation($"Found {entries.Count} entries to undo");
        
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            var entry = entries[i];
            if (!string.IsNullOrEmpty(entry.UndoScript))
            {
                logger.LogInformation($"Undoing: {entry.ToolName}");
                
                var undoResult = await ExecutePowerShellScriptAsync(entry.UndoScript);
                if (!undoResult.Success)
                {
                    logger.LogError($"Failed to undo {entry.ToolName}: {undoResult.Error}");
                }
                else
                {
                    logger.LogInformation($"✓ Successfully undid {entry.ToolName}");
                }
            }
        }

        logger.LogInformation("Undo operation completed");
    }

    private static async Task<PowerShellResult> ExecutePowerShellScriptAsync(string script)
    {
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "FixRunner");
            Directory.CreateDirectory(tempDir);
            var scriptPath = Path.Combine(tempDir, $"script_{Guid.NewGuid():N}.ps1");
            await File.WriteAllTextAsync(scriptPath, script);

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = new Process { StartInfo = psi };
            proc.Start();
            var stdout = await proc.StandardOutput.ReadToEndAsync();
            var stderr = await proc.StandardError.ReadToEndAsync();
            proc.WaitForExit();

            var success = proc.ExitCode == 0;
            return new PowerShellResult { Success = success, Output = stdout, Error = stderr };
        }
        catch (Exception ex)
        {
            return new PowerShellResult { Success = false, Error = ex.Message };
        }
    }

    private static string GeneratePowerShellScript(string toolName, string arguments)
    {
        // In a real implementation, this would generate appropriate PowerShell scripts
        return $"# PowerShell script for {toolName} with args: {arguments}";
    }
}

public class CliOptions
{
    public string? Issue { get; set; }
    public string? Facts { get; set; }
    public bool DryRun { get; set; }
    public bool Undo { get; set; }
}

public class ToolCall
{
    public string ToolName { get; set; } = string.Empty;
    public JsonElement Arguments { get; set; }
}
