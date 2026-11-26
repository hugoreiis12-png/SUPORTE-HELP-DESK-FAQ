using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Exceptions;
using Comprove.HelpDesk.Infrastructure.Configuration;
using Comprove.HelpDesk.Infrastructure.Integration.OpenWa;
using Comprove.HelpDesk.Worker.Consumers;
using Microsoft.Extensions.Options;
using Xunit;

namespace Comprove.HelpDesk.Tests.Integration.Messaging;

// Contrato do adaptador contra um OpenWA simulado: nenhuma chamada sai da máquina.
public sealed class OpenWaGatewayTests
{
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string? SentBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            SentBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private static OpenWaGateway Gateway(StubHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri("https://wa.test/") },
        Options.Create(new OpenWaOptions { SessionId = "sessao-1", ApiKey = "chave-de-teste" }));

    private static readonly WhatsAppMessage Message = new("5521999999999@c.us", "Resolvido", "resolution-notice-1");

    [Fact]
    public async Task Send_uses_the_session_route_with_key_and_idempotency_headers()
    {
        var handler = new StubHandler(HttpStatusCode.Created, """{"messageId":"m1"}""");

        await Gateway(handler).SendTextAsync(Message, default);

        Assert.Equal("https://wa.test/api/sessions/sessao-1/messages/send-text", handler.Request!.RequestUri!.ToString());
        Assert.Equal("chave-de-teste", Assert.Single(handler.Request.Headers.GetValues("X-API-Key")));
        Assert.Equal("resolution-notice-1", Assert.Single(handler.Request.Headers.GetValues("Idempotency-Key")));
        Assert.Equal("""{"chatId":"5521999999999@c.us","text":"Resolvido"}""", handler.SentBody);
    }

    [Theory]
    [InlineData("""{"exists":true,"whatsappId":"552199999999@c.us"}""", "552199999999@c.us")]
    [InlineData("""{"exists":false,"whatsappId":null}""", null)]
    public async Task Contact_check_returns_the_id_reported_by_the_gateway(string body, string? expected)
    {
        var handler = new StubHandler(HttpStatusCode.OK, body);

        var chatId = await Gateway(handler).FindChatIdAsync("5521999999999", default);

        Assert.Equal(expected, chatId);
        Assert.EndsWith("/contacts/check/5521999999999", handler.Request!.RequestUri!.AbsolutePath);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, "{}")]
    [InlineData(HttpStatusCode.BadGateway, "{}")]
    [InlineData(HttpStatusCode.Conflict, """{"code":"IDEMPOTENCY_KEY_IN_PROGRESS"}""")]
    public async Task Temporary_failures_are_retried(HttpStatusCode status, string body)
    {
        var failure = await Assert.ThrowsAsync<HttpRequestException>(
            () => Gateway(new StubHandler(status, body)).SendTextAsync(Message, default));

        Assert.True(MessageRetryPolicy.ShouldRetry(failure, attempt: 1));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "{}")]
    [InlineData(HttpStatusCode.BadRequest, "{}")]
    [InlineData(HttpStatusCode.Conflict, """{"code":"IDEMPOTENCY_OUTCOME_UNKNOWN"}""")]
    public async Task Definitive_failures_and_unknown_outcomes_are_not_resent(HttpStatusCode status, string body)
    {
        var failure = await Assert.ThrowsAsync<WhatsAppRejectedException>(
            () => Gateway(new StubHandler(status, body)).SendTextAsync(Message, default));

        Assert.False(MessageRetryPolicy.ShouldRetry(failure, attempt: 1));
    }

    [Theory]
    [InlineData(false, "", true)]
    [InlineData(true, "https://wa.test", true)]
    [InlineData(true, "http://wa.test", false)]
    [InlineData(true, "", false)]
    public void Enabled_channel_requires_https_and_credentials(bool enabled, string url, bool valid)
    {
        var options = new OpenWaOptions { Enabled = enabled, BaseUrl = url, SessionId = "s", ApiKey = "k" };

        Assert.Equal(valid, Validator.TryValidateObject(options, new ValidationContext(options), [], validateAllProperties: true));
    }
}
