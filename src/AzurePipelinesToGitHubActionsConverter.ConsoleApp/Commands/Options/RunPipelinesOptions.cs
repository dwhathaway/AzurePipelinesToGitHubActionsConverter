using System.Collections.Generic;
using CommandLine;

namespace AzurePipelinesToGitHubActionsConverter.ConsoleApp.Commands.Options
{
    [Verb("runPipelines", HelpText = "Execute a build against 1 or more existing pipelines")]
    public class RunPipelinesOptions
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
        public string? YamlFilename { get; set; }

        [Option('b', "branchName", Required = false, HelpText = "The name of the branch that the pipeline should be run using")]
        public string? BranchName { get; set; }

        [Option('u', "baseUrl", Required = false, HelpText = "The base url of your Azure DevOps instance (normally dev.azure.com)")]
        public string? BaseUrl { get; set; }

        [Option('r', "repositoryName", Required = false, HelpText = "Only process pipelines for a given repository name")]
        public string? RepositoryName { get; set; }

        [Option('f', "pipelineFolderName", Required = false, HelpText = "The folder that the Azure Pipeline is stored in (all others will be ignored)")]
        public string? PipelineFolderName { get; set; }

        [Option('v', "variables", Required = false, HelpText = "A list of pipeline variables to use when running the pipeline")]
        public IEnumerable<string> Variables { get; set; }

        [Option('z', "previewRun", Required = false, Default = false, HelpText = "Whether or not to preview a run")]
        public bool PreviewRun { get; set; }

        [Option('y', "agentPoolName", Required = false, HelpText = "The name of the agent pool that the job should be run against")]
        public string AgentPoolName { get; set; }
    }
}