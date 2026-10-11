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
