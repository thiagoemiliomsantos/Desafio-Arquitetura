# ADR-005: Plataforma .NET 10

**Status:** Aceito  
**Data:** 2026-05-22

## Contexto

Ao iniciar o projeto, era necessário escolher a versão LTS do .NET como plataforma alvo. As opções LTS disponíveis eram .NET 8 e .NET 10. O critério principal foi o horizonte de suporte em relação ao ciclo de vida esperado do sistema.

## Decisão

Usar .NET 10 (LTS) como plataforma alvo em todos os projetos da solução.

## Consequências

**Positivas:**
- Suporte estendido até novembro de 2028 — ampla margem para o ciclo de vida do projeto
- Melhorias de performance no runtime (JIT, GC, Span<T>, SIMD) em relação às versões anteriores
- Minimal API, OpenTelemetry e EF Core 10 na versão mais madura disponível
- Sem necessidade de migração de plataforma no curto ou médio prazo

**Negativas:**
- Algumas bibliotecas de terceiros podem demorar a publicar pacotes compatíveis com .NET 10
- Documentação e exemplos da comunidade ainda mais escassos que para versões anteriores

## Alternativa considerada

**.NET 8 (LTS)** — descartado porque o suporte encerra em novembro de 2026, janela insuficiente para o horizonte do projeto. Adotá-lo exigiria uma migração forçada em menos de um ano.
