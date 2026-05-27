# ADR-004: Minimal API vs Controllers MVC

**Status:** Aceito  
**Data:** 2026-05-22

## Contexto

O .NET oferece duas abordagens para expor endpoints HTTP: Controllers MVC (com atributos `[ApiController]`, `[Route]`, etc.) e Minimal API (introduzida no .NET 6, maturada no .NET 8+). O projeto é um serviço de domínio estreito — dois recursos principais (`/lancamentos` e `/consolidado`) — sem necessidade de convenções MVC.

## Decisão

Usar Minimal API para todos os endpoints. Cada serviço organiza seus endpoints em arquivos de extensão (`IEndpointRouteBuilder`) agrupados por recurso.

## Consequências

**Positivas:**
- Startup mais rápido — sem overhead do MVC pipeline (filters, model binders, formatters desnecessários)
- Menos boilerplate — sem classes de controller, atributos de rota duplicados
- Composição explícita — endpoints mapeados diretamente no `Program.cs` ou em módulos injetados
- Alinhado com a direção do .NET — Minimal API recebe mais investimento do time ASP.NET

**Negativas:**
- Projetos com dezenas de recursos ficam com `Program.cs` longo se não organizado em módulos
- Alguns filtros MVC (action filters) não se aplicam — substituídos por endpoint filters

## Alternativa considerada

**Controllers MVC** — mais familiar para equipes com histórico em ASP.NET. Descartado por adicionar convenções desnecessárias para um serviço com poucos recursos.
