namespace Seal.Models;

public sealed record TimerState(
    Guid SessionId,
    DateTimeOffset StartedAt,
    double DurationSeconds,
    double ElapsedSeconds,
    FocusTask Task);
