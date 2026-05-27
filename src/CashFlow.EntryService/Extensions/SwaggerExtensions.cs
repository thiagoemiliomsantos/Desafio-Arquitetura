using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;

namespace CashFlow.EntryService.Extensions;

/// <summary>Metadados de documentação Swagger anexados ao endpoint durante o registro na Minimal API.</summary>
internal sealed class SwaggerDocMetadata
{
    /// <summary>Exemplo do corpo da requisição.</summary>
    public object? RequestExample { get; init; }

    /// <summary>Mapa de exemplos de resposta indexado pelo status HTTP.</summary>
    public IReadOnlyDictionary<int, object>? ResponseExamples { get; init; }
}

/// <summary>
/// Filtro Swashbuckle que aplica exemplos de request/response registrados via
/// <see cref="SwaggerRouteHandlerBuilderExtensions.WithSwaggerDoc"/> ao documento OpenAPI.
/// Injeta o exemplo em todos os content-types do status (json e problem+json).
/// </summary>
internal sealed class SwaggerExamplesFilter : IOperationFilter
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc/>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var meta = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<SwaggerDocMetadata>()
            .FirstOrDefault();

        if (meta is null) return;

        if (meta.RequestExample is not null &&
            operation.RequestBody?.Content.TryGetValue("application/json", out var requestContent) == true)
        {
            requestContent.Example = ToOpenApiAny(
                JsonSerializer.SerializeToElement(meta.RequestExample, _serializerOptions));
        }

        if (meta.ResponseExamples is null) return;

        foreach (var (statusCode, example) in meta.ResponseExamples)
        {
            if (!operation.Responses.TryGetValue(statusCode.ToString(), out var response)) continue;

            var serialized = JsonSerializer.SerializeToElement(example, _serializerOptions);
            foreach (var content in response.Content.Values)
                content.Example = ToOpenApiAny(serialized);
        }
    }

    private static IOpenApiAny ToOpenApiAny(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new OpenApiObject();
                foreach (var prop in element.EnumerateObject())
                    obj[prop.Name] = ToOpenApiAny(prop.Value);
                return obj;
            case JsonValueKind.Array:
                var arr = new OpenApiArray();
                foreach (var item in element.EnumerateArray())
                    arr.Add(ToOpenApiAny(item));
                return arr;
            case JsonValueKind.String:
                return new OpenApiString(element.GetString()!);
            case JsonValueKind.Number when element.TryGetInt32(out var i):
                return new OpenApiInteger(i);
            case JsonValueKind.Number:
                return new OpenApiDouble(element.GetDouble());
            case JsonValueKind.True:
                return new OpenApiBoolean(true);
            case JsonValueKind.False:
                return new OpenApiBoolean(false);
            default:
                return new OpenApiNull();
        }
    }
}

/// <summary>
/// Extensões de <see cref="RouteHandlerBuilder"/> para documentação Swagger declarativa
/// em Minimal APIs, sem necessidade de XML comments extensos nos endpoints.
/// </summary>
public static class SwaggerRouteHandlerBuilderExtensions
{
    /// <summary>
    /// Associa sumário e exemplos de request/response ao endpoint para exibição no Swagger UI.
    /// </summary>
    /// <param name="builder">Builder do endpoint Minimal API.</param>
    /// <param name="summary">Descrição curta da operação, exibida no Swagger UI.</param>
    /// <param name="requestExample">Exemplo tipado do corpo da requisição (opcional).</param>
    /// <param name="responseExamples">
    /// Pares <c>(statusCode, exemplo)</c> para as respostas declaradas via
    /// <c>.Produces&lt;T&gt;()</c>. Exemplo: <c>[(201, new MyDto(...))]</c>.
    /// </param>
    public static RouteHandlerBuilder WithSwaggerDoc(
        this RouteHandlerBuilder builder,
        string summary,
        object? requestExample = null,
        (int StatusCode, object Example)[]? responseExamples = null)
    {
        var meta = new SwaggerDocMetadata
        {
            RequestExample = requestExample,
            ResponseExamples = responseExamples?.ToDictionary(x => x.StatusCode, x => x.Example)
        };

        return builder
            .WithSummary(summary)
            .WithMetadata(meta);
    }
}

/// <summary>Extensões de configuração do Swagger com suporte a JWT Bearer.</summary>
public static class SwaggerServiceExtensions
{
    /// <summary>
    /// Configura o Swagger com XML comments, exemplos via filtro e botão Authorize para JWT Bearer.
    /// </summary>
    /// <param name="services">Coleção de serviços da aplicação.</param>
    /// <param name="xmlFile">Nome do arquivo XML de documentação gerado pelo compilador.</param>
    public static IServiceCollection AddSwaggerWithJwtBearer(
        this IServiceCollection services,
        string xmlFile)
    {
        services.AddSwaggerGen(options =>
        {
            options.OperationFilter<SwaggerExamplesFilter>();

            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Token JWT obtido em POST /api/auth/token. Cole apenas o token, sem o prefixo 'Bearer'."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
