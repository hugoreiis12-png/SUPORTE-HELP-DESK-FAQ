using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Comprove.HelpDesk.Contracts.Http;
using Xunit;

namespace Comprove.HelpDesk.Tests.Integration.Api;

// Requer MongoDB em replica set (anexos em GridFS).
public sealed class AttachmentTests(HelpDeskApiFactory factory) : IClassFixture<HelpDeskApiFactory>
{
    private static readonly byte[] Content = "<script>alert(1)</script>"u8.ToArray();
    private readonly HttpClient _client = factory.CreateClientFor($"tenant-{Guid.NewGuid():N}", "requester");

    private async Task<FaqResponse> CreateFaqAsync()
    {
        var request = new CreateFaqRequest(
            "Com imagem", "Descrição", "Pedidos do Campo", "Tela", "Falha de Sincronização", "Subtipo",
            "Medio", "Medio", "Media", "Media", Guid.NewGuid().ToString("N"));
        var response = await _client.PostAsJsonAsync("/api/v1/faqs", request);
        return (await response.Content.ReadFromJsonAsync<FaqResponse>())!;
    }

    private Task<HttpResponseMessage> RequestUploadAsync(string ticketId, string contentType, long size) =>
        _client.PostAsJsonAsync($"/api/v1/tickets/{ticketId}/attachments", new RequestAttachmentRequest("a.png", contentType, size));

    private async Task<Uri> UploadUrlAsync(string ticketId, long size)
    {
        var response = await RequestUploadAsync(ticketId, "image/png", size);
        response.EnsureSuccessStatusCode();
        return new Uri((await response.Content.ReadFromJsonAsync<AttachmentUploadResponse>())!.UploadUrl);
    }

    private async Task<HttpResponseMessage> PutAsync(Uri url, byte[] content, string contentType)
    {
        var body = new ByteArrayContent(content);
        body.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return await factory.CreateClient().PutAsync(url.PathAndQuery, body);
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData("image/svg+xml")]
    [InlineData("application/pdf")]
    public async Task Types_outside_the_allowlist_are_rejected(string contentType)
    {
        var faq = await CreateFaqAsync();

        var response = await RequestUploadAsync(faq.TicketId, contentType, Content.Length);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Download_uses_the_declared_type_even_if_the_upload_claims_html()
    {
        var faq = await CreateFaqAsync();
        var url = await UploadUrlAsync(faq.TicketId, Content.Length);

        Assert.Equal(HttpStatusCode.NoContent, (await PutAsync(url, Content, "text/html")).StatusCode);
        var stored = await _client.GetFromJsonAsync<FaqResponse>($"/api/v1/faqs/{faq.Id}");
        var download = await factory.CreateClient().GetAsync(new Uri(stored!.Image!.Url).PathAndQuery);

        Assert.Equal("image/png", download.Content.Headers.ContentType!.MediaType);
        Assert.Equal("nosniff", download.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Contains("sandbox", download.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task Upload_bigger_than_declared_is_rejected()
    {
        var faq = await CreateFaqAsync();
        var url = await UploadUrlAsync(faq.TicketId, Content.Length - 1);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await PutAsync(url, Content, "image/png")).StatusCode);
    }

    [Fact]
    public async Task Unfinished_upload_does_not_hide_the_image_sent_afterwards()
    {
        var faq = await CreateFaqAsync();
        await UploadUrlAsync(faq.TicketId, Content.Length);
        var pending = await _client.GetFromJsonAsync<FaqResponse>($"/api/v1/faqs/{faq.Id}");
        Assert.Null(pending!.Image);

        await PutAsync(await UploadUrlAsync(faq.TicketId, Content.Length), Content, "image/png");

        var finished = await _client.GetFromJsonAsync<FaqResponse>($"/api/v1/faqs/{faq.Id}");
        Assert.NotNull(finished!.Image);
    }
}
