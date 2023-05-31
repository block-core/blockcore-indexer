using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Blockcore.Consensus.TransactionInfo;
using Blockcore.Indexer.Core.Client.Types;
using Newtonsoft.Json;

namespace Blockcore.Indexer.Core.Client
{
   public class BitcoinClient : IBlockchainClient, IDisposable
   {
      ///// <summary>
      ///// Initializes static members of the <see cref="BitcoinClient"/> class.
      ///// </summary>
      //static BitcoinClient()
      //{
      //    // The certificate is self signed and for some reason the name does not match the local certificate.
      //    // This needs some further investigation on how in create the certificate correctly and install it in the local store.
      //    // For now we allow requests with certificates that have a name mismatch.
      //    ServicePointManager.ServerCertificateValidationCallback = CertificateHandler.ValidateCertificate;
      //}

      /// <summary>
      /// Initializes a new instance of the <see cref="BitcoinClient"/> class.
      /// </summary>
      public BitcoinClient()
      {
         Client = new HttpClient();
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="BitcoinClient"/> class.
      /// </summary>
      public BitcoinClient(string uri)
      {
         Url = new Uri(uri);
         var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = CertificateHandler.ValidateCertificate };
         Client = new HttpClient(handler);
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="BitcoinClient"/> class.
      /// </summary>
      public BitcoinClient(string uri, NetworkCredential credentials)
      {
         Url = new Uri(uri);
         Credentials = credentials;
         var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = CertificateHandler.ValidateCertificate };
         Client = new HttpClient(handler);

         // Set basic authentication.
         byte[] token = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", Credentials.UserName, Credentials.Password));
         string base64Token = Convert.ToBase64String(token);
         Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Token);
         Client.DefaultRequestHeaders.Add("x-crypto", new[] { "true" });
         Client.DefaultRequestHeaders.Connection.Add("keep-alive");
      }

      /// <summary>
      /// Gets or sets the credentials.
      /// </summary>
      public NetworkCredential Credentials { get; set; }

      /// <summary>
      /// Gets or sets the Url.
      /// </summary>
      public Uri Url { get; set; }

      /// <summary>
      /// Gets or sets the client.
      /// </summary>
      protected HttpClient Client { get; set; }

      /// <summary>
      /// A static method to create a client.
      /// </summary>
      public static BitcoinClient Create(string connection, int port, string user, string pass, bool secure)
      {
         string schema = secure ? "https" : "http";
         return new BitcoinClient(string.Format("{0}://{1}:{2}", schema, connection, port), new NetworkCredential(user, pass));
      }

      /// <inheritdoc />
      public async Task BackupWalletAsync(string destination)
      {
         await CallAsync("backupwallet", destination);
      }

      /// <inheritdoc />
      public async Task<string> CreateRawTransactionAsync(CreateRawTransaction rawTransaction)
      {
         return await CallAsync<string>("createrawtransaction", rawTransaction.Inputs, rawTransaction.Outputs);
      }

      /// <inheritdoc />
      public async Task<DecodedRawTransaction> DecodeRawTransactionAsync(string rawTransactionHex)
      {
         DecodedRawTransaction res = await CallAsync<DecodedRawTransaction>("decoderawtransaction", rawTransactionHex);
         return res;
      }

      public void Dispose()
      {
         Client.Dispose();
      }

      /// <inheritdoc />
      public async Task<string> DumpPrivkeyAsync(string address)
      {
         return await CallAsync<string>("dumpprivkey", address);
      }

      /// <inheritdoc />
      public async Task EncryptWalletAsync(string passphrase)
      {
         await CallAsync("encryptwallet", passphrase);
      }

      /// <inheritdoc />
      public async Task<string> GetAccountAddressAsync(string account)
      {
         return await CallAsync<string>("getaccountaddress", account);
      }

      /// <inheritdoc />
      public async Task<string> GetAccountAsync(string address)
      {
         return await CallAsync<string>("getaccount", address);
      }

      /// <inheritdoc />
      public async Task<IEnumerable<string>> GetAddressesByAccountAsync(string account)
      {
         return await CallAsync<IEnumerable<string>>("getaddressesbyaccount", account);
      }

