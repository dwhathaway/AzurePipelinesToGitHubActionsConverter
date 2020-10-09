using YamlDotNet.Serialization;

namespace AzurePipelinesToGitHubActionsConverter.Core.GitHubActions
{
    public class WorkflowDispatchTrigger : Trigger
    {
        //on:
        //  workflow_dispatch:
        //  push:
        //    branches:    
        //    - Master
        //    - Develop
        //    branches-ignore:
        //    - 'mona/octocat'
        //    paths:
        //    - '**.js'
        //    paths-ignore:
        //    - 'docs/**'
        //  pull-request
        //    branches:    
        //    - Master
        //    - Develop
        //    branches-ignore:
        //    - 'mona/octocat'
        //    paths:
        //    - '**.js'
        //    paths-ignore:
        //    - 'docs/**'
        //    tags:        
        //    - v1             # Push events to v1 tag
        //    - v1.*           # Push events to v1.0, v1.1, and v1.9 tags
        //  schedule:
        //  - cron:  '*/15 * * * *' # * is a special character in YAML so you have to quote this string


        // DefaultValuesHandling.

        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.Preserve)]
        public object workflow_dispatch { get; set; }

        public WorkflowDispatchTrigger(Trigger trigger)
        {
            this.pull_request = trigger.pull_request;
            this.push = trigger.push;
            this.schedule = trigger.schedule;
        }
    }
}