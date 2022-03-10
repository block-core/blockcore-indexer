namespace Blockcore.Indexer.Cirrus.Storage.Mongo.SmartContracts.Dao;

public interface ILogReaderFactory
{
   ILogReader GetLogReader(string opCode,string methodName);
}
