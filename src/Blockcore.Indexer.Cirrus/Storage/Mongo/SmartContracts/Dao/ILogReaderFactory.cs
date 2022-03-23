namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

public interface ILogReaderFactory
{
   ILogReader GetLogReader(string methodName);
}
