﻿using AzurePipelinesToGitHubActionsConverter.Core.AzurePipelines;
using AzurePipelinesToGitHubActionsConverter.Core.GitHubActions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace AzurePipelinesToGitHubActionsConverter.Core.Conversion
{
    public class PipelineProcessing<TTriggers, TVariables>
    {
        public List<string> VariableList;
        public string MatrixVariableName;
        private readonly bool _verbose;
        private readonly List<VariableGroup> _variableGroups;
        private readonly bool _addWorkflowTrigger;

        public PipelineProcessing(List<VariableGroup> variableGroups, bool verbose = true, bool? addWorkflowTrigger = null)
        {
            _variableGroups = variableGroups;
            _verbose = verbose;
            _addWorkflowTrigger = addWorkflowTrigger ?? false;
        }

        /// <summary>
        /// Process an Azure DevOps Pipeline, converting it to a GitHub Action
        /// </summary>
        /// <param name="azurePipeline">Azure DevOps Pipeline object</param>
        /// <param name="simpleTrigger">When the YAML has a simple trigger, (String[]). Can be null</param>
        /// <param name="complexTrigger">When the YAML has a complex trigger. Can be null</param>
        /// <returns>GitHub Actions object</returns>
        public GitHubActionsRoot ProcessPipeline(AzurePipelinesRoot<TTriggers, TVariables> azurePipeline,
            string[] simpleTrigger, AzurePipelines.Trigger complexTrigger,
            OrderedDictionary simpleVariables, AzurePipelines.Variable[] complexVariables)
        {
            VariableList = new List<string>();
            var generalProcessing = new GeneralProcessing(_verbose);
            var gitHubActions = new GitHubActionsRoot();

            // Name
            if (azurePipeline.name != null)
            {
                gitHubActions.name = azurePipeline.name;
            }

            // Container
            if (azurePipeline.container != null)
            {
                gitHubActions.messages.Add("TODO: Container conversion not yet done, we need help!: https://github.com/samsmithnz/AzurePipelinesToGitHubActionsConverter/issues/39");
            }

            // Triggers for pushs 
            var tp = new TriggerProcessing(_verbose);
            
            if (azurePipeline.trigger != null)
            {
                if (complexTrigger != null)
                {
                    gitHubActions.on = tp.ProcessComplexTrigger(complexTrigger);
                }
                else if (simpleTrigger != null)
                {
                    gitHubActions.on = tp.ProcessSimpleTrigger(simpleTrigger);
                }
            }

            // Triggers for pull requests
            if (azurePipeline.pr != null)
            {
                GitHubActions.Trigger pr = tp.ProcessPullRequest(azurePipeline.pr);

                if (gitHubActions.on == null)
                {
                    gitHubActions.on = pr;
                }
                else
                {
                    gitHubActions.on.pull_request = pr.pull_request;
                }
            }

            // pool/demands
            if (azurePipeline.pool != null && azurePipeline.pool.demands != null)
            {
                gitHubActions.messages.Add("Note: GitHub Actions does not have a 'demands' command on 'runs-on' yet");
            }

            // schedules
            if (azurePipeline.schedules != null)
            {
                string[] schedules = tp.ProcessSchedules(azurePipeline.schedules);

                if (gitHubActions.on == null)
                {
                    gitHubActions.on = new GitHubActions.Trigger();
                }

                gitHubActions.on.schedule = schedules;
            }

            // workflow_dispatch trigger
            if (_addWorkflowTrigger)
            {
                gitHubActions.on = new WorkflowDispatchTrigger(gitHubActions.on);
            }

            // Resources
            if (azurePipeline.resources != null)
            {
                // Note: Containers is in the jobs - this note should be removed once pipeliens and repositories is moved too

                // TODO: There is currently no conversion path for pipelines
                if (azurePipeline.resources.pipelines != null)
                {
                    gitHubActions.messages.Add("TODO: Resource pipelines conversion not yet done: https://github.com/samsmithnz/AzurePipelinesToGitHubActionsConverter/issues/8");
                    
                    if (azurePipeline.resources.pipelines.Length > 0)
                    {
                        if (azurePipeline.resources.pipelines[0].pipeline != null)
                        {
                            ConversionUtility.WriteLine("pipeline: " + azurePipeline.resources.pipelines[0].pipeline, _verbose);
                        }

                        if (azurePipeline.resources.pipelines[0].project != null)
                        {
                            ConversionUtility.WriteLine("project: " + azurePipeline.resources.pipelines[0].project, _verbose);
                        }

                        if (azurePipeline.resources.pipelines[0].source != null)
                        {
                            ConversionUtility.WriteLine("source: " + azurePipeline.resources.pipelines[0].source, _verbose);
                        }

                        if (azurePipeline.resources.pipelines[0].branch != null)
                        {
                            ConversionUtility.WriteLine("branch: " + azurePipeline.resources.pipelines[0].branch, _verbose);
                        }

                        if (azurePipeline.resources.pipelines[0].version != null)
                        {
                            ConversionUtility.WriteLine("version: " + azurePipeline.resources.pipelines[0].version, _verbose);
                        }

                        if (azurePipeline.resources.pipelines[0].trigger != null)
                        {
                            if (azurePipeline.resources.pipelines[0].trigger.autoCancel)
                            {
                                ConversionUtility.WriteLine("autoCancel: " + azurePipeline.resources.pipelines[0].trigger.autoCancel, _verbose);
                            }

                            if (azurePipeline.resources.pipelines[0].trigger.batch)
                            {
                                ConversionUtility.WriteLine("batch: " + azurePipeline.resources.pipelines[0].trigger.batch, _verbose);
                            }
                        }
                    }
                }

                // TODO: There is currently no conversion path for repositories
                if (azurePipeline.resources.repositories != null)
                {
                    gitHubActions.messages.Add("TODO: Resource repositories conversion not yet done: https://github.com/samsmithnz/AzurePipelinesToGitHubActionsConverter/issues/8");

                    if (azurePipeline.resources.repositories.Length > 0)
                    {
                        if (azurePipeline.resources.repositories[0].repository != null)
                        {
                            ConversionUtility.WriteLine("repository: " + azurePipeline.resources.repositories[0].repository, _verbose);
                        }

                        if (azurePipeline.resources.repositories[0].type != null)
                        {
                            ConversionUtility.WriteLine("type: " + azurePipeline.resources.repositories[0].type, _verbose);
                        }

                        if (azurePipeline.resources.repositories[0].name != null)
                        {
                            ConversionUtility.WriteLine("name: " + azurePipeline.resources.repositories[0].name, _verbose);
                        }

                        if (azurePipeline.resources.repositories[0]._ref != null)
                        {
                            ConversionUtility.WriteLine("ref: " + azurePipeline.resources.repositories[0]._ref, _verbose);
                        }

                        if (azurePipeline.resources.repositories[0].endpoint != null)
                        {
                            ConversionUtility.WriteLine("endpoint: " + azurePipeline.resources.repositories[0].endpoint, _verbose);
                        }

                        if (azurePipeline.resources.repositories[0].connection != null)
                        {
                            ConversionUtility.WriteLine("connection: " + azurePipeline.resources.repositories[0].connection, _verbose);
                        }

                        if (azurePipeline.resources.repositories[0].source != null)
                        {
                            ConversionUtility.WriteLine("source: " + azurePipeline.resources.repositories[0].source, _verbose);
                        }
                    }
                }
            }

            // Stages (Note: stages are not yet present in actions, we are merging them into one giant list of jobs, appending the stage name to jobs to keep names unique)
            if (azurePipeline.stages != null)
            {
                // Count the number of jobs and initialize the jobs array with that number
                int jobCounter = 0;

                foreach (Stage stage in azurePipeline.stages)
                {
                    if (stage.jobs != null)
                    {
                        jobCounter += stage.jobs.Length;
                    }
                }

                azurePipeline.jobs = new AzurePipelines.Job[jobCounter];
                // We are going to take each stage and assign it a set of jobs
                int currentIndex = 0;

                foreach (Stage stage in azurePipeline.stages)
                {
                    if (stage.jobs != null)
                    {
                        int j = 0;

                        for (int i = 0; i < stage.jobs.Length; i++)
                        {
                            // Get the job name
                            string jobName = ConversionUtility.GenerateJobName(stage.jobs[i], currentIndex);
                            // Rename the job, using the stage name as prefix, so that we keep the job names unique
                            stage.jobs[j].job = stage.stage + "_Stage_" + jobName;
                            ConversionUtility.WriteLine("This variable is not needed in actions: " + stage.displayName, _verbose);
                            azurePipeline.jobs[currentIndex] = stage.jobs[j];
                            azurePipeline.jobs[currentIndex].condition = stage.condition;

                            // Move over the variables, the stage variables will need to be applied to each job
                            if (stage.variables != null && stage.variables.Count > 0)
                            {
                                azurePipeline.jobs[currentIndex].variables = new OrderedDictionary();

                                foreach (DictionaryEntry stageVariable in stage.variables)
                                {
                                    azurePipeline.jobs[currentIndex].variables.Add(stageVariable.Key, stageVariable.Value);
                                }
                            }

                            j++;
                            currentIndex++;
                        }
                    }
                }
            }

            // Jobs (when no stages are defined)
            if (azurePipeline.jobs != null)
            {
                // If there is a parent strategy, and no child strategy, load in the parent
                // This is not perfect...
                if (azurePipeline.strategy != null)
                {
                    foreach (AzurePipelines.Job item in azurePipeline.jobs)
                    {
                        if (item.strategy == null)
                        {
                            item.strategy = azurePipeline.strategy;
                        }
                    }
                }

                gitHubActions.jobs = ProcessJobs(azurePipeline.jobs, azurePipeline.resources);

                if (gitHubActions.jobs.Count == 0)
                {
                    gitHubActions.messages.Add("Note that although having no jobs is valid YAML, it is not a valid GitHub Action.");
                }
            }

            var vp = new VariablesProcessing(_variableGroups, _verbose);

            // Pool + Steps (When there are no jobs defined)
            if ((azurePipeline.pool != null && azurePipeline.jobs == null) || (azurePipeline.steps != null && azurePipeline.steps.Length > 0))
            {
                // Steps only have one job, so we just create it here
                var sp = new StepsProcessing();

                gitHubActions.jobs = new Dictionary<string, GitHubActions.Job>
                {
                    {
                        "build",
                        new GitHubActions.Job
                        {
                            runs_on = generalProcessing.ProcessPool(azurePipeline.pool),
                            strategy = generalProcessing.ProcessStrategy(azurePipeline.strategy),
                            container = generalProcessing.ProcessContainer(azurePipeline.resources),
                            //resources = ProcessResources(azurePipeline.resources),
                            steps = sp.AddSupportingSteps(azurePipeline.steps, vp)
                        }
                    }
                };

                MatrixVariableName = generalProcessing.MatrixVariableName;
            }

            // Variables
            if (azurePipeline.variables != null)
            {
                if (complexVariables != null)
                {
                    gitHubActions.env = vp.ProcessComplexVariables(complexVariables);
                    VariableList.AddRange(vp.VariableList);
                }
                else if (simpleVariables != null)
                {
                    gitHubActions.env = vp.ProcessSimpleVariables(simpleVariables);
                    VariableList.AddRange(vp.VariableList);
                }
            }
            else if (azurePipeline.parameters != null)
            {
                // For now, convert the parameters to variables
                gitHubActions.env = vp.ProcessSimpleVariables(azurePipeline.parameters);
            }

            return gitHubActions;
        }

        // process the jobs
        private Dictionary<string, GitHubActions.Job> ProcessJobs(AzurePipelines.Job[] jobs, Resources resources)
        {
            // A dictonary is perfect here, as the job_id (a string), must be unique in the action
            Dictionary<string, GitHubActions.Job> newJobs = null;

            if (jobs != null)
            {
                var jobProcessing = new JobProcessing(_variableGroups, _verbose);
                newJobs = new Dictionary<string, GitHubActions.Job>();

                for (int i = 0; i < jobs.Length; i++)
                {
                    string jobName = jobs[i].job;

                    if (jobName == null && jobs[i].deployment != null)
                    {
                        jobName = jobs[i].deployment;
                    }
                    else if (jobName == null && jobs[i].template != null)
                    {
                        jobName = "job_" + (i + 1).ToString() + "_template";
                    }

                    newJobs.Add(jobName, jobProcessing.ProcessJob(jobs[i], resources));
                    MatrixVariableName = jobProcessing.MatrixVariableName;
                    VariableList.AddRange(jobProcessing.VariableList);
                }
            }

            return newJobs;
        }
    }
}
