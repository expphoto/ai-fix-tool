using System.Text.Json;

namespace FixRunner.Services;

public class Journal
{
    private readonly string _logDirectory;
    private readonly string _logFile;

    public Journal()
    {
        _logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "FixRunner",
            "logs"
        );
        
        Directory.CreateDirectory(_logDirectory);
        _logFile = Path.Combine(_logDirectory, $"fixrunner-{DateTime.UtcNow:yyyyMMdd}.jsonl");
    }

    public async Task LogAsync(JournalEntry entry)
    {
        var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions
        {
            WriteIndented = false
        });
        
        await File.AppendAllTextAsync(_logFile, json + Environment.NewLine);
    }

    public async Task<List<JournalEntry>> GetRecentEntriesAsync(int limit = 100)
    {
        if (!File.Exists(_logFile))
        {
            return new List<JournalEntry>();
        }

        var entries = new List<JournalEntry>();
        var lines = await File.ReadAllLinesAsync(_logFile);
        
        foreach (var line in lines.TakeLast(limit))
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                try
                {
                    var entry = JsonSerializer.Deserialize<JournalEntry>(line);
                    if (entry != null)
                    {
                        entries.Add(entry);
                    }
                }
                catch
                {
                    // Skip malformed entries
                }
            }
        }

        return entries;
    }

    public async Task<List<JournalEntry>> GetEntriesForSessionAsync(string sessionId)
    {
        if (!File.Exists(_logFile))
        {
            return new List<JournalEntry>();
        }

        var entries = new List<JournalEntry>();
        var lines = await File.ReadAllLinesAsync(_logFile);
        
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                try
                {
                    var entry = JsonSerializer.Deserialize<JournalEntry>(line);
                    if (entry?.SessionId == sessionId)
                    {
                        entries.Add(entry);
                    }
                }
                catch
                {
                    // Skip malformed entries
                }
            }
        }

        return entries;
    }
}

public class JournalEntry
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public JsonElement Arguments { get; set; }
    public ToolResult Result { get; set; } = new();
    public string? UndoScript { get; set; }
}
