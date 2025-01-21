using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;


namespace Blockcore.Indexer.Core.Models
{

    public class AddressStats
    {
        public int FundedTxoCount { get; set; }
        public long FundedTxoSum { get; set; }
        public int SpentTxoCount { get; set; }
        public long SpentTxoSum { get; set; }
        public int TxCount { get; set; }
    }
    public class AddressResponse
    {
        public string Address { get; set; }
        public AddressStats ChainStats { get; set; }
        public AddressStats MempoolStats { get; set; }
    }

    public class OutspentResponse{
        public bool spent { get; set; }
        public string txid { get; set; }
        public int vin { get; set; }
        public UtxoStatus status { get; set; }
    }

    public class AddressUtxo
    {
        public string Txid { get; set; }
        public int Vout { get; set; }
        public UtxoStatus Status { get; set; }
        public long Value { get; set; }
    }

    public class UtxoStatus
    {
        public bool Confirmed { get; set; }
        public int BlockHeight { get; set; }
        public string BlockHash { get; set; }
        public long BlockTime { get; set; }
    }

    public class RecommendedFees
    {
        public int FastestFee { get; set; }
        public int HalfHourFee { get; set; }
        public int HourFee { get; set; }
        public int EconomyFee { get; set; }
        public int MinimumFee { get; set; }
    }

    public class Vin
    {
        public bool IsCoinbase { get; set; }
        public PrevOut Prevout { get; set; }
        public string Scriptsig { get; set; }
        public string Asm { get; set; }
        public long Sequence { get; set; }
        public string Txid { get; set; }
        public int Vout { get; set; }
        public List<string> Witness { get; set; }
        public string InnserRedeemscriptAsm { get; set; }
        public string InnerWitnessscriptAsm { get; set; }
    }
    public class PrevOut
    {
        public long Value { get; set; }
        public string Scriptpubkey { get; set; }
        public string ScriptpubkeyAddress { get; set; }
        public string ScriptpubkeyAsm { get; set; }
        public string ScriptpubkeyType { get; set; }
    }

    public class MempoolTransaction
    {
        public string Txid { get; set; }

        public int Version { get; set; }

        public int Locktime { get; set; }
        public int Size { get; set; }
        public int Weight { get; set; }
        public int Fee { get; set; }
        public List<Vin> Vin { get; set; }
        public List<PrevOut> Vout { get; set; }
        public UtxoStatus Status { get; set; }
    }

    public class Outspent
    {
        public bool Spent { get; set; }
    }
}
