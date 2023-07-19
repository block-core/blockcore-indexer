using Blockcore.Indexer.Angor.Operations.Types;
using Blockcore.Indexer.Core.Storage.Types;

namespace Blockcore.Indexer.Angor.Storage;

public interface IAngorStorage
{
   Task<ProjectIndexerData?> GetProjectAsync(string projectId);

   Task<QueryResult<ProjectIndexerData>> GetProjectsAsync(int? offset, int limit);
}
