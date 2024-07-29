
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

        Task Block = Task.Run(async () => await contextFactory.CreateDbContext().Blocks.Where(b => b.BlockIndex == blockIndex).ExecuteDeleteAsync());
        Task AddressComputed = Task.Run(async () => await contextFactory.CreateDbContext().AddressComputedTable.Where(t => t.ComputedBlockIndex == blockIndex).ExecuteDeleteAsync());
        Task AddressHistory = Task.Run(async () => await contextFactory.CreateDbContext().AddressHistoryComputedTable.Where(t => t.BlockIndex == blockIndex).ExecuteDeleteAsync());

        await Task.WhenAll(Block, AddressComputed, AddressHistory);

        //Todo -> Error handling for faliure in deletion

        // ConfirmDataDeletion(blockIndex);
        

        // await MergeRewindInputsToUnspentTransactionsAsync(db, blockIndex);

        //pending
    }

    private async Task MergeRewindInputsToUnspentTransactionsAsync(PostgresDbContext db, uint blockIndex) => throw new NotImplementedException();
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
