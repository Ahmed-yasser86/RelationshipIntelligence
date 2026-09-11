using System.Collections.Generic;

namespace ServiceContracts.DTOs
{
    public class NetworkGraphResponse
    {
        public List<NetworkNodeResponse> Nodes { get; set; } = new();
        public List<NetworkEdgeResponse> Edges { get; set; } = new();
        public int ClusterCount { get; set; }
    }
}
