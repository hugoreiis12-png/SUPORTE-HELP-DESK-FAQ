namespace Comprove.HelpDesk.Api.Configuration;

// Chrome exige este cabeçalho para a página HTTPS do Bubble chamar a API em localhost.
public sealed class PrivateNetworkAccessMiddleware(RequestDelegate next)
{
    private const string RequestHeader = "Access-Control-Request-Private-Network";
    private const string ResponseHeader = "Access-Control-Allow-Private-Network";

    public Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey(RequestHeader))
        {
            context.Response.Headers[ResponseHeader] = "true";
        }

        return next(context);
    }
}