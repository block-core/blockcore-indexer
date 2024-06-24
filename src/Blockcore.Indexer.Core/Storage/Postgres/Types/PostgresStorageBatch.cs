using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Blockcore.Indexer.Core.Operations.Types;

namespace Blockcore.Indexer.Core.Storage.Postgres.Types;

public class PostgresStorageBatch : StorageBatch
{
    public long TotalSize { get; set; }
    public List<Transaction> Transactions { get; set; } = [];
    public Dictionary<long, Block> Blocks { get; set; } = [];
    public Dictionary<string, Output> Outputs { get; set; } = [];
    public List<Input> Inputs { get; set; } = [];
    public override long GetBatchSize() => TotalSize;
    public override int GetBlockCount() => Blocks.Count;
    public override IEnumerable<long> GetBlockSizes() => Blocks.Values.Select(x => x.BlockSize).ToList();
    public override int GetInputCount() => Inputs.Count;
    public override int GetOutputCount() => Outputs.Count;
    public override int GetTransactionCount() => Transactions.Count;
    public override bool ValidateBatch(string prevBlockHash){
        string prevHash = prevBlockHash;
        foreach(var mapBlock in Blocks.Values.OrderBy(b => b.BlockIndex)){
            if(mapBlock.PreviousBlockHash != prevHash){
                return false;
            }

            prevHash = mapBlock.BlockHash;
        }

        return true;
    }



}