
using System;
using System.Linq;
using System.Threading.Tasks;
using Blockcore.Indexer.Core.Storage.Postgres.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace Blockcore.Indexer.Core.Storage.Postgres;

public class BlockRewindOperation : IBlockRewindOperation
{
    private readonly IDbContextFactory<PostgresDbContext> contextFactory;

    public BlockRewindOperation(IDbContextFactory<PostgresDbContext> contextFactory)
    {
        this.contextFactory = contextFactory;
    }

    public async Task RewindBlockAsync(uint blockIndex)
    {

        await StoreRewindBlockAsync(contextFactory, blockIndex);

        Task Outputs = contextFactory.CreateDbContext().Outputs.Where(o => o.Outpoint.OutputIndex == blockIndex).ExecuteDeleteAsync();
        Task AddressComputed = contextFactory.CreateDbContext().AddressComputedTable.Where(t => t.ComputedBlockIndex == blockIndex).ExecuteDeleteAsync();
        Task AddressHistory =  contextFactory.CreateDbContext().AddressHistoryComputedTable.Where(t => t.BlockIndex == blockIndex).ExecuteDeleteAsync();

        await Task.WhenAll(Outputs, AddressComputed, AddressHistory);

        //Todo -> it is yet to be observed if 
        // ConfirmDataDeletion(blockIndex);


        await MergeRewindInputsToUnspentTransactionsAsync(blockIndex);
        Task Block = Task.Run(async () => await contextFactory.CreateDbContext().Blocks.Where(b => b.BlockIndex == blockIndex).ExecuteDeleteAsync());

        Task Utxo = Task.Run(async () => await contextFactory.CreateDbContext().unspentOutputs.Where(utxo => utxo.BlockIndex == blockIndex).ExecuteDeleteAsync());

        await Task.WhenAll(Block, Utxo);
    }
    /// <summary>
    /// Inputs spend outputs, when an output is spent it gets deleted from the UnspendOutput table and the action of the delete is represented in the inputs table,
    /// when a rewind happens we need to bring back outputs that have been deleted from the UnspendOutput so we look for those outputs in the inputs table,
    /// however the block index in the inputs table is the one representing the input not the output we are trying to restore so we have to look it up in the outputs table.
    /// </summary>
    private async Task MergeRewindInputsToUnspentTransactionsAsync(uint blockIndex)
    {
        using var db = contextFactory.CreateDbContext();

        var unspentOutputs = await db.Inputs
        .Where(i => i.BlockIndex == blockIndex)
        .Join(db.Outputs,
              input => input.Outpoint,
              output => output.Outpoint,
              (input, output) => new UnspentOutput
              {
                  Value = input.Value,
                  Address = input.Address,
                  BlockIndex = output.BlockIndex,
                  Outpoint = input.Outpoint
              })
        .ToListAsync();

        if (unspentOutputs.Any())
        {
            var uniqueUnspentOutputs = unspentOutputs.GroupBy(u => u.Outpoint).Select(g => g.First()).ToList();


            var duplicateOutpoints = await db.unspentOutputs
                .Where(u => uniqueUnspentOutputs.Select(o => o.Outpoint).Contains(u.Outpoint))
                .Select(u => u.Outpoint)
                .ToListAsync();

            var filteredUnspentOutputs = uniqueUnspentOutputs
                        .Where(u => !duplicateOutpoints.Contains(u.Outpoint))
                        .ToList();

            // TODO: filter out any outputs that belong to the block being reorged.
            // this can happen for outputs that are created and spent in the same block.
            // if they get pushed now such outputs will just get deleted in the next step.

            if (filteredUnspentOutputs.Any())
            {
                await db.BulkInsertAsync(filteredUnspentOutputs);
            }

        }
    }


    private void ConfirmDataDeletion(uint blockIndex) => throw new NotImplementedException();

    private static Task StoreRewindBlockAsync(IDbContextFactory<PostgresDbContext> factory, uint blockIndex)
    {
        var blockTask = Task.Run(async () => await factory.CreateDbContext().Blocks.Where(b => b.BlockIndex == blockIndex).FirstOrDefaultAsync());
        Task.WhenAll(blockTask);

        Block block = blockTask.Result;

        var reorgBlock = new ReorgBlock()
        {
            Created = System.DateTime.UtcNow,
            BlockIndex = blockIndex,
            BlockHash = block.BlockHash,
            Block = block
        };

        return Task.Run(async () => await factory.CreateDbContext().ReorgBlocks.AddAsync(reorgBlock));
    }

}
