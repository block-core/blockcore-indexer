# Blockcore Indexer

[1]: https://github.com/block-core/blockcore-indexer/actions
[2]: https://github.com/block-core/blockcore-indexer/workflows/Pull%20Request/badge.svg
[3]: https://github.com/block-core/blockcore-indexer/workflows/Build%20and%20Release%20Binaries/badge.svg
[4]: https://github.com/block-core/blockcore-indexer/workflows/Build%20and%20Release%20Docker%20Image/badge.svg

 [![Pull Request][2]][1]
 [![Build Status][3]][1] [![Release Status][4]][1]

Blockcore Indexer scans the blockchain of Blockcore-derived chains and stores transaction/address information in a MongoDB database with REST API available for Block Explorers to use.

Blockcore Indexer API can be searched by segwit addresses and Cold-Staking (hot and cold key) script types.

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
Swagger http://[server-url]:[port]/swagger/

##### examples
GET /api/query/address/{address}  
GET /api/query/address/{address}/confirmations/{confirmations}/unspent/transactions  
GET /api/query/address/{address}/unspent/transactions  
GET /api/query/address/{address}/unspent  
GET /api/query/block/Latest/{transactions}  
GET /api/query/block/{blockHash}/{transactions}  
GET /api/query/block/Index/{blockIndex}/{transactions}  
GET /api/query/transaction/{transactionId}  
GET /api/stats  
GET /api/stats/peers  

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

