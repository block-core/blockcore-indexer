# Blockcore Indexer

![Build](https://github.com/block-core/blockcore-indexer/workflows/Build/badge.svg)

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
