namespace Pathly_Core
{
    /// <summary>
    /// Configuration for Azure AI Foundry's Model Router.
    /// Bind this to an "AzureOpenAI" section in appsettings.json / user secrets.
    /// </summary>
    public class AzureFoundrySettings
    {
        /// <summary>
        /// The Azure OpenAI-compatible v1 endpoint for your Foundry resource,
        /// e.g. "https://eduhub-foundry.openai.azure.com/openai/v1"
        /// (found on the Foundry dashboard under "Azure OpenAI endpoint").
        /// Do NOT include a trailing slash or "/chat/completions" — the service adds that.
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// The API key from the Foundry dashboard ("API key" field).
        /// Store this in user secrets / environment variables, not in source control.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// The deployment name you gave the Model Router when you deployed it
        /// (check "View deployments" on the Foundry dashboard if unsure).
        /// This goes in the "model" field of every request.
        /// </summary>
        public string DeploymentName { get; set; } = string.Empty;
    }
}
