# ADR-014: Result Pattern para Handlers de Command

**Status:** Aceito  
**Data:** 2026-05-22

## Contexto

Antes desta decisão, os handlers de command lançavam `DomainException` para falhas de negócio esperadas, e o endpoint envolvia a chamada em `try/catch` para mapear a exceção para `ProblemDetails`. Usar exceções como fluxo de controle para falhas esperadas é um antipadrão: exceções são caras, obscurecem o contrato do método e forçam o chamador a conhecer quais tipos podem ser lançados.

## Decisão

Adotar o **Result Pattern** para handlers de command via `Result<T>` no `CashFlow.SharedKernel`.

### Contrato

```csharp
// CashFlow.SharedKernel/Results/Result.cs
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string? Error { get; }

    public static Result<T> Ok(T value)    => new(true, value, null);
    public static Result<T> Fail(string e) => new(false, default, e);
}
```

### Uso no handler

```csharp
public class CreateEntryCommandHandler : ICommandHandler<CreateEntryCommand, Result<CreateEntryResult>>
{
    public async Task<Result<CreateEntryResult>> HandleAsync(CreateEntryCommand command, CancellationToken ct)
    {
        try
        {
            var entry = Entry.Create(...);
            // persiste...
            return Result<CreateEntryResult>.Ok(new CreateEntryResult(...));
        }
        catch (DomainException ex)
        {
            return Result<CreateEntryResult>.Fail(ex.Message);
        }
    }
}
```

### Uso no endpoint

```csharp
var result = await handler.HandleAsync(command, ct);
return result.IsSuccess
    ? Results.Created($"/api/entries?date={result.Value!.Date:yyyy-MM-dd}", result.Value)
    : Results.Problem(title: "Erro de domínio", detail: result.Error, statusCode: 400);
```

### Escopo de aplicação

| Tipo de handler | Retorno | Motivo |
|----------------|---------|--------|
| Command handlers | `Result<T>` | Falhas de domínio são esperadas e devem ser valores |
| Query handlers | `T?` (nullable) | "Não encontrado" é um resultado esperado, retornar `null` é semântico e idiomático em C# |

Exceptions de infraestrutura (banco inacessível, timeout) **não** são capturadas — propagam normalmente para o middleware de tratamento de erros da aplicação.

## Consequências

**Positivas:**
- Contrato do handler é explícito: o chamador sabe que pode falhar sem precisar consultar documentação
- Elimina `try/catch` no endpoint para erros de domínio esperados
- Testável de forma mais natural: `result.IsFailure.Should().BeTrue()` vs `act.Should().ThrowAsync<DomainException>()`
- A interface `ICommandHandler<TCommand, TResult>` permanece inalterada — `Result<T>` é apenas o tipo de `TResult`

**Negativas:**
- `DomainException` continua existindo nas entidades (os invariantes não mudam) — o handler faz a ponte entre o modelo de exceção do domínio e o modelo de valor da aplicação
- Requer disciplina para não capturar exceções de infraestrutura como `Result.Fail`

## Alternativas consideradas

**OneOf / discriminated unions** — descartado. Adiciona dependência externa sem ganho proporcional para o cenário atual (um tipo de erro por comando).

**Manter try/catch no endpoint** — descartado. Deixa o contrato do handler implícito e espalha lógica de mapeamento de erro pela camada HTTP.
