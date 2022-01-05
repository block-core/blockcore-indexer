namespace Blockcore.Indexer.Core.Sync.SyncTasks
{
   public interface IBlockableItem
   {
      bool Blocked { get; set; }

      void Deplete();
   }
}
