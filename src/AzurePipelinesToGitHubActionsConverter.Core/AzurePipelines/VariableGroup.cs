using System.Collections.Generic;

namespace AzurePipelinesToGitHubActionsConverter.Core.AzurePipelines
{
    public class VariableGroup
    {
        public const string KeyVaultGroupType = "AzureKeyVault";

        public string name { get; set; }

        public string description { get; set; }

        public string type { get; set; }

        public Dictionary<string, Variable> variables;
    }
}