      /// <inheritdoc />
      public async Task<decimal> GetBalanceAsync(string account = null, int minconf = 1)
      {
         if (account == null)
         {
            return await CallAsync<decimal>("getbalance");
         }

         return await CallAsync<decimal>("getbalance", account, minconf);
      }

      /// <inheritdoc />
      public virtual async Task<BlockInfo> GetBlockAsync(string hash)
      {
         return await CallAsync<BlockInfo>("getblock", hash);
      }

      /// <inheritdoc />
      public BlockInfo GetBlock(string hash)
      {
         return Call<BlockInfo>("getblock", hash);
      }

      /// <inheritdoc />
      public string GetBlockHex(string hash)
      {
         return Call<string>("getblock", hash, 0);
      }

      /// <inheritdoc />
      public async Task<int> GetBlockCountAsync()
      {
         return await CallAsync<int>("getblockcount");
      }

      /// <inheritdoc />
      public int GetBlockCount()
      {
         return Call<int>("getblockcount");
      }

      /// <inheritdoc />
      public async Task<string> GetblockHashAsync(long index)
      {
         return await CallAsync<string>("getblockhash", index);
      }

      /// <inheritdoc />
      public string GetblockHash(long index)
      {
         return Call<string>("getblockhash", index);
      }

      /// <inheritdoc />
      public async Task<int> GetConnectionCountAsync()
      {
         return await CallAsync<int>("getconnectioncount");
      }

      /// <inheritdoc />
      public async Task<decimal> GetDifficultyAsync()
      {
         return await CallAsync<decimal>("getdifficulty");
      }

      /// <inheritdoc />
      public async Task<bool> GetGenerateAsync()
      {
         return await CallAsync<bool>("getgenerate");
      }

      /// <inheritdoc />
      public async Task<decimal> GetHashesPerSecAsync()
      {
         return await CallAsync<decimal>("gethashespersec");
      }

      /// <inheritdoc />
      public async Task<ClientInfo> GetInfoAsync()
      {
         return await CallAsync<ClientInfo>("getinfo");
      }

      public async Task<BlockchainInfoModel> GetBlockchainInfo()
      {
         return await CallAsync<BlockchainInfoModel>("getblockchaininfo");
      }

      public async Task<NetworkInfoModel> GetNetworkInfo()
      {
         return await CallAsync<NetworkInfoModel>("getnetworkinfo");
      }

      /// <inheritdoc />
      public async Task<IEnumerable<PeerInfo>> GetPeerInfo()
      {
         return await CallAsync<IEnumerable<PeerInfo>>("getpeerinfo");
      }

      /// <inheritdoc />
      public async Task<string> GetNewAddressAsync(string account)
      {
         return await CallAsync<string>("getnewaddress", account);
      }

      /// <inheritdoc />
      public async Task<IEnumerable<string>> GetRawMemPoolAsync()
      {
         return await CallAsync<IEnumerable<string>>("getrawmempool");
      }

      /// <inheritdoc />
      public IEnumerable<string> GetRawMemPool()
      {
         return Call<IEnumerable<string>>("getrawmempool");
      }

      /// <inheritdoc />
      public async Task<DecodedRawTransaction> GetRawTransactionAsync(string txid, int verbose = 0)
      {
         if (verbose == 0)
         {
            string hex = await CallAsync<string>("getrawtransaction", txid, verbose);
            return new DecodedRawTransaction { Hex = hex };
         }

         DecodedRawTransaction res = await CallAsync<DecodedRawTransaction>("getrawtransaction", txid, verbose);

         return res;
      }

      public DecodedRawTransaction GetRawTransaction(string txid, int verbose = 0)
      {
         if (verbose == 0)
         {
            string hex = Call<string>("getrawtransaction", txid, verbose);
            return new DecodedRawTransaction { Hex = hex };
         }

         DecodedRawTransaction res = Call<DecodedRawTransaction>("getrawtransaction", txid, verbose);

         return res;
      }

      /// <inheritdoc />
      public async Task<EstimateSmartFeeResponse> EstimateSmartFeeAsync(int confirmationTarget, EstimateSmartFeeMode estimateMode = EstimateSmartFeeMode.Conservative)
      {
         var estimateSmart = await CallAsync<EstimateSmartFeeResponse>("estimatesmartfee", confirmationTarget, estimateMode.ToString().ToUpperInvariant());

         return estimateSmart;
      }

