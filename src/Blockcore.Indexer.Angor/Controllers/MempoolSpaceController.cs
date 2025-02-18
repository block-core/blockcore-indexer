using Microsoft.AspNetCore.Mvc;
using Blockcore.Indexer.Core.Storage;
using Blockcore.Indexer.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Blockcore.Indexer.Core.Storage.Types;
using Blockcore.Indexer.Core.Handlers;



namespace Blockcore.Indexer.Angor.Controllers
{
    [ApiController]
    [Route("api")]
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
        public async Task<IActionResult> GetAddressTransactions(string address)
        {
            var transactions = storage.AddressHistory(address, null, 50).Items.Select(t => t.TransactionHash).ToList();
            List<MempoolTransaction> txns = await storage.GetMempoolTransactionListAsync(transactions);
            return Ok(JsonSerializer.Serialize(txns, serializeOption));
        }

        [HttpGet]
        [Route("tx/{txid}/outspends")]
        public async Task<IActionResult> GetTransactionOutspends(string txid)
        {
            List<OutspentResponse> responses = await storage.GetTransactionOutspendsAsync(txid);
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
            var txn = storage.GetRawTransaction(txid);
            if (txn == null)
            {
                return NotFound();
            }
            return Ok(txn);
        }

        [HttpGet]
        [Route("block-height/{height}")]
        public IActionResult GetBlockHeight(int height)
        {
            return Ok(storage.BlockByIndex(height).BlockHash);
        }
    }
}