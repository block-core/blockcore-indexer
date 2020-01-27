FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
#EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["src/Blockcore.Indexer/Blockcore.Indexer.csproj", "src/Blockcore.Indexer/"]
RUN dotnet restore "src/Blockcore.Indexer/Blockcore.Indexer.csproj"
COPY . .
WORKDIR "/src/src/Blockcore.Indexer"
RUN dotnet build "Blockcore.Indexer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Blockcore.Indexer.csproj" -c Release -o /app/publish

FROM base AS final

LABEL Author="Blockcore"
LABEL Maintainer="Blockcore"

WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 9050
ENTRYPOINT ["dotnet", "Blockcore.Indexer.dll"]