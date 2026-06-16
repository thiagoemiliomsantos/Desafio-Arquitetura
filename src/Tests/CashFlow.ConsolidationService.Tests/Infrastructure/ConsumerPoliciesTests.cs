using CashFlow.ConsolidationService.Infrastructure.Messaging;
using FluentAssertions;
using Polly.CircuitBreaker;
using Xunit;

namespace CashFlow.ConsolidationService.Tests.Infrastructure;

public class ConsumerPoliciesTests
{
    [Fact]
    public async Task Build_OnPermanentFailure_RetriesThreeTimes()
    {
        var calls = 0;
        var pipeline = ConsumerPolicies.Build(retryDelay: TimeSpan.Zero);

        var act = async () => await pipeline.ExecuteAsync(ct =>
        {
            calls++;
            throw new InvalidOperationException("Falha simulada.");
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        calls.Should().Be(4, because: "1 tentativa inicial + 3 retries = 4 chamadas ao total");
    }

    [Fact]
    public async Task Build_CircuitBreaker_OpensAfterReachingMinimumThroughput()
    {
        var pipeline = ConsumerPolicies.Build(retryDelay: TimeSpan.Zero);

        // MinimumThroughput=5 com FailureRatio=0.5 → 5 falhas consecutivas abrem o circuito
        for (var i = 0; i < 5; i++)
        {
            try
            {
                await pipeline.ExecuteAsync(ct =>
                {
                    throw new InvalidOperationException("Falha simulada.");
                }, CancellationToken.None);
            }
            catch (InvalidOperationException) { }
        }

        // Com o circuito aberto, a próxima chamada deve lançar BrokenCircuitException imediatamente
        var act = async () => await pipeline.ExecuteAsync(ct => ValueTask.CompletedTask, CancellationToken.None);
        await act.Should().ThrowAsync<BrokenCircuitException>();
    }

    [Fact]
    public async Task Build_OnSuccess_ExecutesOnce()
    {
        var calls = 0;
        var pipeline = ConsumerPolicies.Build(retryDelay: TimeSpan.Zero);

        await pipeline.ExecuteAsync(ct =>
        {
            calls++;
            return ValueTask.CompletedTask;
        }, CancellationToken.None);

        calls.Should().Be(1);
    }
}
