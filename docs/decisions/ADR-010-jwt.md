# ADR-010: JWT Bearer para Autenticação

**Status:** Aceito  
**Data:** 2026-05-22

## Contexto

Os endpoints de ambos os serviços precisam de autenticação. O sistema não gerencia sessões — cada request deve ser autossuficiente. Os serviços são consumidos por clientes que já possuem um token emitido por um Identity Provider externo (ou pelo próprio emissor configurado no projeto).

## Decisão

JWT Bearer via `Microsoft.AspNetCore.Authentication.JwtBearer`. Validação de `audience`, `issuer` e `lifetime` em todos os endpoints. Nenhum dado sensível é incluído nos claims — apenas `sub` (identificador do usuário) e roles quando necessário.

## Consequências

**Positivas:**
- Stateless — sem necessidade de armazenar sessões no servidor
- Padrão de mercado amplamente suportado por Identity Providers (Keycloak, Auth0, Azure AD)
- Integração nativa com ASP.NET Core — configuração via `AddAuthentication().AddJwtBearer()`
- Claims do token disponíveis para logging (`UserId`) sem chamada adicional ao banco

**Negativas:**
- Tokens não podem ser revogados antes da expiração sem uma blocklist (não implementada nesta versão)
- Segredo de assinatura precisa ser gerenciado com cuidado (variável de ambiente, não hardcoded)

## Alternativa considerada

**API Key** — descartada por não suportar claims de identidade e ser mais difícil de auditar por usuário. **mTLS** — descartado por complexidade operacional excessiva para o escopo do projeto.
