using System.IO;
using System.Text.Json;
using Seal.Models;

namespace Seal.Services;

public sealed class JsonSessionRepository : ISessionRepository
{
    private readonly string filePath;

    public JsonSessionRepository(string? customFilePath = null)
    {
        filePath = customFilePath ?? AppDataPaths.SessionsFile;

        if (customFilePath is not null || AppDataPaths.IsDevelopment)
        {
            return;
        }

        var previousFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Tomato",
            "sessions.json");

        if (!File.Exists(filePath) && File.Exists(previousFilePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.Copy(previousFilePath, filePath, overwrite: false);
        }
    }

    public IReadOnlyList<FocusSession> ReadAll()
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<FocusSession>>(File.ReadAllText(filePath)) ?? [];
    }

    public void Save(FocusSession session)
    {
        var sessions = ReadAll().ToList();
        sessions.RemoveAll(item => item.Id == session.Id);
        sessions.Add(session);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var temporaryPath = filePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(sessions));
        File.Move(temporaryPath, filePath, overwrite: true);
    }
}
