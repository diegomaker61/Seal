using System.IO;
using System.Text.Json;
using Seal.Models;

namespace Seal.Services;

public sealed class TimerStateRepository(string filePath)
{
    public TimerState? Load()
    {
        if (!File.Exists(filePath)) return null;

        try
        {
            return JsonSerializer.Deserialize<TimerState>(File.ReadAllText(filePath));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public TimerWorkspace LoadWorkspace()
    {
        if (!File.Exists(filePath)) return new(null, []);
        try
        {
            var json = File.ReadAllText(filePath);
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("Rounds", out _))
                return JsonSerializer.Deserialize<TimerWorkspace>(json) ?? new(null, []);
            var legacy = JsonSerializer.Deserialize<TimerState>(json);
            return legacy is null ? new(null, []) : new(legacy.Task, [legacy]);
        }
        catch (JsonException) { return new(null, []); }
    }

    public void SaveWorkspace(TimerWorkspace workspace)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
        var temporaryPath = filePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(workspace));
        File.Move(temporaryPath, filePath, true);
    }

    public void Save(TimerState? state)
    {
        if (state is null)
        {
            File.Delete(filePath);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
        var temporaryPath = filePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(state));
        File.Move(temporaryPath, filePath, true);
    }
}
