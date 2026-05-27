# ADR-008: PostgreSQL + EF Core como Camada de Persistência

**Status:** Aceito  
**Data:** 2026-05-22

## Contexto

Cada serviço precisa de persistência relacional independente (Database-per-Service, ADR-001). O banco precisa suportar transações ACID para garantir a atomicidade do Outbox Pattern (ADR-002) e tipos como `DECIMAL(18,2)` para valores monetários.

## Decisão

PostgreSQL como banco de dados relacional. EF Core 10 como ORM, usando Migrations para versionamento de schema. Npgsql como provider do EF Core para PostgreSQL.

## Consequências

**Positivas:**
- PostgreSQL suporta `JSONB` nativamente — útil para a coluna `payload` da tabela `outbox`
- EF Core com Migrations garante schema versionado e reproduzível
- `DECIMAL(18,2)` mapeado corretamente pelo Npgsql sem arredondamento
- Testcontainers provê instância PostgreSQL real para testes de integração
- Sem custo de licença (open source)

**Negativas:**
- EF Core adiciona camada de abstração — queries complexas podem precisar de SQL raw
- Migrations em produção exigem cuidado em tabelas grandes (lock)

## Alternativa considerada

**Dapper** — descartado por exigir SQL manual para todas as operações, aumentando o risco de erros de tipo para valores monetários. **SQL Server** — descartado por custo de licença e preferência por stack open source.
