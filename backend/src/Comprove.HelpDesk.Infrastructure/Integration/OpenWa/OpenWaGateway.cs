using System.Net;
using System.Net.Http.Json;
using Comprove.HelpDesk.Application.Abstractions;
using Comprove.HelpDesk.Application.Exceptions;
using Comprove.HelpDesk.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Comprove.HelpDesk.Infrastructure.Integration.OpenWa;

// Adaptador do OpenWA. Não registra chave, telefone nem texto da mensagem. Repetição fica a cargo do Worker.
public sealed class OpenWaGateway(HttpClient http, IOptions<OpenWaOptions> options) : IWhatsAppGateway
{
    private const string ApiKeyHeader = "X-API-Key";
    private const string IdempotencyHeader = "Idempotency-Key";
    private const string KeyInProgress = "IDEMPOTENCY_KEY_IN_PROGRESS";

    public async Task<string?> FindChatIdAsync(string phone, CancellationToken ct)
    {
        using var request = NewRequest(HttpMethod.Get, $"contacts/check/{Uri.EscapeDataString(phone)}");
        using var response = await http.SendAsync(request, ct);
        await EnsureAcceptedAsync(response, ct);
        var contact = await response.Content.ReadFromJsonAsync<ContactCheck>(ct)
            ?? throw new InvalidDataException("Resposta vazia na verificação do contato.");
        return contact.Exists ? contact.WhatsappId : null;
    }

    public async Task SendTextAsync(WhatsAppMessage message, CancellationToken ct)
    {
        using var request = NewRequest(HttpMethod.Post, "messages/send-text");
        request.Headers.Add(IdempotencyHeader, message.IdempotencyKey);
        request.Content = JsonContent.Create(new { chatId = message.ChatId, text = message.Text });
        using var response = await http.SendAsync(request, ct);
        await EnsureAcceptedAsync(response, ct);
    }

    private HttpRequestMessage NewRequest(HttpMethod method, string path)
    {
        var session = Uri.EscapeDataString(options.Value.SessionId);
        var request = new HttpRequestMessage(method, $"api/sessions/{session}/{path}");
        request.Headers.Add(ApiKeyHeader, options.Value.ApiKey);
        return request;
    }

    // 429, 5xx e "envio em andamento" são passageiros. Outro 4xx é definitivo, inclusive resultado desconhecido.
    private static async Task EnsureAcceptedAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var status = response.StatusCode;
        var inProgress = status == HttpStatusCode.Conflict
            && (await response.Content.ReadAsStringAsync(ct)).Contains(KeyInProgress, StringComparison.Ordinal);
        if (inProgress || status is HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout || (int)status >= 500)
        {
            throw new HttpRequestException($"OpenWA indisponível (HTTP {(int)status}).", null, status);
        }

        throw new WhatsAppRejectedException($"OpenWA recusou a chamada (HTTP {(int)status}).");
    }

    private sealed record ContactCheck(bool Exists, string? WhatsappId);
}
