# requisitos — decisões tomadas (2026-07-15)

- **IdP**: ZITADEL no Docker Compose (padrão FirstInstance + bootstrap via management API, reaproveitado do bqtec). Access token 30 min; rotação via refresh token grant (ZITADEL rotaciona o refresh).
- **PCI/tokenização**: PAN nunca persistido (só first4/last4); coluna `external_id` criada para futura tokenização em gateway. PIN cifrado com AES-256-GCM (recuperável, como a prova exige).
- **Modelo**: seed convertido em migration EF + seeder idempotente; colunas novas: `pin_encrypted`, `deleted_at` (soft delete), `external_id`.
- **Arquitetura**: .NET 9, Controllers, EF Core + Npgsql, camadas Api/Application/Domain/Infrastructure — como recomendado abaixo.
- **CRUD**: paginação fixa de 10, `created_at DESC`, filtro `expirationDateFrom/To` aplicado no SQL. PATCH via DTO de merge próprio.
- **Testes**: 46 unit + 18 integração (Testcontainers para repositories e API).
- **Rate limit/CB**: fora do escopo da prova, documentado no README como trade-off (borda/CF; Polly p/ integrações futuras).

Detalhes e justificativas: ver `README.md`.

---

# requisitos não decididos ainda (notas originais)

- usar um IdP para auth
    - decidir entre IdentityServer, ZITADEL ou Keycloak
    - para prova, priorizar setup simples local com Docker Compose
    - considerar access token de 30 minutos e rotação/renovação

- segurança
    - considerar rate limit na camada de infra/rede - CF por exemplo. 
    - circuit breaker e retry para integrações externas
    - validação de entrada e sanitização
    - não registrar dados sensíveis em logs

- CRUD com paginação
    - paginação fixa de 10 itens
    - ordenação por created_at desc
    - filtro por período de vencimento

- PCI DSS e tokenização de cartão
    - considerar que a empresa opera em contexto PCI DSS?
    - preferir armazenar cartão em adquirente/gateway (ex.: pagar.me) e manter token externo?
    - adicionar external_id na tabela de cartões para referenciar o cartão tokenizado no provedor?
    - external_id é importante para futuras jornadas de one-click
    - evitar armazenar PAN completo localmente quando possível, reduzindo escopo PCI?

- modelo de dados sugerido
    - em cards: id, user_id, brand, first_four_digits, last_four_digits, expiration_date, status, credit_limit, external_id
    - external_id deve ser único por provedor
    - opcional: provider_name e provider_customer_id para facilitar múltiplos gateways no futuro

- arquitetura recomendada
    - ASP.NET Core Web API (.NET 9), Controllers
    - EF Core + Npgsql + Migrations
    - camadas: API, Application, Domain, Infrastructure
    - regra de negócio fora de controller (services/use cases)
    - domínio ricos com entidades e validações de negócio
    - services orquestram repositories/gateways e domínio
    - inversão de dependência entre service e infra
    - Swagger com exemplos de request/response
    - executar tudo via Docker Compose

- documentação no README
    - informar decisões de segurança e privacidade
    - informar estratégia de tokenização, external_id e impacto no escopo PCI
    - descrever trade-offs da prova versus produção
    - incluir roadmap: antifraude, 3DS, one-click, Apple Pay e Google Pay