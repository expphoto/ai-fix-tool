using System.Text;
using System.Text.Json;
using NJsonSchema;

namespace FixRunner.Tools;

public class FixAudioIssuesTool : ITool
{
    public string Name => "FixAudioIssues";
    public string Description => "Diagnoses and fixes common audio problems";
    public bool RequiresAdmin => false;

    public JsonSchema Schema => JsonSchema.FromType<FixAudioIssuesArgs>();

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments)
    {
        try
        {
            var args = JsonSerializer.Deserialize<FixAudioIssuesArgs>(arguments);
            
            var issues = DiagnoseAudioIssues(args);
            var fixes = GenerateFixes(issues, args);
            
            var result = new
            {
                IssuesFound = issues,
                FixesToApply = fixes,
                AudioDevices = GetAudioDevices(),
                DefaultDevice = GetDefaultAudioDevice(),
                VolumeSettings = GetVolumeSettings(),
                DriverStatus = GetDriverStatus(),
                EstimatedDuration = "5-10 minutes",
                RequiresRestart = fixes.Any(f => f.RequiresRestart)
            };

            var fixScript = GenerateFixScript(issues, fixes, args);
            var undoScript = CreateUndoScript(issues);

            return new ToolResult
            {
                Success = true,
                Data = result,
                Error = null,
                UndoScript = undoScript
            };
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                Success = false,
                Error = $"Failed to diagnose audio issues: {ex.Message}",
                Data = null
            };
        }
    }

    private List<AudioIssue> DiagnoseAudioIssues(FixAudioIssuesArgs args)
    {
        var issues = new List<AudioIssue>();

        // Simulate audio diagnosis
        issues.Add(new AudioIssue
        {
            Type = "NoSound",
            Description = "No audio output detected",
            Severity = "High",
            Component = "Speakers",
            PossibleCauses = new[] { "Muted volume", "Wrong output device", "Driver issues" }
        });

        issues.Add(new AudioIssue
        {
            Type = "LowVolume",
            Description = "Audio volume too low",
            Severity = "Medium",
            Component = "Volume Settings",
            PossibleCauses = new[] { "Low system volume", "App-specific volume", "Enhancement settings" }
        });

        issues.Add(new AudioIssue
        {
            Type = "Crackling",
            Description = "Audio crackling or distortion",
            Severity = "Medium",
            Component = "Audio Driver",
            PossibleCauses = new[] { "Outdated drivers", "Sample rate mismatch", "Buffer issues" }
        });

        issues.Add(new AudioIssue
        {
            Type = "MicNotWorking",
            Description = "Microphone not detected or not working",
            Severity = "Medium",
            Component = "Microphone",
            PossibleCauses = new[] { "Disabled microphone", "Privacy settings", "Driver issues" }
        });

        issues.Add(new AudioIssue
        {
            Type = "Bluetooth",
            Description = "Bluetooth audio device connection issues",
            Severity = "Low",
            Component = "Bluetooth",
            PossibleCauses = new[] { "Pairing issues", "Driver problems", "Interference" }
        });

        return issues;
    }

    private List<AudioFix> GenerateFixes(List<AudioIssue> issues, FixAudioIssuesArgs args)
    {
        var fixes = new List<AudioFix>();

        foreach (var issue in issues)
        {
            switch (issue.Type)
            {
                case "NoSound":
                    fixes.Add(new AudioFix
                    {
                        Description = "Check volume and unmute if necessary",
                        Command = "sndvol.exe",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new AudioFix
                    {
                        Description = "Set correct default audio device",
                        Command = "mmsys.cpl",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    if (args.UpdateDrivers)
                    {
                        fixes.Add(new AudioFix
                        {
                            Description = "Update audio drivers",
                            Command = "devmgmt.msc",
                            RequiresRestart = true,
                            RiskLevel = "Medium"
                        });
                    }
                    break;

                case "LowVolume":
                    fixes.Add(new AudioFix
                    {
                        Description = "Increase system volume to 100%",
                        Command = "sndvol.exe",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new AudioFix
                    {
                        Description = "Check app-specific volume levels",
                        Command = "sndvol.exe",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    if (args.DisableEnhancements)
                    {
                        fixes.Add(new AudioFix
                        {
                            Description = "Disable audio enhancements",
                            Command = "mmsys.cpl",
                            RequiresRestart = false,
                            RiskLevel = "Low"
                        });
                    }
                    break;

                case "Crackling":
                    if (args.UpdateDrivers)
                    {
                        fixes.Add(new AudioFix
                        {
                            Description = "Update audio drivers to latest version",
                            Command = "devmgmt.msc",
                            RequiresRestart = true,
                            RiskLevel = "Medium"
                        });
                    }
                    fixes.Add(new AudioFix
                    {
                        Description = "Adjust audio format to 16-bit, 44100 Hz",
                        Command = "mmsys.cpl",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new AudioFix
                    {
                        Description = "Disable audio enhancements",
                        Command = "mmsys.cpl",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    break;

                case "MicNotWorking":
                    fixes.Add(new AudioFix
                    {
                        Description = "Enable microphone in Sound settings",
                        Command = "mmsys.cpl",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new AudioFix
                    {
                        Description = "Check microphone privacy settings",
                        Command = "ms-settings:privacy-microphone",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    if (args.UpdateDrivers)
                    {
                        fixes.Add(new AudioFix
                        {
                            Description = "Update microphone drivers",
                            Command = "devmgmt.msc",
                            RequiresRestart = true,
                            RiskLevel = "Medium"
                        });
                    }
                    break;

                case "Bluetooth":
                    fixes.Add(new AudioFix
                    {
                        Description = "Remove and re-pair Bluetooth device",
                        Command = "ms-settings:bluetooth",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    fixes.Add(new AudioFix
                    {
                        Description = "Restart Bluetooth service",
                        Command = "services.msc",
                        RequiresRestart = false,
                        RiskLevel = "Low"
                    });
                    if (args.UpdateDrivers)
                    {
                        fixes.Add(new AudioFix
                        {
                            Description = "Update Bluetooth drivers",
                            Command = "devmgmt.msc",
                            RequiresRestart = true,
                            RiskLevel = "Medium"
                        });
                    }
                    break;
            }
        }

        return fixes;
    }

    private List<object> GetAudioDevices()
    {
        return new List<object>
        {
            new { Name = "Speakers (Realtek High Definition Audio)", Type = "Speakers", Status = "Ready", Default = true },
            new { Name = "Headphones (3.5mm jack)", Type = "Headphones", Status = "Ready", Default = false },
            new { Name = "NVIDIA HDMI Output", Type = "HDMI", Status = "Not plugged in", Default = false },
            new { Name = "Bluetooth Headphones", Type = "Bluetooth", Status = "Connected", Default = false }
        };
    }

    private object GetDefaultAudioDevice()
    {
        return new
        {
            Name = "Speakers (Realtek High Definition Audio)",
            Type = "Speakers",
            Volume = 75,
            Format = "24-bit, 48000 Hz",
            Enhancements = new[] { "Loudness Equalization", "Bass Boost" }
        };
    }

    private object GetVolumeSettings()
    {
        return new
        {
            SystemVolume = 75,
            AppVolumes = new[]
            {
                new { App = "Chrome", Volume = 100 },
                new { App = "Spotify", Volume = 80 },
                new { App = "Discord", Volume = 90 },
                new { App = "System Sounds", Volume = 50 }
            }
        };
    }

    private object GetDriverStatus()
    {
        return new
        {
            AudioDriver = new { Name = "Realtek High Definition Audio", Version = "6.0.9235.1", Status = "Up to date" },
            BluetoothDriver = new { Name = "Intel Wireless Bluetooth", Version = "22.200.0.2", Status = "Update available" }
        };
    }

    private string GenerateFixScript(List<AudioIssue> issues, List<AudioFix> fixes, FixAudioIssuesArgs args)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo Audio Issues Fix Tool");
        sb.AppendLine("echo ====================");
        sb.AppendLine("echo This script will fix common audio problems");
        sb.AppendLine("echo.");
        
        sb.AppendLine("echo Opening Volume Mixer...");
        sb.AppendLine("sndvol.exe");
        sb.AppendLine("echo Check and adjust volume levels as needed");
        sb.AppendLine("pause");
        sb.AppendLine();

        sb.AppendLine("echo Opening Sound settings...");
        sb.AppendLine("mmsys.cpl");
        sb.AppendLine("echo 1. Check default playback device");
        sb.AppendLine("echo 2. Test audio playback");
        sb.AppendLine("echo 3. Check device properties and formats");
        sb.AppendLine("pause");
        sb.AppendLine();

        if (args.UpdateDrivers)
        {
            sb.AppendLine("echo Opening Device Manager for driver updates...");
            sb.AppendLine("devmgmt.msc");
            sb.AppendLine("echo 1. Expand 'Sound, video and game controllers'");
            sb.AppendLine("echo 2. Right-click audio devices and select 'Update driver'");
            sb.AppendLine("echo 3. Also check 'Bluetooth' for Bluetooth audio devices");
            sb.AppendLine("pause");
            sb.AppendLine();
        }

        sb.AppendLine("echo Checking microphone privacy settings...");
        sb.AppendLine("start ms-settings:privacy-microphone");
        sb.AppendLine("echo Ensure microphone access is enabled for apps");
        sb.AppendLine("pause");
        sb.AppendLine();

        sb.AppendLine("echo Audio troubleshooting complete!");
        sb.AppendLine("echo Additional steps:");
        sb.AppendLine("echo 1. Restart your computer if drivers were updated");
        sb.AppendLine("echo 2. Test audio with different applications");
        sb.AppendLine("echo 3. Try different audio formats if crackling persists");
        sb.AppendLine("echo 4. Check physical connections for wired devices");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }

    private string CreateUndoScript(List<AudioIssue> issues)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("@echo off");
        sb.AppendLine("echo Audio Fix Undo");
        sb.AppendLine("echo ==============");
        sb.AppendLine("echo To undo audio fixes:");
        sb.AppendLine("echo 1. Restore previous audio drivers:");
        sb.AppendLine("echo    - Open Device Manager (devmgmt.msc)");
        sb.AppendLine("echo    - Right-click audio device > Properties");
        sb.AppendLine("echo    - Driver tab > Roll Back Driver");
        sb.AppendLine("echo 2. Re-enable audio enhancements:");
        sb.AppendLine("echo    - Open Sound settings (mmsys.cpl)");
        sb.AppendLine("echo    - Properties > Enhancements tab");
        sb.AppendLine("echo    - Re-check desired enhancements");
        sb.AppendLine("echo 3. Restore previous volume levels");
        sb.AppendLine("echo 4. Re-pair Bluetooth devices if removed");
        sb.AppendLine("pause");
        
        return sb.ToString();
    }
}

public class FixAudioIssuesArgs
{
    public bool UpdateDrivers { get; set; } = true;
    public bool DisableEnhancements { get; set; } = true;
    public bool ResetAudioService { get; set; } = false;
    public bool CheckPrivacySettings { get; set; } = true;
}

public class AudioIssue
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public string[] PossibleCauses { get; set; } = Array.Empty<string>();
}

public class AudioFix
{
    public string Description { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public bool RequiresRestart { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
}
