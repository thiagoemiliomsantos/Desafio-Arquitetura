using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CashFlow.ConsolidationService.Domain.Entities;
using CashFlow.ConsolidationService.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CashFlow.ConsolidationService.Tests.Integration;

/// <summary>
/// Testes de integração do pipeline HTTP do ConsolidationService.
/// Cobrem os critérios de aceitação F-03 que requerem validação HTTP end-to-end.
/// </summary>
public class ConsolidationApiIntegrationTests : IClassFixture<ConsolidationServiceFactory>
{
    private readonly HttpClient _client;
    private readonly ConsolidationServiceFactory _factory;

    public ConsolidationApiIntegrationTests(ConsolidationServiceFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<string> GetTokenAsync()
    {
        // ConsolidationService não tem endpoint de token próprio;
        // usa a chave JWT compatível para gerar o token manualmente.
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes("super-secret-key-for-dev-only-change-in-prod"));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "cashflow-entry",
            audience: "cashflow-clients",
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }

    private void AuthorizeClient(string token) =>
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    private async Task SeedSummaryAsync(DateOnly date, decimal credits, decimal debits)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ConsolidationDbContext>();
        var summary = DailySummary.Create(date);
        summary.ApplyEntry("Credit", credits);
        summary.ApplyEntry("Debit", debits);
        db.DailySummaries.Add(summary);
        await db.SaveChangesAsync();
    }

    // ── F-03-AC-01: data com consolidado retorna 200 com campos corretos ─────

    [Fact]
    public async Task Get_ExistingDate_Returns200WithSummary()
    {
        var date = new DateOnly(2025, 4, 10);
        await SeedSummaryAsync(date, 500m, 200m);
        AuthorizeClient(await GetTokenAsync());

        var response = await _client.GetAsync($"/api/consolidation?date={date:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalCredits").GetDecimal().Should().Be(500m);
        body.GetProperty("totalDebits").GetDecimal().Should().Be(200m);
        body.GetProperty("balance").GetDecimal().Should().Be(300m);
    }

    // ── F-03-AC-02: data sem consolidado retorna 404 ─────────────────────────

    [Fact]
    public async Task Get_NonExistentDate_Returns404()
    {
        AuthorizeClient(await GetTokenAsync());

        var response = await _client.GetAsync("/api/consolidation?date=2099-12-31");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── F-03-AC-03: balance = totalCredits - totalDebits ─────────────────────

    [Fact]
    public async Task Get_ExistingDate_BalanceIsCorrectlyCalculated()
    {
        var date = new DateOnly(2025, 4, 11);
        await SeedSummaryAsync(date, 1000m, 350m);
        AuthorizeClient(await GetTokenAsync());

        var response = await _client.GetAsync($"/api/consolidation?date={date:yyyy-MM-dd}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var credits = body.GetProperty("totalCredits").GetDecimal();
        var debits = body.GetProperty("totalDebits").GetDecimal();
        var balance = body.GetProperty("balance").GetDecimal();
        balance.Should().Be(credits - debits);
    }

    // ── F-03-AC-04: sem JWT retorna 401 ──────────────────────────────────────

    [Fact]
    public async Task Get_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/consolidation?date=2025-01-01");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── F-03-AC-05: GET sem parâmetro date retorna 422 ───────────────────────

    [Fact]
    public async Task Get_WithoutDateParam_Returns422()
    {
        AuthorizeClient(await GetTokenAsync());

        var response = await _client.GetAsync("/api/consolidation");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── F-03-AC-06: GET com data inválida retorna 422 ────────────────────────

    [Fact]
    public async Task Get_WithInvalidDate_Returns422()
    {
        AuthorizeClient(await GetTokenAsync());

        var response = await _client.GetAsync("/api/consolidation?date=nao-e-data");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
