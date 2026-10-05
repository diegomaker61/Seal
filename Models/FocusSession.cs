namespace Seal.Models;

public sealed record FocusSession(
    Guid Id,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    double FocusSeconds,
    bool Completed);
