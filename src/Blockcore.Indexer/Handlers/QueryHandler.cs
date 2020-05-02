namespace Blockcore.Indexer.Api.Handlers
{
   using System.Collections.Generic;
   using System.Linq;
   using Blockcore.Indexer.Api.Handlers.Types;
   using Blockcore.Indexer.Settings;
   using Blockcore.Indexer.Extensions;
   using Blockcore.Indexer.Storage;
   using Microsoft.Extensions.Options;
   using NBitcoin;
   using Blockcore.Indexer.Crypto;
   using Blockcore.Indexer.Operations.Types;
   using Blockcore.Indexer.Storage.Types;

   /// <summary>
   /// A handler that make request on the blockchain.
   /// </summary>
   public class QueryHandler
   {
      private readonly IndexerSettings configuration;

      private readonly ChainSettings chainConfiguration;

      private readonly IStorage storage;

      private readonly SyncConnection connection;

      /// <summary>
      /// Initializes a new instance of the <see cref="QueryHandler"/> class.
      /// </summary>
      public QueryHandler(
         IOptions<IndexerSettings> configuration,
         IOptions<ChainSettings> chainConfiguration,
         SyncConnection connection,
         IStorage storage)
      {
         this.storage = storage;
         this.connection = connection;
         this.configuration = configuration.Value;
         this.chainConfiguration = chainConfiguration.Value;
      }

      public QueryAddress GetAddressTransactions(string address, long confirmations)
      {
         Storage.Types.SyncTransactionAddressBalance stats = storage.AddressGetBalance(address, confirmations);

         if (stats == null)
         {
            return new QueryAddress();
         }

         // filter
         var transactions = stats.Items.ToList();
         var confirmed = transactions.Where(t => t.Confirmations >= confirmations).ToList();
         var unconfirmed = transactions.Where(t => t.Confirmations < confirmations).ToList();

         return new QueryAddress
         {
            Symbol = chainConfiguration.Symbol,
            Address = address,
            Balance = stats.Available,
            TotalReceived = stats.Received,
            TotalSent = stats.Sent,
            UnconfirmedBalance = stats.Unconfirmed,
            Transactions = confirmed.Select(t => new QueryAddressItem
            {
               PubScriptHex = t.ScriptHex,
               CoinBase = t.CoinBase,
               CoinStake = t.CoinStake,
               Index = t.Index,
               SpendingTransactionHash = t.SpendingTransactionHash,
               SpendingBlockIndex = t.SpendingBlockIndex,
               TransactionHash = t.TransactionHash,
               Type = t.Type,
               Value = t.Value,
               BlockIndex = t.BlockIndex,
               Confirmations = t.Confirmations,
               Time = t.Time
            }),
            UnconfirmedTransactions = unconfirmed.Select(t => new QueryAddressItem
            {
               PubScriptHex = t.ScriptHex,
               CoinBase = t.CoinBase,
               CoinStake = t.CoinStake,
               Index = t.Index,
               SpendingTransactionHash = t.SpendingTransactionHash,
               SpendingBlockIndex = t.SpendingBlockIndex,
               TransactionHash = t.TransactionHash,
               Type = t.Type,
               Value = t.Value,
               BlockIndex = t.BlockIndex,
               Confirmations = t.Confirmations,
               Time = t.Time
            })
         };
      }

      public QueryAddress GetAddress(string address, long confirmations)
      {
         Storage.Types.SyncTransactionAddressBalance stats = storage.AddressGetBalance(address, confirmations);

         if (stats == null)
         {
            return new QueryAddress();
         }

         return new QueryAddress
         {
            Symbol = chainConfiguration.Symbol,
            Address = address,
            Balance = stats.Available,
            TotalReceived = stats.Received,
            TotalSent = stats.Sent,
            UnconfirmedBalance = stats.Unconfirmed,
            Transactions = Enumerable.Empty<QueryAddressItem>(),
            UnconfirmedTransactions = Enumerable.Empty<QueryAddressItem>()
         };
      }

      public QueryAddress GetAddressUtxo(string address, long confirmations)
      {
         Storage.Types.SyncTransactionAddressBalance stats = storage.AddressGetBalanceUtxo(address, confirmations);

         if (stats == null)
         {
            return new QueryAddress();
         }

         return new QueryAddress
         {
            Symbol = chainConfiguration.Symbol,
            Address = address,
            Balance = stats.Available,
            TotalReceived = stats.Received,
            TotalSent = stats.Sent,
            UnconfirmedBalance = stats.Unconfirmed,
            Transactions = Enumerable.Empty<QueryAddressItem>(),
            UnconfirmedTransactions = Enumerable.Empty<QueryAddressItem>()
         };
      }

      public QueryAddress GetAddressUtxoTransactions(string address, long confirmations)
      {
         Storage.Types.SyncTransactionAddressBalance stats = storage.AddressGetBalanceUtxo(address, confirmations);

         if (stats == null)
         {
            return new QueryAddress();
         }

         // filter
         var transactions = stats.Items.ToList();
         var confirmed = transactions.Where(t => t.Confirmations >= confirmations).ToList();
         var unconfirmed = transactions.Where(t => t.Confirmations < confirmations).ToList();

         return new QueryAddress
         {
            Symbol = chainConfiguration.Symbol,
            Address = address,
            Balance = stats.Available,
            TotalReceived = stats.Received,
            TotalSent = stats.Sent,
            UnconfirmedBalance = stats.Unconfirmed,
            Transactions = confirmed.Select(t => new QueryAddressItem
            {
               PubScriptHex = t.ScriptHex,
               CoinBase = t.CoinBase,
               Index = t.Index,
               SpendingTransactionHash = t.SpendingTransactionHash,
               SpendingBlockIndex = t.SpendingBlockIndex,
               TransactionHash = t.TransactionHash,
               Type = t.Type,
               Value = t.Value,
               BlockIndex = t.BlockIndex,
               Confirmations = t.Confirmations,
               Time = t.Time
            }),
            UnconfirmedTransactions = unconfirmed.Select(t => new QueryAddressItem
            {
               PubScriptHex = t.ScriptHex,
               CoinBase = t.CoinBase,
               CoinStake = t.CoinStake,
               Index = t.Index,
               SpendingTransactionHash = t.SpendingTransactionHash,
               SpendingBlockIndex = t.SpendingBlockIndex,
               TransactionHash = t.TransactionHash,
               Type = t.Type,
               Value = t.Value,
               BlockIndex = t.BlockIndex,
               Confirmations = t.Confirmations,
               Time = t.Time
            })
         };
      }

      public QueryAddress GetAddressUtxoUnconfirmedTransactions(string address, long confirmations)
      {
         Storage.Types.SyncTransactionAddressBalance stats = storage.AddressGetBalanceUtxo(address, confirmations);

         if (stats == null)
         {
            return new QueryAddress();
         }

         // filter
         var transactions = stats.Items
             .Where(s => s.Confirmations < confirmations)
             .ToList();

         return new QueryAddress
         {
            Symbol = chainConfiguration.Symbol,
            Address = address,
            Balance = stats.Available,
            TotalReceived = stats.Received,
            TotalSent = stats.Sent,
            UnconfirmedBalance = stats.Unconfirmed,
            Transactions = Enumerable.Empty<QueryAddressItem>(),
            UnconfirmedTransactions = transactions.Select(t => new QueryAddressItem
            {
               PubScriptHex = t.ScriptHex,
               CoinBase = t.CoinBase,
               CoinStake = t.CoinStake,
               Index = t.Index,
               SpendingTransactionHash = t.SpendingTransactionHash,
               SpendingBlockIndex = t.SpendingBlockIndex,
               TransactionHash = t.TransactionHash,
               Type = t.Type,
               Value = t.Value,
               BlockIndex = t.BlockIndex,
               Confirmations = t.Confirmations,
               Time = t.Time
            })
         };
      }

      public QueryAddress GetAddressUtxoConfirmedTransactions(string address, long confirmations)
      {
         Storage.Types.SyncTransactionAddressBalance stats = storage.AddressGetBalanceUtxo(address, confirmations);

         if (stats == null)
         {
            return new QueryAddress();
         }

         // filter
         var transactions = stats.Items
             .Where(s => s.Confirmations >= confirmations)
             .ToList();

         return new QueryAddress
         {
            Symbol = chainConfiguration.Symbol,
            Address = address,
            Balance = stats.Available,
            TotalReceived = stats.Received,
            TotalSent = stats.Sent,
            UnconfirmedBalance = stats.Unconfirmed,
            Transactions = transactions.Select(t => new QueryAddressItem
            {
               PubScriptHex = t.ScriptHex,
               CoinBase = t.CoinBase,
               CoinStake = t.CoinStake,
               Index = t.Index,
               SpendingTransactionHash = t.SpendingTransactionHash,
               SpendingBlockIndex = t.SpendingBlockIndex,
               TransactionHash = t.TransactionHash,
               Type = t.Type,
               Value = t.Value,
               BlockIndex = t.BlockIndex,
               Confirmations = t.Confirmations,
               Time = t.Time
            }),
            UnconfirmedTransactions = Enumerable.Empty<QueryAddressItem>()
         };
      }

      public QueryBlock GetBlock(string blockHash, bool getTransactions = true)
      {
         Storage.Types.SyncBlockInfo block = storage.BlockGetByHash(blockHash);

         if (block == null)
         {
            return new QueryBlock();
         }

         QueryBlock queryBlock = Map(block);

         if (getTransactions)
         {
            IEnumerable<Storage.Types.SyncTransactionInfo> transactions = storage.BlockTransactionGetByBlockIndex(block.BlockIndex);
            queryBlock.Transactions = Map(transactions);
         }

         return queryBlock;
      }

      public QueryBlocks BlockGetByLimitOffset(int offset, int limit)
      {
         (IEnumerable<SyncBlockInfo> Items, int Total) result = storage.BlockGetByLimitOffset(offset, limit);
         IEnumerable<QueryBlock> blocks = result.Items.Select(b => Map(b));

         QueryBlocks query = new QueryBlocks
         {
            Blocks = blocks,
            Total = result.Total
         };

         return query;
      }

      public QueryBlock GetBlock(long blockIndex, bool getTransactions = true)
      {
         Storage.Types.SyncBlockInfo block = storage.BlockGetByIndex(blockIndex);

         if (block == null)
         {
            return new QueryBlock();
         }

         QueryBlock queryBlock = Map(block);

         if (getTransactions)
         {
            IEnumerable<SyncTransactionInfo> transactions = storage.BlockTransactionGetByBlockIndex(block.BlockIndex);
            queryBlock.Transactions = Map(transactions);
         }

         return queryBlock;

      }

      public QueryBlock GetLastBlock(bool getTransactions = true)
      {
         Storage.Types.SyncBlockInfo block = storage.BlockGetCompleteBlockCount(1).FirstOrDefault();

         if (block == null)
         {
            return new QueryBlock();
         }

         QueryBlock queryBlock = Map(block);

         if (getTransactions)
         {
            IEnumerable<SyncTransactionInfo> transactions = storage.BlockTransactionGetByBlockIndex(block.BlockIndex);
            queryBlock.Transactions = Map(transactions);
         }

         return queryBlock;
      }

      public QueryTransaction GetTransaction(string transactionId)
      {
         Storage.Types.SyncTransactionInfo transaction = storage.BlockTransactionGet(transactionId);
         Storage.Types.SyncTransactionItems transactionItems = storage.TransactionItemsGet(transactionId);

         if (transactionItems == null)
         {
            return new QueryTransaction();
         }

         var result = new QueryTransaction
         {
            Symbol = chainConfiguration.Symbol,
            BlockHash = transaction?.BlockHash ?? null,
            BlockIndex = transaction?.BlockIndex ?? null,
            Confirmations = transaction?.Confirmations ?? 0,
            Timestamp = transaction?.Timestamp ?? 0,
            TransactionId = transaction?.TransactionHash ?? transactionId,

            RBF = transactionItems.RBF,
            LockTime = transactionItems.LockTime.ToString(),
            Version = transactionItems.Version,
            IsCoinbase = transactionItems.IsCoinbase,
            IsCoinstake = transactionItems.IsCoinstake,

            Inputs = transactionItems.Inputs.Select(i => new QueryTransactionInput
            {
               CoinBase = i.InputCoinBase,
               InputAddress = i.InputAddress,
               InputIndex = i.PreviousIndex,
               InputTransactionId = i.PreviousTransactionHash,
               ScriptSig = i.ScriptSig,
               ScriptSigAsm = new Script(NBitcoin.DataEncoders.Encoders.Hex.DecodeData(i.ScriptSig)).ToString(),
               WitScript = i.WitScript,
               SequenceLock = i.SequenceLock
            }),
            Outputs = transactionItems.Outputs.Select(o => new QueryTransactionOutput
            {
               Address = o.Address,
               Balance = o.Value,
               Index = o.Index,
               OutputType = o.OutputType,
               ScriptPubKey = o.ScriptPubKey,
               SpentInTransaction = o.SpentInTransaction,
               ScriptPubKeyAsm = new Script(NBitcoin.DataEncoders.Encoders.Hex.DecodeData(o.ScriptPubKey)).ToString()
            }),
         };

         return result;
      }

      public QueryMempoolTransactions GetMempoolTransactions(int count)
      {
         IEnumerable<Transaction> transactions = storage.GetMemoryTransactions();

         return new QueryMempoolTransactions
         {
            CoinTag = chainConfiguration.Symbol,
            Transactions = transactions.Select(t => new QueryMempoolTransaction { TransactionId = t.GetHash().ToString() }).Take(count).ToList()
         };
      }

      private IEnumerable<QueryTransaction> Map(IEnumerable<Storage.Types.SyncTransactionInfo> transactions)
      {
         IEnumerable<QueryTransaction> list = transactions.Select(t => Map(t));
         return list;
      }

      private QueryTransaction Map(SyncTransactionInfo transaction)
      {
         return new QueryTransaction
         {
            BlockHash = transaction.BlockHash,
            BlockIndex = transaction.BlockIndex,
            Confirmations = transaction.Confirmations,
            TransactionId = transaction.TransactionHash,
            Timestamp = transaction.Timestamp
         };
      }

      private QueryBlock Map(SyncBlockInfo block)
      {
         return new QueryBlock
         {
            Symbol = chainConfiguration.Symbol,
            BlockHash = block.BlockHash,
            BlockIndex = block.BlockIndex,
            BlockSize = block.BlockSize,
            BlockTime = block.BlockTime,
            NextBlockHash = block.NextBlockHash,
            PreviousBlockHash = block.PreviousBlockHash,
            Synced = block.SyncComplete,
            TransactionCount = block.TransactionCount,
            Bits = block.Bits,
            Confirmations = block.Confirmations,
            Merkleroot = block.Merkleroot,
            Nonce = block.Nonce,
            PosBlockSignature = block.PosBlockSignature,
            PosBlockTrust = block.PosBlockTrust,
            PosChainTrust = block.PosChainTrust,
            PosFlags = block.PosFlags,
            PosHashProof = block.PosHashProof,
            PosModifierv2 = block.PosModifierv2,
            Version = block.Version,
            Transactions = new List<QueryTransaction>()
         };
      }
   }
}
