using Seal.Models;

namespace Seal.Services;

public sealed class StatisticsService(ISessionRepository repository)
{
    public IReadOnlyList<FocusSession> ReadAll() => repository.ReadAll();

    public IReadOnlyList<FocusSession> ForDay(DateTime day) => repository.ReadAll()
        .Where(session => session.StartedAt.LocalDateTime.Date == day.Date)
        .ToList();
}
