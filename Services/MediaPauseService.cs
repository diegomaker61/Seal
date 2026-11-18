using System.Runtime.InteropServices;
using Windows.Media.Control;

namespace Seal.Services;

public sealed class MediaPauseService
{
    private readonly SemaphoreSlim operationLock = new(1, 1);
    private readonly List<GlobalSystemMediaTransportControlsSession> pausedSessions = [];
    private int generation;

    public void ForgetPausedSessions()
    {
        generation++;
        pausedSessions.Clear();
    }

    public async Task<bool> PausePlayingAsync()
    {
        await operationLock.WaitAsync();
        var currentGeneration = generation;
        try
        {
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync()
                .AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            var success = true;
            foreach (var session in manager.GetSessions())
            {
                try
                {
                    if (session.GetPlaybackInfo().PlaybackStatus != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                    {
                        continue;
                    }

                    if (await session.TryPauseAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)))
                    {
                        if (generation == currentGeneration && !pausedSessions.Contains(session)) pausedSessions.Add(session);
                    }
                    else
                    {
                        success = false;
                    }
                }
                catch (Exception error) when (IsMediaError(error))
                {
                    success = false;
                }
            }

            return success;
        }
        catch (Exception error) when (IsMediaError(error))
        {
            return false;
        }
        finally
        {
            operationLock.Release();
        }
    }

    public async Task<bool> ResumePausedAsync()
    {
        await operationLock.WaitAsync();
        try
        {
            var sessions = pausedSessions.ToList();
            pausedSessions.Clear();
            var success = true;
            foreach (var session in sessions)
            {
                try
                {
                    if (session.GetPlaybackInfo().PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused)
                    {
                        success &= await session.TryPlayAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
                    }
                }
                catch (Exception error) when (IsMediaError(error))
                {
                    success = false;
                }
            }

            return success;
        }
        finally
        {
            operationLock.Release();
        }
    }

    private static bool IsMediaError(Exception error) => error is COMException or UnauthorizedAccessException or TimeoutException;
}
