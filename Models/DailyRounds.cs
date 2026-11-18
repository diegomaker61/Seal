namespace Seal.Models;

public sealed record DailyRounds(int FocusMinutes, IReadOnlyDictionary<Guid, int> Completed);
