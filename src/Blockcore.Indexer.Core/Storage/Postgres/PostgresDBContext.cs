using System;
using Blockcore.Indexer.Core.Storage.Postgres.Types;
using Blockcore.Indexer.Core.Storage.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Input = Blockcore.Indexer.Core.Storage.Postgres.Types.Input;
using Output = Blockcore.Indexer.Core.Storage.Postgres.Types.Output;
using ReorgBlock = Blockcore.Indexer.Core.Storage.Postgres.Types.ReorgBlock;
public class PostgresDbContext : DbContext
{
    public PostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options)
    {

    }
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // options.UseNpgsql(options => options.UseAdminDatabase("postgres"));   
        // options.UseNpgsql("Host=127.0.0.1;Port=5432;Database=BLKCHAIN;Username=postgres;Password=drb;");
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

        ConfigueIndexes(modelBuilder);
    }

    private void ConfigureBlockEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Block>()
            .HasKey(b => b.BlockIndex);

    }

    private void ConfigureTransactionEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>()
            .HasKey(t => t._Id);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Block)
            .WithMany(b => b.Transactions)
            .HasForeignKey(t => t.BlockIndex);
    }

    private void ConfigureInputEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Input>()
        .HasKey(i => i._Id);

        modelBuilder.Entity<Input>()
            .HasOne<Transaction>(i => i.Transaction)
            .WithMany(t => t.Inputs)
            .HasForeignKey(i => i.TrxHash)
            .HasPrincipalKey(t => t.Txid);

        modelBuilder.Entity<Input>()
            .OwnsOne(i => i.Outpoint, outpoint =>
            {
                outpoint.Property(o => o.TransactionId).HasColumnName("TransactionId");
                outpoint.Property(o => o.OutputIndex).HasColumnName("OutputIndex");
                outpoint.HasIndex(o => o.TransactionId).HasMethod("hash");
            });

        //Todo -> EFcore doesnt support complex types in navigational properties, needs to be mitigated
        // modelBuilder.Entity<Input>()
        //     .HasOne<Output>()
        //     .WithMany()
        //     .HasForeignKey(i => new { i.Outpoint.TransactionId, i.Outpoint.OutputIndex })
        //     .HasPrincipalKey(o => new { o.Outpoint.TransactionId, o.Outpoint.OutputIndex });

    }

    private void ConfigureOutputEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Output>()
            .HasKey(o => o._Id);


        modelBuilder.Entity<Output>()
            .OwnsOne(i => i.Outpoint, outpoint =>
            {
                outpoint.Property(o => o.TransactionId).HasColumnName("TransactionId");
                outpoint.Property(o => o.OutputIndex).HasColumnName("OutputIndex");
                outpoint.HasIndex(o => o.TransactionId).HasMethod("hash");
            });

        //Todo -> EFcore doesnt support complex types in navigational properties, needs to be mitigated
        // modelBuilder.Entity<Output>()
        //     .HasOne<Transaction>(o => o.Transaction)
        //     .WithMany(t => t.Outputs)
        //     .HasForeignKey(o => o.Outpoint.TransactionId)
        //     .HasPrincipalKey(t => t.Txid);
    }
    private void ConfigureMempoolTransactionEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MempoolTransaction>()
            .HasKey(t => t._Id);
    }

    private void ConfigureMempoolInputEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MempoolInput>()
            .HasKey(i => i._Id);

        modelBuilder.Entity<MempoolInput>()
            .HasOne<MempoolTransaction>(i => i.Transaction)
            .WithMany(t => t.Inputs)
            .HasForeignKey(i => i.Txid)
            .HasPrincipalKey(t => t.TransactionId);
    }

    private void ConfigureMempoolOutputEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MempoolOutput>()
            .HasKey(o => o._Id);

        //Todo -> EFcore doesnt support complex types in navigational properties, needs to be mitigated
        // modelBuilder.Entity<MempoolOutput>()
        //     .HasOne<MempoolTransaction>(o => o.Transaction)
        //     .WithMany(t => t.Outputs)
        //     .HasForeignKey(o => o.outpoint.TransactionId)
        //     .HasPrincipalKey(t => t.TransactionId);
    }

    private void ConfigureUnspentOutputEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UnspentOutput>()
            .HasKey(uo => uo._Id);
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

    private void ConfigueIndexes(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<Block>()
            .HasIndex(b => b.BlockHash)
            .HasMethod("hash");

        modelBuilder.Entity<Block>()
            .HasIndex(b => b.BlockIndex)
            .IsDescending(true);

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.BlockIndex)
            .IsDescending(true);

        modelBuilder.Entity<Transaction>()
                    .HasIndex(t => t.Txid)
                    .IsUnique();
                    
        modelBuilder.Entity<Output>()
            .HasIndex(o => o.BlockIndex)
            .IsDescending(true);

        modelBuilder.Entity<Output>()
            .HasIndex(o => o.Address);

        modelBuilder.Entity<Input>()
            .HasIndex(i => i.Address);

        modelBuilder.Entity<Input>()
            .HasIndex(i => i.BlockIndex)
            .IsDescending(true);

        modelBuilder.Entity<UnspentOutput>()
            .HasIndex(uo => uo.Address);

        modelBuilder.Entity<UnspentOutput>()
            .HasIndex(uo => uo.BlockIndex);

        modelBuilder.Entity<AddressComputedEntry>()
            .HasIndex(e => e.Address);

        modelBuilder.Entity<AddressHistoryComputedEntry>()
            .HasIndex(e => e.BlockIndex)
            .IsDescending(true);

        modelBuilder.Entity<AddressHistoryComputedEntry>()
            .HasIndex(e => e.Position);

        modelBuilder.Entity<AddressHistoryComputedEntry>()
            .HasIndex(e => e.Address);

        modelBuilder.Entity<MempoolTransaction>()
            .HasIndex(m => m.TransactionId)
            .HasMethod("hash");

        modelBuilder.Entity<MempoolTransaction>()
            .HasIndex(m => m.AddressOutputs);

        modelBuilder.Entity<MempoolTransaction>()
            .HasIndex(m => m.AddressInputs);

    }

}