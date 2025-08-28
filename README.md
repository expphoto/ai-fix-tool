# FixRunner - Windows-First Safe Repair Runner

## Overview

FixRunner is a Windows-first agent runner that executes only allow-listed “tools” (safe repair actions) via PowerShell with JSON‑schema validated arguments, full JSONL journaling, and reversible actions (undo). Built with .NET 8, it runs headless on the CLI and is designed to be extended with a GUI later.

## What This Tool Does

FixRunner ships with a set of tools, including the three required starters for Outlook and networking triage:

- DisableOutlookAddins: Turn off COM add‑ins to isolate Outlook crashes
- CreateTestOutlookProfile: Create and set a clean Outlook profile
- ResetWinHTTP: Reset WinHTTP proxy and optionally import IE proxy
- CheckOfficeInstallation: Quick Office health check (diagnostic)
- RepairOffice: Launch Office Click‑to‑Run repair
- ResetNetworkAdapter: Reset IP, Winsock, TCP/IP stack
- CleanDiskSpace, CheckSystemHealth, and other utilities

## What I Did

I created a complete Windows troubleshooting toolkit with the following architecture:

1. Allow-listed tools only: ITool-based registry with per-tool JSON schemas
2. JSON Schema validation: NJsonSchema validates tool args prior to execution
3. PowerShell execution: Forward scripts run via `powershell.exe` with output capture
4. Journal: JSONL logs under `%ProgramData%\FixRunner\logs\`
5. Undo: Tools emit `UndoScript`; `--undo` replays in reverse order
6. CLI first: `--issue` planner-driven flow with `--dry-run` and execution
7. Cross-compile: Ready to publish `win-x64` from macOS/Linux
8. LLM-ready: Local mock planner today; API integration later

## How to Use

### Prerequisites
- .NET 8 SDK installed
- Windows 10/11 target system (for running the compiled tool)
- OpenAI API key (optional, for LLM-enhanced diagnostics)

### Building from Source (cross-compiling from macOS/Linux)

1. **Clone or download the project**
2. **Navigate to project directory**:
   ```bash
   cd FixRunner
   ```
3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```
4. **Publish for Windows** (single-file, self-contained):
   ```bash
   dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true
   ```
5. **Find the executable** in `bin/Release/net8.0/win-x64/publish/FixRunner.exe`

### Running the Tool

CLI usage (planner-driven):
```bash
FixRunner.exe --issue "Outlook won't open" --dry-run
FixRunner.exe --issue "Outlook won't open" --execute
FixRunner.exe --undo
```

### Output and Logging

- Console output: Progress of plan and PowerShell results
- Journal files: JSONL entries saved to `%ProgramData%\FixRunner\logs\`
- Logged fields: timestamp, tool name, args, success, undo script presence

### Safety

- Allow-list: Only tools in `ToolRegistry` can be invoked
- Schema validation: Arguments validated by NJsonSchema before execution
- No raw shell: Arbitrary command execution is not exposed or accepted
- Undo: Each tool provides an `UndoScript` for `--undo`

## Project Structure

```
FixRunner/
├── Program.cs                 # Argument parsing, session control, PowerShell exec
├── ITool.cs                   # Tool interface + ToolResult
├── FixRunner.csproj           # Project configuration (.NET 8)
├── Services/
│   ├── Journal.cs            # JSONL logging
│   └── PlannerClient.cs      # Mock planner; LLM-ready
└── Tools/
    ├── ToolRegistry.cs       # Allow-listed tool registry
    ├── DisableOutlookAddinsTool.cs
    ├── CreateTestOutlookProfileTool.cs
    ├── ResetWinHTTPTool.cs
    ├── CheckOfficeInstallationTool.cs
    ├── RepairOfficeTool.cs
    ├── ResetNetworkAdapterTool.cs
    ├── CleanDiskSpaceTool.cs
    ├── UpdateDriversTool.cs
    ├── CheckSystemHealthTool.cs
    ├── FixWindowsUpdateTool.cs
    ├── FixPrinterIssuesTool.cs
    ├── FixBlueScreenTool.cs
    ├── FixSlowPerformanceTool.cs
    ├── FixAudioIssuesTool.cs
    ├── FixWiFiIssuesTool.cs
    ├── FixStartupIssuesTool.cs
    ├── FixFileExplorerIssuesTool.cs
    └── FixRegistryIssuesTool.cs
```

## Example Plan → Execute → Undo

Dry run for Outlook not opening:
```text
FixRunner.exe --issue "Outlook won't open" --dry-run
Proposed repair plan:
1. DisableOutlookAddins
   Args: { "Scope": "CurrentUser", "BackupRegistry": true }
2. CreateTestOutlookProfile
   Args: { "ProfileName": "FixRunnerTestProfile" }
```

Execute:
```text
FixRunner.exe --issue "Outlook won't open" --execute
Executing step 1/2: DisableOutlookAddins
✓ DisableOutlookAddins completed successfully
Executing step 2/2: CreateTestOutlookProfile
✓ CreateTestOutlookProfile completed successfully
All repair steps completed successfully
```

Undo (replays undo scripts in reverse):
```text
FixRunner.exe --undo
Found 2 entries to undo
Undoing: CreateTestOutlookProfile
✓ Successfully undid CreateTestOutlookProfile
Undoing: DisableOutlookAddins
✓ Successfully undid DisableOutlookAddins
Undo operation completed
```

## Tool JSON Schema Examples

DisableOutlookAddins schema (excerpt):
```json
{
  "type": "object",
  "properties": {
    "Scope": { "type": "string", "enum": ["CurrentUser", "AllUsers"] },
    "BackupRegistry": { "type": "boolean" }
  },
  "required": ["Scope"]
}
```

## Development

### Adding New Tools

1. Create a new class implementing `ITool`
2. Add tool to `ToolRegistry._tools` dictionary
3. Follow the pattern of existing tools for consistency

### Environment Variables

- `OPENAI_API_KEY`: If set, planner uses OpenAI-compatible API
- `OPENAI_BASE_URL`: Optional, defaults to `https://api.openai.com/v1`
- `OPENAI_MODEL`: Optional, defaults to `gpt-4o-mini`

## Troubleshooting

### Build Issues
- Ensure .NET 8 SDK is installed: `dotnet --version`
- For cross-compilation issues, verify RID: `dotnet --list-runtimes`

### Runtime Issues
- Run as Administrator for tools that require elevation
- Review `%ProgramData%\FixRunner\logs\` for detailed error logs

### LLM Planning Notes
- Planning uses allow-listed functions and JSON schemas; the assistant cannot execute arbitrary shell commands.
- If the API call fails or no API key is present, planning falls back to a local rules engine.

## License

This project is provided as-is for educational and troubleshooting purposes. Use at your own risk and always backup important data before running system repairs.
