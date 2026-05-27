# ADR-002: Outbox Pattern para confiabilidade de eventos

**Status:** Aceito  
**Data:** 2026-05-22

## Contexto

Ao criar um lançamento, precisamos salvar no banco E publicar um evento no RabbitMQ. Se publicarmos no broker na mesma operação HTTP (sem transação), arriscamos:
- Salvar no banco mas falhar no broker → evento perdido
- Publicar no broker mas falhar no banco → evento fantasma

O requisito de ≤ 5% de perda de lançamentos torna isso inaceitável.

## Decisão

Outbox Pattern: o evento é salvo na mesma transação do banco. Um hosted service (`OutboxPublisher`) lê a tabela `outbox` periodicamente e publica no broker, marcando como `published = true` apenas após confirmação.

## Consequências

**Positivas:**
- Garantia de at-least-once delivery
- O broker pode cair sem perder lançamentos — eles ficam na outbox até o broker voltar
- Atomicidade garantida pelo banco relacional

**Negativas:**
- Latência adicional de até 5s (intervalo do background worker)
- Carga extra no banco (tabela outbox cresce; precisa de cleanup periódico)
- Possível processamento duplicado no consumer → idempotência obrigatória

## Alternativa considerada

Transações distribuídas (2PC) — descartada por complexidade e baixa compatibilidade com brokers de mensagem.
