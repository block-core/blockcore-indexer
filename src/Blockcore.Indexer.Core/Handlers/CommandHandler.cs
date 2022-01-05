using System.Threading.Tasks;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Storage;

namespace Blockcore.Indexer.Core.Handlers
{
   /// <summary>
   /// Handler to make get info about a blockchain.
   /// </summary>
   public class CommandHandler
   {
      private readonly SyncConnection syncConnection;

      private readonly IStorage storage;
      readonly ICryptoClientFactory clientFactory;

      /// <summary>
      /// Initializes a new instance of the <see cref="StatsHandler"/> class.
      /// </summary>
      public CommandHandler(SyncConnection connection, IStorage storage, ICryptoClientFactory clientFactory)
      {
         this.storage = storage;
         this.clientFactory = clientFactory;
         syncConnection = connection;
      }

      public async Task<string> SendTransaction(string transactionHex)
      {
         // todo: consider adding support for retries.
         // todo: check how a failure is porpageted

         SyncConnection connection = syncConnection;
         var client = clientFactory.Create(connection.ServerDomain, connection.RpcAccessPort, connection.User, connection.Password, connection.Secure);
         string trxid = await client.SentRawTransactionAsync(transactionHex);
         return trxid;
      }
   }
}
