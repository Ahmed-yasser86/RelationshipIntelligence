using ContactsManager.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using ServiceContracts;
using System.Threading.Tasks;

namespace RelationshipIntelligence.Api.Controllers
{
    public class NetworkController : CustomWebController
    {
        private readonly INetworkAnalysisService _network;

        public NetworkController(INetworkAnalysisService network)
        {
            _network = network;
        }

        [HttpGet]
        public async Task<IActionResult> GetNetworkGraph()
        {
            return Ok(await _network.GetGraphAsync());
        }
    }
}
