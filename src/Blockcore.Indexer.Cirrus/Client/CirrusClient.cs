using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Storage.Types;
using Blockcore.Indexer.Core.Client;
using Blockcore.Indexer.Core.Client.Types;
using Blockcore.Indexer.Core.Operations.Types;

namespace Blockcore.Indexer.Cirrus.Client
{
   public class CirrusClient : BitcoinClient
   {
      readonly SyncConnection connection;

      public Uri ApiUrl { get; set; }

      public CirrusClient(SyncConnection connection)
      : base(string.Format("{0}://{1}:{2}", connection.Secure ? "https" : "http", connection.ServerDomain, connection.RpcAccessPort), new NetworkCredential(connection.User, connection.Password))
      {
         this.connection = connection;

         string url = string.Format("{0}://{1}:{2}", connection.Secure ? "https" : "http", connection.ServerDomain, connection.ApiAccessPort);
         ApiUrl = new Uri(url);

      }
      public override async Task<BlockInfo> GetBlockAsync(string hash)
      {
         return await CallAsync<CirrusBlockInfo>("getblock", hash);
      }

      public async Task<ReceiptResponse> GetReceiptAsync(string hash)
      {
         return await CallAsync<ReceiptResponse>("getreceipt", hash);
      }

      public async Task<GetCodeResponse> GetContractCodeAsync(string hash)
      {
         HttpResponseMessage httpResponse = await Client.GetAsync($"{ApiUrl}api/SmartContracts/code?address={hash}");
         return httpResponse.IsSuccessStatusCode ? await httpResponse.Content.ReadAsAsync<GetCodeResponse>() : null;
      }

      public async Task<ContractReceiptResponse> GetContractInfoAsync(string trxHash)
      {
         HttpResponseMessage httpResponse = await Client.GetAsync($"{ApiUrl}api/indexer/contract/info?txHash={trxHash}");

         if(!httpResponse.IsSuccessStatusCode)
         {
            throw new ApplicationException(httpResponse.Content.ReadAsStringAsync().Result);
         }

         return await httpResponse.Content.ReadAsAsync<ContractReceiptResponse>();
      }

   }
}
