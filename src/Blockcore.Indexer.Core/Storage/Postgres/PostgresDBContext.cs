using System.Globalization;
using Blockcore.Indexer.Core.Storage.Postgres.Types;
using Blockcore.NBitcoin.Protocol;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<Block>()
        .HasKey(b => b.BlockHash);

        modelBuilder.Entity<Transaction>()
        .HasKey(t => t.Txid);

        modelBuilder.Entity<Input>()
        .HasKey(i => new { i.Txid, i.Vout });

        modelBuilder.Entity<Output>()
        .HasKey(o => new { o.outpoint.Txid, o.outpoint.Vout });

        modelBuilder.Entity<MempoolTransaction>()
        .HasKey(t => t.Txid);

        modelBuilder.Entity<MempoolInput>()
        .HasKey(i => new { i.Txid, i.Vout });

        modelBuilder.Entity<MempoolOutput>()
        .HasKey(o => new { o.outpoint.Txid, o.outpoint.Vout });

        modelBuilder.Entity<UnspentOutput>()
        .HasKey(uo => new { uo.Outpoint.Txid, uo.Outpoint.Vout });


        modelBuilder.Entity<Transaction>()
        .HasOne(t => t.Block)
        .WithMany(b => b.Transactions)
        .HasForeignKey(t => t.BlockIndex);

        modelBuilder.Entity<Input>()
        .HasOne<Transaction>(i => i.Transaction)
        .WithMany(t => t.Inputs)
        .HasForeignKey(i => i.Txid);

        modelBuilder.Entity<Input>()
        .HasOne<Transaction>(i => i.Transaction)
        .WithMany(t => t.Inputs)
        .HasForeignKey(i => i.outpoint.Txid);

        modelBuilder.Entity<Output>()
        .HasOne<Transaction>(o => o.Transaction)
        .WithMany(t => t.Outputs)
        .HasForeignKey(o => o.outpoint.Txid);

        modelBuilder.Entity<MempoolInput>()
        .HasOne<MempoolTransaction>(i => i.Transaction)
        .WithMany(t => t.Inputs)
        .HasForeignKey(i => i.Txid);

        modelBuilder.Entity<MempoolOutput>()
        .HasOne<MempoolTransaction>(o => o.Transaction)
        .WithMany(t => t.Outputs)
        .HasForeignKey(o => o.outpoint.Txid);

        // modelBuilder.Entity<UnspentOutput>() .HasOne(t => t.)

        modelBuilder.Entity<Block>()
        .HasIndex(b => b.BlockHash)
        .HasMethod("hash");

        modelBuilder.Entity<Block>()
        .HasIndex(b => b.BlockIndex)
        .IsDescending();

        modelBuilder.Entity<Transaction>()
        .HasIndex(t => t.Txid)
        .HasMethod("hash");
    }
}