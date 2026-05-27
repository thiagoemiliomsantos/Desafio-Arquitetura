# ADR-011: OpenTelemetry + Serilog para Observabilidade

**Status:** Aceito  
**Data:** 2026-05-22

## Contexto

Microsserviços distribuídos requerem os três pilares de observabilidade: traces (para rastrear requests entre serviços), logs (para diagnóstico detalhado) e métricas (para alertas e dashboards). A solução precisa ser vendor-neutral e executável localmente.

## Decisão

- **Traces:** OpenTelemetry SDK com exportador OTLP → Jaeger (local) / qualquer backend OTLP (produção)
- **Logs:** Serilog com sink de console estruturado (JSON) e arquivo rotativo; enriquecimento automático com `TraceId` e `SpanId` do contexto OpenTelemetry
- **Métricas:** OpenTelemetry Metrics com exportador Prometheus (preparado para futuro; não obrigatório nesta versão)

Campos obrigatórios em todo log de request: `TraceId`, `SpanId`, `UserId`, `Endpoint`, `StatusCode`, `DurationMs`.

## Consequências

**Positivas:**
- Vendor-neutral — troca de backend (Jaeger → Zipkin → Datadog) sem alterar código
- Correlação automática entre logs e traces via `TraceId` no contexto de atividade
- Serilog com destructuring mascarado evita vazamento de dados sensíveis em logs
- OpenTelemetry é o padrão emergente da CNCF para observabilidade em .NET

**Negativas:**
- Configuração inicial de OpenTelemetry tem mais boilerplate que logging simples
- Jaeger local requer container adicional em `docker-compose`
- `TraceId` nos logs só é populado automaticamente se o Serilog estiver integrado ao contexto de atividade do .NET — requer configuração explícita

## Alternativa considerada

**Application Insights** — descartado por acoplamento ao Azure. **Logging somente via `ILogger`** — descartado por não fornecer traces distribuídos entre os dois serviços.
