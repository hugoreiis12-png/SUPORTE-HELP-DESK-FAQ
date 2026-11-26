namespace Comprove.HelpDesk.Api.Middlewares;

// Propaga X-Correlation-Id (B20). Valor recebido inválido é descartado, evitando injeção em log.
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";
    private const int MaxLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ReadOrCreate(context.Request.Headers[HeaderName].FirstOrDefault());
        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        await next(context);
    }

    private static string ReadOrCreate(string? candidate) =>
        IsValid(candidate) ? candidate! : Guid.NewGuid().ToString("N");

    private static bool IsValid(string? value) =>
        !string.IsNullOrEmpty(value) && value.Length <= MaxLength && value.All(c => char.IsLetterOrDigit(c) || c == '-');
}
