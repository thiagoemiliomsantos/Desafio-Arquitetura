using CashFlow.ConsolidationService.Application.Commands;
using CashFlow.ConsolidationService.Domain.Entities;
using CashFlow.ConsolidationService.Domain.Repositories;
using CashFlow.SharedKernel.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CashFlow.ConsolidationService.Tests.Application;

public class EntryEventProcessorTests
{
    private readonly Mock<IDailySummaryRepository> _repo = new();
    private readonly EntryEventProcessor _processor;

    public EntryEventProcessorTests()
    {
        _processor = new EntryEventProcessor(_repo.Object, NullLogger<EntryEventProcessor>.Instance);
    }

    private static EntryCreatedEvent MakeEvent(string type = "Credit", decimal amount = 100m) =>
        new(Guid.NewGuid(), Guid.NewGuid(), type, amount, null, new DateOnly(2025, 1, 15), DateTime.UtcNow);

    [Fact]
    public async Task ProcessAsync_WhenEventAlreadyProcessed_ReturnsFalseAndSkipsUpsert()
    {
        var ev = MakeEvent();
        _repo.Setup(r => r.IsEventAlreadyProcessedAsync(ev.EventId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(true);

        var result = await _processor.ProcessAsync(ev);

        result.Should().BeFalse();
        _repo.Verify(r => r.UpsertAsync(It.IsAny<DailySummary>(), It.IsAny<CancellationToken>()), Times.Never);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenNewEvent_UpsertsSummaryAndRegistersEvent()
    {
        var ev = MakeEvent("Credit", 250m);
        var date = ev.Date;

        _repo.Setup(r => r.IsEventAlreadyProcessedAsync(ev.EventId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(false);
        _repo.Setup(r => r.GetByDateAsync(date, It.IsAny<CancellationToken>()))
             .ReturnsAsync((DailySummary?)null);

        var result = await _processor.ProcessAsync(ev);

        result.Should().BeTrue();
        _repo.Verify(r => r.UpsertAsync(It.Is<DailySummary>(s => s.TotalCredits == 250m), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.RegisterProcessedEventAsync(ev.EventId, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_WhenSummaryExists_AccumulatesBalance()
    {
        var ev = MakeEvent("Debit", 80m);
        var date = ev.Date;
        var existing = DailySummary.Create(date);
        existing.ApplyEntry("Credit", 500m);

        _repo.Setup(r => r.IsEventAlreadyProcessedAsync(ev.EventId, It.IsAny<CancellationToken>()))
             .ReturnsAsync(false);
        _repo.Setup(r => r.GetByDateAsync(date, It.IsAny<CancellationToken>()))
             .ReturnsAsync(existing);

        await _processor.ProcessAsync(ev);

        _repo.Verify(r => r.UpsertAsync(
            It.Is<DailySummary>(s => s.TotalCredits == 500m && s.TotalDebits == 80m),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