      /// <inheritdoc />
      public async Task<decimal> GetReceivedByAccountAsync(string account, int minconf = 1)
      {
         return await CallAsync<decimal>("getreceivedbyaccount", account, minconf);
      }

      /// <inheritdoc />
      public async Task<decimal> GetReceivedByAddressAsync(string address, int minconf = 1)
      {
         return await CallAsync<decimal>("getreceivedbyaddress", address, minconf);
      }

      /// <inheritdoc />
      public async Task<TransactionInfo> GetTransactionAsync(string txid)
      {
         return await CallAsync<TransactionInfo>("gettransaction", txid);
      }

      /// <inheritdoc />
      public async Task<TransactionOutputInfo> GetTxOutAsync(string txid, int outputIndex, bool includemempool = true)
      {
         return await CallAsync<TransactionOutputInfo>("gettxout", txid, outputIndex, includemempool);
      }

      /// <inheritdoc />
      public async Task<WorkInfo> GetWorkAsync()
      {
         return await CallAsync<WorkInfo>("getwork");
      }

      /// <inheritdoc />
      public async Task<bool> GetWorkAsync(string data)
      {
         return await CallAsync<bool>("getwork", data);
      }

      /// <inheritdoc />
      public async Task<string> HelpAsync(string command = "")
      {
         return await CallAsync<string>("help", command);
      }

      /// <inheritdoc />
      public async Task ImportPrivkeyAsync(string bitcoinprivkey, string label, bool rescan = true)
      {
         await CallAsync("importprivkey", bitcoinprivkey, label, rescan);
      }

      /// <inheritdoc />
      public async Task<IEnumerable<TransactionAccountInfo>> ListAccountsAsync(int minconf = 1)
      {
         return await CallAsync<IEnumerable<TransactionAccountInfo>>("listaccounts", minconf);
      }

      /// <inheritdoc />
      public async Task<IEnumerable<TransactionInfo>> ListReceivedByAccountAsync(int minconf = 1, bool includeEmpty = false)
      {
         return await CallAsync<IEnumerable<TransactionInfo>>("listreceivedbyaccount", minconf, includeEmpty);
      }

      /// <inheritdoc />
      public async Task<IEnumerable<TransactionInfo>> ListReceivedByAddressAsync(int minconf = 1, bool includeEmpty = false)
      {
         return await CallAsync<IEnumerable<TransactionInfo>>("listreceivedbyaddress", minconf, includeEmpty);
      }

      /// <inheritdoc />
      public async Task<IEnumerable<TransactionAccountInfo>> ListTransactionsAsync(string account, int count = 10)
      {
         return await CallAsync<IEnumerable<TransactionAccountInfo>>("listtransactions", account, count);
      }

      /// <inheritdoc />
      public async Task<IEnumerable<TransactionUnspentInfo>> ListUnspent(int minconf = 1, int maxconf = 999999)
      {
         return await CallAsync<IEnumerable<TransactionUnspentInfo>>("listunspent", minconf, maxconf);
      }

      /// <inheritdoc />
      public async Task<bool> MoveAsync(string fromAccount, string toAccount, decimal amount, int minconf = 1, string comment = "")
      {
         return await CallAsync<bool>("move", fromAccount, toAccount, amount, minconf, comment);
      }


      /// <inheritdoc />
      public async Task<string> SendFromAsync(string fromAccount, string toAddress, decimal amount, int minconf = 1, string comment = "", string commentTo = "")
      {
         return await CallAsync<string>("sendfrom", fromAccount, toAddress, amount, minconf, comment, commentTo);
      }

      /// <inheritdoc />
      public async Task<string> SendToAddressAsync(string address, decimal amount, string comment, string commentTo)
      {
         return await CallAsync<string>("sendtoaddress", address, amount, comment, commentTo);
      }

      /// <inheritdoc />
      public async Task<string> SentRawTransactionAsync(string hexString)
      {
         return await CallAsync<string>("sendrawtransaction", hexString);
      }

      /// <inheritdoc />
      public async Task SetAccountAsync(string address, string account)
      {
         await CallAsync("setaccount", address, account);
      }

