# Guia de Migrations

Como as migrations funcionam neste projeto e como evoluí-las com segurança.

## O que há nesta pasta

| Arquivo | O que é | Editar? |
|---|---|---|
| `20260715..._InitialCreate.cs` | A migration: operações de schema (`CreateTable`, `CreateIndex`...) geradas como diff do modelo | Raramente — só para adicionar SQL manual (`migrationBuilder.Sql(...)`), e nunca depois de aplicada em ambiente compartilhado |
| `20260715..._InitialCreate.Designer.cs` | Foto do modelo *no momento* daquela migration (metadado do EF) | Nunca |
| `CardsDbContextModelSnapshot.cs` | Snapshot do modelo **atual** acumulado — é contra ele que o próximo `migrations add` calcula o diff | Nunca manualmente; regenerado a cada add/remove |

## O pipeline (de onde o schema vem)

```
Entidades (Cards.Domain)                    CreditCard, User
        +                                        │
Configurações (Persistence/Configurations)  colunas, índices, constraints, query filter
        ↓
Modelo EF em memória
        ↓  dotnet ef migrations add <Nome>   (diff contra o ModelSnapshot)
Migration (esta pasta)
        ↓  Database.MigrateAsync() no startup da API (Program.cs)
Schema no PostgreSQL  (+ tabela __EFMigrationsHistory registrando o que já foi aplicado)
```

Consequência prática: **nunca altere o schema direto no banco ou editando a migration antiga** — altere a entidade/configuração e gere uma migration nova. O `MigrateAsync()` do startup aplica apenas o que falta (consultando `__EFMigrationsHistory`), então subir a API em um banco atualizado é no-op.

## Comandos (da raiz do repo)

```bash
# gerar uma migration a partir das mudanças no modelo
dotnet ef migrations add NomeDaMudanca -p src/Cards.Infrastructure

# desfazer a última migration AINDA NÃO aplicada/commitada
dotnet ef migrations remove -p src/Cards.Infrastructure

# aplicar manualmente em um banco (a API já faz isso no startup)
CARDS_CONNECTION_STRING="Host=localhost;Port=5432;Database=cards;Username=cards;Password=cards" \
  dotnet ef database update -p src/Cards.Infrastructure

# gerar o SQL completo e idempotente (artefato para DBA/CI, se necessário)
dotnet ef migrations script --idempotent -p src/Cards.Infrastructure
```

Notas de ambiente:

- Se o `dotnet ef` reclamar de `libhostfxr.so`, exporte `DOTNET_ROOT=$HOME/.dotnet` (o tool global precisa achar o runtime).
- O `dotnet ef` usa a `CardsDbContextFactory` (design-time): `migrations add`/`remove`/`script` **não abrem conexão** com banco nenhum; só o `database update` conecta — e lê `CARDS_CONNECTION_STRING` do ambiente.

## Decisões deste projeto

- **Schema da prova preservado**: a `InitialCreate` reproduz o `prova_cartoes_seed_postgresql.sql` fornecido (mesmos nomes, checks e índices) e adiciona `pin_encrypted`, `deleted_at` e `external_id` — justificativas no README da raiz.
- **Dados de seed ficam fora das migrations**: a massa inicial vive no `DatabaseSeeder` (C#), não em SQL, porque os PINs são cifrados em runtime com a chave do ambiente — os bytes de `pin_encrypted` não podem ser congelados num script. O seeder é idempotente (só roda com o banco vazio).
- **Checklist ao evoluir o modelo**: alterar entidade/configuração → `migrations add` → **ler o código gerado** (o diff do EF é bom, não é infalível — confira nomes de constraint e ordem de operações destrutivas) → rodar os testes de integração (eles aplicam as migrations num Postgres real via Testcontainers) → commitar migration + Designer + Snapshot juntos.
