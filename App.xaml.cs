using System.Windows;
using Seal.Services;

namespace Seal;

public partial class App : Application
{
    private SingleInstanceGuard? instanceGuard;

    protected override void OnStartup(StartupEventArgs e)
    {
        instanceGuard = new SingleInstanceGuard(@"Local\Seal.Desktop.SingleInstance");

        if (!instanceGuard.IsAcquired)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);

        var repository = new JsonSessionRepository();
        var timer = new FocusTimer(repository, TimeSpan.FromMinutes(30));
        MainWindow = new MainWindow(timer, new StatisticsService(repository));
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        instanceGuard?.Dispose();
        base.OnExit(e);
    }
}
