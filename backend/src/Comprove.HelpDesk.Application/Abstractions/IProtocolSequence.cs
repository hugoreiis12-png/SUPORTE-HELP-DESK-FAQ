namespace Comprove.HelpDesk.Application.Abstractions;

public interface IProtocolSequence
{
    Task<long> NextAsync(int year, CancellationToken ct);
}
