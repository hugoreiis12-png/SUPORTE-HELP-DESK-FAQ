using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;

namespace Comprove.HelpDesk.Tests.Integration.Api;

// API em memória contra um MongoDB real em replica set. Usa uma database própria para não tocar na de desenvolvimento.
public sealed class HelpDeskApiFactory : WebApplicationFactory<Program>
{
    private const string Issuer = "bubble-comprove";
    private const string Audience = "helpdesk-api";
    private const string SigningKey = "tests-only-signing-key-0123456789abcdef";
    private const string LinkSigningKey = "tests-only-link-key-0123456789abcdef0123";
    private const string TestDatabase = "helpdesk_tests";

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("BubbleSecurity:SigningKey", SigningKey)
            .UseSetting("BubbleSecurity:Issuer", Issuer)
            .UseSetting("BubbleSecurity:Audience", Audience)
            .UseSetting("Mongo:DatabaseName", TestDatabase)
            .UseSetting("RabbitMq:Password", "guest")
            .UseSetting("Storage:LinkSigningKey", LinkSigningKey)
            .UseSetting("Storage:PublicBaseUrl", "http://localhost/");

    public HttpClient CreateClientFor(string tenant, string subject, params string[] roles)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(tenant, subject, roles));
        return client;
    }

    public HttpClient CreateClientWithProfile(string tenant, string subject, string name, string phone)
    {
        var client = CreateClient();
        var token = CreateToken(tenant, subject, [], new Claim("name", name), new Claim("phone", phone));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string CreateToken(string tenant, string subject, string[] roles, params Claim[] profile)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var claims = new List<Claim>
        {
            new("sub", subject),
            new("tenant_id", tenant),
            new("jti", Guid.NewGuid().ToString("N")),
        };
        claims.AddRange(roles.Select(role => new Claim("roles", role)));
        claims.AddRange(profile);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
