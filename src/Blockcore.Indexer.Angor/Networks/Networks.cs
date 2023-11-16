using Blockcore.Networks;

namespace Blockcore.Indexer.Angor.Networks
{
    public static class Networks
    {
        public static NetworksSelector Bitcoin
        {
            get
            {
                return new NetworksSelector(() => new BitcoinMain(), () => new BitcoinTest(), () => null);
            }
        }

     
    }
}