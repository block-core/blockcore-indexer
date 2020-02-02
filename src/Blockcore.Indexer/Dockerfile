FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim
WORKDIR /usr/local/app/

RUN apt-get update \
    && apt-get install -y curl \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

RUN curl -Ls https://github.com/sondreb/blockcore-indexer/releases/download/v0.0.1/Blockcore.Indexer-${{version}}-linux-x64.tar.gz \
    | tar -xvz -C .

EXPOSE 9910
ENTRYPOINT ["dotnet", "Blockcore.Indexer.dll"]
