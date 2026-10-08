using System.IO;

namespace Seal.Services;

public static class AppDataPaths
{
#if DEBUG
    public const bool IsDevelopment = true;
#else
    public const bool IsDevelopment = false;
#endif

    public static string DirectoryPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        IsDevelopment ? "Seal.Development" : "Seal");

    public static string SessionsFile => Path.Combine(DirectoryPath, "sessions.json");
    public static string TasksFile => Path.Combine(DirectoryPath, "tasks.json");
    public static string InstanceMutexName => IsDevelopment
        ? @"Local\Seal.Desktop.Development.SingleInstance"
        : @"Local\Seal.Desktop.SingleInstance";
}
