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

   public ProjectsSyncRunner(IOptions<IndexerSettings> configuration, ILogger<ProjectsSyncRunner> logger, IAngorStorage angorStorage, IAngorMongoDb angorMongoDb, SyncConnection syncConnection)
      : base(configuration, logger)
   {
      this.logger = logger;
      this.angorStorage = angorStorage;
      AngorMongoDb = angorMongoDb;
      this.syncConnection = syncConnection;
   }

   public override async Task<bool> OnExecute()
   {

      var test = AngorMongoDb.OutputTable.AsQueryable()
         .Where(_ => _.Outpoint.OutputIndex == 1 &&
                     _.Address == "TX_NULL_DATA" &&
                     _.CoinBase == false)
         .ToList();

      var tasks = test.Select(CheckAndAddProjectAsync);

      await Task.WhenAll(tasks);

      return false;
   }

   async Task CheckAndAddProjectAsync(OutputTable output)
   {
      var script = Script.FromHex(output.ScriptHex);

      if (script.Length != 35 || script.ToOps().Count != 2)
         return;

      var extKey = new BitcoinExtPubKey(AngorTestKey, new BitcoinSignet()).ExtPubKey;

      var founderKey = new PubKey(script.ToOps().Last().PushData);

      var projectid = GetProjectIdDerivation(founderKey.ToHex());

      if (projectid == 0)
         return;

      var angorKey = extKey.Derive(projectid).PubKey;

      var checkForExistingProject = await AngorMongoDb.ProjectTable.AsQueryable()
         .AnyAsync(_ => _.AngorKey == angorKey.ToHex());

      if (checkForExistingProject) return;

      var verifyAngorKeyOutputExists = await AngorMongoDb.OutputTable
         .AsQueryable()
         .Where(_ =>
            //Outpoint with both parameters is the id of the table
            _.Outpoint.TransactionId == output.Outpoint.TransactionId &&
            _.Outpoint.OutputIndex == 0 &&
            //direct lookup for the exiting key
            _.ScriptHex == angorKey.WitHash.ScriptPubKey.ToHex())
         .AnyAsync();

      if (!verifyAngorKeyOutputExists) return;

      await AngorMongoDb.ProjectTable.InsertOneAsync(new Project
      {
         TransactionId = output.Outpoint.TransactionId,
         AngorKey = angorKey.ToHex(),
         BlockIndex = output.BlockIndex,
         FounderKey = founderKey.ToHex()
      });
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
