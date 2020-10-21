﻿using AzurePipelinesToGitHubActionsConverter.Core.AzurePipelines;
using AzurePipelinesToGitHubActionsConverter.Core.Conversion.Serialization;
using AzurePipelinesToGitHubActionsConverter.Core.Extensions;
using AzurePipelinesToGitHubActionsConverter.Core.GitHubActions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace AzurePipelinesToGitHubActionsConverter.Core.Conversion
{
    public class VariablesProcessing
    {
        private readonly bool _verbose;
        public List<string> VariableList;
        public VariablesProcessing(bool verbose)
        {
            _verbose = verbose;
            VariableList = new List<string>();
        }

        //process all (simple) variables
        public Dictionary<string, string> ProcessSimpleVariables(Dictionary<string, string> variables)
        {
            if (variables != null)
            {
                //update variables from the $(variableName) format to ${{variableName}} format, by piping them into a list for replacement later.
                foreach (string item in variables.Keys)
                {
                    VariableList.Add(item);
                }
            }

            return variables;
        }

        //process all (complex) variables
        public Dictionary<string, string> ProcessComplexVariables(AzurePipelines.Variable[] variables)
        {
            Dictionary<string, string> processedVariables = new Dictionary<string, string>();

            if (variables != null)
            {
                //update variables from the $(variableName) format to ${{variableName}} format, by piping them into a list for replacement later.
                for (int i = 0; i < variables.Length; i++)
                {
                    //name/value pairs
                    if (variables[i].name != null && variables[i].value != null)
                    {
                        processedVariables.Add(variables[i].name, variables[i].value);
                        VariableList.Add(variables[i].name);
                    }

                    //groups
                    if (variables[i].group != null)
                    {
                        if (!processedVariables.ContainsKey("group"))
                        {
                            processedVariables.Add("group", variables[i].group);
                        }
                        else
                        {
                            ConversionUtility.WriteLine("group: only 1 variable group is supported at present", _verbose);
                        }
                    }

                    //template
                    if (variables[i].template != null)
                    {
                        processedVariables.Add("template", variables[i].template);
                    }
                }
            }

            return processedVariables;
        }

        public Dictionary<string, string> ProcessComplexVariablesV2(List<AzurePipelines.Variable> variables)
        {
            Dictionary<string, string> processedVariables = new Dictionary<string, string>();

            if (variables != null)
            {
                //update variables from the $(variableName) format to ${{variableName}} format, by piping them into a list for replacement later.
                for (int i = 0; i < variables.Count; i++)
                {
                    //name/value pairs
                    if (variables[i].name != null && variables[i].value != null)
                    {
                        processedVariables.Add(variables[i].name, variables[i].value);
                        VariableList.Add(variables[i].name);
                    }

                    //groups
                    if (variables[i].group != null)
                    {
                        if (processedVariables.ContainsKey("group") == false)
                        {
                            processedVariables.Add("group", variables[i].group);
                        }
                        else
                        {
                            ConversionUtility.WriteLine("group: only 1 variable group is supported at present", _verbose);
                        }
                    }

                    //template
                    if (variables[i].template != null)
                    {
                        processedVariables.Add("template", variables[i].template);
                    }
                }
            }

            return processedVariables;
        }

        public Dictionary<string, string> ProcessComplexParametersV2(List<AzurePipelines.Parameter> parameter)
        {
            Dictionary<string, string> processedVariables = new Dictionary<string, string>();

            if (parameter != null)
            {
                //update variables from the $(variableName) format to ${{variableName}} format, by piping them into a list for replacement later.
                for (int i = 0; i < parameter.Count; i++)
                {
                    //name/value pairs
                    if (parameter[i].name != null )
                    {
                        if (parameter[i].@default == null)
                        {
                            parameter[i].@default = "";
                        }

                        processedVariables.Add(parameter[i].name, parameter[i].@default);
                        VariableList.Add(parameter[i].name);
                    }
                }
            }

            return processedVariables;
        }

        public Dictionary<string, string> ProcessParametersAndVariablesV2(string parametersYaml, string variablesYaml)
        {
            List<Parameter> parameters = null;

            if (parametersYaml != null)
            {
                try
                {
                    Dictionary<string, string> simpleParameters = GenericObjectSerialization.DeserializeYaml<Dictionary<string, string>>(parametersYaml);
                    parameters = new List<Parameter>();

                    foreach (KeyValuePair<string, string> item in simpleParameters)
                    {
                        parameters.Add(new Parameter
                        {
                            name = item.Key,
                            @default = item.Value
                        });
                    }
                }
                catch (Exception ex)
                {
                    ConversionUtility.WriteLine($"DeserializeYaml<Dictionary<string, string>>(parametersYaml) swallowed an exception: " + ex.Message, _verbose);
                    parameters = GenericObjectSerialization.DeserializeYaml<List<Parameter>>(parametersYaml);
                }
            }

            List<Variable> variables = null;

            if (variablesYaml != null)
            {
                try
                {
                    Dictionary<string, string> simpleVariables = GenericObjectSerialization.DeserializeYaml<Dictionary<string, string>>(variablesYaml);
                    variables = new List<Variable>();

                    foreach (KeyValuePair<string, string> item in simpleVariables)
                    {
                        variables.Add(new Variable
                        {
                            name = item.Key,
                            value = item.Value
                        });
                    }
                }
                catch (Exception ex)
                {
                    ConversionUtility.WriteLine($"DeserializeYaml<Dictionary<string, string>>(variablesYaml) swallowed an exception: " + ex.Message, _verbose);
                    variables = GenericObjectSerialization.DeserializeYaml<List<Variable>>(variablesYaml);
                }
            }

            Dictionary<string, string> env = new Dictionary<string, string>();
            Dictionary<string, string> processedParameters = ProcessComplexParametersV2(parameters);
            Dictionary<string, string> processedVariables = ProcessComplexVariablesV2(variables);

            foreach (KeyValuePair<string, string> item in processedParameters)
            {
                if (env.ContainsKey(item.Key) == false)
                {
                    env.Add(item.Key, item.Value);
                }
            }

            foreach (KeyValuePair<string, string> item in processedVariables)
            {
                if (env.ContainsKey(item.Key) == false)
                {
                    env.Add(item.Key, item.Value);
                }
            }

            if (env.Count > 0)
            {
                return env;
            }
            else
            {
                return null;
            }
        }

        public List<string> SearchForVariables(string input)
        {
            List<string> variableList = new List<string>();

            if (input != null)
            {
                string[] stepLines = input.Split(System.Environment.NewLine);

                foreach (string line in stepLines)
                {
                    List<string> variableResults = FindPipelineVariablesInString(line);
                    variableResults.AddRange(FindPipelineParametersInString(line));

                    if (variableResults.Count > 0)
                    {
                        variableList.AddRange(variableResults);
                    }
                }
            }

            return variableList;
        }

        //Search GitHub object for all environment variables
        public void ProcessEnvVars(GitHubActionsRoot gitHubActions)
        {
            // We want to 1) Identify env vars at the workflow, stage/job level(s)
            //  2) Pre-process these var values to see if they refer to other env vars, as the syntax rules are nuanced
            if (gitHubActions.env != null)
            {
                processVarDict(gitHubActions.env);
            }

            if (gitHubActions.jobs != null)
            {
                foreach (var job in gitHubActions.jobs)
                {
                    processVarDict(job.Value.env);
                }
            }
        }

        private void processVarDict(Dictionary<string, string> envVarTable)
        {
            if (envVarTable != null)
            {
                // add all vars, sans the 'group' reserved key
                var envVars = envVarTable.Keys.Where(v => v != "group").Distinct().ToList();

                // add all vars found to our list - these will be the env vars used in other parts of the workflow
                VariableList.AddRange(envVars);

                // Now, process the values of these env vars - nuanced rules in place for how we refer to var in objects 'above' vs siblings
                foreach (var key in envVars)
                {
                    var varValue = envVarTable[key];
                    var varsUsed = FindPipelineVariables(varValue);

                    if (varsUsed.Count > 0)
                    {
                        foreach (Match varMatched in varsUsed)
                        {
                            var varName = varMatched.Groups[1].Value;

                            // is this var defined at the job level?
                            if (envVars.Contains(varName))
                            {
                                // strip any prefix & wrapper, just refer to the var via $var - replace the full match with $var
                                varValue = varValue.Replace(varMatched.Value, $"${varName}");
                            }
                        }

                        // put the modified value back in the env dict
                        envVarTable[key] = varValue;
                    }
                }
            }
        }

        private List<string> FindPipelineVariablesInString(string text)
        {
            //Used https://stackoverflow.com/questions/378415/how-do-i-extract-text-that-lies-between-parentheses-round-brackets
            //With the addition of the \$ search to capture strings like: "$(variable)"
            //\$\(           # $ char and escaped parenthesis, means "starts with a '$(' character"
            //    (          # Parentheses in a regex mean "put (capture) the stuff 
            //               #     in between into the Groups array" 
            //       [^)]    # Any character that is not a ')' character
            //       *       # Zero or more occurrences of the aforementioned "non ')' char"
            //    )          # Close the capturing group
            //\)             # "Ends with a ')' character"  
            MatchCollection results = Regex.Matches(text, @"\$\(([^)]*)\)");
            List<string> list = results.Cast<Match>().Select(match => match.Value).ToList();

            for (int i = 0; i < list.Count; i++)
            {
                string item = list[i];

                //Remove leading "$(" and trailing ")"
                if (list[i].Length > 3)
                {
                    list[i] = list[i].Substring(0, item.Length - 1);
                    list[i] = list[i].Remove(0, 2);
                }
            }

            return list;
        }

        private List<string> FindPipelineParametersInString(string text)
        {
            //Used https://stackoverflow.com/questions/378415/how-do-i-extract-text-that-lies-between-parentheses-round-brackets
            //With the addition of the \$ search to capture strings like: "$(variable)"
            //\$\(           # $ char and escaped parenthesis, means "starts with a '$(' character"
            //    (          # Parentheses in a regex mean "put (capture) the stuff 
            //               #     in between into the Groups array" 
            //       [^)]    # Any character that is not a ')' character
            //       *       # Zero or more occurrences of the aforementioned "non ')' char"
            //    )          # Close the capturing group
            //\)             # "Ends with a ')' character"  
            MatchCollection results = Regex.Matches(text, @"\$\{\{([^}}]*)\}\}");
            List<string> list = results.Cast<Match>().Select(match => match.Value).ToList();

            for (int i = 0; i < list.Count; i++)
            {
                string item = list[i];

                //Remove leading "${{" and trailing "}}"
                if (list[i].Length > 5)
                {
                    list[i] = list[i].Substring(0, item.Length - 2);
                    list[i] = list[i].Remove(0, 3);
                }
            }

            return list;
        }

        public MatchCollection FindPipelineVariables(string yaml)
        {
            // match anything in ${}, ${{}}, $(), $[], but NOT $var
            //  Allowed ADO var chars here: https://docs.microsoft.com/en-us/azure/devops/pipelines/process/variables?view=azure-devops&tabs=yaml%2Cbatch#variable-characters
            var varPattern = @"\$(?:\{|\{|\(|\[|\{\{)([^\r\n(){}\[\]$]+)(?:\}\}|\}|\]|\})?(?:\}\}|\]|\)|\}|\})";

            return Regex.Matches(yaml, varPattern);
        }

        public MatchCollection FindPipelineVariable(string yaml, string var)
        {
            // match "var" in ${}, ${{}}, $(), $[], but NOT $var
            var varPattern = string.Format(@"\$(?:\{|\{|\(|\[|\{\{)({0})(?:\}\}|\}|\]|\})?(?:\}\}|\]|\)|\}|\})", var);

            return Regex.Matches(yaml, varPattern);
        }

        public string ProcessVariableConversions(string yaml, string matrixVariableName = null)
        {
            // Replace variables with the format "${{ [prefix.]MyVar }}"
            var matches = FindPipelineVariables(yaml);

            foreach (Match match in matches)
            {
                var varName = match.Groups[1].Value;
                
                if (varName != matrixVariableName)
                {
                    yaml = yaml.Replace(match.Value, $"${{{{ env.{varName} }}}}");
                }
                else // matrix var
                {
                    yaml = yaml.Replace(match.Value, $"${{{{ matrix.{varName} }}}}");
                }
            }

            return yaml;
        }

        public string ProcessADOtoActionsEnv(string yaml)
        {
            // GitHub Actions-specific env variables need to replace old ADO vars
            //  We do this AFTER ProcessVariableConversions() bc we can depend on ${{}} formatting
            return yaml
                .ReplaceAnyCase("${{ env.rev:r }}", "${ GITHUB_RUN_NUMBER }") // need to verify, moved over from older code; prefer prettier [github.] context usage as below
                .ReplaceAnyCase("${{ env.Build.BuildId }}", "${{ github.run_id }}")
                .ReplaceAnyCase("${{ env.Build.BuildNumber }}", "${{ github.run_number }}")
                .ReplaceAnyCase("${{ env.Build.DefinitionName }}", "${{ github.workflow }}")
                .ReplaceAnyCase("${{ env.Build.SourcesDirectory }}", "${{ github.workspace }}") // workspace is shared work folder, may need to create subfolder(s)
                .ReplaceAnyCase("${{ env.Build.ArtifactStagingDirectory }}", "${{ github.workspace }}") // workspace is shared work folder, may need to create subfolder(s)
                .ReplaceAnyCase("${{ env.Build.SourceBranch }}", "${{ github.ref }}")
                .ReplaceAnyCase("${{ env.Build.SourceBranchName }}", "${{ github.ref }}") // not exact mapping "main" vs "refs/heads/main"
                .ReplaceAnyCase("${{ env.Build.RepositoryName }}", "${{ github.repository }}") // not exact mapping "gitutil" vs "naterickard/gitutil"
                .ReplaceAnyCase("env.parameters.", "env.");            
        }
    }
}
