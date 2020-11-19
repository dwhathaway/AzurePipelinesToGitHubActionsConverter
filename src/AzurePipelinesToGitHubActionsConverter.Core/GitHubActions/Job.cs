using System.Collections.Generic;
using System.Collections.Specialized;

namespace AzurePipelinesToGitHubActionsConverter.Core.GitHubActions
{
    public class Job
    {
        public string name { get; set; } //https://help.github.com/en/articles/workflow-syntax-for-github-actions#jobsjob_idname

        public string runs_on { get; set; } //https://help.github.com/en/articles/workflow-syntax-for-github-actions#jobsjob_idruns-on

        public Strategy strategy { get; set; } //https://help.github.com/en/articles/workflow-syntax-for-github-actions#jobsjob_idstrategy

        public Container container { get; set; } //https://docs.microsoft.com/en-us/azure/devops/pipelines/yaml-schema?view=azure-devops&tabs=schema#job

        //public string container { get; set; } //https://docs.microsoft.com/en-us/azure/devops/pipelines/yaml-schema?view=azure-devops&tabs=schema#job

        public int timeout_minutes { get; set; } //https://help.github.com/en/articles/workflow-syntax-for-github-actions#jobsjob_idtimeout-minutes

        public string[] needs { get; set; } //https://help.github.com/en/articles/workflow-syntax-for-github-actions#jobsjob_idneeds

        public OrderedDictionary env { get; set; } //https://help.github.com/en/articles/workflow-syntax-for-github-actions#jobsjob_idenv

        //as "if" is a reserved word in C#, added an "_", and remove this "_" when serializing
        public string _if { get; set; } //https://help.github.com/en/articles/workflow-syntax-for-github-actions#jobsjob_idif

        public bool continue_on_error { get; set; }

        public List<Step> steps { get; set; } //https://help.github.com/en/articles/workflow-syntax-for-github-actions#jobsjob_idsteps

        public string job_message;
    }
}
