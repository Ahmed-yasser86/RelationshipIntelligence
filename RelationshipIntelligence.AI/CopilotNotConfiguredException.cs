using System;

namespace RelationshipIntelligence.AI
{
    public sealed class CopilotNotConfiguredException : InvalidOperationException
    {
        public CopilotNotConfiguredException()
            : base("No AI provider is configured. Open the co-pilot settings and choose a provider, model, and API key first.")
        {
        }
    }
}
