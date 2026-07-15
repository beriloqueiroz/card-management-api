# Startup — preparação do ambiente (Fase 0)

Sequência de comandos para preparar o WSL2 para a stack .NET. Docker já está instalado (verificado: Docker 29.4 + Compose v5). Execute na ordem.

## 1. Instalar o .NET 9 SDK (script oficial da Microsoft)

```bash
# baixa o script oficial de instalação
curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh

# instala o SDK 9.0 em ~/.dotnet (não precisa de sudo, não mexe no apt)
/tmp/dotnet-install.sh --channel 9.0 --install-dir "$HOME/.dotnet"
```

## 2. Configurar PATH e DOTNET_ROOT no zsh

```bash
cat >> ~/.zshrc <<'EOF'

# .NET SDK
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
EOF

# aplica na sessão atual
source ~/.zshrc
```

## 3. Verificar a instalação

```bash
dotnet --version        # esperado: 9.0.x
dotnet --list-sdks
```

Na primeira execução o dotnet pode demorar alguns segundos (expansão de pacotes) — normal.

## 4. Instalar a ferramenta de migrations do EF Core

```bash
dotnet tool install --global dotnet-ef
dotnet ef --version     # confirma que o tool está no PATH (~/.dotnet/tools)
```

## 5. VS Code — extensão C#

Instale a extensão **C# Dev Kit** (`ms-dotnettools.csdevkit`) no VS Code para IntelliSense, debug e explorador de testes. Pela linha de comando, se preferir:

```bash
code --install-extension ms-dotnettools.csdevkit
```

## 6. Conferências finais

```bash
docker compose version   # já ok (v5.1.3)
docker run --rm hello-world   # opcional: sanidade do daemon
```

`psql` não é necessário localmente — quando precisar inspecionar o banco, use `docker exec -it <container-postgres> psql -U postgres`.

---

# Fase 1 — Scaffold da solução

Execute a partir de `/root/develop/redefrota/prova_dev`.

## 1. Solução e projetos

```bash
dotnet new sln -n Cards

dotnet new classlib -n Cards.Domain         -o src/Cards.Domain         -f net9.0
dotnet new classlib -n Cards.Application    -o src/Cards.Application    -f net9.0
dotnet new classlib -n Cards.Infrastructure -o src/Cards.Infrastructure -f net9.0
dotnet new webapi   -n Cards.Api            -o src/Cards.Api            -f net9.0 --use-controllers
dotnet new xunit    -n Cards.UnitTests        -o tests/Cards.UnitTests        -f net9.0
dotnet new xunit    -n Cards.IntegrationTests -o tests/Cards.IntegrationTests -f net9.0
```

## 2. Adicionar tudo à solução

```bash
dotnet sln add src/Cards.Domain src/Cards.Application src/Cards.Infrastructure src/Cards.Api
dotnet sln add tests/Cards.UnitTests tests/Cards.IntegrationTests
```

## 3. Referências entre projetos (direção da dependência)

```bash
# Application conhece o Domain
dotnet add src/Cards.Application reference src/Cards.Domain

# Infrastructure implementa as portas da Application (e usa o Domain)
dotnet add src/Cards.Infrastructure reference src/Cards.Application src/Cards.Domain

# Api referencia Application (casos de uso) e Infrastructure (só para DI/composition root)
dotnet add src/Cards.Api reference src/Cards.Application src/Cards.Infrastructure

# Testes
dotnet add tests/Cards.UnitTests reference src/Cards.Domain src/Cards.Application
dotnet add tests/Cards.IntegrationTests reference src/Cards.Api src/Cards.Infrastructure
```

## 4. Compilar para validar

```bash
dotnet build
```

## Boilerplate gerado (o que os templates criam)

Sim, os templates geram código de exemplo — nada disso é "mágica" obrigatória, e vamos substituir quase tudo:

- **`classlib`** → um `Class1.cs` vazio em cada projeto. **Pode apagar** os três.
- **`webapi --use-controllers`** → `Program.cs`, `appsettings*.json`, `Properties/launchSettings.json` e um exemplo `Controllers/WeatherForecastController.cs` + `WeatherForecast.cs`. **Apague os dois arquivos do WeatherForecast**; o resto a gente evolui.
- **`xunit`** → um `UnitTest1.cs` em cada projeto de teste. **Pode apagar.**

```bash
rm src/Cards.Domain/Class1.cs src/Cards.Application/Class1.cs src/Cards.Infrastructure/Class1.cs
rm src/Cards.Api/Controllers/WeatherForecastController.cs src/Cards.Api/WeatherForecast.cs
rm tests/Cards.UnitTests/UnitTest1.cs tests/Cards.IntegrationTests/UnitTest1.cs
```

Observação: o template webapi do .NET 9 **não traz mais o Swagger UI** (só `Microsoft.AspNetCore.OpenApi`, que expõe o JSON em `/openapi/v1.json`). Como a prova pede Swagger UI para execução manual, na Fase 3 eu adiciono o pacote `Swashbuckle.AspNetCore` e configuro a UI com exemplos — não precisa fazer nada disso agora.

---

# Fase 2+ — Dependências NuGet (todas as fases, rode uma vez só)

Equivalente ao `npm install <pkg>`: cada comando grava um `<PackageReference>` no `.csproj` do projeto. Versões pinadas na linha 9.0.x para casar com o net9.0.

## 1. Infrastructure — EF Core + PostgreSQL

```bash
# provider PostgreSQL do EF Core (traz o EF Core junto, como dependência)
dotnet add src/Cards.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.0.4

# suporte design-time: é o que permite ao `dotnet ef` gerar migrations neste projeto
dotnet add src/Cards.Infrastructure package Microsoft.EntityFrameworkCore.Design --version 9.0.7

# bind de seção do appsettings para classes de Options (ex.: chave de cifra do PIN)
dotnet add src/Cards.Infrastructure package Microsoft.Extensions.Options.ConfigurationExtensions --version 9.0.7
```

## 2. Api — Swagger UI + validação de JWT

```bash
# Swagger UI (o template do .NET 9 só expõe o JSON do OpenAPI; a prova pede UI)
dotnet add src/Cards.Api package Swashbuckle.AspNetCore --version 8.1.4

# middleware que valida os JWTs emitidos pelo ZITADEL (discovery/JWKS)
dotnet add src/Cards.Api package Microsoft.AspNetCore.Authentication.JwtBearer --version 9.0.7
```

## 3. IntegrationTests — Testcontainers + host de teste

```bash
# sobe um PostgreSQL real descartável em container para os testes de repository
dotnet add tests/Cards.IntegrationTests package Testcontainers.PostgreSql --version 4.1.0

# WebApplicationFactory: hospeda a Cards.Api em memória para testar os endpoints
dotnet add tests/Cards.IntegrationTests package Microsoft.AspNetCore.Mvc.Testing --version 9.0.7
```

(Os unit tests não precisam de nada além do template xunit — os fakes serão escritos à mão, sem lib de mock.)

## 4. Validar

```bash
dotnet build
```

---

Quando terminar (com `dotnet build` verde), me avise que sigo escrevendo o código da **Fase 2** (entidades, DbContext, migration, seeder).
