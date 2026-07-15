# Cards API — Prova Técnica .NET Pleno

API REST para gestão de cartões de crédito do usuário autenticado. ASP.NET Core (.NET 9), PostgreSQL + EF Core, autenticação OIDC via ZITADEL, tudo orquestrado por Docker Compose.

## Como rodar

Pré-requisito: Docker + Docker Compose.

```bash
docker compose up --build
```

Aguarde o serviço `cards-zitadel-bootstrap` concluir (ele provisiona o IdP, cerca de 30s, se der erro executo do comando acima novamente) e a `cards-api` subir. Depois:

- **Swagger**: http://localhost:8000/swagger
- **ZITADEL** (console/login): http://auth.localhost:8080

> `auth.localhost` resolve para `127.0.0.1` automaticamente nos browsers. Para usar `curl` contra o IdP, adicione ao `/etc/hosts`: `127.0.0.1 auth.localhost`.

### Usuários de teste (provisionados no IdP e no banco)

| Usuário | E-mail | Senha | Cartões |
|---|---|---|---|
| Mariana Alves | `mariana.alves@cardcorp.test` | `Cards@2026!` | 12 (valida paginação) |
| Rafael Souza | `rafael.souza@cardcorp.test` | `Cards@2026!` | 4 |
| Camila Rocha | `camila.rocha@cardcorp.test` | `Cards@2026!` | 7 |

### Autenticando no Swagger

1. Clique em **Authorize** e mantenha os scopes marcados (`openid profile email offline_access`).
2. Faça login com um dos usuários acima (no primeiro login o ZITADEL pode oferecer cadastro de MFA/passkey — use "skip").
3. Os endpoints passam a responder com o token da sessão.

## Autenticação e rotação de token

- **Access token JWT com validade de 30 minutos**, emitido pelo ZITADEL (`ZITADEL_DEFAULTINSTANCE_OIDCSETTINGS_ACCESSTOKENLIFETIME=30m`). Tokens expirados são rejeitados com `401` pela validação JWT (issuer, assinatura via JWKS, expiração e audience do projeto).
- **Rotação/renovação**: fluxo *refresh token grant* do OIDC. O scope `offline_access` emite um refresh token; a cada renovação o ZITADEL **rotaciona o refresh token** — o valor anterior é invalidado e um novo access token (mais 30 min) e um novo refresh token são emitidos, exatamente como a prova exige.

```bash
# renovação (o client_id é gerado no bootstrap; recupere com:
#   docker exec cards-api cat /zitadel/out/swagger_client_id)
curl -X POST http://auth.localhost:8080/oauth/v2/token \
  -d grant_type=refresh_token \
  -d client_id=<CLIENT_ID> \
  -d refresh_token=<REFRESH_TOKEN>
```

- A API identifica o usuário pelo claim `email` do token (fallback: endpoint `userinfo` do IdP com o próprio token do chamador) e o mapeia para a tabela `users` do seed. Token válido de usuário fora do seed recebe `403`.
- Toda operação é limitada ao usuário autenticado: consultas por id são filtradas por `user_id` na query — cartão de outro usuário é indistinguível de inexistente (`404`).

## Endpoints

Todas as rotas exigem autenticação.

| Método | Rota | Comportamento |
|---|---|---|
| GET | `/api/cards` | Lista paginada (blocos fixos de 10, mais recente primeiro) com filtro opcional de vencimento |
| GET | `/api/cards/{id}` | Consulta um cartão do usuário |
| POST | `/api/cards` | Cria cartão (recebe `cardNumber` e `pin`; nunca os devolve) |
| PUT | `/api/cards/{id}` | Substituição completa dos campos editáveis |
| PATCH | `/api/cards/{id}` | Atualização parcial (merge) |
| DELETE | `/api/cards/{id}` | Remoção (soft delete) |
| GET | `/api/cards/{id}/pin` | **Endpoint exclusivo** de consulta da senha |

### Listagem, paginação e filtro

- `GET /api/cards?page=2&expirationDateFrom=2028-01-01&expirationDateTo=2028-06-30`
- `page` inicia em 1; o tamanho é **fixo em 10** (imposto no servidor, o cliente não altera).
- Ordenação: `created_at DESC` (mais novo primeiro), com desempate estável por id.
- `expirationDateFrom`/`expirationDateTo` (formato `yyyy-MM-dd`, inclusivos) filtram o **vencimento do cartão**; o filtro é aplicado na query SQL, antes da materialização.
- Resposta: `{ page, pageSize, totalItems, totalPages, items[] }`; o número do cartão aparece apenas mascarado (`5321 **** **** 5336`).

