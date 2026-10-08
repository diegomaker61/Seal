using Microsoft.Win32;

namespace Seal.Services;

public sealed class WindowsStartupService
{
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private static string EntryName => AppDataPaths.IsDevelopment ? "Seal.Development" : "Seal";

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);

        if (enabled)
        {
            var executable = Environment.ProcessPath
                ?? throw new InvalidOperationException("Unable to locate the application executable.");
            key.SetValue(EntryName, $"\"{executable}\"");
        }
        else
        {
            key.DeleteValue(EntryName, throwOnMissingValue: false);
        }
    }
}
