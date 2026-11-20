namespace Seal.Models;

public sealed record TimerWorkspace(FocusTask? SelectedTask, IReadOnlyList<TimerState> Rounds);
