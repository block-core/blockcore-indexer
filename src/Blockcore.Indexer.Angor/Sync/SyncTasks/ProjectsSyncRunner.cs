using Blockcore.Consensus.ScriptInfo;
using Blockcore.Indexer.Angor.Networks;
using Blockcore.Indexer.Angor.Storage;
using Blockcore.Indexer.Angor.Storage.Mongo;
using Blockcore.Indexer.Angor.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Storage.Types;
using Blockcore.Indexer.Core.Sync.SyncTasks;
using Blockcore.NBitcoin;
using Blockcore.NBitcoin.BIP32;
using Blockcore.NBitcoin.Crypto;
using Blockcore.NBitcoin.DataEncoders;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Blockcore.Indexer.Angor.Sync.SyncTasks;

public class ProjectsSyncRunner : TaskRunner
{
   readonly IAngorStorage angorStorage;
   readonly IAngorMongoDb AngorMongoDb;
   ILogger<ProjectsSyncRunner> logger;

   readonly SyncConnection syncConnection;

   public static string AngorTestKey = "tpubD8JfN1evVWPoJmLgVg6Usq2HEW9tLqm6CyECAADnH5tyQosrL6NuhpL9X1cQCbSmndVrgLSGGdbRqLfUbE6cRqUbrHtDJgSyQEY2Uu7WwTL";
   public static string AngorMainKey = "xpub661MyMwAqRbcGNxKe9aFkPisf3h32gHLJm8f9XAqx8FB1Nk6KngCY8hkhGqxFr2Gyb6yfUaQVbodxLoC1f3K5HU9LM1CXE59gkEXSGCCZ1B";

   ExtPubKey extendedPublicKey;

   public ProjectsSyncRunner(IOptions<IndexerSettings> configuration, ILogger<ProjectsSyncRunner> logger, IAngorStorage angorStorage, IAngorMongoDb angorMongoDb, SyncConnection syncConnection)
      : base(configuration, logger)
   {
      this.logger = logger;
      this.angorStorage = angorStorage;
      AngorMongoDb = angorMongoDb;
      this.syncConnection = syncConnection;

      Delay = TimeSpan.FromMinutes(1);

      // use the rpc port but this is not a reliable way to determine mainnet
      if (syncConnection.RpcAccessPort == 8333) // mainnet
      {
         extendedPublicKey = new BitcoinExtPubKey(AngorMainKey, new BitcoinMain()).ExtPubKey;
      }
      else
      {
         extendedPublicKey = new BitcoinExtPubKey(AngorTestKey, new BitcoinSignet()).ExtPubKey;
      }
   }

   private bool CanRunProjectSync()
   {
      return !( //sync with other runners
         !Runner.GlobalState.IndexModeCompleted ||
         Runner.GlobalState.Blocked ||
         Runner.GlobalState.ReorgMode ||
         Runner.GlobalState.StoreTip == null ||
         Runner.GlobalState.IndexMode);
   }

   public override async Task<bool> OnExecute()
   {
      if (!CanRunProjectSync())
         return false;

      var blockIndexed = await AngorMongoDb.ProjectTable.AsQueryable().AnyAsync()
         ? AngorMongoDb.ProjectTable.AsQueryable().Max(p => p.BlockIndex)
         : 0;

      var investmentslookup = AngorMongoDb.OutputTable.AsQueryable()
         .Where(_ => _.BlockIndex > blockIndexed &&
                     _.Address == "TX_NULL_DATA" &&
                     _.Outpoint.OutputIndex == 1 &&
                     _.CoinBase == false) // &&
                     //_.ScriptHex.Length == 136)
         .OrderBy(_ => _.BlockIndex)
         .ToList();

      var projectTasks = investmentslookup.Select(CheckAndAddProjectAsync).ToList();

      await Task.WhenAll(projectTasks);

      var projects = projectTasks
         .Where(x => x.Result != null)
         .Select(x => x.Result!)
         .ToList();

      if (projects.Any())
      {
         await AngorMongoDb.ProjectTable.InsertManyAsync(projects);
      }

      return false;
   }

   async Task<Project?> CheckAndAddProjectAsync(OutputTable output)
   {
      var script = Script.FromHex(output.ScriptHex);

      var parsedData = new DataFromOps();

      if (!parsedData.TryParse(script.ToOps()))
         return null;

      var checkForExistingProject = await AngorMongoDb.ProjectTable.AsQueryable()
         .AnyAsync(_ => _.FounderKey == parsedData.FounderPubKey.ToHex());

      if (checkForExistingProject) return null;

      var projectId = GetProjectIdDerivation(parsedData.FounderPubKey);

      if (projectId == 0)
         return null;

      var angorKey = extendedPublicKey.Derive(projectId).PubKey;

      var encoder = new Bech32Encoder("angor");

      var projectIdentifier = encoder.Encode(0, angorKey.WitHash.ToBytes());

      var angorFeeOutput = await AngorMongoDb.OutputTable
         .AsQueryable()
         .Where(_ =>
            //Outpoint with both parameters is the id of the table
            _.Outpoint == new Outpoint{TransactionId = output.Outpoint.TransactionId , OutputIndex = 0} &&
                                         //direct lookup for the exiting key
                                         _.ScriptHex == angorKey.WitHash.ScriptPubKey.ToHex())
         .FirstOrDefaultAsync();

      if (angorFeeOutput == null) return null;

      return new Project
      {
         AngorKey = projectIdentifier,
         TransactionId = output.Outpoint.TransactionId,
         AngorKeyScriptHex = angorKey.WitHash.ScriptPubKey.ToHex(),
         BlockIndex = output.BlockIndex,
         FounderKey = parsedData.FounderPubKey.ToHex(),
         NostrEventId = parsedData.keyType == 1 ? parsedData.NostrEventId : string.Empty,
         AddressOnFeeOutput = angorFeeOutput.Address
      };
   }

   private class DataFromOps
   {
      public PubKey FounderPubKey { get; set; }
      public short keyType { get; set; }
      public string NostrEventId { get; set; }

      public bool TryParse(IList<Op> ops)
      {
         if (ops.Count != 4 ||
             ops.First().Name != Op.GetOpName(OpcodeType.OP_RETURN) ||
             ops[1].PushData.Length != 33 ||
             ops[2].PushData.Length != 2 ||
             ops[3].PushData.Length != 32)
            return false;

         FounderPubKey = new PubKey(ops[1].PushData);
         keyType = BitConverter.ToInt16(ops[2].PushData);
         NostrEventId = Encoders.Hex.EncodeData(ops[3].PushData);
         return true;
      }
   }

   private uint GetProjectIdDerivation(PubKey founderKey)
   {
      ExtKey.UseBCForHMACSHA512 = true;
      Hashes.UseBCForHMACSHA512 = true;

      var hashOfid = Hashes.Hash256(founderKey.ToBytes());

      return (uint)(hashOfid.GetLow64() & int.MaxValue);
   }
}