      /// <inheritdoc />
      public async Task SetGenerateAsync(bool generate, int genproclimit = 1)
      {
         await CallAsync("setgenerate", generate, genproclimit);
      }

      /// <inheritdoc />
      public async Task SetTxFeeAsync(decimal amount)
      {
         await CallAsync("settxfee", amount);
      }

      /// <inheritdoc />
      public async Task<SignedRawTransaction> SignRawTransactionAsync(SignRawTransaction rawTransaction)
      {
         string hex = rawTransaction.RawTransactionHex;
         List<SignRawTransactionInput> inputs = rawTransaction.Inputs.Any() ? rawTransaction.Inputs : null;
         List<string> privateKeys = rawTransaction.PrivateKeys.Any() ? rawTransaction.PrivateKeys : null;

         SignedRawTransaction res = await CallAsync<SignedRawTransaction>("signrawtransaction", hex, inputs, privateKeys);
         return res;
      }

      /// <inheritdoc />
      public async Task StopAsync()
      {
         await CallAsync("stop");
      }

      /// <inheritdoc />
      public async Task<ValidateAddressResult> ValidateAddressAsync(string address)
      {
         return await CallAsync<ValidateAddressResult>("validateaddress", address);
      }

      /// <inheritdoc />
      public async Task WalletLockAsync()
      {
         await CallAsync("walletlock");
      }

      /// <inheritdoc />
      public async Task WalletPassphraseAsync(string passphrase, int sectimeout)
      {
         await CallAsync("walletpassphrase", passphrase, sectimeout);
      }

      /// <inheritdoc />
      public async Task WalletPassphraseChangeAsync(string passphrase, string newPassphrase)
      {
         await CallAsync("walletpassphrasechange", passphrase, newPassphrase);
      }

      /// <summary>
      /// Create a crypto client exception.
      /// </summary>
      private static BitcoinClientException CreateException(HttpResponseMessage response, int code, string msg, HttpStatusCode? statusCode = null)
      {
         return new BitcoinClientException(string.Format("{0} ({1})", msg, code))
         {
            StatusCode = statusCode ?? response.StatusCode,
            RawMessage = response.Content.ReadAsStringAsync().Result,
            ErrorCode = code,
            ErrorMessage = msg
         };
      }

      /// <summary>
      /// Send the request and wrap any exception.
      /// </summary>
      private static async Task<HttpResponseMessage> SendAsync(HttpClient client, Uri url, HttpContent content)
      {
         try
         {
            return await client.PostAsync(url, content);
         }
         catch (Exception ex)
         {
            throw new BitcoinCommunicationException(string.Format("Daemon Failed Url = '{0}'", url), ex);
         }
      }

      /// <summary>
      /// Send the request and wrap any exception.
      /// </summary>
      private static HttpResponseMessage Send(HttpClient client, Uri url, HttpContent content)
      {
         try
         {
            return client.PostAsync(url, content).GetAwaiter().GetResult();
         }
         catch (Exception ex)
         {
            throw new BitcoinCommunicationException(string.Format("Daemon Failed Url = '{0}'", url), ex);
         }
      }

      /// <summary>
      /// Make a call to crypto API.
      /// </summary>
      protected async Task<T> CallAsync<T>(string method, params object[] parameters)
      {
         var rpcReq = new JsonRpcRequest(1, method, parameters);

         string serialized = JsonConvert.SerializeObject(rpcReq);

         // serialize json for the request
         byte[] byteArray = Encoding.UTF8.GetBytes(serialized);

         using (var request = new StreamContent(new MemoryStream(byteArray)))
         {
            request.Headers.ContentType = new MediaTypeHeaderValue("application/json-rpc");

            HttpResponseMessage response = await SendAsync(Client, Url, request);

            T ret = await CheckResponseOkAsync<T>(response);

            return ret;
         }
      }

      /// <summary>
      /// Make a call to crypto API.
      /// </summary>
      private T Call<T>(string method, params object[] parameters)
      {
         var rpcReq = new JsonRpcRequest(1, method, parameters);

         string serialized = JsonConvert.SerializeObject(rpcReq);

         // serialize json for the request
         byte[] byteArray = Encoding.UTF8.GetBytes(serialized);

         using (var request = new StreamContent(new MemoryStream(byteArray)))
         {
            request.Headers.ContentType = new MediaTypeHeaderValue("application/json-rpc");

            HttpResponseMessage response = Send(Client, Url, request);
            T ret = CheckResponseOk<T>(response);

            return ret;
         }
      }

