namespace Comprove.HelpDesk.Application.Abstractions;

// Transação multi-documento. Chamadas aninhadas reutilizam a transação corrente.
public interface ITransactionRunner
{
    Task RunAsync(Func<CancellationToken, Task> work, CancellationToken ct);
}
