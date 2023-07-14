FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Blockcore.Indexer.Angor/Blockcore.Indexer.Angor.csproj", "Blockcore.Indexer.Angor/"]
RUN dotnet restore "Blockcore.Indexer.Angor/Blockcore.Indexer.Angor.csproj"
COPY . .
WORKDIR "/src/Blockcore.Indexer.Angor"
RUN dotnet build "Blockcore.Indexer.Angor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Blockcore.Indexer.Angor.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Blockcore.Indexer.Angor.dll"]
