using CashFlow.EntryService.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CashFlow.EntryService.Endpoints;

/// <summary>Registro do endpoint de geração de token JWT para uso em Development.</summary>
public static class TokenEndpoints
{
    /// <summary>
    /// Mapeia <c>POST /api/auth/token</c>, disponível apenas em ambiente de desenvolvimento
    /// para permitir testes no Swagger UI sem necessidade de um serviço de identidade externo.
    /// Em produção, substituir por Keycloak, Azure AD B2C ou outro IDP.
    /// </summary>
    /// <param name="app">Builder de rotas da aplicação.</param>
    public static IEndpointRouteBuilder MapTokenEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/token", (TokenRequest request, IConfiguration config) =>
        {
            if (request.Username != "dev" || request.Password != "dev")
                return Results.Problem("Credenciais inválidas.", statusCode: 401);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
            var expiry = DateTime.UtcNow.AddHours(8);

            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: [new Claim(ClaimTypes.Name, request.Username)],
                expires: expiry,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return Results.Ok(new TokenResponse(
                new JwtSecurityTokenHandler().WriteToken(token),
                expiry
            ));
        })
        .AllowAnonymous()
        .Produces<TokenResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSwaggerDoc(
            summary: "[DEV] Gera token JWT para testes no Swagger UI",
            requestExample: new TokenRequest("dev", "dev"),
            responseExamples:
            [
                (200, new TokenResponse(
                    "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6ImRldiIsIm5iZiI6MTc0ODE5MDAwMCwiZXhwIjoxNzQ4MjE4ODAwLCJpYXQiOjE3NDgxOTAwMDB9.exemplo",
                    new DateTime(2025, 5, 25, 20, 0, 0, DateTimeKind.Utc))),
                (401, new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Title = "Não autorizado",
                    Status = 401,
                    Detail = "Credenciais inválidas."
                })
            ]);

        return app;
    }
}

/// <summary>Payload da requisição de autenticação.</summary>
/// <param name="Username">Usuário. Em Development, use <c>dev</c>.</param>
/// <param name="Password">Senha. Em Development, use <c>dev</c>.</param>
public record TokenRequest(string Username, string Password);

/// <summary>Token JWT emitido após autenticação bem-sucedida.</summary>
/// <param name="Token">Token JWT assinado, válido por 8 horas.</param>
/// <param name="ExpiresAt">Timestamp UTC de expiração do token.</param>
public record TokenResponse(string Token, DateTime ExpiresAt);