      /// <summary>
      /// Make a call to crypto API.
      /// </summary>
      private async Task CallAsync(string method, params object[] parameters)
      {
         var rpcReq = new JsonRpcRequest(1, method, parameters);

         string s = JsonConvert.SerializeObject(rpcReq);

         // serialize json for the request
         byte[] byteArray = Encoding.UTF8.GetBytes(s);

         using (var request = new StreamContent(new MemoryStream(byteArray)))
         {
            request.Headers.ContentType = new MediaTypeHeaderValue("application/json-rpc");

            HttpResponseMessage response = await SendAsync(Client, Url, request);
            await CheckResponseOkAsync<string>(response);
         }
      }

      /// <summary>
      /// Check the crypto client response is ok.
      /// </summary>
      private async Task<T> CheckResponseOkAsync<T>(HttpResponseMessage response)
      {
         try
         {
            using (Stream jsonStream = await response.Content.ReadAsStreamAsync())
            {
               using (var jsonStreamReader = new StreamReader(jsonStream))
               {
                  string jsonResult = await jsonStreamReader.ReadToEndAsync();

                  if (response.StatusCode != HttpStatusCode.OK)
                  {
                     HandleError(jsonResult, response);
                  }
                  else if (jsonResult.Contains("Method not found")) // Dirty hack due to "Method not found" not returning error status code.
                  {
                     HandleError(jsonResult, response, HttpStatusCode.NotImplemented);
                  }

                  JsonRpcResponse<T> ret = JsonConvert.DeserializeObject<JsonRpcResponse<T>>(jsonResult);

                  if (ret?.Error != null)
                  {
                     HandleError(jsonResult, response);
                  }

                  return ret == null ? default(T) : ret.Result;
               }
            }
         }
         catch (BitcoinClientException)
         {
            throw;
         }
         catch (Exception ex)
         {
            throw new BitcoinClientException(string.Format("Failed parsing the result, StatusCode={0}, row message={1}", response.StatusCode, response.Content.ReadAsStringAsync().Result), ex);
         }
      }

      private void HandleError(string body, HttpResponseMessage response, HttpStatusCode? statusCode = null)
      {
         JsonRpcResponse<dynamic> errRet = null;

         try
         {
            // This should parse to dynamic, since if the response is:
            // {"result":null,"error":{"code":-32601,"message":"Method not found"},"id":1}
            // Then it will fail to serialize to for example integer.
            errRet = JsonConvert.DeserializeObject<JsonRpcResponse<dynamic>>(body);
         }
         catch
         {
            throw CreateException(response, 0, body, statusCode);
         }

         int code = errRet != null && errRet.Error != null ? errRet.Error.Code : 0;
         string msg = errRet != null && errRet.Error != null ? errRet.Error.Message : "Error";

         throw CreateException(response, code, msg, statusCode);
      }

      /// <summary>
      /// Check the crypto client response is ok.
      /// </summary>
      private T CheckResponseOk<T>(HttpResponseMessage response)
      {
         try
         {
            using (Stream jsonStream = response.Content.ReadAsStreamAsync().Result)
            {
               using (var jsonStreamReader = new StreamReader(jsonStream))
               {
                  string jsonResult = jsonStreamReader.ReadToEndAsync().Result;

                  if (response.StatusCode != HttpStatusCode.OK)
                  {
                     HandleError(jsonResult, response);
                  }

                  JsonRpcResponse<T> ret = JsonConvert.DeserializeObject<JsonRpcResponse<T>>(jsonResult);

                  if (ret?.Error != null)
                  {
                     HandleError(jsonResult, response);
                  }

                  return ret == null ? default(T) : ret.Result;
               }
            }
         }
         catch (BitcoinClientException)
         {
            throw;
         }
         catch (Exception ex)
         {
            throw new BitcoinClientException(string.Format("Failed parsing the result, StatusCode={0}, row message={1}", response.StatusCode, response.Content.ReadAsStringAsync().Result), ex);
         }
      }
   }
}
