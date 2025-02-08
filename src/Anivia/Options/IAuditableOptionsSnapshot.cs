using Microsoft.Extensions.Options;

namespace Anivia.Options;

public interface IAuditableOptionsSnapshot<out T> : IOptionsMonitor<T> where T : class, new()
{
    void Update(Action<T> updateAction);
}