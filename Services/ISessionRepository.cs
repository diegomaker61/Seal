using Seal.Models;

namespace Seal.Services;

public interface ISessionRepository
{
    IReadOnlyList<FocusSession> ReadAll();
    void Save(FocusSession session);
}
