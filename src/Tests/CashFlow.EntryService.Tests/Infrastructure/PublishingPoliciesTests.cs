using CashFlow.EntryService.Infrastructure.Messaging;
using FluentAssertions;
using Xunit;

namespace CashFlow.EntryService.Tests.Infrastructure;

public class PublishingPoliciesTests
{
    [Fact]
    public async Task Build_OnPermanentFailure_RetriesThreeTimes()
    {
        var calls = 0;
        var pipeline = PublishingPolicies.Build(retryDelay: TimeSpan.Zero);

        var act = async () => await pipeline.ExecuteAsync(ct =>
        {
            calls++;
            throw new InvalidOperationException("Falha simulada.");
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        calls.Should().Be(4, because: "1 tentativa inicial + 3 retries = 4 chamadas ao total");
    }

    [Fact]
    public async Task Build_OnSuccess_ExecutesOnce()
    {
        var calls = 0;
        var pipeline = PublishingPolicies.Build(retryDelay: TimeSpan.Zero);

        await pipeline.ExecuteAsync(ct =>
        {
            calls++;
            return ValueTask.CompletedTask;
        }, CancellationToken.None);

        calls.Should().Be(1);
    }

    [Fact]
    public async Task Build_OnTransientFailure_RetriesUntilSuccess()
    {
        var calls = 0;
        var pipeline = PublishingPolicies.Build(retryDelay: TimeSpan.Zero);

        await pipeline.ExecuteAsync(ct =>
        {
            calls++;
            if (calls < 3) throw new InvalidOperationException("Falha transiente.");
            return ValueTask.CompletedTask;
        }, CancellationToken.None);

        calls.Should().Be(3, because: "falha nas 2 primeiras tentativas, sucesso na terceira");
    }
}
