namespace Blockcore.Indexer.Api.Handlers
{
   using System.Threading.Tasks;
   using Blockcore.Indexer.Client;
   using Blockcore.Indexer.Operations.Types;
   using Blockcore.Indexer.Storage;

   /// <summary>
   /// Handler to make get info about a blockchain.
   /// </summary>
   public class CommandHandler
   {
      private readonly SyncConnection syncConnection;

      private readonly IStorage storage;

      /// <summary>
      /// Initializes a new instance of the <see cref="StatsHandler"/> class.
      /// </summary>
      public CommandHandler(SyncConnection connection, IStorage storage)
      {
         this.storage = storage;
         syncConnection = connection;
      }

      public async Task<string> SendTransaction(string transactionHex)
      {
         // todo: consider adding support for retries.
         // todo: check how a failure is porpageted

         SyncConnection connection = syncConnection;
         BitcoinClient client = CryptoClientFactory.Create(connection.ServerDomain, connection.RpcAccessPort, connection.User, connection.Password, connection.Secure);
         string trxid = await client.SentRawTransactionAsync(transactionHex);
         return trxid;
      }
   }
}
