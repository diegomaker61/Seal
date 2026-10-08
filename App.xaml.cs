using System.Windows;
using Seal.Services;

namespace Seal;

public partial class App : Application
{
    private SingleInstanceGuard? instanceGuard;

    protected override void OnStartup(StartupEventArgs e)
    {
        instanceGuard = new SingleInstanceGuard(AppDataPaths.InstanceMutexName);

        if (!instanceGuard.IsAcquired)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);

        var repository = new JsonSessionRepository();
        var settingsRepository = new SettingsRepository();
        var settings = settingsRepository.Load();
        var timer = new FocusTimer(repository, TimeSpan.FromMinutes(settings.FocusMinutes));
        var tasks = new TaskCatalog(new JsonTaskRepository());
        MainWindow = new MainWindow(timer, new StatisticsService(repository), tasks, settings, settingsRepository);
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        instanceGuard?.Dispose();
        base.OnExit(e);
    }
}
