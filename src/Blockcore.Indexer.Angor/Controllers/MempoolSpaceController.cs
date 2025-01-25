using Microsoft.AspNetCore.Mvc;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Blockcore.Indexer.Core.Storage.Types;
using Blockcore.Indexer.Core.Handlers;


static class MempoolSpaceHelpers
{
    static List<string> ComputeWitScript(string witScript)
    {
        List<string> scripts = new();
        int index = 0;
        while (index < witScript.Length)
        {
            string sizeHex = witScript.Substring(index, 2);
            int size = int.Parse(sizeHex, System.Globalization.NumberStyles.HexNumber);
            index += 2;
            string script = witScript.Substring(index, size * 2);
            scripts.Add(script);
        }

        return scripts;
    }

    public static MempoolTransaction MapToMempoolTransaction(QueryTransaction queryTransaction, IStorage storage)
    {
        MempoolTransaction mempoolTransaction = new()
        {
            Txid = queryTransaction.TransactionId,
            Version = (int)queryTransaction.Version,
            Locktime = int.Parse(queryTransaction.LockTime.Split(':').Last()),
            Size = queryTransaction.Size,
            Weight = queryTransaction.Weight,
            Fee = (int)queryTransaction.Fee,
            Status = new()
            {
                Confirmed = queryTransaction.Confirmations > 0,
                BlockHeight = (int)queryTransaction.BlockIndex,
                BlockHash = queryTransaction.BlockHash,
                BlockTime = queryTransaction.Timestamp
            },
            Vin = queryTransaction.Inputs.Select(input =>
            {
                Output output = storage.GetOutputFromOutpoint(input.InputTransactionId, input.InputIndex);
                return new Vin()
                {
                    IsCoinbase = input.CoinBase != null,
                    Prevout = new PrevOut()
                    {
                        Value = output.Value,
                        Scriptpubkey = output.ScriptHex,
                        ScriptpubkeyAddress = output.Address,
                        ScriptpubkeyAsm = null,
                        ScriptpubkeyType = null
                    },
                    Scriptsig = input.ScriptSig,
                    Asm = input.ScriptSigAsm,
                    Sequence = long.Parse(input.SequenceLock),
                    Txid = input.InputTransactionId,
                    Vout = input.InputIndex,
                    Witness = ComputeWitScript(input.WitScript),
                    InnserRedeemscriptAsm = null,
                    InnerWitnessscriptAsm = null
                };
            }).ToList(),


            Vout = queryTransaction.Outputs.Select(output => new PrevOut()
            {
                Value = output.Balance,
                Scriptpubkey = output.ScriptPubKey,
                ScriptpubkeyAddress = output.Address,
                ScriptpubkeyAsm = output.ScriptPubKeyAsm,
            }).ToList(),
        };

        return mempoolTransaction;
    }
}

namespace Blockcore.Indexer.Angor.Controllers
{
    [ApiController]
    [Route("api/mempoolspace")]
    public class MempoolSpaceController : Controller
    {
        private readonly IStorage storage;
        private readonly StatsHandler statsHandler;

        private readonly JsonSerializerOptions serializeOption = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true
        };

        public MempoolSpaceController(IStorage storage, StatsHandler statsHandler)
        {
            this.storage = storage;
            this.statsHandler = statsHandler;
        }

        [HttpGet]
        [Route("address/{address}")]
        public IActionResult GetAddress([MinLength(4)][MaxLength(100)] string address)
        {
            AddressResponse addressResponse = storage.AddressResponseBalance(address);
            return Ok(JsonSerializer.Serialize(addressResponse, serializeOption));
        }

        [HttpGet]
        [Route("address/{address}/txs")]
        public IActionResult GetAddressTransactions(string address)
        {
            var transactions = storage.AddressHistory(address, null, 50).Items.Select(t => t.TransactionHash).ToList();
            List<QueryTransaction> queryTransactions = storage.GetMempoolTransactionList(transactions);
            List<MempoolTransaction> txns = queryTransactions.Select(trx => MempoolSpaceHelpers.MapToMempoolTransaction(trx, storage)).ToList();
            return Ok(JsonSerializer.Serialize(txns, serializeOption));
        }

        [HttpGet]
        [Route("tx/{txid}/outspends")]
        public IActionResult GetTransactionOutspends(string txid)
        {
            List<OutspentResponse> responses = storage.GetTransactionOutspends(txid); 
            return Ok(JsonSerializer.Serialize(responses, serializeOption));
        }

        [HttpGet]
        [Route("fees/recommended")]
        public IActionResult GetRecommendedFees()
        {
            RecommendedFees recommendedFees = new();
            var statsFees = statsHandler.GetFeeEstimation([1, 3, 6, 12, 48]);
            statsFees.Wait();
            var Fees = statsFees.Result.Fees.Select(fee => ConvertToSatsPerVByte(fee.FeeRate)).ToList();
            recommendedFees.FastestFee = (int)Fees[0];
            recommendedFees.HalfHourFee = (int)Fees[1];
            recommendedFees.HourFee = (int)Fees[2];
            recommendedFees.EconomyFee = (int)Fees[3];
            recommendedFees.MinimumFee = (int)Fees[4];
            
            return Ok(JsonSerializer.Serialize(recommendedFees, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            }));
        }

        private double ConvertToSatsPerVByte(double fee)
        {
            return fee / 1_000;
        }

        [HttpGet]
        [Route("tx/{txid}/hex")]
        public IActionResult GetTransactionHex(string txid)
        {
            var transactionHex = storage.GetRawTransaction(txid);
            return Ok(transactionHex);
        }

        [HttpGet]
        [Route("block-height/0")]
        public IActionResult GetBlockHeightZero()
        {
            var block = storage.BlockByIndex(0);
            return Ok(block);
        }
    }
}