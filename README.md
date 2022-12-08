<p align="center">
  <p align="center">
    <img src="https://avatars3.githubusercontent.com/u/53176002?s=200&v=4" height="100" alt="Blockcore" />
  </p>
  <h3 align="center">
    About Blockcore Indexer
  </h3>
  <p align="center">
    Basic and easy to use block indexer
  </p>
  <p align="center">
      <a href="https://github.com/block-core/blockcore-indexer/actions"><img src="https://github.com/block-core/blockcore-indexer/workflows/Pull%20Request/badge.svg" /></a>
      <a href="https://github.com/block-core/blockcore-indexer/actions"><img src="https://github.com/block-core/blockcore-indexer/workflows/Build%20and%20Release%20Binaries/badge.svg" /></a>
      <a href="https://github.com/block-core/blockcore-indexer/actions"><img src="https://github.com/block-core/blockcore-indexer/workflows/Build%20and%20Release%20Docker%20Image/badge.svg" /></a>
  </p>
</p>

# Blockcore Indexer

Blockcore Indexer scans the blockchain of Blockcore-derived chains and stores transaction/address information in a MongoDB database with REST API available for Block Explorers to use.

Blockcore Indexer API can be searched by segwit addresses and Cold-Staking (hot and cold key) script types.

## How to run

There are multiple ways you can run the indexer software, either from source code, from binaries or using docker.

When you run the indexer, you can either run it with a global configuration hosted by Blockcore, or with a custom local configuration.



### Docker (manual)

```sh
# Run interactively and read globla configuration for CITY
docker run -it blockcore/indexer:latest --CHAIN=CITY
```
When this is run, the following configuration is downloaded and applied:

