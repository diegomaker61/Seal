using System.Diagnostics;
using Seal.Models;

namespace Seal.Services;

public sealed class FocusTimer(ISessionRepository repository, TimeSpan duration)
{
    private readonly Stopwatch stopwatch = new();
    private Guid sessionId;
    private DateTimeOffset startedAt;
    private bool completed;
    private TimeSpan configuredDuration = duration;
    private FocusTask sessionTask = new(Guid.Empty, "default");

    public TimeSpan Duration { get; private set; } = duration;
    public bool IsRunning => stopwatch.IsRunning;
    public TimeSpan Elapsed => stopwatch.Elapsed < Duration ? stopwatch.Elapsed : Duration;
    public TimeSpan Remaining => Duration - Elapsed;
    public bool HasStarted => sessionId != Guid.Empty;

    public void Start(FocusTask? task = null)
    {
        if (completed)
        {
            Reset();
        }

        if (!HasStarted)
        {
            Duration = configuredDuration;
            sessionId = Guid.NewGuid();
            startedAt = DateTimeOffset.Now;
            sessionTask = task ?? new FocusTask(Guid.Empty, "default");
        }

        stopwatch.Start();
    }

    public void ConfigureDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.FromMinutes(10) || duration > TimeSpan.FromMinutes(60))
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        configuredDuration = duration;
        if (!HasStarted)
        {
            Duration = duration;
        }
    }

    public void Pause()
    {
        stopwatch.Stop();
        Checkpoint();
    }

    public bool Update()
    {
        if (!IsRunning || stopwatch.Elapsed < Duration)
        {
            return false;
        }

        stopwatch.Stop();
        completed = true;
        Checkpoint();
        return true;
    }

    public void Checkpoint()
    {
        if (!HasStarted)
        {
            return;
        }

        repository.Save(new FocusSession(
            sessionId,
            startedAt,
            DateTimeOffset.Now,
            Elapsed.TotalSeconds,
            completed,
            sessionTask.Id,
            sessionTask.Name));
    }

    public void Reset()
    {
        Pause();
        stopwatch.Reset();
        sessionId = Guid.Empty;
        completed = false;
        Duration = configuredDuration;
    }
}
