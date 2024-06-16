namespace Blockcore.Indexer.Core.Storage.Postgres.Types
{
    public class Outpoint
    {
        public string Txid { get; set; }

        public int Vout { get; set; }

        public override string ToString()
        {
            return Txid + "-" + Vout;
        }
    }
}