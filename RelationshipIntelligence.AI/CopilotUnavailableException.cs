using System;

namespace RelationshipIntelligence.AI
{
    public sealed class CopilotUnavailableException : InvalidOperationException
    {
        public CopilotUnavailableException(string message, Exception? inner = null)
            : base(message, inner)
        {
        }
    }
}
