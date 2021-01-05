using System.Collections.Specialized;
using YamlDotNet.Serialization;

namespace AzurePipelinesToGitHubActionsConverter.Core.GitHubActions
{
    public class Step
    {
        public string name { get; set; }

        public string uses { get; set; }

        private string _run = null;

        public string run
        {
            get
            {
                return _run;
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _run = value;
                }
            }
        }

        public string shell { get; set; }

        public OrderedDictionary with { get; set; } // A key value pair similar to env

        public OrderedDictionary env { get; set; } // Similar to the job env: https://help.github.com/en/articles/workflow-syntax-for-github-actions#jobsjob_idenv

        public string id { get; set; }

        // as "if" is a reserved word in C#, added an "_", and remove this "_" when serializing
        public string _if { get; set; } // https://help.github.com/en/articles/workflow-syntax-for-github-actions#jobsjob_idif

        public bool continue_on_error { get; set; }

        public int timeout_minutes { get; set; }

        public string step_message;

        [YamlIgnore]
        public StepDependencies DependsOn { get; set; }

        public bool IsDependentOn(StepDependencies dependsOn)
        {
            return ((DependsOn & dependsOn) == dependsOn);
        }
    }
}
