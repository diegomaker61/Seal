using System.Diagnostics;
using Seal.Models;

namespace Seal.Services;

public sealed class FocusTimer(ISessionRepository repository, TimeSpan duration, TimerStateRepository? stateRepository = null)
{
    private readonly Stopwatch stopwatch = new();
    private TimeSpan restoredElapsed;
    private Guid sessionId;
    private DateTimeOffset startedAt;
    private bool completed;
    private TimeSpan configuredDuration = duration;
    private FocusTask sessionTask = new(Guid.Empty, "default");

    public TimeSpan Duration { get; private set; } = duration;
    public bool IsRunning => stopwatch.IsRunning;
    public TimeSpan Elapsed => restoredElapsed + stopwatch.Elapsed < Duration ? restoredElapsed + stopwatch.Elapsed : Duration;
    public FocusTask? CurrentTask => HasStarted ? sessionTask : null;
    public TimeSpan Remaining => Duration - Elapsed;
    public bool HasStarted => sessionId != Guid.Empty;

    public void Restore()
    {
        var state = stateRepository?.Load();
        if (state is null || state.SessionId == Guid.Empty || state.Task is null ||
            !double.IsFinite(state.DurationSeconds) || !double.IsFinite(state.ElapsedSeconds) ||
            state.DurationSeconds is < 600 or > 3600 || state.ElapsedSeconds < 0 ||
            state.ElapsedSeconds >= state.DurationSeconds)
        {
            return;
        }

        stopwatch.Reset();
        sessionId = state.SessionId;
        startedAt = state.StartedAt;
        Duration = TimeSpan.FromSeconds(state.DurationSeconds);
        restoredElapsed = TimeSpan.FromSeconds(state.ElapsedSeconds);
        sessionTask = state.Task;
        completed = false;
    }

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
        SaveState();
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
        if (!IsRunning || Elapsed < Duration)
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
        SaveState();
    }

    public void Reset()
    {
        Pause();
        stopwatch.Reset();
        restoredElapsed = TimeSpan.Zero;
        sessionId = Guid.Empty;
        completed = false;
        Duration = configuredDuration;
        SaveState();
    }

    private void SaveState() => stateRepository?.Save(HasStarted && !completed && Remaining > TimeSpan.Zero
        ? new TimerState(sessionId, startedAt, Duration.TotalSeconds, Elapsed.TotalSeconds, sessionTask)
        : null);
}

