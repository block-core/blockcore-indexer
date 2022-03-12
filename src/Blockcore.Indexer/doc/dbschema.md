### Blockcore Indexer uses mongodb to store the blockchain data

The tables can be found here  
https://github.com/block-core/blockcore-indexer/blob/master/src/Blockcore.Indexer/Storage/Mongo/Types

And the indexer builder here  
https://github.com/block-core/blockcore-indexer/blob/master/src/Blockcore.Indexer/Storage/Mongo/MongoBuilder.cs

**List of tables the indexer uses:**

`Block` - Stores information about a block and creates an index based on the block hash and block height.  

`Transaction` - Stores the serialized raw transaction and index by the transaction hash (it's optional and transactions will be stored in this table if the config flag `StoreRawTransactions` is true otherwise trx will be pulled using RPC.  

`TransactionBlock` - Stores a mapping between a block height and a transaction.  

`Output` - Stores information about outputs (this includes the script in hex format, block index, amount, if its a coinbase or coinstake, and the address), indexed on block index, outpoint and address.

`UnspentOutput` - Stores information about unspent outputs (this includes address, block index, value and outpoint).

`Input` - Stores information about inputs (this includes the trx hash the input appeared in and the output, amount and address its spending from) indexed on block index, outpoint and address. on the initial sync the address and amount fields are empty, they get populated when sync is complete and indexes are built by scanning the entire blockchain and copying info form the output.

`AddressComputed` - A computed table that gets populated on demand when an address balance is queried, the balance is calculated and stored to this table. entries in this table are deleted if an address that was updated was part of a reorg.

`AddressHistoryComputed` -  A computed table that gets populated on demand when an address balance and history is queried, intputs and outputs are aggregated to transactions and stored as history in this table. entries in this table are deleted if an address that was updated was part of a reorg. this table uses a position field that is an incremental number unique to an address and is used for paging on address history.

`Mempool` - Stores information about transactions found in the node mempool, when a transaction is included in a block its deleted from this table.

`RichList` - Stores the last 250 most rich addresses found on the blockchain. this is computed as a background job that run periodically.

`ReorgBlock` - Stores information about blocks that have been reorged, blocks that are not part of the main chain.
