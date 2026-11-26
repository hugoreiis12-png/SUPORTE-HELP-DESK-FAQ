namespace Comprove.HelpDesk.Application.Exceptions;

// 404: não existe neste tenant (nunca 403, para não vazar existência — ADR-006).
public sealed class TicketNotFoundException : Exception
{
    public TicketNotFoundException(string ticketId) : base($"Chamado {ticketId} não encontrado.")
    {
    }
}

// 404: FAQ inexistente neste tenant.
public sealed class FaqNotFoundException : Exception
{
    public FaqNotFoundException(string reference) : base($"FAQ {reference} não encontrado.")
    {
    }
}

// 403: existe, mas o ator não tem permissão para o recurso.
public sealed class TicketAccessDeniedException : Exception
{
    public TicketAccessDeniedException() : base("Ação não permitida para este usuário.")
    {
    }
}

// 409: Version mudou entre a leitura e a gravação.
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException() : base("O chamado foi alterado por outra operação. Recarregue e tente de novo.")
    {
    }
}

// Índice único de protocolo colidiu; o caso de uso gera outra sequência.
public sealed class ProtocolCollisionException : Exception
{
    public ProtocolCollisionException() : base("Colisão de protocolo.")
    {
    }
}
