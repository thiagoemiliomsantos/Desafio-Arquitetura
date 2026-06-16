using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace CashFlow.EntryService.Tests.Integration;

/// <summary>
/// Testes de integração para o rate limiting do EntryService.
/// Classe separada de <see cref="EntryApiIntegrationTests"/> para garantir
/// uma instância isolada de <see cref="EntryServiceFactory"/> (e portanto um rate limiter zerado).
/// Cobre o critério F-01-AC-07.
/// </summary>
public class RateLimitIntegrationTests : IClassFixture<EntryServiceFactory>
{
    private readonly HttpClient _client;

    public RateLimitIntegrationTests(EntryServiceFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── F-01-AC-07: mais de 60 req/10s resulta em 429 ────────────────────────

    [Fact]
    public async Task Post_ExceedingRateLimit_Returns429()
    {
        var tokenResp = await _client.PostAsJsonAsync("/api/auth/token", new { username = "dev", password = "dev" });
        var tokenJson = await tokenResp.Content.ReadFromJsonAsync<JsonElement>();
        var token = tokenJson.GetProperty("token").GetString()!;
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // PermitLimit=50 + QueueLimit=10 → a partir da 61ª requisição simultânea → 429
        var request = new { type = "Credit", amount = 1m, date = "2099-01-01" };
        var tasks = Enumerable.Range(0, 65)
            .Select(_ => _client.PostAsJsonAsync("/api/entries", request))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        responses.Should().Contain(r => (int)r.StatusCode == 429,
            because: "65 requisições simultâneas excedem o limite de 60 (50 permits + 10 queue)");
    }
}
