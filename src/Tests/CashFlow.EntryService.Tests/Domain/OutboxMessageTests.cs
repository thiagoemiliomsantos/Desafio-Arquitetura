using CashFlow.EntryService.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CashFlow.EntryService.Tests.Domain;

public class OutboxMessageTests
{
    [Fact]
    public void Create_SetsFieldsCorrectly()
    {
        var message = OutboxMessage.Create("EntryCreatedEvent", "{\"id\":\"abc\"}");

        message.Id.Should().NotBeEmpty();
        message.EventType.Should().Be("EntryCreatedEvent");
        message.Payload.Should().Be("{\"id\":\"abc\"}");
        message.Published.Should().BeFalse();
        message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_StartsAsUnpublished()
    {
        var message = OutboxMessage.Create("EntryCreatedEvent", "{}");

        message.Published.Should().BeFalse();
    }

    [Fact]
    public void MarkAsPublished_SetsPublishedTrue()
    {
        var message = OutboxMessage.Create("EntryCreatedEvent", "{}");

        message.MarkAsPublished();

        message.Published.Should().BeTrue();
    }

    [Fact]
    public void Create_TwoMessages_HaveDistinctIds()
    {
        var m1 = OutboxMessage.Create("EntryCreatedEvent", "{}");
        var m2 = OutboxMessage.Create("EntryCreatedEvent", "{}");

        m1.Id.Should().NotBe(m2.Id);
    }
}
