using System;
using Blockcore.Indexer.Core.Storage.Postgres.Types;
using Blockcore.Indexer.Core.Storage.Types;
using Microsoft.EntityFrameworkCore;
using Input = Blockcore.Indexer.Core.Storage.Postgres.Types.Input;
using Output = Blockcore.Indexer.Core.Storage.Postgres.Types.Output;
using ReorgBlock = Blockcore.Indexer.Core.Storage.Postgres.Types.ReorgBlock;
public class PostgresDbContext : DbContext
{
    public PostgresDbContext() : base()
    {

    }
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql("Host=127.0.0.1;Port=5432;Database=IndexerBenchmark;Username=postgres;Password=drb;");
    }

    public DbSet<Block> Blocks { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Input> Inputs { get; set; }
    public DbSet<Output> Outputs { get; set; }
    public DbSet<MempoolTransaction> mempoolTransactions { get; set; }
    public DbSet<MempoolInput> mempoolInputs { get; set; }
    public DbSet<MempoolOutput> mempoolOutputs { get; set; }
    public DbSet<UnspentOutput> unspentOutputs { get; set; }
    public DbSet<ReorgBlock> ReorgBlocks { get; set; }
    public DbSet<PeerDetails> Peers { get; set; }
    public DbSet<RichListEntry> RichList { get; set; }
    public DbSet<AddressComputedEntry> AddressComputedTable { get; set; }
    public DbSet<AddressHistoryComputedEntry> AddressHistoryComputedTable { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureBlockEntity(modelBuilder);
        ConfigureTransactionEntity(modelBuilder);
        ConfigureInputEntity(modelBuilder);
        ConfigureOutputEntity(modelBuilder);
        ConfigureMempoolTransactionEntity(modelBuilder);
        ConfigureMempoolInputEntity(modelBuilder);
        ConfigureMempoolOutputEntity(modelBuilder);
        ConfigureUnspentOutputEntity(modelBuilder);
        ConfigureReorgBlockEntity(modelBuilder);
        ConfigurePeerEntity(modelBuilder);
        ConfigureRichListEntity(modelBuilder);
        ConfigureAddressComputedEntity(modelBuilder);
        ConfigureAddressHistoryComputedEntity(modelBuilder);
    }


    private void ConfigureBlockEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Block>()
            .HasKey(b => b.BlockHash);

        modelBuilder.Entity<Block>()
            .HasIndex(b => b.BlockHash)
            .HasMethod("hash");

        modelBuilder.Entity<Block>()
            .HasIndex(b => b.BlockIndex)
            .IsDescending();
    }

    private void ConfigureTransactionEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>()
            .HasKey(t => t.Txid);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Block)
            .WithMany(b => b.Transactions)
            .HasForeignKey(t => t.BlockIndex);

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.Txid)
            .HasMethod("hash");
    }

    private void ConfigureInputEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Input>()
            .HasKey(i => new { i.TrxHash, i.BlockIndex });

        modelBuilder.Entity<Input>()
            .HasOne<Transaction>(i => i.Transaction)
            .WithMany(t => t.Inputs)
            .HasForeignKey(i => i.TrxHash);
    }

    private void ConfigureOutputEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Output>()
            .HasKey(o => new { o.Outpoint.TransactionId, o.Outpoint.OutputIndex });

        modelBuilder.Entity<Output>()
            .HasOne<Transaction>(o => o.Transaction)
            .WithMany(t => t.Outputs)
            .HasForeignKey(o => o.Outpoint.TransactionId);
    }

    private void ConfigureMempoolTransactionEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MempoolTransaction>()
            .HasKey(t => t.TransactionId);
    }

    private void ConfigureMempoolInputEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MempoolInput>()
            .HasKey(i => new { i.Txid, i.Vout });

        modelBuilder.Entity<MempoolInput>()
            .HasOne<MempoolTransaction>(i => i.Transaction)
            .WithMany(t => t.Inputs)
            .HasForeignKey(i => i.Txid);
    }

    private void ConfigureMempoolOutputEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MempoolOutput>()
            .HasKey(o => new { o.outpoint.TransactionId, o.outpoint.OutputIndex });

        modelBuilder.Entity<MempoolOutput>()
            .HasOne<MempoolTransaction>(o => o.Transaction)
            .WithMany(t => t.Outputs)
            .HasForeignKey(o => o.outpoint.TransactionId);
    }

    private void ConfigureUnspentOutputEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UnspentOutput>()
            .HasKey(uo => new { uo.Outpoint.TransactionId, uo.Outpoint.OutputIndex });
    }

    private void ConfigureReorgBlockEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReorgBlock>()
            .HasKey(rb => rb.BlockIndex);
    }
    private void ConfigurePeerEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PeerDetails>()
            .HasKey(p => p.Addr);
    }

    private void ConfigureRichListEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RichListEntry>()
            .HasKey(r => r.Address);
    }
    private void ConfigureAddressComputedEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AddressComputedEntry>()
        .HasKey(r => r.Address);
    }

    private void ConfigureAddressHistoryComputedEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AddressHistoryComputedEntry>()
        .HasKey(r => r.Address);
    }
}