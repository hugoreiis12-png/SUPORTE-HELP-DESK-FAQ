using System.Text;
using Comprove.HelpDesk.Api.Configuration;
using Comprove.HelpDesk.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Comprove.HelpDesk.Api.Auth;

// Token curto assinado pelo Bubble (D-04). Claims obrigatórias: sub, tenant_id, roles, exp, iat, jti.
public static class BubbleAuthenticationExtensions
{
    public const string SupportAgentPolicy = "SupportAgent";
    private const string SubjectClaim = "sub";
    private const string RolesClaim = "roles";
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    public static IServiceCollection AddBubbleAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(BubbleSecurityOptions.Section);
        services.AddOptions<BubbleSecurityOptions>().Bind(section).ValidateDataAnnotations().ValidateOnStart();
        var security = section.Get<BubbleSecurityOptions>() ?? new BubbleSecurityOptions();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => ConfigureJwt(options, security));

        services.AddAuthorization(options =>
            options.AddPolicy(SupportAgentPolicy, policy => policy.RequireClaim(RolesClaim, RequestContext.SupportAgentRole)));
        return services;
    }

    private static void ConfigureJwt(JwtBearerOptions options, BubbleSecurityOptions security)
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = security.Issuer,
            ValidateAudience = true,
            ValidAudience = security.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(security.SigningKey)),
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = ClockSkew,
            NameClaimType = SubjectClaim,
            RoleClaimType = RolesClaim,
        };
        options.Events = new JwtBearerEvents { OnTokenValidated = RequireTenantClaim };
    }

    // tenant_id é a fonte de isolamento (ADR-006): token sem tenant é rejeitado com 401.
    private static Task RequireTenantClaim(Microsoft.AspNetCore.Authentication.JwtBearer.TokenValidatedContext context)
    {
        var tenant = context.Principal?.FindFirst(RequestContextFactory.TenantClaim)?.Value;
        if (string.IsNullOrWhiteSpace(tenant))
        {
            context.Fail("Token sem tenant_id.");
        }

        return Task.CompletedTask;
    }
}
