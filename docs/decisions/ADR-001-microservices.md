# ADR-001: Microsserviços vs. Monolito Modular

**Status:** Aceito  
**Data:** 2026-05-22

## Contexto

O sistema precisa garantir que o serviço de lançamentos continue funcionando mesmo que o de consolidação esteja indisponível. Isso é um requisito explícito de negócio, não apenas preferência técnica.

## Decisão

Dois microsserviços independentes com banco de dados separado (Database-per-Service).

## Consequências

**Positivas:**
- Falha isolada: ConsolidationService pode cair sem impactar EntryService
- Deploy independente; times podem evoluir cada serviço separadamente
- Escala horizontal independente (Lançamentos tem carga maior)

**Negativas:**
- Consistência eventual — consolidado pode ter dados levemente defasados
- Mais infraestrutura para operar (dois bancos, um broker)
- Queries cross-service são impossíveis sem chamada remota ou evento

## Alternativa considerada

**Monolito modular** com módulos de `Lancamentos` e `Consolidado` no mesmo processo — mais simples de operar, porém não atende o requisito de independência de falhas sem circuit breaker no próprio processo.
