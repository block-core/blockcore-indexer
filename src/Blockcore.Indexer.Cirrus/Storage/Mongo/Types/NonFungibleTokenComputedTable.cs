using System.Collections.Generic;

namespace Blockcore.Indexer.Cirrus.Storage.Mongo.Types;

public class NonFungibleTokenComputedTable : SmartContractComputedBase
{
   public string Name { get; set; }
   public string Symbol { get; set; }

   public string Owner { get; set; }
   public override string ContractType { get; } = "NonFungibleToken";


   public List<NonFungibleToken> Tokens { get; set; }
   public bool OwnerOnlyMinting { get; set; }
   public string PendingOwner { get; set; }

   public List<string> PreviousOwners { get; set; }

   List<NonFungibleToken> _newTokens;
   public List<NonFungibleToken> GetNewTokens() => _newTokens;
   public void AddNewToken(NonFungibleToken token) => _newTokens.Add(token);
   List<NonFungibleToken> updatedTokens;
   public List<NonFungibleToken> GetUpdatedTokens() => updatedTokens;
   public void GetTokenToUpdate(NonFungibleToken token) => _newTokens.Add(token);
}
