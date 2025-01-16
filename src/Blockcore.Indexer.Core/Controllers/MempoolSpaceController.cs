using Microsoft.AspNetCore.Mvc;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using DBreeze.Utils;
using System.Linq;
using System.Collections.Generic;
using Blockcore.NBitcoin;
using System;


static class MempoolSpaceHelpers
{
    static List<string> ComputeWitScript(string witScript){
        List<string> scripts = new();
        int index = 0;
        while (index < witScript.Length){
            string sizeHex = witScript.Substring(index, 2);
            int size = int.Parse(sizeHex, System.Globalization.NumberStyles.HexNumber);
            index += 2;
            string script = witScript.Substring(index, size * 2);
            scripts.Add(script); 
        }

        return scripts;
    }

    public static MempoolTransaction MapToMempoolTransaction(QueryTransaction queryTransaction)
    {
        MempoolTransaction mempoolTransaction = new() {
            Txid = queryTransaction.TransactionId,
            Version = (int)queryTransaction.Version,
            Locktime = int.Parse(queryTransaction.LockTime.Split(':').Last()),
            Size = queryTransaction.Size,
            Weight = queryTransaction.Weight,
            Fee = (int)queryTransaction.Fee,
            Status = new(){
                Confirmed = queryTransaction.Confirmations > 0,
                BlockHeight = (int)queryTransaction.BlockIndex,
                BlockHash = queryTransaction.BlockHash,
                BlockTime = queryTransaction.Timestamp
            },
            Vin = queryTransaction.Inputs.Select(input => new Vin(){
                IsCoinbase = input.CoinBase != null,
                //TODO pending fetching outpoint info, Isn't used currently by the mempool api
                Prevout = new PrevOut(){
                    Value = 0,
                    Scriptpubkey = null,
                    ScriptpubkeyAddress = null,
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
            }).ToList(),
            Vout = queryTransaction.Outputs.Select(output => new PrevOut(){
                Value = output.Balance,
                Scriptpubkey = output.ScriptPubKey,
                ScriptpubkeyAddress = output.Address,
                ScriptpubkeyAsm = output.ScriptPubKeyAsm,
            }).ToList(),
        };

        return mempoolTransaction;
    }
}

namespace Blockcore.Indexer.Core.Controllers
{
    [ApiController]
    [Route("api/mempoolspace")]
    public class MempoolSpaceController : Controller
    {
        private readonly IStorage storage;

        private readonly JsonSerializerOptions serializeOption = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true
        };

        public MempoolSpaceController(IStorage storage)
        {
            this.storage = storage;
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

            List<QueryTransaction> queryTransactions = transactions.Select(storage.GetTransaction).ToList();
            //add a mapper to convert the 
            List<MempoolTransaction> txns = queryTransactions.Select(MempoolSpaceHelpers.MapToMempoolTransaction).ToList();

            return Ok(txns);
        }

        [HttpGet]
        [Route("tx/{txid}/outspends")]
        public IActionResult GetTransactionOutspends(string txid)
        {
            return Ok();
        }

        [HttpGet]
        [Route("fees/recommended")]
        public IActionResult GetRecommendedFees()
        {
            return Ok();
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