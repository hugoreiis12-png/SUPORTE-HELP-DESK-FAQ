using Comprove.HelpDesk.Application.Abstractions;

namespace Comprove.HelpDesk.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
