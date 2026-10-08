using System.IO;
using System.Text.Json;
using Seal.Models;

namespace Seal.Services;

public sealed class JsonTaskRepository : ITaskRepository
{
    private readonly string filePath;

    public JsonTaskRepository(string? filePath = null)
    {
        this.filePath = filePath ?? AppDataPaths.TasksFile;
    }

    public IReadOnlyList<FocusTask> ReadAll() => File.Exists(filePath)
        ? JsonSerializer.Deserialize<List<FocusTask>>(File.ReadAllText(filePath)) ?? []
        : [];

    public void SaveAll(IReadOnlyList<FocusTask> tasks)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var temporaryPath = filePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(tasks));
        File.Move(temporaryPath, filePath, overwrite: true);
    }
}