### Campos editáveis e semântica de PUT / PATCH

Campos editáveis: `cardholderName`, `nickname`, `brand`, `cardNumber`, `expirationDate`, `creditLimit`, `status`, `pin`. Campos não editáveis: `id`, `userId`, `createdAt` (e os de controle `updatedAt`/`deletedAt`).

- **PUT** — substituição completa: todos os campos editáveis são obrigatórios, exceto `nickname` (opcional; **ausente/`null` limpa o apelido**). Campo obrigatório ausente → `400`.
- **PATCH** — merge parcial com DTO próprio (documentado, alternativa ao JSON Patch): **somente os campos presentes no payload são aplicados**; ausentes permanecem como estão. Payload sem nenhum campo editável → `400`. Limpar `nickname` só via PUT (semântica de merge não distingue "ausente" de "null").

### Validações e erros

- `status` ∈ `ACTIVE | BLOCKED | CANCELLED`; `creditLimit >= 0`; `cardNumber` com 13–19 dígitos; `pin` com 4–6 dígitos; datas no formato ISO.
- **Luhn não é validado propositalmente**: os PANs de exemplo da própria prova não passam no checksum (decisão documentada; em produção seria validado no gateway/tokenizador).
- Erros padronizados em **ProblemDetails (RFC 7807)** com status coerentes: `400` validação, `401` sem/expirado token, `403` usuário não registrado, `404` inexistente ou de outro usuário, `500` genérico sem vazar detalhes internos.

## Decisões técnicas

### Arquitetura

```
src/
  Cards.Domain/          entidades ricas (CreditCard, User), value objects (CardNumber, Pin), invariantes
  Cards.Application/     casos de uso (CardsService), DTOs, portas (ICreditCardRepository, IPinCipher, ...)
  Cards.Infrastructure/  EF Core + Npgsql, migrations, repositórios, cifra AES-GCM, seeder
  Cards.Api/             controllers finos, auth JWT, ProblemDetails, Swagger (composition root)
```

Regras de negócio ficam no domínio/serviços — controllers apenas orquestram. A Application não referencia nenhum framework (inversão de dependência: a Infrastructure implementa as portas).

### Evolução do modelo de dados (vs. o SQL fornecido)

O script fornecido foi convertido em **migration EF Core** (schema idêntico: mesmos nomes, constraints e índices) + **seeder idempotente** com a mesma massa de dados e IDs fixos (reproduzíveis para teste manual). Três colunas foram adicionadas em `credit_cards`, conforme a própria prova permite:

| Coluna | Motivo |
|---|---|
| `pin_encrypted BYTEA` | O seed não tinha senha; a prova exige recuperar a senha original sem armazená-la em texto puro |
| `deleted_at TIMESTAMPTZ` | Soft delete: o DELETE some das consultas comuns preservando rastreabilidade |
| `external_id VARCHAR` | Referência futura ao cartão tokenizado em adquirente/gateway (one-click; reduz escopo PCI) |

Como o seed não define senha, os cartões da massa inicial recebem o **PIN padrão `1234`** (cifrado), documentado aqui de propósito para facilitar a validação manual.

### Dados sensíveis (PAN e PIN)

- **O PAN completo nunca é persistido**: na criação/atualização o número é validado e imediatamente reduzido a `first_four_digits` + `last_four_digits` (modelo do seed) — não há coluna capaz de guardar o número inteiro, o que reduz drasticamente o escopo PCI DSS. Respostas usam sempre o formato mascarado `5321 **** **** 5336`.
- **PIN cifrado com AES-256-GCM** (cifra autenticada; nonce aleatório por operação, payload = nonce+tag+ciphertext). Hash não atenderia o requisito de devolver a senha original. A chave vem de configuração/ambiente (`PinEncryption__Key`, base64 de 32 bytes) — a do compose é **apenas de desenvolvimento**.
- A consulta do PIN existe **apenas** em `GET /api/cards/{id}/pin`: exige dono autenticado, gera **log de auditoria** (usuário, cartão, timestamp — nunca o valor) e responde com `Cache-Control: no-store`.
- `cardNumber` e `pin` não aparecem em nenhuma resposta comum, listagem, log ou mensagem de erro (as mensagens de validação não ecoam valores recebidos; não há logging de corpo de requisição).

