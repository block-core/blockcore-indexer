using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NLog;

namespace Blockcore.Indexer.Core.Storage.Postgres.Types
{
    public class Transaction
    {
        public byte[] RawTransaction { get; set; }
        [Key]
        public string Txid { get; set; }
        public long BlockIndex { get; set; }
        public int TransactionIndex { get; set; }
        public short NumberOfOutputs { get; set; }
        public ICollection<Input> Inputs { get; set; }
        public ICollection<Output> Outputs { get; set; }
        public virtual Block Block { get; set; }
    }
}

