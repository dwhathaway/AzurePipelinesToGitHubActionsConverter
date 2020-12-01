using System;

namespace AzurePipelinesToGitHubActionsConverter.Core.GitHubActions
{
    [Flags]
    public enum StepDependencies
    {
        None = 0,
        AzureLogin = 1,
        JavaSetup = 2,
        GradleSetup = 4,
        MSBuildSetup = 8
    }
}
