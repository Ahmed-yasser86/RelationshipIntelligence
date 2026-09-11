using System;

namespace ServiceContracts.DTOs
{
    public class NetworkEdgeResponse
    {
        public Guid From { get; set; }
        public Guid To { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
