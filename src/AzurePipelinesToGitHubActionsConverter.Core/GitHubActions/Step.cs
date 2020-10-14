using System.Collections.Generic;
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
                if (string.IsNullOrEmpty(value) == false)
                {
                    //Spaces on the beginning or end seem to be a problem for the YAML serialization, so we Trim() here
                    //  Also, accidental carriage returns in scripts (such as a path including a \r) need to be accounted for
                    // If this script step includes escaped carriage returns (\\r), switch these to "\\\\r" so that we don't accidentally improperly match these as CRs; we'll fix these up later when we serialize
                    value = value.Replace("\\r", "\\\\r").Trim();
                }

                _run = value;
            }
        }
        public string shell { get; set; }
        public Dictionary<string, string> with { get; set; } //A key value pair similar to env
        public Dictionary<string, string> env { get; set; } //Similar to the job env: https://help.github.com/en/articles/workflow-syntax-for-github-actions#jobsjob_idenv
        //as "if" is a reserved word in C#, added an "_", and remove this "_" when serializing
        public string _if { get; set; } //https://help.github.com/en/articles/workflow-syntax-for-github-actions#jobsjob_idif
        public bool continue_on_error { get; set; }
        public int timeout_minutes { get; set; }

        //This is used for tracking errors, so we don't want it to convert to YAML
        //[YamlIgnore]
        public string step_message;
    }
}
