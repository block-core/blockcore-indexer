using System.Collections.Generic;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Client.Types;

namespace Blockcore.Indexer.Core.Client
{
   public interface IBlockchainClient
   {
      Task<string> SentRawTransactionAsync(string transactionHex);
      Task<int> GetConnectionCountAsync();
      Task<string> GetblockHashAsync(long blockBlockIndex);
      Task<BlockInfo> GetBlockAsync(string storeTipBlockHash);
      IEnumerable<string> GetRawMemPool();
      string GetBlockHex(string blockHash);
      DecodedRawTransaction GetRawTransaction(string itemItem, int verbose);
      Task<BlockchainInfoModel> GetBlockchainInfo();
      Task<NetworkInfoModel> GetNetworkInfo();
      Task<IEnumerable<PeerInfo>> GetPeerInfo();
      Task<DecodedRawTransaction> GetRawTransactionAsync(string transactionId, int verbose);
      int GetBlockCount();
   }
}
