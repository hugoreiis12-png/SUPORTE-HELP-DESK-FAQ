namespace Comprove.HelpDesk.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
