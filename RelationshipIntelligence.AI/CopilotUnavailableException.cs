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

    public static class CopilotErrors
    {
        /// <summary>
        /// Turns a model-call failure into a message the user can act on:
        /// a provider rejection names the status and points at Setup,
        /// anything else stays a connectivity problem.
        /// </summary>
        public static CopilotUnavailableException FromModelFailure(Exception ex)
        {
            if (ex is Microsoft.SemanticKernel.HttpOperationException http)
            {
                if (http.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    return new CopilotUnavailableException(
                        "The AI provider is rate-limiting requests right now. Wait a moment and try again.", ex);
                return new CopilotUnavailableException(
                    $"The AI provider rejected the request ({http.StatusCode}). " +
                    "Check the model id and API key in Setup, then try again.", ex);
            }
            return new CopilotUnavailableException(
                "The assistant could not reach the language model. Check provider settings and try again.", ex);
        }
    }
}
