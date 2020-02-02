FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /app
COPY . ./
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 9910
ENTRYPOINT ["dotnet", "Blockcore.Indexer.dll"]
