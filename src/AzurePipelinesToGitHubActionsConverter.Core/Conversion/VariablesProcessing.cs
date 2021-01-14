using AzurePipelinesToGitHubActionsConverter.Core.AzurePipelines;
using AzurePipelinesToGitHubActionsConverter.Core.Conversion.Serialization;
using AzurePipelinesToGitHubActionsConverter.Core.Extensions;
using AzurePipelinesToGitHubActionsConverter.Core.GitHubActions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace AzurePipelinesToGitHubActionsConverter.Core.Conversion
{
    public class VariablesProcessing
    {
        const string GroupKey = "group";
        private readonly bool _verbose;
        private readonly List<VariableGroup> VariableGroups;
        public List<string> VariableList;
        private List<string> secrets = new List<string>();
        private List<string> usedSecrets = new List<string>();
        public VariableGroup KeyVaultGroup;

        private class SystemVariableMapping
        {
            public string ADOVar { get; set; }
            public string ActionsVar { get; set; }
            public string Format { get; set; }

            public static SystemVariableMapping Define(string adoVar, string actionsVar, string format = null)
            {
                return new SystemVariableMapping { ADOVar = adoVar, ActionsVar = actionsVar, Format = format };
            }
        }

        private List<SystemVariableMapping> SystemVarMapping = new List<SystemVariableMapping>()
        {
            SystemVariableMapping.Define("Agent.WorkFolder", "github.workspace"), // runner.workspace will not validate in Workflow or Job-level env
            SystemVariableMapping.Define("Agent.BuildDirectory", "runner.workspace"),
            SystemVariableMapping.Define("Pipeline.Workspace", "runner.workspace"),
            SystemVariableMapping.Define("Build.BuildId", "github.run_id"),
            SystemVariableMapping.Define("Build.BuildNumber", "github.run_number"),
            SystemVariableMapping.Define("Build.DefinitionName", "github.workflow"),
            SystemVariableMapping.Define("Build.Reason", "github.event_name"),
            SystemVariableMapping.Define("Build.SourcesDirectory", "github.workspace"), // workspace is shared work folder, may need to create subfolder(s)
            SystemVariableMapping.Define("Build.ArtifactStagingDirectory", "github.workspace"), // workspace is shared work folder, may need to create subfolder(s)
            SystemVariableMapping.Define("Build.StagingDirectory", "github.workspace"), // workspace is shared work folder, may need to create subfolder(s)
            SystemVariableMapping.Define("Build.SourceBranch", "github.ref"),
            SystemVariableMapping.Define("Build.SourceBranchName", "github.ref"), // not exact mapping "main" vs "refs/heads/main"
            SystemVariableMapping.Define("Build.SourceVersion", "github.sha"),
            SystemVariableMapping.Define("Build.RepositoryName", "github.repository"), // not exact mapping [repo name] vs [account/org name]/[repo name]
            SystemVariableMapping.Define("System.ArtifactsDirectory", "github.workspace"), // workspace is shared work folder, may need to create subfolder(s)
            SystemVariableMapping.Define("System.DefaultWorkingDirectory", "github.workspace"), // workspace is shared work folder, may need to create subfolder(s)
            SystemVariableMapping.Define("System.StageAttempt", "github.run_number"),
            SystemVariableMapping.Define("rev:r", "github.run_number"),
        };

        public VariablesProcessing(List<VariableGroup> variableGroups = null, bool verbose = true)
        {
            VariableList = new List<string>();
            VariableGroups = variableGroups;
            _verbose = verbose;
        }

        public VariableGroup FindVariableGroup(OrderedDictionary variables, string type = null)
        {
            return FindVariableGroup(variables?.Cast<DictionaryEntry>(), type);
        }

        public VariableGroup FindVariableGroup(IEnumerable<DictionaryEntry> variables, string type = null)
        {
            var varGroupName = variables?
                .Where(de => de.StringKey() == GroupKey)
                .Select(de => de.StringValue()).FirstOrDefault();

            var group = getVariableGroup(varGroupName);

            if (group?.type == type || type == null)
            {
                return group;
            }

            return null;
        }

        private VariableGroup getVariableGroup(string groupName)
        {
            return VariableGroups.SingleOrDefault(g => g.name == groupName);
        }

        public bool AnyKeyVaultSecretsConsumed()
        {
            return usedSecrets.Any(s => KeyVaultGroup?.variables.ContainsKey(s) ?? false);
        }

        // process all (simple) variables
        public OrderedDictionary ProcessSimpleVariables(OrderedDictionary variables)
        {
            if (variables != null)
            {
                // update variables from the $(variableName) format to ${{variableName}} format, by piping them into a list for replacement later.
                foreach (string key in variables.Keys)
                {
                    VariableList.Add(key);
                }
            }

            return variables;
        }

        // process all (complex) variables
        public OrderedDictionary ProcessComplexVariables(AzurePipelines.Variable[] variables)
        {
            var processedVariables = new OrderedDictionary();

            if (variables != null)
            {
                // update variables from the $(variableName) format to ${{variableName}} format, by piping them into a list for replacement later.
                for (int i = 0; i < variables.Length; i++)
                {
                    // name/value pairs
                    if (variables[i].name != null && variables[i].value != null)
                    {
                        processedVariables.Add(variables[i].name, variables[i].value);
                        VariableList.Add(variables[i].name);
                    }

                    // groups
                    if (variables[i].group != null)
                    {
                        if (!processedVariables.Contains(GroupKey))
                        {
                            processedVariables.Add(GroupKey, variables[i].group);
                        }
                        else
                        {
                            ConversionUtility.WriteLine("group: only 1 variable group is supported at present", _verbose);
                        }
                    }

                    // template
                    if (variables[i].template != null)
                    {
                        processedVariables.Add("template", variables[i].template);
                    }
                }
            }

            return processedVariables;
        }

        public OrderedDictionary ProcessComplexVariablesV2(List<AzurePipelines.Variable> variables)
        {
            var processedVariables = new OrderedDictionary();

            if (variables != null)
            {
                // update variables from the $(variableName) format to ${{variableName}} format, by piping them into a list for replacement later.
                for (int i = 0; i < variables.Count; i++)
                {
                    // name/value pairs
                    if (variables[i].name != null && variables[i].value != null)
                    {
                        processedVariables.Add(variables[i].name, variables[i].value);
                        VariableList.Add(variables[i].name);
                    }

                    // groups
                    if (variables[i].group != null)
                    {
                        if (!processedVariables.Contains(GroupKey))
                        {
                            processedVariables.Add(GroupKey, variables[i].group);
                        }
                        else
                        {
                            ConversionUtility.WriteLine("group: only 1 variable group is supported at present", _verbose);
                        }

                        var group = getVariableGroup(variables[i].group);
                        
                        // for a KV-backed group, we'll track it and add all vars to our list
                        if (group.type == VariableGroup.KeyVaultGroupType)
                        {
                            KeyVaultGroup = group;

                            VariableList.AddRange(group.variables.Keys);
                        }
                    }

                    // template
                    if (variables[i].template != null)
                    {
                        processedVariables.Add("template", variables[i].template);
                    }
                }
            }

            // in the case that this set of vars does not have a KV-backed group, but we've already identified one, let's add it here in case we need to access these vars within the current Job (KV Action must be added as a Step in a Job)
            // this effectively adds the KV group to ALL subsequent env dictionaries if it's found at the top level or a prior Job
            if (KeyVaultGroup != null)
            {
                if (!processedVariables.Contains(GroupKey))
                {
                    processedVariables.Add(GroupKey, KeyVaultGroup.name);
                }
            }

            return processedVariables;
        }

        public Dictionary<string, string> ProcessComplexParametersV2(List<AzurePipelines.Parameter> parameter)
        {
            var processedVariables = new Dictionary<string, string>();

            if (parameter != null)
            {
                // update variables from the $(variableName) format to ${{variableName}} format, by piping them into a list for replacement later.
                for (int i = 0; i < parameter.Count; i++)
                {
                    // name/value pairs
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

        public OrderedDictionary ProcessParametersAndVariablesV2(string parametersYaml, string variablesYaml)
        {
            List<Parameter> parameters = null;

            if (parametersYaml != null)
            {
                try
                {
                    var simpleParameters = GenericObjectSerialization.DeserializeYaml<OrderedDictionary>(parametersYaml);
                    parameters = new List<Parameter>();

                    parameters.AddRange(
                        simpleParameters.Cast<DictionaryEntry>().Select(de => new Parameter
                        {
                            name = de.StringKey(),
                            @default = de.StringValue()
                        }));
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
                    var simpleVariables = GenericObjectSerialization.DeserializeYaml<OrderedDictionary>(variablesYaml);
                    variables = new List<Variable>();

                    variables.AddRange(
                        simpleVariables.Cast<DictionaryEntry>().Select(de => new Variable
                        {
                            name = de.StringKey(),
                            value = de.StringValue()
                        }));
                }
                catch (Exception ex)
                {
                    ConversionUtility.WriteLine($"DeserializeYaml<Dictionary<string, string>>(variablesYaml) swallowed an exception: " + ex.Message, _verbose);
                    variables = GenericObjectSerialization.DeserializeYaml<List<Variable>>(variablesYaml);
                }
            }

            var env = new OrderedDictionary();
            var processedParameters = ProcessComplexParametersV2(parameters);
            var processedVariables = ProcessComplexVariablesV2(variables);

            foreach (var item in processedParameters)
            {
                if (!env.Contains(item.Key))
                {
                    env.Add(item.Key, item.Value);
                }
            }

            foreach (DictionaryEntry item in processedVariables)
            {
                if (!env.Contains(item.Key))
                {
                    env.Add(item.Key, item.Value);
                }
            }

            return env.Count > 0 ? env : null;
        }

        public List<string> SearchForVariables(string input)
        {
            var variableList = new List<string>();

            if (input != null)
            {
                string[] stepLines = input.Split(System.Environment.NewLine);

                foreach (string line in stepLines)
                {
                    var variableResults = FindPipelineVariablesInString(line);
                    variableResults.AddRange(FindPipelineParametersInString(line));

                    if (variableResults.Count > 0)
                    {
                        variableList.AddRange(variableResults);
                    }
                }
            }

            return variableList;
        }

        // Search GitHub object for all environment variables
        public void ProcessEnvVars(GitHubActionsRoot gitHubActions, bool variableCompatMode = false)
        {
            DictionaryEntry[] rawEnvValues = null;

            // We want to 1) Identify env vars at the workflow, stage/job level(s)
            // 2) Pre-process these var values to see if they refer to other env vars, as the syntax rules are nuanced
            if (gitHubActions.env != null)
            {
                // grab the initial values here before processing, so we can compare job vs wf level env
                rawEnvValues = new DictionaryEntry[gitHubActions.env.Count];
                gitHubActions.env.CopyTo(rawEnvValues, 0);

                processVarDict(gitHubActions, gitHubActions.env);

                // do we need to output legacy ADO system vars to ease pipeline conversion?
                if (variableCompatMode)
                {
                    foreach (var mapping in SystemVarMapping)
                    {
                        if (!mapping.ActionsVar.StartsWith("runner")) // runner context not available at workflow level (?)
                        {
                            // output both context-style and ENV style vars, i.e. Build.SourceBranch and BUILD_SOURCEBRANCH
                            gitHubActions.env.Add(mapping.ADOVar, $"${{{{ { mapping.ActionsVar } }}}}");

                            if (mapping.ADOVar.Contains('.'))
                            {
                                gitHubActions.env.Add(mapping.ADOVar.ToUpper().Replace('.', '_'), $"${{{{ { mapping.ActionsVar } }}}}");
                            }
                        }
                    }

                    gitHubActions.messages.Add("Note: VariableCompatMode detected; A mapping of system vars between Azure DevOps and GitHub Actions has been added to env");
                }
            }

            if (gitHubActions.jobs != null)
            {
                foreach (var job in gitHubActions.jobs)
                {
                    processVarDict(gitHubActions, job.Value.env, rawEnvValues);

                    // no vars left? Remove the table
                    if (job.Value.env?.Count == 0)
                    {
                        job.Value.env = null;
                    }
                }
            }
        }

        private void processVarDict(GitHubActionsRoot actionsRoot, OrderedDictionary envVarTable, DictionaryEntry[] parentVarTable = null)
        {
            if (envVarTable != null)
            {
                // do we want to filter out env vars that are identical to vars in the parent context? This can easily occur due to templated pipeline conversions
                if (parentVarTable != null)
                {
                    // so, for any 'child' level env vars that also exist at the 'parent' level and contain the same value, let's remove them
                    var sameVars = envVarTable
                        .Cast<DictionaryEntry>()
                        .Where(de => de.StringKey() != GroupKey && parentVarTable.Any(pv => pv.StringKey() == de.StringKey()))
                        .ToList();

                    foreach (var sameVar in sameVars)
                    {
                        if (sameVar.StringValue() == parentVarTable.FirstOrDefault(pv => pv.StringKey() == sameVar.StringKey()).StringValue())
                        {
                            envVarTable.Remove(sameVar.Key);
                        }
                    }
                }

                // don't want to output the group name in our env section, so find and remove it
                var varGroup = FindVariableGroup(envVarTable.Cast<DictionaryEntry>());

                if (varGroup != null)
                {
                    // separate group from vars
                    int groupIndex = envVarTable
                        .Cast<DictionaryEntry>()
                        .ToList()
                        .FindIndex(v => v.StringValue() == varGroup.name);
                    envVarTable.RemoveAt(groupIndex);

                    foreach (var groupVar in varGroup.variables)
                    {
                        // is the var marked as secret? if this is a normal ADO var group, we can convert vars directly or via secrets API
                        if (!groupVar.Value.isSecret) // not secret
                        {
                            // for non-secret values, we'll convert them over directly to env vars
                            envVarTable.Insert(groupIndex++, groupVar.Key, groupVar.Value.value);
                        }
                        else // secret
                        {
                            // track the secret definition as we'll later look to replace usages
                            secrets.Add(groupVar.Key);

                            if (KeyVaultGroup?.name != varGroup.name)
                            {
                                // for secret, non-KV values, we'll convert them to env vars using the secrets syntax
                                envVarTable.Insert(groupIndex++, groupVar.Key, $"${{{{ secrets.{ groupVar.Key } }}}}");
                            }
                        }
                    }

                    if (KeyVaultGroup?.name == varGroup.name)
                    {
                        actionsRoot.messages.AddIfUnique($"Note: The consumed values from a variable group ({ varGroup.name }) have been imported from Azure DevOps. Please review variable usage and any secret values used in this workflow, which have been migrated to GitHub secrets syntax");
                    }
                }

                var envVars = new string[envVarTable.Count];
                envVarTable.Keys.CopyTo(envVars, 0);

                // add all non-group vars found to our list - these will be the env vars used in other parts of the workflow
                VariableList.AddRange(envVars);

                // Now, process the values of these env vars - nuanced rules in place for how we refer to var in objects 'above' vs siblings
                foreach (var key in envVars)
                {
                    var varValue = envVarTable[key].ToString();
                    var varsUsed = FindPipelineVariables(varValue);

                    if (varsUsed.Count > 0)
                    {
                        foreach (Match varMatched in varsUsed)
                        {
                            var varName = varMatched.Groups[1].Value;

                            // is this var an env var that needs to be populated? GH Actions will not substitute env vars within the env dict
                            if (envVars.Contains(varName))
                            {
                                // sub in the actul value of this env var since Actions does not allow complex/compound env vars
                                var value = envVarTable[varName].ToString();
                                varValue = varValue.Replace(varMatched.Value, value);
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

                // Remove leading "$(" and trailing ")"
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

                // Remove leading "${{" and trailing "}}"
                if (list[i].Length > 5)
                {
                    list[i] = list[i].Substring(0, item.Length - 2);
                    list[i] = list[i].Remove(0, 3);
                }
            }

            return list;
        }

        private MatchCollection FindPipelineVariables(string input)
        {
            // match anything in ${}, ${{}}, $(), $[], but NOT $var
            return FindPipelineVariables(input, @"[^\r\n()\{\}\[\]$]+");
        }

        private MatchCollection FindPipelineVariables(string input, string varNamePattern)
        {
            // match "varNamePattern" in ${}, ${{}}, $(), $[], but NOT $var
            // Allowed ADO var chars here: https://docs.microsoft.com/en-us/azure/devops/pipelines/process/variables?view=azure-devops&tabs=yaml%2Cbatch#variable-characters
            var varPattern = @"\$(?:\{|\{|\(|\[|\{\{)(" + varNamePattern + @")(?:\}\}|\}|\]|\})?(?:\}\}|\]|\)|\}|\})";

            return Regex.Matches(input, varPattern, RegexOptions.IgnoreCase);
        }

        public IEnumerable<string> FindVariables(string input)
        {
            var matches = FindPipelineVariables(input);

            // actual var name will be returned in grouping 1
            return matches.Cast<Match>().Select(m => m.Groups[1].Value.Trim());
        }

        public string ProcessVariableConversions(string yaml, List<string> messages, string matrixVariableName = null)
        {
            // Replace variables with the format "${{ [prefix.]MyVar }}"
            var matches = FindPipelineVariables(yaml);
            var uniqueVars = VariableList.Distinct().ToList();
            usedSecrets.Clear();

            foreach (Match match in matches)
            {
                // actual var name will be returned in grouping 1
                var varName = match.Groups[1].Value.Trim();

                // Only do replacement if this is one of the vars we've identified... stops false positives like job.status
                var definedVar = uniqueVars.SingleOrDefault(v => v.ToUpper() == varName.ToUpper());

                if (definedVar != null)
                {
                    var isSecret = secrets.Contains(definedVar);

                    // Imported KV secrets are used differently than env vars
                    if (isSecret && KeyVaultGroup != null)
                    {
                        yaml = yaml.Replace(match.Value, $"${{{{ steps.{ KeyVaultGroup.name }.outputs.{ definedVar } }}}}");
                    }
                    else if (varName != matrixVariableName) // other typical vars, including secrets converted to GH secrets API
                    {
                        yaml = yaml.Replace(match.Value, $"${{{{ env.{ definedVar } }}}}");
                    }
                    else // matrix var
                    {
                        yaml = yaml.Replace(match.Value, $"${{{{ matrix.{ definedVar } }}}}");
                    }

                    // keep track of any that we use, so we can handle those we don't further below
                    if (isSecret)
                    {
                        usedSecrets.Add(definedVar);
                    }
                }
                else
                {
                    // see if this matches a system var we know how to replace
                    var systemVar = SystemVarMapping.FirstOrDefault(v => v.ADOVar.ToLower() == varName.ToLower());

                    if (systemVar?.ActionsVar != null)
                    {
                        yaml = yaml.Replace(match.Value, $"${{{{ { systemVar.ActionsVar } }}}}");
                    }
                    else // otherwise, only convert the usage syntax to env if it doesn't look like a system var or in antoher context already
                    {
                        var varParts = varName.Split('.');

                        // is this var usage already prefixed?
                        if (varParts.Length < 2)
                        {
                            // Not prefixed, convert the syntax
                            yaml = yaml.Replace(match.Value, $"${{{{ env.{ varName } }}}}");
                        }
                        else if (varParts[0] != "secrets" && varParts[0] != "github" && varParts[0] != "runner")
                        {
                            // possibly have an unmapped system var here that we should notify was not converted
                            messages?.AddIfUnique($"Variable conversions completed but unable to convert { varName }");
                        }
                    }
                }
            }

            // if we've converted to GH secrets syntax, let's remove any secrets that were not used
            if (KeyVaultGroup?.name == null)
            {
                // do we have unused secrets that were pulled in?
                foreach (var secret in secrets.Except(usedSecrets))
                {
                    // If so, let's clean them from the env definition. This will remove any unused group vars that were pulled in
                    string pattern = @"(?:\s|^)*" + Regex.Escape($"{ secret }: ${{{{ secrets.{ secret } }}}}") + @"(?:\s|$)";

                    yaml = Regex.Replace(yaml, pattern, System.Environment.NewLine, RegexOptions.IgnoreCase);
                }
            }
            else // We have imported a Key Vault - let's whittle down the keys we're importing to only those that are used
            {
                var secretList = KeyVaultGroup.variables.Select(v => v.Key);

                string pattern = @"(?:\s|^)?(secrets: " + Regex.Escape(string.Join(",", secretList)) + ")";

                string usedSecretsList = $" secrets: '{ string.Join(",", usedSecrets) }'";

                yaml = Regex.Replace(yaml, pattern, usedSecretsList, RegexOptions.IgnoreCase);
            }

            yaml = yaml.ReplaceAnyCase("${{ env.rev:r }}", "${ GITHUB_RUN_NUMBER }"); // need to verify, moved over from older code; prefer prettier [github.] context usage
            yaml = yaml.ReplaceAnyCase("env.parameters.", "env.");
            
            return yaml;
        }

        public string ProcessIndexedVariables(string input)
        {
            // Find indexed var usage, i.e. variables['MyVar']
            var varFormat = "variables[";
            var varStart = input.IndexOf(varFormat);

            while (varStart >= 0)
            {
                var varEnd = input.IndexOf(']', varStart);

                if (varEnd >= 0)
                {
                    var nameStart = varStart + varFormat.Length + 1;
                    var nameEnd = varEnd - nameStart - 1;
                    var varName = input.Substring(nameStart, nameEnd);

                    input = input.Remove(varStart, varEnd - varStart + 1);
                    input = input.Insert(varStart, $"env['{varName}']");
                }

                varStart = input.IndexOf(varFormat, varStart + 1);
            }

            return input;
        }
    }
}
