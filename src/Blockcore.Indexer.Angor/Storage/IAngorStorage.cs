using Blockcore.Indexer.Angor.Operations.Types;
using Blockcore.Indexer.Core.Storage.Types;

namespace Blockcore.Indexer.Angor.Storage;

public interface IAngorStorage
{
   Task<ProjectIndexerData?> GetProjectAsync(string projectId);

   Task<QueryResult<ProjectIndexerData>> GetProjectsAsync(int? offset, int limit);

   Task<QueryResult<ProjectInvestment>> GetProjectInvestmentsAsync(string projectId, int? offset, int limit);

   Task<ProjectInvestment> GetInvestmentsByInvestorPubKeyAsync(string projectId);
}