#### Por que não armazenamos o PAN cifrado "para pagamentos futuros"?

A pergunta natural é: guardar o número cifrado não habilitaria cobranças no futuro? Não — e a decisão de descartá-lo é deliberada, por três motivos:

1. **PAN cifrado continua sendo PAN para o PCI DSS**: se o dado é recuperável, a aplicação entra no escopo completo da norma (gestão de chaves auditada/HSM, segmentação de rede, scans e auditorias recorrentes). É um passivo permanente comprado contra uma feature hipotética.
2. **PAN armazenado sozinho não autoriza pagamento**: uma cobrança exige CVV — que o PCI DSS **proíbe armazenar após a autorização, em qualquer forma** ("sensitive authentication data") — e, nos fluxos modernos, 3DS. O dado guardado não entregaria a receita que o motivou.
3. **O caminho correto para esse futuro já está no schema**: é a função do `external_id` — o número vai uma única vez para um gateway/cofre certificado PCI, que devolve um token permanente; a cobrança one-click usa o token (idealmente o PAN nem passa por este backend: o cliente fala direto com o gateway via campos hospedados/SDK).

Nuance assumida: este projeto **já** armazena um segredo recuperável — o PIN — porque a prova o exige explicitamente (seção 4.6). Esse é exatamente o tipo de custo que só se paga mediante requisito concreto; replicá-lo para o PAN, sem requisito, iria na contramão da regra 7 (mascaramento, sem recuperação) e da minimização de dados. Em produção, nem o PIN seria consultável — ver a tabela de trade-offs abaixo.

### Testes

```bash
dotnet test                                   # tudo (68 testes)
dotnet test tests/Cards.UnitTests             # domínio + serviços (fakes em memória)
dotnet test tests/Cards.IntegrationTests      # requer Docker (Testcontainers)
```

- **Unit**: invariantes do domínio (mascaramento, status, limites, soft delete) e regras do serviço (paginação, filtros, isolamento por usuário, semântica PUT/PATCH, PIN).
- **Integração**: repositório contra **PostgreSQL real** (Testcontainers) — paginação dos 12 cartões da Mariana, filtro aplicado no SQL, escopo por dono, soft delete; a **API completa** (WebApplicationFactory + migrations + seed) — 401/404, mascaramento, CRUD, PIN, ProblemDetails; e um **E2E** que percorre o ciclo de vida inteiro do cartão usando os **payloads literais do PDF da prova** como specs executáveis. Toda a suíte compartilha **um único container** Postgres (um database por classe de teste). A autenticação nos testes usa um scheme de teste que injeta o claim de e-mail (o IdP fica fora do loop de teste).

## Trade-offs da prova × produção

| Tema | Na prova | Em produção |
|---|---|---|
| Chave de cifra do PIN | env var no compose | KMS/HSM com rotação de chave e envelope encryption |
| PIN recuperável | exigido pela prova | PIN não seria consultável; verificação por comparação (HSM) e re-emissão em vez de leitura |
| Cartão | só first4/last4 locais | PAN tokenizado em adquirente/gateway (ex.: pagar.me) referenciado por `external_id` |
| Rate limit | fora do escopo | na borda (Cloudflare/API gateway) + `429` na aplicação |
| Resiliência | sem integrações externas | circuit breaker/retry (Polly) para gateways e IdP |
| Rotação do refresh token | delegada ao ZITADEL | idem + detecção de reuso e revogação em cascata |
| Auditoria | log estruturado | trilha imutável (event sourcing/append-only) + SIEM |

## Roadmap (evoluções naturais)

Antifraude transacional, 3-D Secure (3DS2), one-click com cartão tokenizado (`external_id`), Apple Pay / Google Pay (DPAN), observabilidade (OpenTelemetry) e CI com os testes de integração.

## Desenvolvimento local (fora do Docker)

```bash
# infra apenas (Postgres + ZITADEL + bootstrap):
docker compose up postgres zitadel-postgres zitadel zitadel-bootstrap

# API com hot reload (requer SDK .NET 9 e a entrada 127.0.0.1 auth.localhost no /etc/hosts):
dotnet run --project src/Cards.Api

# migrations:
dotnet ef migrations add <Nome> -p src/Cards.Infrastructure
```
