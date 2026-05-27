using CashFlow.ConsolidationService.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CashFlow.ConsolidationService.Tests.Domain;

public class ProcessedEventTests
{
    [Fact]
    public void Register_SetsEventIdAndProcessedAt()
    {
        var eventId = Guid.NewGuid();

        var processed = ProcessedEvent.Register(eventId);

        processed.EventId.Should().Be(eventId);
        processed.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Register_DifferentEventIds_AreDistinct()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var p1 = ProcessedEvent.Register(id1);
        var p2 = ProcessedEvent.Register(id2);

        p1.EventId.Should().NotBe(p2.EventId);
    }
}
