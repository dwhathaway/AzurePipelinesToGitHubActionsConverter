using System.Collections.Generic;
using CommandLine;

namespace AzurePipelinesToGitHubActionsConverter.ConsoleApp.Commands.Options
{
    [Verb("extractAndConvert", HelpText = "Extract 1 or more pipelines from Azure DevOps and convert to GitHub Actions")]
    public class ExtractAndConvertOptions
    {
        [Option('a', "account", Required = true, HelpText = "The name of the Azure DevOps account")]
        public string Account { get; set; }

        [Option('p', "project", Required = true, HelpText = "The name of the Azure DevOps project")]
        public string Project { get; set; }

        [Option('t', "personalAccessToken", Required = true, HelpText = "The Personal Access Token (PAT) to authenticate with Azure DevOps ")]
        public string PersonalAccessToken { get; set; }

        [Option('i', "pipelineIds", Required = false, HelpText = "A list of specific Pipeline IDs to process")]
        public IEnumerable<long> PipelineIds { get; set; }

        [Option('y', "yamlFilename", Required = false, HelpText = "The name of a specific Pipeline Yaml file to process")]
        public string YamlFilename { get; set; }

        [Option('b', "baseUrl", Required = false, HelpText = "The base url of your Azure DevOps instance (normally dev.azure.com)")]
        public string BaseUrl { get; set; }

        [Option('r', "repositoryName", Required = false, HelpText = "Only process pipelines for a given repository name")]
        public string RepositoryName { get; set; }

        [Option('o', "outputFolder", Required = false, HelpText = "The folder path where the .github/workflows folder should be created")]
        public string OutputFolder { get; set; }

        [Option('x', "pipelineFolderName", Required = false, HelpText = "The folder that the Azure Pipeline is stored in (all others will be ignored)")]
        public string PipelineFolderName { get; set; }

        [Option('d', "includeIdInFilename", Required = false, Default = false, HelpText = "Whether or not to include the ID of the pipeline in the output file name - useful for processing large projects where multiple repos may reuse the same file name")]
        public bool? IncludeIdInFilename { get; set; }

        [Option('f', "addWorkflowTrigger", Required = false, Default = false, HelpText = "Whether or not to add a workflow_dispatch trigger to the resulting GitHub Actions workflow")]
        public bool? AddWorkflowTrigger { get; set; }

        [Option('k', "keyVaultName", Required = false, HelpText = "Name of an Azure Key Vault containing the secrets you wish to access")]
        public string KeyVaultName { get; set; }

        [Option('s', "keyVaultSecrets", Required = false, HelpText = "Comma delimited list of secrets to read from the supplied Key Vault name")]
        public string keyVaultSecrets { get; set; }
    }
}
