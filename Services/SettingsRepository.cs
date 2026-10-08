using System.IO;
using System.Text.Json;
using Seal.Models;

namespace Seal.Services;

public sealed class SettingsRepository
{
    private readonly string filePath;

    public SettingsRepository(string? filePath = null)
    {
        this.filePath = filePath ?? Path.Combine(AppDataPaths.DirectoryPath, "settings.json");
    }

    public AppSettings Load()
    {
        var settings = File.Exists(filePath)
            ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(filePath)) ?? new AppSettings()
            : new AppSettings();
        settings.Validate();
        return settings;
    }

    public void Save(AppSettings settings)
    {
        settings.Validate();
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath + ".tmp", JsonSerializer.Serialize(settings));
        File.Move(filePath + ".tmp", filePath, true);
    }
}
