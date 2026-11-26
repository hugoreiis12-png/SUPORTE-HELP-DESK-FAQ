using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Comprove.HelpDesk.Application.Exceptions;
using Comprove.HelpDesk.Contracts.Integration;
using Comprove.HelpDesk.Domain.Exceptions;
using Comprove.HelpDesk.Infrastructure.Configuration;
using Comprove.HelpDesk.Worker.Consumers;
using Xunit;

namespace Comprove.HelpDesk.Tests.Integration.Messaging;

// Não precisa de infraestrutura: regras de repetição, roteamento e configuração.
public sealed class MessagingPolicyTests
{
    public static TheoryData<Exception> PermanentFailures => new()
    {
        new DomainRuleViolationException("regra"),
        new TicketNotFoundException("t-1"),
        new JsonException(),
        new InvalidDataException(),
    };

    [Theory]
    [MemberData(nameof(PermanentFailures))]
    public void Permanent_failures_go_straight_to_the_dlq(Exception failure) =>
        Assert.False(MessageRetryPolicy.ShouldRetry(failure, attempt: 1));

    [Fact]
    public void Transient_failures_are_retried_up_to_the_limit()
    {
        Assert.True(MessageRetryPolicy.ShouldRetry(new TimeoutException(), attempt: 1));
        Assert.True(MessageRetryPolicy.ShouldRetry(new ConcurrencyConflictException(), attempt: 1));
        Assert.False(MessageRetryPolicy.ShouldRetry(new TimeoutException(), MessageRetryPolicy.MaxAttempts));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 4)]
    [InlineData(20, 30)]
    public void Delay_grows_and_is_capped(int attempt, int expectedSeconds) =>
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), MessageRetryPolicy.DelayAfter(attempt));

    [Theory]
    [InlineData(RabbitTopology.TicketCreatedRoutingKey, true)]
    [InlineData(RabbitTopology.TicketStatusChangedRoutingKey, true)]
    [InlineData(RabbitTopology.CommentCreatedRoutingKey, false)]
    public void Only_events_with_a_queue_require_a_destination(string routingKey, bool expected) =>
        Assert.Equal(expected, RabbitTopology.HasConsumer(routingKey));

    [Theory]
    [InlineData(false, "", true)]
    [InlineData(true, "", false)]
    [InlineData(true, "https://comprove.example/", true)]
    public void Comprove_url_is_required_only_with_the_sync_enabled(bool syncEnabled, string baseUrl, bool valid)
    {
        var options = new ComproveOptions { FaqSyncEnabled = syncEnabled, BaseUrl = baseUrl };

        Assert.Equal(valid, Validator.TryValidateObject(options, new ValidationContext(options), [], validateAllProperties: true));
    }
}
