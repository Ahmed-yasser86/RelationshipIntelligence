using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ServiceContracts;
using System;
using System.Threading.Tasks;

namespace RelationshipIntelligence.AI
{
    /// <summary>
    /// Builds a Semantic Kernel instance from the current user's provider settings.
    /// One OpenAI-compatible connector covers OpenAI, Gemini, and custom endpoints:
    /// only the base URL, model, and key differ.
    /// </summary>
    public sealed class KernelFactory
    {
        private readonly IAiProviderSettingsService _settingsService;

        public KernelFactory(IAiProviderSettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public async Task<Kernel> CreateAsync(RelationshipPlugin? plugin = null)
        {
            var settings = await _settingsService.GetAsync();
            var apiKey = await _settingsService.GetUnprotectedKeyAsync();

            if (string.IsNullOrWhiteSpace(settings.Model) || string.IsNullOrWhiteSpace(apiKey))
                throw new CopilotNotConfiguredException();

            var endpoint = string.IsNullOrWhiteSpace(settings.BaseUrl)
                ? "https://api.openai.com/v1"
                : settings.BaseUrl.Trim().TrimEnd('/');

            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(
                modelId: settings.Model.Trim(),
                endpoint: new Uri(endpoint),
                apiKey: apiKey);

            var kernel = builder.Build();
            if (plugin != null)
                kernel.Plugins.AddFromObject(plugin, "relationships");

            return kernel;
        }
    }
}
