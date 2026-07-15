# Multi-stage build: SDK compiles, runtime image ships only the published output.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

COPY Cards.sln ./
COPY src/Cards.Domain/Cards.Domain.csproj src/Cards.Domain/
COPY src/Cards.Application/Cards.Application.csproj src/Cards.Application/
COPY src/Cards.Infrastructure/Cards.Infrastructure.csproj src/Cards.Infrastructure/
COPY src/Cards.Api/Cards.Api.csproj src/Cards.Api/
RUN dotnet restore src/Cards.Api/Cards.Api.csproj

COPY src/ src/
RUN dotnet publish src/Cards.Api/Cards.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8000
EXPOSE 8000
ENTRYPOINT ["dotnet", "Cards.Api.dll"]
