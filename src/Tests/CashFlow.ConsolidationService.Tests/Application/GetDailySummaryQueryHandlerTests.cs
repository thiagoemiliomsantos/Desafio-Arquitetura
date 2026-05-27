using CashFlow.ConsolidationService.Application.Queries;
using CashFlow.ConsolidationService.Domain.Entities;
using CashFlow.ConsolidationService.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace CashFlow.ConsolidationService.Tests.Application;

public class GetDailySummaryQueryHandlerTests
{
    private readonly Mock<IDailySummaryRepository> _repo = new();
    private readonly GetDailySummaryQueryHandler _handler;

    public GetDailySummaryQueryHandlerTests()
    {
        _handler = new GetDailySummaryQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenExists_ReturnsDto()
    {
        var date = new DateOnly(2024, 1, 15);
        var summary = DailySummary.Create(date);
        summary.ApplyEntry("Credit", 500m);
        summary.ApplyEntry("Debit", 200m);

        _repo.Setup(r => r.GetByDateAsync(date, It.IsAny<CancellationToken>()))
             .ReturnsAsync(summary);

        var result = await _handler.HandleAsync(new GetDailySummaryQuery(date), CancellationToken.None);

        result.Should().NotBeNull();
        result!.TotalCredits.Should().Be(500m);
        result.TotalDebits.Should().Be(200m);
        result.Balance.Should().Be(300m);
        result.Date.Should().Be(date);
    }

    [Fact]
    public async Task HandleAsync_WhenNotFound_ReturnsNull()
    {
        var date = new DateOnly(2024, 1, 15);
        _repo.Setup(r => r.GetByDateAsync(date, It.IsAny<CancellationToken>()))
             .ReturnsAsync((DailySummary?)null);

        var result = await _handler.HandleAsync(new GetDailySummaryQuery(date), CancellationToken.None);

        result.Should().BeNull();
    }
}

public class DailySummaryTests
{
    [Fact]
    public void ApplyEntry_Credit_IncreasesTotalCredits()
    {
        var summary = DailySummary.Create(DateOnly.FromDateTime(DateTime.Today));
        summary.ApplyEntry("Credit", 100m);
        summary.TotalCredits.Should().Be(100m);
        summary.TotalDebits.Should().Be(0m);
        summary.Balance.Should().Be(100m);
    }

    [Fact]
    public void ApplyEntry_Debit_IncreasesTotalDebits()
    {
        var summary = DailySummary.Create(DateOnly.FromDateTime(DateTime.Today));
        summary.ApplyEntry("Debit", 50m);
        summary.TotalDebits.Should().Be(50m);
        summary.Balance.Should().Be(-50m);
    }

    [Fact]
    public void ApplyMultipleEntries_CalculatesBalanceCorrectly()
    {
        var summary = DailySummary.Create(DateOnly.FromDateTime(DateTime.Today));
        summary.ApplyEntry("Credit", 1000m);
        summary.ApplyEntry("Debit", 300m);
        summary.ApplyEntry("Credit", 500m);
        summary.ApplyEntry("Debit", 200m);

        summary.TotalCredits.Should().Be(1500m);
        summary.TotalDebits.Should().Be(500m);
        summary.Balance.Should().Be(1000m);
    }

    [Fact]
    public void ApplyEntry_UnknownType_ThrowsInvalidOperationException()
    {
        var summary = DailySummary.Create(DateOnly.FromDateTime(DateTime.Today));
        var act = () => summary.ApplyEntry("Unknown", 100m);
        act.Should().Throw<InvalidOperationException>().WithMessage("*desconhecido*");
    }
}
