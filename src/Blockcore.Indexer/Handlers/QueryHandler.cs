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

   /// <summary>
   /// A handler that make request on the blockchain.
   /// </summary>
   public class QueryHandler
   {
      private readonly IndexerSettings configuration;

      private readonly ChainSettings chainConfiguration;

      private readonly IStorage storage;

      /// <summary>
      /// Initializes a new instance of the <see cref="QueryHandler"/> class.
      /// </summary>
      public QueryHandler(IOptions<IndexerSettings> configuration, IOptions<ChainSettings> chainConfiguration, IStorage storage)
      {
         this.storage = storage;
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

         var queryBlock = new QueryBlock
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
            ChainWork = block.ChainWork,
            Difficulty = block.Difficulty,
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
            Transactions = Enumerable.Empty<string>()
         };

         if (getTransactions)
         {
            IEnumerable<Storage.Types.SyncTransactionInfo> transactions = storage.BlockTransactionGetByBlockIndex(block.BlockIndex);
            queryBlock.Transactions = transactions.Select(s => s.TransactionHash);
         }

         return queryBlock;
      }

      public QueryBlocks GetBlocks(long blockIndex, int count)
      {
         var blocks = new List<QueryBlock>();

         if (blockIndex == -1)
         {
            QueryBlock lastBlock = GetLastBlock(false);
            blocks.Add(lastBlock);
            blockIndex = lastBlock.BlockIndex - 1;
            count--;
         }

         for (long i = 0; i < count; i++)
         {
            blocks.Add(GetBlock((int)blockIndex - i, false));
         }

         return new QueryBlocks
         {
            Blocks = blocks
         };
      }

      public QueryBlock GetBlock(long blockIndex, bool getTransactions = true)
      {
         Storage.Types.SyncBlockInfo block = storage.BlockGetByIndex(blockIndex);

         if (block == null)
         {
            return new QueryBlock();
         }

         var queryBlock = new QueryBlock
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
            Transactions = Enumerable.Empty<string>()
         };

         if (getTransactions)
         {
            IEnumerable<Storage.Types.SyncTransactionInfo> transactions = storage.BlockTransactionGetByBlockIndex(block.BlockIndex);
            queryBlock.Transactions = transactions.Select(s => s.TransactionHash);
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

         var queryBlock = new QueryBlock
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
            Transactions = Enumerable.Empty<string>()
         };

         if (getTransactions)
         {
            IEnumerable<Storage.Types.SyncTransactionInfo> transactions = storage.BlockTransactionGetByBlockIndex(block.BlockIndex);
            queryBlock.Transactions = transactions.Select(s => s.TransactionHash);
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

         return new QueryTransaction
         {
            Symbol = chainConfiguration.Symbol,
            BlockHash = transaction?.BlockHash ?? null,
            BlockIndex = transaction?.BlockIndex ?? null,
            Confirmations = transaction?.Confirmations ?? 0,
            Timestamp = transaction?.Timestamp.UnixTimeStampToDateTime() ?? null,
            TransactionId = transaction?.TransactionHash ?? transactionId,

            RBF = transactionItems.RBF,
            LockTime = transactionItems.LockTime.ToString(),
            Version = transactionItems.Version,
            IsCoinbase = transactionItems.IsCoinbase,
            IsCoinstake = transactionItems.IsCoinstake,
            Inputs = transactionItems.Inputs.Select(i => new QueryTransactionInput
            {
               CoinBase = i.InputCoinBase,
               InputAddress = string.Empty,
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
   }
}
