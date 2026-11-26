using Comprove.HelpDesk.Application.Exceptions;
using Comprove.HelpDesk.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Comprove.HelpDesk.Api.Middlewares;

// Erros explícitos e tipados (B20). Toda resposta de falha traz correlation_id.
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private const int UnprocessableStatus = StatusCodes.Status422UnprocessableEntity;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception ex)
    {
        var (status, title) = Classify(ex);
        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(ex, "Erro não tratado.");
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status500InternalServerError ? null : ex.Message,
        };
        problem.Extensions["correlation_id"] = context.Items[CorrelationIdMiddleware.ItemKey];
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem);
    }

    private static (int Status, string Title) Classify(Exception ex) => ex switch
    {
        DomainRuleViolationException => (UnprocessableStatus, "Regra de negócio violada."),
        TicketNotFoundException => (StatusCodes.Status404NotFound, "Chamado não encontrado."),
        FaqNotFoundException => (StatusCodes.Status404NotFound, "FAQ não encontrado."),
        TicketAccessDeniedException => (StatusCodes.Status403Forbidden, "Ação não permitida."),
        ConcurrencyConflictException => (StatusCodes.Status409Conflict, "Conflito de atualização."),
        ProtocolCollisionException => (StatusCodes.Status409Conflict, "Conflito ao gerar protocolo."),
        _ => (StatusCodes.Status500InternalServerError, "Erro interno."),
    };
}
