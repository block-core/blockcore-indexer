using Blockcore.Consensus.ScriptInfo;
using Blockcore.Indexer.Angor.Networks;
using Blockcore.Indexer.Angor.Storage;
using Blockcore.Indexer.Angor.Storage.Mongo;
using Blockcore.Indexer.Angor.Storage.Mongo.Types;
using Blockcore.Indexer.Core.Operations.Types;
using Blockcore.Indexer.Core.Settings;
using Blockcore.Indexer.Core.Storage.Mongo.Types;
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

      var ops = script.ToOps();

      if (ops.Count != 3 ||
          ops.First().Name != Op.GetOpName(OpcodeType.OP_RETURN) ||
          ops[1].PushData.Length != 33 ||
          ops[2].PushData.Length != 32)
         return null;

      var founderKey = new PubKey(script.ToOps()[1].PushData);

      var checkForExistingProject = await AngorMongoDb.ProjectTable.AsQueryable()
         .AnyAsync(_ => _.FounderKey == founderKey.ToHex());

      if (checkForExistingProject) return null;

      var projectId = GetProjectIdDerivation(founderKey.ToHex());

      if (projectId == 0)
         return null;

      var angorKey = extendedPublicKey.Derive(projectId).PubKey;

      var encoder = new Bech32Encoder("angor");

      var projectIdentifier = encoder.Encode(0, angorKey.WitHash.ToBytes());

      var nPubKey = Encoders.Hex.EncodeData(script.ToOps()[2].PushData); //Schnorr signature not supported

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
         FounderKey = founderKey.ToHex(),
         NPubKey = nPubKey,
         AddressOnFeeOutput = angorFeeOutput.Address
      };
   }

   private uint GetProjectIdDerivation(string founderKey)
   {
      ExtKey.UseBCForHMACSHA512 = true;
      Hashes.UseBCForHMACSHA512 = true;

      var key = new PubKey(founderKey);

      var hashOfid = Hashes.Hash256(key.ToBytes());

      var projectid = hashOfid.GetLow32();

      var ret = projectid / 2; // the max size of bip32 derivation range is 2,147,483,648 (2^31) the max number of uint is 4,294,967,295 so we must divide by zero

      if (ret > int.MaxValue)
         throw new Exception();

      return ret;
   }
}
