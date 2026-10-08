# Seal

**English** | [Português (Brasil)](README.pt-BR.md)

Seal is an open-source focus app for Windows, built with C# and WPF on .NET 10. It combines a configurable 10–60 minute Pomodoro timer, tasks, and statistics in a minimalist interface inspired by the Windows 11 dark theme.

## Features

- Focus timer with pause, resume, reset, and a progress bar.
- Visual focus border on all connected monitors.
- Always-on-top windows with rounded corners and drag support.
- Task management and sessions linked to the selected task.
- Annual activity calendar, daily metrics, and hourly session chart.
- Local history and single-instance protection.

## Installation

Download the Windows package from **Releases**, extract the ZIP, and run `Seal.exe`. Keep the package files in the same folder. Self-contained packages include .NET; framework-dependent builds require the **.NET Desktop Runtime 10**.

## Usage

1. Choose a task from the dropdown above the timer.
2. Click **Play** to start. Use **Pause** to pause and **Play** to resume.
3. Use **Reset** to save the focus time and prepare another session. A sound signals the end of the configured session.
4. Open **Tasks** to add, rename, or remove tasks. Names can contain up to 15 characters. At least one task must remain; `default` can be removed when another task exists.
5. Open **Statistics** to view your history. Select a task or **All tasks**, navigate between years and days, or click a calendar day. **Today** returns to the current day, and **Refresh** reloads the data.

**Global shortcut:** press `Ctrl + Alt + P` to start, pause, or resume the session while using another app. If another program or Seal version has registered this combination, the timer button tooltip indicates that the shortcut is unavailable.

Hover over icons to see their functions. Drag an empty area or the header to move a window. A session's task can only be changed after completing or resetting the session.

In statistics, **Rounds** counts completed sessions, **Focus time** includes partial sessions, and **Completion** shows the percentage of completed sessions. Removed tasks are excluded from filters and totals.

While the timer is running, a subtle 2-pixel border in #03FCEC appears on each connected monitor. It disappears when the session is paused, reset, or completed. The border does not take focus or intercept clicks.

## Settings

Open **Settings** using the gear button next to **Tasks**. Click the shortcut field and press a combination with Ctrl, Alt or Windows; shortcuts already in use are rejected. You can also toggle the border on all monitors, choose **Start with Windows**, and set focus duration between 10 and 60 minutes (default: 30). Click **Save** to apply or **Cancel** to discard.

Border and shortcut changes apply immediately. Duration changes apply to the next session without modifying an ongoing countdown. Preferences are stored in `settings.json` in the profile's data folder. Windows startup uses a per-user, per-profile entry; enabling it in a development build does not change the installed version's startup settings.

## Local data

History and tasks are saved in `%LOCALAPPDATA%\Seal`, in `sessions.json` and `tasks.json`. The timer does not automatically resume when the app is reopened. This version does not include automatic breaks or cloud synchronization.

## Development

Requirements: Windows and the .NET 10 SDK.

**Debug** builds use `%LOCALAPPDATA%\Seal.Development`, display **Seal (Dev)**, and can run alongside the installed app. Tests do not modify production history or tasks, and legacy history is not imported. **Release** builds use `%LOCALAPPDATA%\Seal` and should be used for public releases.

```powershell
dotnet run --project Seal.csproj
```

To publish a self-contained Windows x64 package:

```powershell
dotnet publish Seal.csproj -c Release -r win-x64 --self-contained true -o bin/publish/win-x64
```

The code and interface are in English; documentation is available in English and Brazilian Portuguese. The architecture separates the timer, persistence, tasks, and presentation.

## Project structure

```text
Seal/
├── Assets/
│   ├── Icons/                 # App images and icon
│   ├── MaterialIcons/         # Vector icons and license
│   └── Theme/                 # Control styles
├── Models/                    # Sessions, tasks, and filters
├── Services/                  # Timer, statistics, and persistence
├── App.xaml / App.xaml.cs     # Resources and startup
├── MainWindow.xaml(.cs)       # Timer and task selection
├── StatisticsWindow.xaml(.cs) # Calendar and metrics
├── TasksWindow.xaml(.cs)      # Task management
├── WindowDrag.cs              # Window dragging
├── Seal.csproj / Seal.slnx    # Project and solution
├── README.md                  # English documentation
├── README.pt-BR.md            # Portuguese documentation
└── LICENSE                    # GNU GPL v3.0
```

`FocusTimer` controls sessions. `TaskCatalog` manages and validates tasks. JSON repositories implement the persistence interfaces, and `StatisticsService` queries history. Dependencies are composed in `App.xaml.cs`. The `bin/`, `obj/`, and `.vs/` folders contain generated files.

## License

Released under the **GNU GPL v3.0**. See [LICENSE](LICENSE).

[Google Material Icons](https://github.com/google/material-design-icons) use the Apache 2.0 license, included in [Assets/MaterialIcons/LICENSE.txt](Assets/MaterialIcons/LICENSE.txt).
