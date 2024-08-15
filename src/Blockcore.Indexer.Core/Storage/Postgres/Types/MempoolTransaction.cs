using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Blockcore.Indexer.Core.Storage.Types;

namespace Blockcore.Indexer.Core.Storage.Postgres.Types
{
   public class MempoolInput
   {
      public Guid _Id { get; set; }
      public MempoolInput()
      {
         _Id = Guid.NewGuid();
      }
      public Outpoint Outpoint { get; set; }
      public string Address { get; set; }
      public long Value { get; set; }
      public string Txid { get; set; }
      public uint Vout { get; set; }
      public virtual MempoolTransaction Transaction { get; set; }
   }
   public class MempoolOutput
   {
      public Guid _Id { get; set; }
      public MempoolOutput()
      {
         _Id = Guid.NewGuid();
      }
      public Outpoint outpoint { get; set; }
      public string Address { get; set; }
      public string ScriptHex { get; set; }
      public long Value { get; set; }
      public long BlockIndex { get; set; }
      public bool CoinBase { get; set; }
      public bool CoinStake { get; set; }
      public virtual MempoolTransaction Transaction { get; set; }
   }
   public class MempoolTransaction
   {
      public Guid _Id { get; set; }
      public MempoolTransaction()
      {
         _Id = Guid.NewGuid();
      }
      public long FirstSeen { get; set; }
      public string TransactionId { get; set; }
      public ICollection<MempoolInput> Inputs { get; set; }
      public ICollection<MempoolOutput> Outputs { get; set; }
      public List<string> AddressOutputs { get; set; }
      public List<string> AddressInputs { get; set; }

   }
}
