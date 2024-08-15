using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;

namespace Blockcore.Indexer.Core.Storage.Postgres.Types
{
    public class Transaction
    {
        [Key]
        public Guid _Id { get; set; }
        public Transaction(){
            _Id = Guid.NewGuid();
        }
        public byte[] RawTransaction { get; set; }
        public string Txid { get; set; }
        public long BlockIndex { get; set; }
        public int TransactionIndex { get; set; }
        public short NumberOfOutputs { get; set; }
        public ICollection<Input> Inputs { get; set; }
        public ICollection<Output> Outputs { get; set; }
        public virtual Block Block { get; set; }
    }
}

