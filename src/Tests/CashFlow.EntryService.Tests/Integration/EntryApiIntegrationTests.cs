using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CashFlow.EntryService.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CashFlow.EntryService.Tests.Integration;

/// <summary>
/// Testes de integração do pipeline HTTP do EntryService.
/// Cobrem os critérios de aceitação F-01 e F-02 que requerem validação HTTP end-to-end.
/// </summary>
public class EntryApiIntegrationTests : IClassFixture<EntryServiceFactory>
{
    private readonly HttpClient _client;
    private readonly EntryServiceFactory _factory;

    public EntryApiIntegrationTests(EntryServiceFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<string> GetTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/token", new { username = "dev", password = "dev" });
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("token").GetString()!;
    }

    private void AuthorizeClient(string token) =>
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    private static object ValidRequest(string date = "2025-01-15") =>
        new { type = "Credit", amount = 100.50m, description = "Venda", date };

    // ── F-01-AC-01: POST retorna 201 com dados do lançamento ─────────────────

    [Fact]
    public async Task Post_WithValidRequest_Returns201WithId()
    {
        AuthorizeClient(await GetTokenAsync());

        var response = await _client.PostAsJsonAsync("/api/entries", ValidRequest("2025-02-01"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("type").GetString().Should().Be("Credit");
        body.GetProperty("amount").GetDecimal().Should().Be(100.50m);
    }

    // ── F-01-AC-02: tipo inválido retorna 422 com campo "type" no mapa ───────

    [Fact]
    public async Task Post_InvalidType_Returns422WithTypeError()
    {
        AuthorizeClient(await GetTokenAsync());
        var request = new { type = "Pix", amount = 100m, date = "2025-02-02" };

        var response = await _client.PostAsJsonAsync("/api/entries", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").TryGetProperty("Type", out _).Should().BeTrue();
    }

    // ── F-01-AC-03: amount ≤ 0 retorna 422 com campo "amount" ───────────────

    [Fact]
    public async Task Post_NegativeAmount_Returns422WithAmountError()
    {
        AuthorizeClient(await GetTokenAsync());
        var request = new { type = "Credit", amount = -1m, date = "2025-02-03" };

        var response = await _client.PostAsJsonAsync("/api/entries", request);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").TryGetProperty("Amount", out _).Should().BeTrue();
    }

    // ── F-01-AC-04: sem JWT retorna 401 ──────────────────────────────────────

    [Fact]
    public async Task Post_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/api/entries", ValidRequest("2025-02-04"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── F-01-AC-05: evento é gravado na tabela outbox na mesma transação ─────

    [Fact]
    public async Task Post_WithValidRequest_WritesOutboxEntry()
    {
        AuthorizeClient(await GetTokenAsync());

        var postResp = await _client.PostAsJsonAsync("/api/entries", ValidRequest("2025-02-05"));
        postResp.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EntryDbContext>();
        db.Outbox.Should().Contain(o => !o.Published);
    }

    // ── F-02-AC-01: GET retorna 200 com lançamentos existentes ───────────────

    [Fact]
    public async Task Get_DateWithEntries_Returns200WithList()
    {
        AuthorizeClient(await GetTokenAsync());
        var postResp = await _client.PostAsJsonAsync("/api/entries", ValidRequest("2025-03-10"));
        postResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await _client.GetAsync("/api/entries?date=2025-03-10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        items.Should().HaveCountGreaterThan(0);
    }

    // ── F-02-AC-02: GET retorna 200 com lista vazia ───────────────────────────

    [Fact]
    public async Task Get_DateWithNoEntries_Returns200WithEmptyList()
    {
        AuthorizeClient(await GetTokenAsync());

        var response = await _client.GetAsync("/api/entries?date=2099-12-31");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        items.Should().BeEmpty();
    }

    // ── F-02-AC-03: GET sem parâmetro date retorna 422 ───────────────────────

    [Fact]
    public async Task Get_WithoutDateParam_Returns422()
    {
        AuthorizeClient(await GetTokenAsync());

        var response = await _client.GetAsync("/api/entries");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── F-02-AC-04: GET com data inválida retorna 422 ────────────────────────

    [Fact]
    public async Task Get_WithInvalidDate_Returns422()
    {
        AuthorizeClient(await GetTokenAsync());

        var response = await _client.GetAsync("/api/entries?date=nao-e-data");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── F-02-AC-05: GET sem JWT retorna 401 ──────────────────────────────────

    [Fact]
    public async Task Get_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/entries?date=2025-01-01");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
