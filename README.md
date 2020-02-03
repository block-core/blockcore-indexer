# Blockcore Indexer

[1]: https://github.com/block-core/blockcore-indexer/actions
[2]: https://github.com/block-core/blockcore-indexer/workflows/Build%20and%20Release%20Binaries/badge.svg
[3]: https://github.com/block-core/blockcore-indexer/workflows/Build%20and%20Release%20Docker%20Image/badge.svg

[![Build Status][2]][1] [![Release Status][2]][1]

Blockcore Indexer scans the blockchain of Blockcore-derived chains and stores transaction/address information in a MongoDB database with REST API available for Block Explorers to use.

Blockcore Indexer API can be searched by segwit addresses and Cold-Staking (hot and cold key) script types.

### Technologies
- .NET Core
- NBitcoin and Stratis.Bitcoin
- Running a full Bitcoin/Altcoin node either daemon or qt 
- Running a MongoDB instance as indexing storage
- Kestrel Web Server with OpenAPI documentation

We user [docker](https://www.docker.com/) (with docker-compose)

#### DB schema
Can be found here:  
https://github.com/block-core/blockcore-indexer/blob/master/src/Blockcore.Indexer/doc/dbschema.md

#### Api
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


## Docker

```sh
docker build -t blockcoreindexer .
```

```sh
docker run -p 9901:9901 --name cityindexer blockcoreindexer:latest
```

```sh
// Run an individual chain from the docker sub-folders. Timeout should be high to avoid blockchain database storage issues.
docker-compose up --timeout 600
```

```sh
// Cleanup the majority of resources (doesn't delete volumes)
docker system prune -a
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
docker pull blockcore/indexer:0.0.3
```

