using CashFlow.EntryService.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CashFlow.EntryService.Tests.Domain;

public class EntryTests
{
    [Fact]
    public void Create_WithValidData_ReturnsEntry()
    {
        var date = new DateOnly(2024, 1, 15);

        var entry = Entry.Create(EntryType.Credit, 100m, "Venda", date);

        entry.Id.Should().NotBeEmpty();
        entry.Type.Should().Be(EntryType.Credit);
        entry.Amount.Should().Be(100m);
        entry.Description.Should().Be("Venda");
        entry.Date.Should().Be(date);
        entry.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_WithNullDescription_SetsDescriptionNull()
    {
        var entry = Entry.Create(EntryType.Debit, 50m, null, DateOnly.FromDateTime(DateTime.Today));

        entry.Description.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Create_WithNonPositiveAmount_ThrowsDomainException(decimal amount)
    {
        var act = () => Entry.Create(EntryType.Credit, amount, null, DateOnly.FromDateTime(DateTime.Today));

        act.Should().Throw<DomainException>()
            .WithMessage("*positivo*");
    }

    [Theory]
    [InlineData(EntryType.Credit)]
    [InlineData(EntryType.Debit)]
    public void Create_BothTypes_AreAccepted(EntryType type)
    {
        var act = () => Entry.Create(type, 1m, null, DateOnly.FromDateTime(DateTime.Today));

        act.Should().NotThrow();
    }

    [Fact]
    public void Create_WithMinValueDate_ThrowsDomainException()
    {
        var act = () => Entry.Create(EntryType.Credit, 100m, null, DateOnly.MinValue);

        act.Should().Throw<DomainException>().WithMessage("*data*");
    }
}
