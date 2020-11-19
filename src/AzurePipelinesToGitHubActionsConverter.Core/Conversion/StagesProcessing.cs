using AzurePipelinesToGitHubActionsConverter.Core.AzurePipelines;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace AzurePipelinesToGitHubActionsConverter.Core.Conversion
{
    public class StagesProcessing
    {
        private readonly bool _verbose;
        private readonly VariablesProcessing _variableProcessing;

        public StagesProcessing(VariablesProcessing vp, bool verbose = true)
        {
            _variableProcessing = vp;
            _verbose = verbose;
        }

        public Dictionary<string, GitHubActions.Job> ProcessStagesV2(JToken stagesJson, string strategyYaml)
        {
            AzurePipelines.Job[] jobs = null;
            var stages = new List<AzurePipelines.Stage>();

            if (stagesJson != null)
            {
                // for each stage
                foreach (JToken stageJson in stagesJson)
                {
                    var stage = new AzurePipelines.Stage
                    {
                        stage = stageJson["stage"]?.ToString(),
                        displayName = stageJson["displayName"]?.ToString(),
                        condition = stageJson["condition"]?.ToString()
                    };

                    if (stageJson["dependsOn"] != null)
                    {
                        var gp = new GeneralProcessing(_verbose);
                        stage.dependsOn = gp.ProcessDependsOnV2(stageJson["dependsOn"].ToString());
                    }

                    if (stageJson["variables"] != null)
                    {
                        stage.variables = _variableProcessing.ProcessParametersAndVariablesV2(null, stageJson["variables"].ToString());
                    }

                    if (stageJson["jobs"] != null)
                    {
                        var jp = new JobProcessing(_variableProcessing, _verbose);
                        stage.jobs = jp.ExtractAzurePipelinesJobsV2(stageJson["jobs"], strategyYaml);
                    }

                    stages.Add(stage);
                }

                // process the jobs
                if (stages != null)
                {
                    int jobCount = 0;

                    foreach (Stage stage in stages)
                    {
                        if (stage.jobs != null)
                        {
                            jobCount += stage.jobs.Length;
                        }
                    }

                    jobs = new AzurePipelines.Job[jobCount];

                    // Giant nested loop ahead. Loop through stages, looking for all jobs
                    int jobIndex = 0;

                    foreach (Stage stage in stages)
                    {
                        if (stage.jobs != null)
                        {
                            for (int i = 0; i < stage.jobs.Length; i++)
                            {
                                jobs[jobIndex] = stage.jobs[i];

                                // propagate stage dependency to the Job level
                                if (stage.dependsOn?.Length > 0)
                                {
                                    var dependsOnJobs = stages.Where(s => stage.dependsOn.Contains(s.stage)).Select(s => s.jobs.Last());
                                    jobs[jobIndex].dependsOn = dependsOnJobs.Select(j => j.job).ToArray();
                                }

                                if (stage.variables != null)
                                {
                                    if (jobs[jobIndex].variables == null)
                                    {
                                        jobs[jobIndex].variables = new OrderedDictionary();
                                    }

                                    foreach (DictionaryEntry stageVariable in stage.variables)
                                    {
                                        // Add the stage variable if it doesn't already exist
                                        if (!jobs[jobIndex].variables.Contains(stageVariable.Key))
                                        {
                                            jobs[jobIndex].variables.Add(stageVariable.Key, stageVariable.Value);
                                        }
                                    }
                                }

                                if (stage.condition != null)
                                {
                                    jobs[jobIndex].condition = stage.condition;
                                }

                                // Get the job name
                                string jobName = ConversionUtility.GenerateJobName(stage.jobs[i], jobIndex);
                                // Rename the job, using the stage name as prefix, so that we keep the job names unique
                                jobs[jobIndex].job = stage.stage + "_Stage_" + jobName;
                                jobIndex++;
                            }
                        }
                    }
                }
            }

            // Build the final list of GitHub jobs and return it
            if (jobs != null)
            {
                var gitHubJobs = new Dictionary<string, GitHubActions.Job>();

                foreach (var job in jobs)
                {
                    var jobProcessing = new JobProcessing(_variableProcessing, _verbose);
                    gitHubJobs.Add(job.job, jobProcessing.ProcessJob(job, null));
                }

                return gitHubJobs;
            }

            return null;
        }
    }
}
