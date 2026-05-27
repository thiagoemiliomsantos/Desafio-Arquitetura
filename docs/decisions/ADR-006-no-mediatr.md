# ADR-006: CQRS sem MediatR — Handlers via DI Manual

**Status:** Aceito  
**Data:** 2026-05-22

## Contexto

MediatR tornou-se pago para uso comercial a partir da versão 12+. O projeto aplica CQRS (ADR-003) e precisa de um mecanismo para despachar commands e queries para seus handlers sem acoplamento direto entre o endpoint e a implementação.

## Decisão

Implementar interfaces genéricas de handler no `SharedKernel` e injetá-las diretamente nos endpoints via DI do ASP.NET Core:

```csharp
// SharedKernel
public interface ICommandHandler<TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct);
}

public interface IQueryHandler<TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct);
}
```

Cada handler é registrado no contêiner com seu tipo de interface. Os endpoints Minimal API recebem o handler pelo tipo de interface via injeção de parâmetro.

## Consequências

**Positivas:**
- Zero dependência externa — sem licença comercial, sem risco de mudança de licença futura
- Handlers são classes POCO testáveis de forma isolada (injeção de dependências padrão)
- Controle total sobre o pipeline — middlewares de validação e logging implementados como decorators DI
- Alinhado com o padrão recomendado pela comunidade após a mudança de licença do MediatR

**Negativas:**
- Sem pipeline behaviors automáticos (o MediatR oferecia `IPipelineBehavior<T>`) — decorators precisam ser registrados explicitamente
- Mais código de bootstrap para registrar handlers no contêiner (mitigado com scan automático por convenção)

## Alternativa considerada

**Wolverine** (open source) — descartado por adicionar dependência externa e trazer conceitos de messaging que excedem o escopo de CQRS simples do projeto.
