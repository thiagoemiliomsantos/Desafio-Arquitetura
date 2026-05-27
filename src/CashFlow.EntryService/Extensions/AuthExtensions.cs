using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CashFlow.EntryService.Extensions;

/// <summary>Extensões de configuração de autenticação JWT.</summary>
public static class AuthExtensions
{
    /// <summary>Registra autenticação JWT Bearer com validação de issuer, audience e chave de assinatura.</summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="configuration">Configuração da aplicação (chaves <c>Jwt:Key</c>, <c>Jwt:Issuer</c>, <c>Jwt:Audience</c>).</param>
    /// <remarks>
    /// <para><b>Produção:</b> forneça <c>Jwt:Key</c> via variável de ambiente ou secrets manager
    /// (AWS Secrets Manager, Azure Key Vault, HashiCorp Vault). A chave deve ter no mínimo 256 bits.
    /// Nunca versione a chave em repositório.</para>
    /// <para>O endpoint <c>POST /api/auth/token</c> existe apenas em <c>IsDevelopment()</c>.
    /// Em produção, substitua por um IDP externo (Keycloak, Azure AD B2C, Auth0)
    /// e configure o <c>Authority</c> do JwtBearer apontando para ele.</para>
    /// </remarks>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });

        services.AddAuthorization();

        return services;
    }
}
