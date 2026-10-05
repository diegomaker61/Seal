using Seal.Models;

namespace Seal.Services;

public interface ITaskRepository
{
    IReadOnlyList<FocusTask> ReadAll();
    void SaveAll(IReadOnlyList<FocusTask> tasks);
}
