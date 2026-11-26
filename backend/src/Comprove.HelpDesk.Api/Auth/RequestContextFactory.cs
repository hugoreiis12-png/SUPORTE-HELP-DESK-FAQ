using Comprove.HelpDesk.Api.Middlewares;
using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Domain.ValueObjects;

namespace Comprove.HelpDesk.Api.Auth;

// Tenant e ator vêm SEMPRE do token validado. Nenhum campo do corpo entra aqui.
public static class RequestContextFactory
{
    public const string TenantClaim = "tenant_id";
    private const string SubjectClaim = "sub";
    private const string RolesClaim = "roles";
    private const string NameClaim = "name";
    private const string EmailClaim = "email";
    private const string UnitClaim = "unit";
    private const string PhoneClaim = "phone";

    public static RequestContext Create(HttpContext http)
    {
        var user = http.User;
        var tenant = TenantId.From(user.FindFirst(TenantClaim)?.Value);
        var actor = user.FindFirst(SubjectClaim)?.Value ?? throw new InvalidOperationException("Token sem sub.");
        var roles = user.FindAll(RolesClaim).Select(claim => claim.Value).ToArray();
        var correlationId = http.Items[CorrelationIdMiddleware.ItemKey] as string ?? http.TraceIdentifier;
        return new RequestContext(
            tenant,
            actor,
            roles,
            correlationId,
            user.FindFirst(NameClaim)?.Value,
            user.FindFirst(EmailClaim)?.Value,
            user.FindFirst(UnitClaim)?.Value,
            user.FindFirst(PhoneClaim)?.Value);
    }
}
