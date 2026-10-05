using System.Threading;

namespace Seal.Services;

public sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex mutex;
    private bool disposed;

    public SingleInstanceGuard(string name)
    {
        mutex = new Mutex(initiallyOwned: true, name, out var createdNew);
        IsAcquired = createdNew;
    }

    public bool IsAcquired { get; }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        if (IsAcquired)
        {
            mutex.ReleaseMutex();
        }

        mutex.Dispose();
        disposed = true;
    }
}
