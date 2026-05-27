using CashFlow.EntryService.Application.Commands;
using CashFlow.EntryService.Domain.Entities;
using CashFlow.EntryService.Domain.Repositories;
using CashFlow.SharedKernel.Results;
using FluentAssertions;
using Moq;
using Xunit;

namespace CashFlow.EntryService.Tests.Application;

public class CreateEntryCommandHandlerTests
{
    private readonly Mock<IEntryRepository> _entryRepo = new();
    private readonly Mock<IOutboxRepository> _outboxRepo = new();
    private readonly CreateEntryCommandHandler _handler;

    public CreateEntryCommandHandlerTests()
    {
        _handler = new CreateEntryCommandHandler(_entryRepo.Object, _outboxRepo.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidData_ReturnsSuccessResult()
    {
        var command = new CreateEntryCommand(EntryType.Credit, 100.50m, "Venda", new DateOnly(2024, 1, 15));

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Type.Should().Be(EntryType.Credit);
        result.Value.Amount.Should().Be(100.50m);
        result.Value.Date.Should().Be(new DateOnly(2024, 1, 15));
        result.Value.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_PersistsEntryAndOutbox()
    {
        var command = new CreateEntryCommand(EntryType.Debit, 200m, "Despesa", DateOnly.FromDateTime(DateTime.Today));

        await _handler.HandleAsync(command, CancellationToken.None);

        _entryRepo.Verify(r => r.AddAsync(It.IsAny<Entry>(), It.IsAny<CancellationToken>()), Times.Once);
        _outboxRepo.Verify(r => r.AddAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        _entryRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NegativeAmount_ReturnsFailureResult()
    {
        var command = new CreateEntryCommand(EntryType.Credit, -50m, null, DateOnly.FromDateTime(DateTime.Today));

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("positivo");
    }

    [Theory]
    [InlineData(EntryType.Credit)]
    [InlineData(EntryType.Debit)]
    public async Task HandleAsync_ValidTypes_ReturnSuccess(EntryType type)
    {
        var command = new CreateEntryCommand(type, 1m, null, DateOnly.FromDateTime(DateTime.Today));

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

public class CreateEntryRequestValidatorTests
{
    private readonly CreateEntryRequestValidator _validator = new();

    [Theory]
    [InlineData("Credit")]
    [InlineData("credit")]
    [InlineData("CREDIT")]
    [InlineData("Debit")]
    [InlineData("debit")]
    public async Task Validate_ValidTypes_PassesValidation(string type)
    {
        var request = new CreateEntryRequest(type, 100m, null, new DateOnly(2024, 1, 15));
        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Pix")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public async Task Validate_InvalidType_FailsValidation(string? type)
    {
        var request = new CreateEntryRequest(type!, 100m, null, new DateOnly(2024, 1, 15));
        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public async Task Validate_NonPositiveAmount_FailsValidation(decimal amount)
    {
        var request = new CreateEntryRequest("Credit", amount, null, new DateOnly(2024, 1, 15));
        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Fact]
    public async Task Validate_DefaultDate_FailsValidation()
    {
        var request = new CreateEntryRequest("Credit", 100m, null, DateOnly.MinValue);
        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Date");
    }

    [Fact]
    public async Task Validate_AllFieldsValid_PassesValidation()
    {
        var request = new CreateEntryRequest("Credit", 1250.75m, "Venda NF 4521", new DateOnly(2024, 1, 15));
        var result = await _validator.ValidateAsync(request);
        result.IsValid.Should().BeTrue();
    }
}
