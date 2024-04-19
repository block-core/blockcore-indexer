FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

COPY src/Directory.Build.props Blockcore.Indexer.Angor/
COPY src/Blockcore.Indexer.Angor/ Blockcore.Indexer.Angor/
COPY src/Directory.Build.props Blockcore.Indexer.Core/
COPY src/Blockcore.Indexer.Core/ Blockcore.Indexer.Core/

RUN dotnet build Blockcore.Indexer.Angor/Blockcore.Indexer.Angor.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish Blockcore.Indexer.Angor/Blockcore.Indexer.Angor.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Blockcore.Indexer.Angor.dll"]
