# ADR-009: Polly para Resiliência

**Status:** Aceito  
**Data:** 2026-05-22

## Contexto

Dois serviços externos críticos podem falhar: o banco de dados (durante escrita/leitura) e o RabbitMQ (durante publicação pelo OutboxPublisher e consumo pelo ConsolidationService). Precisamos de retry, circuit breaker e timeout sem implementar esses padrões manualmente.

## Decisão

Usar Polly v8+ (integrado com `Microsoft.Extensions.Http.Resilience`) para definir políticas de resiliência:

- **EntryService:** timeout de 2s por operação de escrita; retry com backoff exponencial (3 tentativas) no OutboxPublisher
- **ConsolidationService:** circuit breaker (5 falhas → aberto por 30s); retry com jitter (3 tentativas) no consumer RabbitMQ

## Consequências

**Positivas:**
- API fluente e bem conhecida no ecossistema .NET
- Polly v8 integra nativamente com `IHttpClientFactory` e `HttpResilienceHandler`
- Circuit breaker protege ConsolidationService de cascatear falhas para o banco
- Retry com jitter evita thundering herd após falha do broker

**Negativas:**
- Políticas mal configuradas (timeout muito agressivo, retry sem jitter) podem piorar a situação sob carga
- Polly não substitui health checks — ambos são necessários

## Alternativa considerada

Implementação manual de retry/circuit breaker — descartada por risco de bugs em lógica de concorrência que o Polly já resolveu e testou extensivamente.