[CITY.json](https://github.com/block-core/chaininfo/blob/master/chains/CITY.json)

The configuration is used by multiple software, not just Indexer, and the Indexer part is the "Indexer". For most global configuration, it's only this configuration:

```json
  "Indexer": {
    "StoreRawTransactions": true
  },
```

The default configurations are read from [appsetting.json](src/Blockcore.Indexer/appsettings.json), which has these fields:

```json
   "Indexer": {
      "ConnectionString": "mongodb://{Symbol}-mongo",
      "DatabaseNameSubfix": true,

      "RpcDomain": "{Symbol}-chain",
      "RpcSecure": false,
      "RpcUser": "rpcuser",
      "RpcPassword": "rpcpassword",

      // Notification parameters
      "NotifyUrl": "https://apiurl",
      "NotifyBatchCount": 0,

      // Syncing parameters
      "SyncBlockchain": true,
      "SyncMemoryPool": true,
      "MongoBatchSize": 10000000,
      "MongoBatchCount" : 10000,
      "ParallelRequestsToTransactionRpc": 50,
      "DetailedTrace": 0,
      "MaxItemsInQueue": 10,
      "SyncInterval": 5,
      "AverageInterval": 10,

      // Store the trx hex in mongo storage or read it from RPC
      "StoreRawTransactions": true,
      "NumberOfPullerTasksForIBD" : 5,
      "MaxItemsInBlockingCollection" : 1000
   }
```
The most important setting if you're running a manual setup, is the `ConnectionString` which by default attempts to connect to another docker container with the name `city-mongo` in this instance. The other important setting is `RpcDomain`, which is the fullnode that has the blockchain data. Ensure you have configured the correct `RpcUser` and `RpcPassword`.

To override these settings, you can provide them through environment variables or parameters like the following:

```sh
docker run -it blockcore/indexer:latest --chain=CITY -e "Indexer:ConnectionString=mongodb://127.0.0.1:27017" -e "Indexer:RpcDomain=127.0.0.1" -e "Indexer:RpcUser=rpcuser1" -e "Indexer:RpcPassword=rpcpassword1"
```

## Run from source

```sh
dotnet run --project src/Blockcore.Indexer/Blockcore.Indexer.csproj --chain=CITY --Indexer:ConnectionString=mongodb://127.0.0.1:27017 --Indexer:RpcDomain=127.0.0.1 --Indexer:RpcUser=rpcuser1 --Indexer:RpcPassword=rpcpassword1
```

You can also run the node from source like this, from the `blockcore` repository:

```sh
# It is important to set server=1 which is off by default, to enable RPC.
dotnet run --project src/Node/Blockcore.Node/Blockcore.Node.csproj --CHAIN=CITY -server=1 -rpcuser=rpcuser1 -rpcpassword=rpcpassword1
```

When starting it the node, you should see something like:

```
info: Blockcore.Features.RPC.RPCFeature[0]
      RPC listening on:
      http://[::1]:4334/
      http://127.0.0.1:4334/
```

## Usage examples

If you want to quickly learn how to use the indexer for a custom solution, such as an block explorer, please look at our [Blockcore Explorer](https://github.com/block-core/blockcore-explorer) source code on how to implement paging and other features.

## Compatibility

While we do our best to keep compatibility of APIs going forward, we will continue to change and improve our APIs that can result in breaking changes for consumers of the APIs.

We will attempt to avoid breaking changes within major releases.

The 0.0.X releases of Blockcore Indexer is not compatible with the 0.1.X.

All our technologies are available on docker, so can easily be upgraded and downgraded when there are compatibility issues.

### Technologies
- .NET Core
- Blockcore Platform
- Running a full Bitcoin/Blockcore node
- Running a MongoDB instance as indexing storage
- REST API that can be consumed by Block Explorer

#### DB schema
Can be found here:  
https://github.com/block-core/blockcore-indexer/blob/master/src/Blockcore.Indexer/doc/dbschema.md

#### API
OpenAPI http://[server-url]:[port]/docs/

## Development

To get started working on the indexer is super easy. All you need is a locally running node that has RPC enabled and a MongoDB instance.

The process of configuration flows like this:

1. Startup looks at what chain it should start, this is always in the form of ticker symbol, such as BTC or CITY.
2. Configuration is read from appsettings.json.
3. Configuration is downloaded from https://chains.blockcore.net/
4. Configuration is read from appsettings.Development.json.

Out of the box, the configuration when you run from Visual Studio will connect to a local running node and local MongoDB instance.

When you have the node running, pick it from the dropdown menu (green play button) in Visual Studio and run. The indexer should start indexing the blocks and store them in your local MongoDB instance.

1. Download [Blockcore Reference Node](https://github.com/block-core/blockcore-nodes/releases).
2. Download [MongoDB](https://www.mongodb.com/).
3. Install MongoDB
4. Run your reference node of choice, with these parameters: 

```
-server -rpcallowip=127.0.0.1 -rpcbind=127.0.0.1 -rpcpassword=rpcpassword -rpcuser=rpcuser
```

5. As soon as you have connections and your node is starting to download blocks, you can start the Blockcore Indexer.

*Happy debugging and coding!*

### Improved performance for initial sync and indexing

Running the node and indexer from Visual Studio will involve a reduction in performance on initial syncing of the blockchain and indexing the data. For optimal performance, 
use the dotnet from terminal:

```sh
# Run the node and let it sync to tip
dotnet run Blockcore.Node.csproj --chain=STRAX -txindex=1  -server -iprangefiltering=0 -rpcallowip=127.0.0.1 -rpcbind=127.0.0.1 -rpcpassword=rpcpassword -rpcuser=rpcuser -testnet

# Run the indexer
set ASPNETCORE_ENVIRONMENT=Development
dotnet run Blockcore.Indexer.csproj --chain=TSTRAX --no-launch-profile
```

### Local MongoDB database server on docker

There is a basic docker-compose.yml that can be used to start a MongoDB database server on docker. Navigate to the docker/database folder and start the container in background mode:

```
sudo docker-compose up -d
```

## Release Process

1. New changes to the codebase must come as pull requests. This will trigger the [pull-request.yml](.github/workflows/pull-request.yml) workflow.

2. When a pull request is merged to master, this will trigger [build.yml](.github/workflows/build.yml). Build will produce a draft release, or update existing.

3. After manual testing and verification of the draft release (which contains binaries created by build), a project responsible can release the draft release to the public, either as a release or pre-release.

4. The [release.yml](.github/workflows/release.yml) workflow picks up the release events, and builds the [docker image](src/Blockcore.Indexer/Dockerfile.Release) based on the newly released binary packages.

5. Newly built and released container can then be installed using either :latest tag (not adviseable) or the specific version (advised)

```sh
docker pull blockcore/indexer:latest
```

```sh
docker pull blockcore/indexer:0.0.6
```

## Troubleshooting


```
StatusCode = Unauthorized Error =  Blockcore.Indexer.Client.BitcoinClientException: StatusCode='Unauthorized' Error=Error (0)
   at Blockcore.Indexer.Client.BitcoinClient.HandleError(String body, HttpResponseMessage response, Nullable`1 statusCode) in /home/runner/work/blockcore-indexer/blockcore-indexer/src/Blockcore.Indexer/Client/BitcoinClient.cs:line 617
```

Issue: This error log is often related to caller IP not being part of the "rpcallowip" list.


```
Blockcore.Indexer.Client.BitcoinCommunicationException: Daemon Failed Url = 'http://city-chain:4334/'
 ---> System.AggregateException: One or more errors occurred. (Connection refused)
 ---> System.Net.Http.HttpRequestException: Connection refused
 ---> System.Net.Sockets.SocketException (111): Connection refused
```

Issue: This happens when the DNS name is not accessible.