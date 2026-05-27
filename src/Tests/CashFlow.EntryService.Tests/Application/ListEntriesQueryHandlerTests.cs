using CashFlow.EntryService.Application.Queries;
using CashFlow.EntryService.Domain.Entities;
using CashFlow.EntryService.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace CashFlow.EntryService.Tests.Application;

public class ListEntriesQueryHandlerTests
{
    private readonly Mock<IEntryRepository> _repo = new();
    private readonly ListEntriesQueryHandler _handler;

    public ListEntriesQueryHandlerTests()
    {
        _handler = new ListEntriesQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task HandleAsync_WithEntries_ReturnsMappedDtos()
    {
        var date = new DateOnly(2024, 1, 15);
        var entries = new List<Entry>
        {
            Entry.Create(EntryType.Credit, 100m, "Venda", date),
            Entry.Create(EntryType.Debit,  50m,  "Despesa", date)
        };

        _repo.Setup(r => r.GetByDateAsync(date, It.IsAny<CancellationToken>()))
             .ReturnsAsync(entries);

        var result = await _handler.HandleAsync(new ListEntriesQuery(date), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Type.Should().Be(EntryType.Credit);
        result[0].Amount.Should().Be(100m);
        result[0].Description.Should().Be("Venda");
        result[0].Date.Should().Be(date);
        result[1].Type.Should().Be(EntryType.Debit);
        result[1].Amount.Should().Be(50m);
    }

    [Fact]
    public async Task HandleAsync_WithNullDescription_MapsNullCorrectly()
    {
        var date = new DateOnly(2024, 1, 15);
        var entries = new List<Entry> { Entry.Create(EntryType.Credit, 10m, null, date) };

        _repo.Setup(r => r.GetByDateAsync(date, It.IsAny<CancellationToken>()))
             .ReturnsAsync(entries);

        var result = await _handler.HandleAsync(new ListEntriesQuery(date), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Description.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithNoEntries_ReturnsEmptyList()
    {
        var date = new DateOnly(2024, 1, 15);

        _repo.Setup(r => r.GetByDateAsync(date, It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<Entry>());

        var result = await _handler.HandleAsync(new ListEntriesQuery(date), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
