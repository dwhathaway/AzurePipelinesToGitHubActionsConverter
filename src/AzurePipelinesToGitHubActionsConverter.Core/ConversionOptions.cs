namespace AzurePipelinesToGitHubActionsConverter.Core
{
    public static class ConversionOptions
    {
        public static string Account { get; set; }

        public static string RepositoryName { get; set; }

        public static bool AddWorkflowTrigger { get; set; }

        public static bool VariableCompatMode { get; set; }
    }
}