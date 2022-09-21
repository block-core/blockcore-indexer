using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Blockcore.Indexer.Cirrus.Client.Types;
using Blockcore.Indexer.Cirrus.Models;
using Blockcore.Indexer.Cirrus.Storage;
using Blockcore.Indexer.Core.Storage.Types;
using Microsoft.AspNetCore.Mvc;

namespace Blockcore.Indexer.Cirrus.Controllers;

[ApiController]
[Route("unity3d")]
public class Unity3dController : Controller
{
   private readonly ICirrusStorage cirrusMongoData;

   public Unity3dController(ICirrusStorage cirrusMongoData)
   {
      this.cirrusMongoData = cirrusMongoData;
   }

   [HttpGet]
   [Route("get-owned-nfts")]
   public IActionResult GetAddressAssetsInUnity3dFormat([MinLength(30)][MaxLength(100)][FromQuery] string ownerAddress, [Range(0, 50)] int limit, [Range(0, long.MaxValue)] int? offset = 0)
   {
      QueryResult<QueryAddressAsset> nfts = cirrusMongoData.GetNonFungibleTokensForAddressAsync(ownerAddress, offset, limit).Result;

      Dictionary<string, List<int>> unityResponse = nfts.Items.GroupBy(_ => _.ContractId)
         .ToDictionary(_ => _.Key, _ => _.Select(k => Convert.ToInt32(k.Id)).ToList());

      return Ok(new Unity3dNftResponse { OwnedIDsByContractAddress = unityResponse });
   }
}
