using AzurePipelinesToGitHubActionsConverter.Core.AzurePipelines;
using AzurePipelinesToGitHubActionsConverter.Core.Conversion.Serialization;
using AzurePipelinesToGitHubActionsConverter.Core.Extensions;
using AzurePipelinesToGitHubActionsConverter.Core.GitHubActions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace AzurePipelinesToGitHubActionsConverter.Core.Conversion
{
    public class StepsProcessing
    {
        const string CheckoutStepId = "6D15AF64-176C-496D-B583-FD2AE21D4DF4@1";

        // TODO: Add more task types
        public GitHubActions.Step ProcessStep(AzurePipelines.Step step, VariablesProcessing variablesProcessing)
        {
            GitHubActions.Step gitHubStep = null;

            if (step.task != null)
            {
                step = CleanStepInputs(step);

                // TODO: Should we be handling versions seperately? Currently the version is bundled with the step name
                switch (step.task.ToUpper()) //Set to upper case to handle case sensitivity comparisons e.g. NPM hangles Npm, NPM, or npm. 
                {
                    case "ANT@1":
                        gitHubStep = CreateAntStep(step);
                        break;
                    case "ARCHIVEFILES@2":
                        gitHubStep = CreateArchiveFilesStep(step);
                        break;
                    case "AZUREAPPSERVICEMANAGE@0":
                        gitHubStep = CreateAzureAppServiceManageStep(step);
                        break;
                    case "AZUREFILECOPY@4":
                        gitHubStep = CreateAzureFileCopyStep(step);
                        break;
                    case "AZURERESOURCEGROUPDEPLOYMENT@2":
                        gitHubStep = CreateAzureManageResourcesStep(step);
                        break;
                    case "AZUREFUNCTIONAPP@1":
                    case "AZUREFUNCTIONAPPCONTAINER@1":
                    case "AZURERMWEBAPPDEPLOYMENT@3":
                    case "AZUREWEBAPPCONTAINER@1":
                    case "AZURERMWEBAPPDEPLOYMENT@4":
                    case "AZUREWEBAPP@1":
                        gitHubStep = CreateAzureWebAppDeploymentStep(step);
                        break;
                    case "BASH@3":
                        gitHubStep = CreateScriptStep(step, ShellType.Bash);
                        break;
                    case "BATCHSCRIPT@1":
                    case "CMDLINE@1":
                    case "CMDLINE@2":
                        gitHubStep = CreateScriptStep(step, ShellType.Cmd);
                        break;
                    case "COPYFILES@2":
                        gitHubStep = CreateCopyFilesStep(step);
                        break;
                    case "DOCKER@0":
                    case "DOCKER@1":
                    case "DOCKER@2":
                        gitHubStep = CreateDockerStep(step);
                        break;
                    case "DOTNETCORECLI@2":
                        gitHubStep = CreateDotNetCommandStep(step);
                        break;
                    case "DOWNLOADBUILDARTIFACTS@0":
                        gitHubStep = CreateDownloadBuildArtifacts(step);
                        break;
                    case "DOWNLOADPIPELINEARTIFACT@2":
                       gitHubStep = CreateDownloadPipelineArtifact(step);
                       break;
                    case "GRADLE@2":
                        gitHubStep = CreateGradleStep(step);
                        break;
                    //case "KUBERNETES@1":
                    //    gitHubStep = CreateKubernetesStep(step);
                    //    break;
                    case "MAVEN@3":
                        gitHubStep = CreateMavenStep(step);
                        break;
                    case "NPM@1":
                        gitHubStep = CreateNPMStep(step);
                        break;
                    case "NODETOOL@0":
                        gitHubStep = CreateNodeToolStep(step);
                        break;
                    case "NUGETCOMMAND@2":
                        gitHubStep = CreateNuGetCommandStep(step);
                        break;
                    case "NUGETTOOLINSTALLER@1":
                        gitHubStep = CreateNuGetToolInstallerStep();
                        break;
                    case "POWERSHELL@1":
                    case "POWERSHELL@2":
                        gitHubStep = CreateScriptStep(step, ShellType.PowerShell);
                        break;
                    case "PUBLISHPIPELINEARTIFACT@0":
                    case "PUBLISHPIPELINEARTIFACT@1":
                    case "PUBLISHBUILDARTIFACTS@1":
                        gitHubStep = CreatePublishBuildArtifactsStep(step);
                        break;
                    case "PYTHONSCRIPT@0":
                        gitHubStep = CreatePythonStep(step);
                        break;
                    case "PUBLISHTESTRESULTS@2":
                        gitHubStep = CreatePublishTestResultsStep(step);
                        break;
                    case "SQLAZUREDACPACDEPLOYMENT@1":
                        gitHubStep = CreateSQLAzureDacPacDeployStep(step);
                        break;
                    case "USEDOTNET@2":
                        gitHubStep = CreateUseDotNetStep(step);
                        break;
                    case "USEPYTHONVERSION@0":
                        gitHubStep = CreateUsePythonStep(step);
                        break;
                    case "USERUBYVERSION@0":
                        gitHubStep = CreateUseRubyStep(step);
                        break;
                    case "VSBUILD@1":
                        gitHubStep = CreateMSBuildStep(step);
                        break;
                    case "VSTEST@2":
                        gitHubStep = CreateFunctionalTestingStep(step);
                        break;
                    case "XAMARINANDROID@1":
                        gitHubStep = CreateXamarinAndroidStep(step);
                        break;
                    case "XAMARINIOS@2":
                        gitHubStep = CreateXamariniOSStep(step);
                        break;
                    default:
                        gitHubStep = CreateUnconvertedStep(step);
                        break;
                }
            }
            else if (step.script != null)
            {
                gitHubStep = new GitHubActions.Step
                {
                    run = step.script,
                    with = step.inputs
                };
            }
            else if (step.pwsh != null)
            {
                gitHubStep = CreateScriptStep(step, ShellType.PowerShell);
            }
            else if (step.powershell != null)
            {
                gitHubStep = CreateScriptStep(step, ShellType.PowerShell);
            }
            else if (step.bash != null)
            {
                gitHubStep = CreateScriptStep(step, ShellType.Bash);
            }
            else if (step.publish != null)
            {
                // The shortcut to the build publish step
                // https://docs.microsoft.com/en-us/azure/devops/pipelines/yaml-schema?view=azure-devops&tabs=schema#publish
                gitHubStep = CreatePublishBuildArtifactsStep(step);
            }
            else if (step.template != null)
            {
                gitHubStep = CreateTemplateStep(step);
            }

            if (gitHubStep != null)
            {
                // Add in generic name and conditions
                if (step.displayName != null)
                {
                    gitHubStep.name = step.displayName;
                }

                if (step.condition != null)
                {
                    gitHubStep._if = ConditionsProcessing.TranslateConditions(step.condition, variablesProcessing);
                }

                // Double check the with. Sometimes we start to add a property, but for various reasons, we don't use it, and have to null out the with so it doesn't display an empty node in the final yaml
                if (gitHubStep.with != null)
                {
                    if (gitHubStep.with.Count >= 0)
                    {
                        // Look to see if there is non-null data in the collection
                        var foundData = gitHubStep.with.Cast<DictionaryEntry>().Any(de => de.Value != null);

                        // If no data was found, null out the with property
                        if (!foundData)
                        {
                            gitHubStep.with = null;
                        }
                    }
                }

                gitHubStep.continue_on_error = step.continueOnError;

                if (step.timeoutInMinutes != 0)
                {
                    gitHubStep.timeout_minutes = step.timeoutInMinutes;
                }
            }

            return gitHubStep;
        }

        // Convert all of the input keys to lowercase, to make pattern matching easier later
        private AzurePipelines.Step CleanStepInputs(AzurePipelines.Step step)
        {
            var newInputs = new OrderedDictionary();

            if (step.inputs != null)
            {
                foreach (DictionaryEntry item in step.inputs)
                {
                    newInputs.Add((item.StringKey()).ToLower(), item.Value);
                }

                step.inputs = newInputs;
            }

            return step;
        }

        private GitHubActions.Step CreateUnconvertedStep(AzurePipelines.Step step, string error = null)
        {
            var gitHubStep = CreateScriptStep(step, ShellType.PowerShell);
            string newYaml = GenericObjectSerialization.SerializeYaml<AzurePipelines.Step>(step);
            string[] newYamlSplit = newYaml.Split(System.Environment.NewLine);
            StringBuilder yamlBuilder = new StringBuilder();

            for (int i = 0; i < newYamlSplit.Length; i++)
            {
                string line = newYamlSplit[i];

                if (line.Trim().Length > 0)
                {
                    yamlBuilder.Append("#");
                    yamlBuilder.Append(line);
                }
            }

            gitHubStep.step_message = error ?? $"Note: Error! This step does not have a conversion path yet: { step.task }";
            gitHubStep.run = $"Write-Host { gitHubStep.step_message } { yamlBuilder }";

            return gitHubStep;
        }

        private GitHubActions.Step CreateDotNetCommandStep(AzurePipelines.Step step)
        {
            if (step.inputs != null)
            {
                string runScript = "dotnet ";

                if (step.inputs.Contains("command") == true)
                {
                    runScript += GetStepInput(step, "command") + " ";
                }

                if (step.inputs.Contains("projects") == true)
                {
                    runScript += GetStepInput(step, "projects") + " ";
                }

                if (step.inputs.Contains("packagestopack") == true)
                {
                    runScript += GetStepInput(step, "packagesToPack") + " ";
                }

                if (step.inputs.Contains("arguments") == true)
                {
                    runScript += GetStepInput(step, "arguments") + " ";
                }

                // Remove the new line characters
                runScript = runScript.Replace("\n", "");
                
                return new GitHubActions.Step
                {
                    run = runScript
                };
            }
            else
            {
                return new GitHubActions.Step
                {
                    step_message = "This DotNetCoreCLI task is misconfigured, inputs are required"
                };
            }
        }

        private GitHubActions.Step CreateDownloadBuildArtifacts(AzurePipelines.Step step)
        {
            var gitHubStep = new GitHubActions.Step
            {
                uses = "actions/download-artifact@v2",
                with = new OrderedDictionary()
            };

            var artifactName = GetStepInput(step, "artifactname");

            // if artifactname is specified, we're downloading a single artifact; if not, then it will download all artifacts
            if (!string.IsNullOrEmpty(artifactName))
            {
                gitHubStep.with.Add("name", artifactName);
            }

            var path = GetStepInput(step, "downloadPath");

            // if the downloadPath is specified, it will download to that parh, otherwise it will download to the current working directory
            if (!string.IsNullOrEmpty(path))
            {
                gitHubStep.with.Add("path", path);
            }

            // https://github.com/actions/download-artifact

            // From: 
            // - task: DownloadBuildArtifacts@0
            //   displayName: 'Download the build artifacts'
            //   inputs:
            //     buildType: 'current'
            //     downloadType: 'single'
            //     artifactName: 'drop'
            //     downloadPath: '$(build.artifactstagingdirectory)'

            // To:
            // - name: Download serviceapp artifact
            //   uses: actions/download-artifact@v2
            //   with:
            //     name: serviceapp
            //     path: /my/path

            return gitHubStep;
        }

        private GitHubActions.Step CreateDownloadPipelineArtifact(AzurePipelines.Step step)
        {
            // From: 
            // - task: DownloadPipelineArtifact@2
            //   inputs:
            //     #source: 'current' # Options: current, specific
            //     #project: # Required when source == Specific
            //     #pipeline: # Required when source == Specific
            //     #preferTriggeringPipeline: false # Optional
            //     #runVersion: 'latest' # Required when source == Specific# Options: latest, latestFromBranch, specific
            //     #runBranch: 'refs/heads/master' # Required when source == Specific && RunVersion == LatestFromBranch
            //     #runId: # Required when source == Specific && RunVersion == Specific
            //     #tags: # Optional
            //     #artifact: # Optional
            //     #patterns: '**' # Optional
            //     #path: '$(Pipeline.Workspace)' 

            // To:
            // - name: Download serviceapp artifact
            //   uses: actions/download-artifact@v2
            //   with:
            //     name: serviceapp

            // No real analogue for DownloadPipelineArtifact@2 when using 'specific' mode, but we'll map this to a simple DownloadArtifact action

            var gitHubStep = new GitHubActions.Step
            {
                uses = "actions/download-artifact@v2",
                with = new OrderedDictionary()
            };

            var artifactName = GetStepInput(step, "artifact");

            // if artifactname is specified, we're downloading a single artifact; if not, then it will download all artifacts
            if (!string.IsNullOrEmpty(artifactName))
            {
                gitHubStep.with.Add("name", artifactName);
            }

            var path = GetStepInput(step, "path");

            // if the downloadPath is specified, it will download to that parh, otherwise it will download to the current working directory
            if (!string.IsNullOrEmpty(path))
            {
                gitHubStep.with.Add("path", path);
            }

            return gitHubStep;
        }

        private GitHubActions.Step CreateAzureFileCopyStep(AzurePipelines.Step step)
        {
            // From:
            // - task: AzureFileCopy@4
            // displayName: 'Copy artifacts to sebuildarchive'
            // inputs:
            //     sourcePath: ${{variables.ArchiveFolder}}
            //     azureSubscription: MCBuildArchive
            //     destination: azureBlob
            //     storage: sebuildarchive
            //     containerName: minecraft
            //     blobPrefix: $(RC_TAG)/${{parameters.Definition}}/${{parameters.BuildId}}

            // To:
            // - name: Azure CLI script
            // uses: azure/CLI@v1
            // with:
            //     azcliversion: 2.0.72 // defaults to latest
            //     inlineScript: |
            //     az storage blob upload-batch --destination { containerName } --source { sourcePath }


            var destination = GetStepInput(step, "destination");

            if (destination.ToLower() != "azureblob")
            {
                return CreateUnconvertedStep(step, "NOTE: AzureFileCopy conversion failed; can not convert task with destination of 'AzureVMs'");
            }

            var sourcePath = GetStepInput(step, "sourcePath");
            var containerName = GetStepInput(step, "containerName");

            var gitHubStep = new GitHubActions.Step
            {
                name = step.displayName,
                uses = "Azure/CLI@v1",
                with = new OrderedDictionary
                {
                    { "inlineScript", $"az storage blob upload-batch --destination { containerName } --source { sourcePath }" }
                },
                DependsOn = GitHubActions.StepDependencies.AzureLogin
            };

            var blobPrefix = GetStepInput(step, "blobPrefix");

            if (!string.IsNullOrEmpty(blobPrefix))
            {
                gitHubStep.with["inlineScript"] += $" --destination-path { blobPrefix }";
            }

            var azureSubscription = GetStepInput(step, "azureSubscription");

            if (!string.IsNullOrEmpty(azureSubscription))
            {
                gitHubStep.with["inlineScript"] += $" --subscription { azureSubscription }";
            }

            var storage = GetStepInput(step, "storage");

            if (!string.IsNullOrEmpty(storage))
            {
                gitHubStep.with["inlineScript"] += $" --account-name { storage }";
            }

            return gitHubStep;
        }

        private GitHubActions.Step CreateCopyFilesStep(AzurePipelines.Step step)
        {
            // Use PowerShell to copy files
            var contents = GetStepInput(step, "contents");
            var paths = contents.Split(System.Environment.NewLine).TakeWhile(s => !string.IsNullOrWhiteSpace(s));
            var targetFolder = GetStepInput(step, "targetfolder");
            
            // ensure folder exists - this tracks with CopyFiles@2 behavior
            step.script += System.Environment.NewLine + $"md -Force { targetFolder }" + System.Environment.NewLine;

            foreach (var path in paths)
            {
                step.script += System.Environment.NewLine + $"Copy '{ GetStepInput(step, "sourcefolder") }/{ path }' '{ targetFolder }'" + System.Environment.NewLine;
            }

            return CreateScriptStep(step, ShellType.PowerShell);
        }

        private GitHubActions.Step CreateDockerStep(AzurePipelines.Step step)
        {
            //From: https://docs.microsoft.com/en-us/azure/devops/pipelines/tasks/build/docker?view=azure-devops
            //- task: Docker@2
            //  displayName: Build
            //  inputs:
            //    command: build
            //    repository: contosoRepository
            //    dockerfile: MyDockerFile
            //    containerRegistry: dockerRegistryServiceConnection
            //    tags: tag1
            //    arguments: --secret id=mysecret,src=mysecret.txt

            //- task: Docker@1
            //  displayName: Push
            //  inputs:
            //    azureSubscriptionEndpoint: '$(Azure.ServiceConnectionId)'
            //    azureContainerRegistry: '$(ACR.FullName)'
            //    imageName: '$(ACR.ImageName)'
            //    command: push


            //To: https://github.com/marketplace/actions/docker-build-push
            //- name: Build the Docker image
            //  run: docker build . --file MyDockerFile --tag my-image-name:$(date +%s)


            //Docker 1 inputs
            //string azureSubscriptionEndpoint = GetStepInput(step, "azureSubscriptionEndpoint");
            //string azureContainerRegistry = GetStepInput(step, "azureContainerRegistry");

            // Docker 2 inputs
            string command = GetStepInput(step, "command");
            string containerRegistry = GetStepInput(step, "containerRegistry");
            string repository = GetStepInput(step, "repository");
            string tags = GetStepInput(step, "tags");
            string dockerFile = GetStepInput(step, "dockerfile");
            //string buildContext = GetStepInput(step, "buildContext");
            string arguments = GetStepInput(step, "arguments");
            string imageName = GetStepInput(step, "imageName");

            // Very very simple. Needs more branches and logic
            string dockerScript = "";
            string stepMessage = "";
            switch (command)
            {
                case "build":
                    dockerScript += "docker build .";
                    break;
                case "push":
                    dockerScript += "docker push";
                    break;
                case "buildAndPush":
                    dockerScript += "docker build-push .";
                    stepMessage = "Note: No conversion path currently exists for build-push (does it need two tasks in GitHub?)";
                    break;
                case "login":
                    dockerScript += "docker login";
                    break;
                case "logout":
                    dockerScript += "docker logout";
                    break;
            }

            if (dockerFile != null)
            {
                dockerScript += " --file " + dockerFile;
            }

            if (containerRegistry != null)
            {
                dockerScript += " " + containerRegistry.Replace("\n", " ").Trim();
            }
            
            if (repository != null)
            {
                dockerScript += " " + repository;
            }

            if (imageName != null)
            {
                dockerScript += " " + imageName;
            }

            if (tags != null)
            {
                string[] splitTags = tags.Split("\n");
                string newTags = "";

                foreach (string item in splitTags)
                {
                    if (item.Trim().Length > 0)
                    {
                        newTags += item.Trim() + ",";
                    }
                }

                dockerScript += " --tags " + newTags;//tags.Replace("\n", ",").Trim();

                if (dockerScript[dockerScript.Length - 1] == ',')
                {
                    dockerScript = dockerScript.Substring(0, dockerScript.Length - 1);
                }
            }

            if (arguments != null)
            {
                dockerScript += " " + arguments;
            }

            step.script = dockerScript;
            GitHubActions.Step gitHubStep = CreateScriptStep(step);

            if (stepMessage != "")
            {
                gitHubStep.step_message = stepMessage;
            }

            return gitHubStep;
        }

        private GitHubActions.Step CreateScriptStep(AzurePipelines.Step step, ShellType shellType = null)
        {
            string targetType = GetStepInput(step, "targetType");
            string arguments = GetStepInput(step, "arguments");

            if (targetType?.ToUpper() == "FILEPATH")
            {
                string filePath = GetStepInput(step, "filePath");
                step.script = filePath + " " + arguments;
            }
            else if (targetType == null && string.IsNullOrEmpty(step.script))
            {
                string fileName = GetStepInput(step, "filename");

                if (fileName != null)
                {
                    step.script = fileName + " " + arguments;
                }
            }

            var gitHubStep = new GitHubActions.Step
            {
                run = step.script,
                shell = shellType?.Value
            };

            if (gitHubStep.run == null)
            {
                if (step.powershell != null)
                {
                    gitHubStep.run = step.powershell;
                }
                else if (step.pwsh != null)
                {
                    gitHubStep.run = step.pwsh;
                }
                else if (step.bash != null)
                {
                    gitHubStep.run = step.bash;
                }
                else
                {
                    if (step.inputs != null)
                    {
                        string runValue = GetStepInput(step, "script") ?? GetStepInput(step, "inlinescript");
                        gitHubStep.run = runValue;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(gitHubStep.run))
            {
                // Spaces on the beginning or end seem to be a problem for the YAML serialization, so we Trim() here
                // Also, accidental carriage returns in scripts (such as a path including a \r) need to be accounted for
                // If this script step includes escaped carriage returns (\\r), switch these to "\\\\r" so that we don't accidentally improperly match these as CRs; we'll fix these up later when we serialize
                gitHubStep.run = gitHubStep.run.Replace("\\r", "\\\\r").Trim();

                var lines = gitHubStep.run.Split(System.Environment.NewLine);

                var emptyLines = lines.Where(l => string.IsNullOrWhiteSpace(l));

                if (emptyLines.Any())
                {
                    gitHubStep.run = string.Join(System.Environment.NewLine, lines.Except(emptyLines));
                }

                if (string.IsNullOrWhiteSpace(gitHubStep.run.Last().ToString())) // yaml.net can choose an alternate serialization style if the last char is whitespace
                {
                    gitHubStep.run = gitHubStep.run.Remove(gitHubStep.run.Length - 1);
                }

                if (gitHubStep.run.StartsWith(System.Environment.NewLine))
                {
                    int i = 0;
                }

                if (string.IsNullOrWhiteSpace(gitHubStep.run.First().ToString()))
                {
                    var leadingWhitespace = true;
                }
            }

            if (string.IsNullOrWhiteSpace(gitHubStep.shell))
            {
                gitHubStep.shell = null;
            }

            if (gitHubStep.run?.Contains("\\\\r") ?? false)
            {
                gitHubStep.step_message = "Note: Script step converted with detected possible carriage return (\\\\r)... Please review script for proper line breaks and correct paths";
            }

            return gitHubStep;
        }

        public GitHubActions.Step CreateCheckoutStep(OrderedDictionary checkoutInputs = null)
        {
            // Add the check out step to get the code
            var checkoutStep = new GitHubActions.Step
            {
                name = "Checkout repo",
                uses = "actions/checkout@v2"
            };

            if (checkoutInputs != null)
            {
                var repo = GetStepInput(checkoutInputs, "repository");

                if (repo == "self") // default is the repo the workflow is running in, so just remove this
                {
                    checkoutInputs.Remove("repository");
                }

                checkoutStep.with = checkoutInputs;
            }

            return checkoutStep;
        }

        public GitHubActions.Step CreateAzureLoginStep()
        {
            // Goal:
            // - name: Azure Login
            //   uses: Azure/login@v1
            //   with:
            //     creds: ${{ secrets.AZURE_SP }}
            var gitHubStep = new GitHubActions.Step
            {
                name = "Azure Login",
                uses = "Azure/login@v1",
                with = new OrderedDictionary
                {
                    { "creds", "${{ secrets.AZURE_SP }}" }
                },
                id = "AzureLogin"
            };

            // Add note that 'AZURE_SP' secret is required
            gitHubStep.step_message = @"Note: 'AZURE_SP' secret is required to be setup and added into GitHub Secrets: https://help.github.com/en/actions/automating-your-workflow-with-github-actions/creating-and-using-encrypted-secrets";

            return gitHubStep;
        }

        public GitHubActions.Step CreateAzureKeyVaultStep(VariableGroup keyVaultGroup, IEnumerable<string> secrets = null)
        {
            // Goal:
            // - name: Retrieve Key Vault Secrets
            //   uses: Azure/get-keyvault-secrets@v1.1
            //   with:
            //     keyvault: <kv Name>
            //     secrets: 'one,two,three'
            var gitHubStep = new GitHubActions.Step
            {
                name = "Retrieve Key Vault Secrets",
                uses = "Azure/get-keyvault-secrets@v1.1",
                with = new OrderedDictionary
                {
                    { "keyvault", keyVaultGroup.name }
                },
                id = keyVaultGroup.name,
                DependsOn = GitHubActions.StepDependencies.AzureLogin
            };

            if (secrets == null) // if no list is passed, we'll create a list with ALL secrets here, and whittle it down later once we've discovered which secrets are consumed
            {
                secrets = keyVaultGroup.variables.Select(v => v.Key);
            }

            gitHubStep.with.Add("secrets", $"{ string.Join(",", secrets) }");

            return gitHubStep;
        }

        private GitHubActions.Step CreateUseDotNetStep(AzurePipelines.Step step)
        {
            var gitHubStep = new GitHubActions.Step
            {
                uses = "actions/setup-dotnet@v1",
                with = new OrderedDictionary
                {
                    { "dotnet-version", GetStepInput(step, "version") }
                }
            };
            // Pipelines
            // - task: UseDotNet@2
            //   displayName: 'Use .NET Core sdk'
            //   inputs:
            //     packageType: sdk
            //     version: 2.2.203
            //     installationPath: $(Agent.ToolsDirectory)/dotnet

            // Actions
            // - uses: actions/setup-dotnet@v1
            //   with:
            //     dotnet-version: '2.2.103' # SDK Version to use.
            return gitHubStep;
        }

        private GitHubActions.Step CreateAzureManageResourcesStep(AzurePipelines.Step step)
        {
            // coming from:
            // - task: AzureResourceGroupDeployment@2
            //   displayName: 'Deploy ARM Template to resource group'
            //   inputs:
            //     azureSubscription: 'connection to Azure Portal'
            //     resourceGroupName: $(ResourceGroupName)
            //     location: '[resourceGroup().location]'
            //     csmFile: '$(build.artifactstagingdirectory)/drop/ARMTemplates/azuredeploy.json'
            //     csmParametersFile: '$(build.artifactstagingdirectory)/drop/ARMTemplates/azuredeploy.parameters.json'
            //     overrideParameters: '-environment $(AppSettings.Environment) -locationShort $(ArmTemplateResourceGroupLocation)'

            // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-cli
            // - name: Swap web service staging slot to production
            //   uses: Azure/cli@v1.0.0
            //   with:
            //     inlineScript: az deployment group create --resource-group <resource-group-name> --template-file <path-to-template>

            string resourceGroup = GetStepInput(step, "resourcegroupname");
            string armTemplateFile = GetStepInput(step, "csmfile");
            string armTemplateParametersFile = GetStepInput(step, "csmparametersfile");
            string overrideParameters = GetStepInput(step, "overrideParameters");

            string script = "az deployment group create --resource-group " + resourceGroup +
                " --template-file " + armTemplateFile;

            // Add parameters
            if (string.IsNullOrEmpty(armTemplateParametersFile) == false || string.IsNullOrEmpty(overrideParameters) == false)
            {
                string parameters = " --parameters ";

                if (string.IsNullOrEmpty(armTemplateParametersFile) == false)
                {
                    parameters += " " + armTemplateParametersFile;
                }
                if (string.IsNullOrEmpty(overrideParameters) == false)
                {
                    parameters += " " + overrideParameters;
                }

                script += parameters;
            }

            return new GitHubActions.Step
            {
                uses = "Azure/cli@v1.0.0",
                with = new OrderedDictionary
                {
                    { "inlineScript", script }
                },
                DependsOn = GitHubActions.StepDependencies.AzureLogin
            };
        }

        private GitHubActions.Step CreateAzureWebAppDeploymentStep(AzurePipelines.Step step)
        {
            string webappName = GetStepInput(step, "webappname");
            string appName = GetStepInput(step, "appName");
            string package = GetStepInput(step, "package");
            string slotName = GetStepInput(step, "slotname");
            string imageName = GetStepInput(step, "imageName");

            var gitHubStep = new GitHubActions.Step
            {
                uses = "Azure/webapps-deploy@v2",
                with = new OrderedDictionary(),
                DependsOn = GitHubActions.StepDependencies.AzureLogin
            };

            if (webappName != null)
            {
                gitHubStep.with.Add("app-name", webappName);
            }
            else if (appName != null)
            {
                gitHubStep.with.Add("app-name", appName);
            }

            if (package != null)
            {
                gitHubStep.with.Add("package", package);
            }

            if (slotName != null)
            {
                gitHubStep.with.Add("slot-name", slotName);
            }

            if (imageName != null)
            {
                gitHubStep.with.Add("images", imageName);
            }

            // coming from:
            // - task: AzureRmWebAppDeployment@3
            //   displayName: 'Azure App Service Deploy: web service'
            //   inputs:
            //     azureSubscription: 'connection to Azure Portal'
            //     WebAppName: $(WebServiceName)
            //     DeployToSlotFlag: true
            //     ResourceGroupName: $(ResourceGroupName)
            //     SlotName: 'staging'
            //     Package: '$(build.artifactstagingdirectory)/drop/MyProject.Service.zip'
            //     TakeAppOfflineFlag: true
            //     JSONFiles: '**/appsettings.json'

            // Going to:
            // - name: Deploy web service to Azure WebApp
            //   uses: Azure/webapps-deploy@v1
            //   with:
            //     app-name: myproject-service
            //     package: serviceapp
            //     slot-name: staging   

            return gitHubStep;
        }

        private GitHubActions.Step CreateNuGetCommandStep(AzurePipelines.Step step)
        {
            string command = GetStepInput(step, "command");

            if (string.IsNullOrEmpty(command) == false)
            {
                command = "restore";
            }

            string restoresolution = GetStepInput(step, "restoresolution");

            var gitHubStep = CreateScriptStep(step, ShellType.PowerShell);
            gitHubStep.run = "nuget " + command + " " + restoresolution;

            //coming from:
            //# NuGet
            //# Restore, pack, or push NuGet packages, or run a NuGet command. Supports NuGet.org and authenticated feeds like Azure Artifacts and MyGet. Uses NuGet.exe and works with .NET Framework apps. For .NET Core and .NET Standard apps, use the .NET Core task.
            //- task: NuGetCommand@2
            //  inputs:
            //    #command: 'restore' # Options: restore, pack, push, custom
            //    #restoreSolution: '**/*.sln' # Required when command == Restore
            //    #feedsToUse: 'select' # Options: select, config
            //    #vstsFeed: # Required when feedsToUse == Select
            //    #includeNuGetOrg: true # Required when feedsToUse == Select
            //    #nugetConfigPath: # Required when feedsToUse == Config
            //    #externalFeedCredentials: # Optional
            //    #noCache: false 
            //    #disableParallelProcessing: false 
            //    restoreDirectory: 
            //    #verbosityRestore: 'Detailed' # Options: quiet, normal, detailed
            //    #packagesToPush: '$(Build.ArtifactStagingDirectory)/**/*.nupkg;!$(Build.ArtifactStagingDirectory)/**/*.symbols.nupkg' # Required when command == Push
            //    #nuGetFeedType: 'internal' # Required when command == Push# Options: internal, external
            //    #publishVstsFeed: # Required when command == Push && NuGetFeedType == Internal
            //    #publishPackageMetadata: true # Optional
            //    #allowPackageConflicts: # Optional
            //    #publishFeedCredentials: # Required when command == Push && NuGetFeedType == External
            //    #verbosityPush: 'Detailed' # Options: quiet, normal, detailed
            //    #packagesToPack: '**/*.csproj' # Required when command == Pack
            //    #configuration: '$(BuildConfiguration)' # Optional
            //    #packDestination: '$(Build.ArtifactStagingDirectory)' # Optional
            //    #versioningScheme: 'off' # Options: off, byPrereleaseNumber, byEnvVar, byBuildNumber
            //    #includeReferencedProjects: false # Optional
            //    #versionEnvVar: # Required when versioningScheme == ByEnvVar
            //    #majorVersion: '1' # Required when versioningScheme == ByPrereleaseNumber
            //    #minorVersion: '0' # Required when versioningScheme == ByPrereleaseNumber
            //    #patchVersion: '0' # Required when versioningScheme == ByPrereleaseNumber
            //    #packTimezone: 'utc' # Required when versioningScheme == ByPrereleaseNumber# Options: utc, local
            //    #includeSymbols: false # Optional
            //    #toolPackage: # Optional
            //    #buildProperties: # Optional
            //    #basePath: # Optional, specify path to nuspec files
            //    #verbosityPack: 'Detailed' # Options: quiet, normal, detailed
            //    #arguments: # Required when command == Custom

            //Going to:
            //- name: Nuget Push
            //  run: nuget push *.nupkg

            return gitHubStep;
        }

        // https://github.com/warrenbuckley/Setup-Nuget
        private GitHubActions.Step CreateNuGetToolInstallerStep()
        {
            var gitHubStep = new GitHubActions.Step
            {
                uses = "warrenbuckley/Setup-Nuget@v1",
                step_message = "Note: This is a third party action: https://github.com/warrenbuckley/Setup-Nuget"
            };

            // coming from:
            // # NuGet tool installer
            // # Acquires a specific version of NuGet from the internet or the tools cache and adds it to the PATH. Use this task to change the version of NuGet used in the NuGet tasks.
            // - task: NuGetToolInstaller@0
            //   inputs:
            //     #versionSpec: '4.3.0' 
            //     #checkLatest: false # Optional

            // Going to:
            // - name: Setup Nuget.exe
            //   uses: warrenbuckley/Setup-Nuget@v1

            return gitHubStep;
        }

        // https://github.com/Azure/sql-action
        private GitHubActions.Step CreateSQLAzureDacPacDeployStep(AzurePipelines.Step step)
        {
            string serverName = GetStepInput(step, "servername");
            string dacPacFile = GetStepInput(step, "dacpacfile");
            string arguments = GetStepInput(step, "additionalarguments");

            var gitHubStep = new GitHubActions.Step
            {
                uses = "azure/sql-action@v1",
                with = new OrderedDictionary
                {
                    { "server-name", serverName },
                    { "connection-string", "${{ secrets.AZURE_SQL_CONNECTION_STRING }}" },
                    { "dacpac-package", dacPacFile },
                    { "arguments", arguments }
                },
                step_message = "Note: Connection string needs to be specified - this is different than Pipelines where the server, database, user, and password were specified separately. It's recommended you use secrets for the connection string."
            };

            //coming from:
            //- task: SqlAzureDacpacDeployment@1
            //  displayName: 'Azure SQL dacpac publish'
            //  inputs:
            //    azureSubscription: 'my connection to Azure Portal'
            //    ServerName: '$(databaseServerName).database.windows.net'
            //    DatabaseName: '$(databaseName)'
            //    SqlUsername: '$(databaseLoginName)'
            //    SqlPassword: '$(databaseLoginPassword)'
            //    DacpacFile: '$(build.artifactstagingdirectory)/drop/MyDatabase.dacpac'
            //    additionalArguments: '/p:BlockOnPossibleDataLoss=true'  

            //Going to:
            //- uses: azure/sql-action@v1
            //  with:
            //    server-name: REPLACE_THIS_WITH_YOUR_SQL_SERVER_NAME
            //    connection-string: ${{ secrets.AZURE_SQL_CONNECTION_STRING }}
            //    dacpac-package: './yourdacpacfile.dacpac'

            return gitHubStep;
        }

        private GitHubActions.Step CreateMSBuildStep(AzurePipelines.Step step)
        {
            // coming from:
            // # Visual Studio build
            // # Build with MSBuild and set the Visual Studio version property
            // - task: VSBuild@1
            //   inputs:
            //     solution: 'MySolution.sln' 
            //     vsVersion: 'latest' # Optional. Options: latest, 16.0, 15.0, 14.0, 12.0, 11.0
            //     msbuildArgs: # Optional
            //     platform: # Optional
            //     configuration: # Optional
            //     clean: false # Optional
            //     maximumCpuCount: false # Optional
            //     restoreNugetPackages: false # Optional
            //     msbuildArchitecture: 'x86' # Optional. Options: x86, x64
            //     logProjectEvents: true # Optional
            //     createLogFile: false # Optional
            //     logFileVerbosity: 'normal' # Optional. Options: quiet, minimal, normal, detailed, diagnostic

            // Going to:
            // - run: msbuild MySolution.sln /p:configuration=release

            string solution = GetStepInput(step, "solution");
            string platform = GetStepInput(step, "platform");
            string configuration = GetStepInput(step, "configuration");
            string msbuildArgs = GetStepInput(step, "msbuildArgs");
            string run = "msbuild '" + solution + "'";

            if (configuration != null)
            {
                run += " /p:configuration='" + configuration + "'";
            }

            if (platform != null)
            {
                run += " /p:platform='" + platform + "'";
            }

            if (msbuildArgs != null)
            {
                run += " " + msbuildArgs;
            }

            step.script = run;

            // To script
            var gitHubStep = CreateScriptStep(step);
            gitHubStep.run = run;
            gitHubStep.DependsOn = GitHubActions.StepDependencies.MSBuildSetup;

            return gitHubStep;
        }

        public GitHubActions.Step CreateMSBuildSetupStep()
        {
            // To:
            // - name: Setup MSBuild.exe
            //   uses: microsoft/setup-msbuild@v1.0.0

            return new GitHubActions.Step
            {
                uses = "microsoft/setup-msbuild@v1.0.0"
            };
        }

        private GitHubActions.Step CreateFunctionalTestingStep(AzurePipelines.Step step)
        {
            //From:
            //- task: VSTest@2
            //  displayName: 'Run functional smoke tests on website and web service'
            //  inputs:
            //    searchFolder: '$(build.artifactstagingdirectory)'
            //    testAssemblyVer2: **\MyProject.FunctionalTests\MyProject.FunctionalTests.dll
            //    uiTests: true
            //    runSettingsFile: '$(build.artifactstagingdirectory)/drop/FunctionalTests/MyProject.FunctionalTests/test.runsettings'
            //    overrideTestrunParameters: |
            //     -ServiceUrl "https://$(WebServiceName)-staging.azurewebsites.net/" 
            //     -WebsiteUrl "https://$(WebsiteName)-staging.azurewebsites.net/" 
            //     -TestEnvironment "$(AppSettings.Environment)" 

            //To:
            //- name: Functional Tests
            //  run: |
            //    $vsTestConsoleExe = "C:\\Program Files (x86)\\Microsoft Visual Studio\\2019\\Enterprise\\Common7\\IDE\\Extensions\\TestPlatform\\vstest.console.exe"
            //    $targetTestDll = "functionaltests\FeatureFlags.FunctionalTests.dll"
            //    $testRunSettings = "/Settings:`"functionaltests\test.runsettings`" "
            //    $parameters = " -- TestEnvironment=""Beta123"" ServiceUrl=""https://featureflags-data-eu-service-staging.azurewebsites.net/"" WebsiteUrl=""https://featureflags-data-eu-web-staging.azurewebsites.net/"" "
            //    #Note that the `" is an escape character to quote strings, and the `& is needed to start the command
            //    $command = "`& `"$vsTestConsoleExe`" `"$targetTestDll`" $testRunSettings $parameters " 
            //    Write-Host "$command"
            //    Invoke-Expression $command

            // Defined in the github windows runner.
            // TODO: fix this hardcoded VS path
            string vsTestConsoleLocation = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\Extensions\TestPlatform\";

            string run = "";
            run += "$vsTestConsoleExe = \"" + vsTestConsoleLocation + "vstest.console.exe\"\n";
            run += "$targetTestDll = \"" + GetStepInput(step, "testassemblyver2") + "\"\n";
            run += "$testRunSettings = \"/Settings:`\"" + GetStepInput(step, "runsettingsfile") + "`\" \"\n";

            string parametersInput = GetStepInput(step, "overridetestrunparameters");

            if (parametersInput != null)
            {
                // Split it two ways, there are 3 combinations, parameters are on each new line, parameters are all on one line, a combination of both multi and single lines.
                // 1. Multiline
                string[] multiLineParameters = parametersInput.Split("\n-");
                StringBuilder parameters = new StringBuilder();

                foreach (string multiLineItem in multiLineParameters)
                {
                    // 2. Single line 
                    string[] singleLineParameters = multiLineItem.Split(" -");

                    foreach (string item in singleLineParameters)
                    {
                        string[] items = item.Replace("\n", "").Split(" ");

                        if (items.Length == 2)
                        {
                            // build the new format [var name]=[var value]
                            parameters.Append(items[0]);
                            parameters.Append("=");
                            parameters.Append(items[1]);
                            parameters.Append(" ");
                        }
                        else
                        {
                            for (int i = 0; i < items.Length - 1; i++)
                            {
                                // if it's an even number (and hence the var name):
                                if (i % 2 == 0)
                                {
                                    // Sometimes the first item has an extra -, remove this.
                                    if (items[i].ToString().StartsWith("-") == true)
                                    {
                                        items[i] = items[i].TrimStart('-');
                                    }

                                    // build the new format [var name]=[var value]
                                    parameters.Append(items[i]);
                                    parameters.Append("=");
                                }
                                else // It's an odd number (and hence the var value)
                                {
                                    // build the new format [var name]=[var value]
                                    parameters.Append(items[i]);
                                    parameters.Append(" ");
                                }
                            }
                        }
                    }
                }

                run += "$parameters = \" -- " + parameters.ToString() + "\"\n";
                //run += "$parameters = \"poop\"\n";
            }

            run += "#Note that the `\" is an escape character to quote strings, and the `& is needed to start the command\n";
            run += "$command = \"`& `\"$vsTestConsoleExe`\" `\"$targetTestDll`\" $testRunSettings $parameters \"\n";
            run += "Write-Host \"$command\"\n";
            run += "Invoke-Expression $command";

            // To PowerShell script
            step.script = run;

            return CreateScriptStep(step, ShellType.PowerShell);
        }

        public GitHubActions.Step CreateSetupJavaStep(string javaVersion)
        {
            return new GitHubActions.Step
            {
                name = "Setup JDK " + javaVersion,
                uses = "actions/setup-java@v1",
                with = new OrderedDictionary
                {
                    { "java-version", javaVersion }
                }
            };
        }

        public GitHubActions.Step CreateSetupGradleStep()
        {
            // Going to: 
            // - name: Grant execute permission for gradlew
            //   run: chmod +x gradlew

            var step = new AzurePipelines.Step
            {
                name = "Grant execute permission for gradlew",
                script = "chmod +x gradlew"
            };

            return CreateScriptStep(step);
        }

        public GitHubActions.Step CreateGradleStep(AzurePipelines.Step step)
        {
            // coming from:
            // - task: Gradle@2
            //  inputs:
            //    workingDirectory: ''
            //    gradleWrapperFile: 'gradlew'
            //    gradleOptions: '-Xmx3072m'
            //    publishJUnitResults: false
            //    testResultsFiles: '**/TEST-*.xml'
            //    tasks: 'assembleDebug'

            // Going to:
            // - name: Build with Gradle
            //   run: ./gradlew build

            step.script = "./gradlew build";

            var newStep = CreateScriptStep(step);
            newStep.DependsOn = GitHubActions.StepDependencies.JavaSetup | GitHubActions.StepDependencies.GradleSetup;

            return newStep;
        }

        // TODO: Finish this Kubernetes Step
        //public GitHubActions.Step CreateKubernetesStep(AzurePipelines.Step step)
        //{
        //    //coming from: https://docs.microsoft.com/en-us/azure/devops/pipelines/tasks/deploy/kubernetes?view=azure-devops

        //    //Azure Resource Manager service connection
        //    //- task: Kubernetes@1
        //    //  displayName: kubectl apply
        //    //  inputs:
        //    //    connectionType: Azure Resource Manager
        //    //    azureSubscriptionEndpoint: Contoso
        //    //    azureResourceGroup: contoso.azurecr.io
        //    //    kubernetesCluster: Contoso
        //    //    useClusterAdmin: false

        //    //Kubernetes Service Connection
        //    //- task: Kubernetes@1
        //    //  displayName: kubectl apply
        //    //  inputs:
        //    //    connectionType: Kubernetes Service Connection
        //    //    kubernetesServiceEndpoint: Contoso

        //    //This YAML example demonstrates the apply command:
        //    //- task: Kubernetes@1
        //    //  displayName: kubectl apply using arguments
        //    //  inputs:
        //    //    connectionType: Azure Resource Manager
        //    //    azureSubscriptionEndpoint: $(azureSubscriptionEndpoint)
        //    //    azureResourceGroup: $(azureResourceGroup)
        //    //    kubernetesCluster: $(kubernetesCluster)
        //    //    command: apply
        //    //    arguments: -f mhc-aks.yaml

        //    //This YAML example demonstrates the use of a configuration file with the apply command:
        //    //- task: Kubernetes@1
        //    //  displayName: kubectl apply using configFile
        //    //  inputs:
        //    //    connectionType: Azure Resource Manager
        //    //    azureSubscriptionEndpoint: $(azureSubscriptionEndpoint)
        //    //    azureResourceGroup: $(azureResourceGroup)
        //    //    kubernetesCluster: $(kubernetesCluster)
        //    //    command: apply
        //    //    useConfigurationFile: true
        //    //    configuration: mhc-aks.yaml

        //    //This YAML example demonstrates the setting up of ImagePullSecrets:
        //    //- task: Kubernetes@1
        //    //  displayName: kubectl apply for secretType dockerRegistry
        //    //  inputs:
        //    //    azureSubscriptionEndpoint: $(azureSubscriptionEndpoint)
        //    //    azureResourceGroup: $(azureResourceGroup)
        //    //    kubernetesCluster: $(kubernetesCluster)
        //    //    command: apply
        //    //    arguments: -f mhc-aks.yaml
        //    //    secretType: dockerRegistry
        //    //    containerRegistryType: Azure Container Registry
        //    //    azureSubscriptionEndpointForSecrets: $(azureSubscriptionEndpoint)
        //    //    azureContainerRegistry: $(azureContainerRegistry)
        //    //    secretName: mysecretkey2
        //    //    forceUpdate: true

        //    //This YAML example creates generic secrets from literal values specified for the secretArguments input:
        //    //- task: Kubernetes@1
        //    //  displayName: secretType generic with literal values
        //    //  inputs:
        //    //    azureSubscriptionEndpoint: $(azureSubscriptionEndpoint)
        //    //    azureResourceGroup: $(azureResourceGroup)
        //    //    kubernetesCluster: $(kubernetesCluster)
        //    //    command: apply
        //    //    arguments: -f mhc-aks.yaml
        //    //    secretType: generic
        //    //    secretArguments: --from-literal=contoso=5678
        //    //    secretName: mysecretkey

        //    //This YAML example creates a ConfigMap by pointing to a ConfigMap file:
        //    //- task: Kubernetes@1
        //    //  displayName: kubectl apply
        //    //  inputs:
        //    //    configMapName: myconfig
        //    //    useConfigMapFile: true
        //    //    configMapFile: src/configmap

        //    //Going to:


        //    string arguments = GetStepInput(step, "arguments");
        //    string command = GetStepInput(step, "command");
        //    string configMapFile = GetStepInput(step, "configMapFile");
        //    string configMapName = GetStepInput(step, "configMapName");
        //    string configuration = GetStepInput(step, "configuration");
        //    string connectionType = GetStepInput(step, "connectionType");
        //    string containerRegistryType = GetStepInput(step, "containerRegistryType");
        //    string azureContainerRegistry = GetStepInput(step, "azureContainerRegistry");
        //    string azureSubscriptionEndpoint = GetStepInput(step, "azureSubscriptionEndpoint");
        //    string azureSubscriptionEndpointForSecrets = GetStepInput(step, "azureSubscriptionEndpointForSecrets");
        //    string azureResourceGroup = GetStepInput(step, "azureResourceGroup");
        //    string forceUpdate = GetStepInput(step, "forceUpdate");
        //    string kubernetesCluster = GetStepInput(step, "kubernetesCluster");
        //    string kubernetesServiceEndpoint = GetStepInput(step, "kubernetesServiceEndpoint");
        //    string secretArguments = GetStepInput(step, "secretArguments");
        //    string secretName = GetStepInput(step, "secretName");
        //    string secretType = GetStepInput(step, "secretType");
        //    string useClusterAdmin = GetStepInput(step, "useClusterAdmin");
        //    string useConfigMapFile = GetStepInput(step, "useConfigMapFile");
        //    string useConfigurationFile = GetStepInput(step, "useConfigurationFile");


        //    step.script = "";
        //    GitHubActions.Step gitHubStep = CreateScriptStep("", step);

        //    return gitHubStep;
        //}

        private GitHubActions.Step CreateAntStep(AzurePipelines.Step step)
        {
            // coming from:
            // - task: Ant@1
            //   inputs:
            //     workingDirectory: ''
            //     buildFile: 'build.xml'
            //     javaHomeOption: 'JDKVersion'
            //     jdkVersionOption: '1.8'
            //     jdkArchitectureOption: 'x64'
            //     publishJUnitResults: true
            //     testResultsFiles: '**/TEST-*.xml'  

            // Going to:
            // - name: Build with Ant
            //   run: ant -noinput -buildfile build.xml

            string buildFile = GetStepInput(step, "buildFile");

            string antCommand = "ant -noinput -buildfile " + buildFile;
            step.script = antCommand;

            var newStep = CreateScriptStep(step);
            newStep.DependsOn = GitHubActions.StepDependencies.JavaSetup;

            return newStep;
        }

        private GitHubActions.Step CreateMavenStep(AzurePipelines.Step step)
        {
            // coming from:
            // - task: Maven@3
            //   inputs:
            //     mavenPomFile: 'Maven/pom.xml'
            //     mavenOptions: '-Xmx3072m'
            //     javaHomeOption: 'JDKVersion'
            //     jdkVersionOption: '1.8'
            //     jdkArchitectureOption: 'x64'
            //     publishJUnitResults: true
            //     testResultsFiles: '**/surefire-reports/TEST-*.xml'
            //     goals: 'package' 

            // Going to:
            // - name: Build with Maven
            //   run: mvn -B package --file pom.xml

            string pomFile = GetStepInput(step, "mavenPomFile");

            string pomCommand = "mvn -B package --file " + pomFile;
            step.script = pomCommand;

            var newStep = CreateScriptStep(step);
            newStep.DependsOn = GitHubActions.StepDependencies.JavaSetup;

            return newStep;
        }

        private GitHubActions.Step CreateNPMStep(AzurePipelines.Step step)
        {
            // https://docs.microsoft.com/en-us/azure/devops/pipelines/tasks/package/npm?view=azure-devops
            // coming from:
            // # npm
            // # Install and publish npm packages, or run an npm command. Supports npmjs.com and authenticated registries like Azure Artifacts.
            // - task: Npm@1
            //   inputs:
            //     #command: 'install' # Options: install, publish, custom
            //     #workingDir: # Optional
            //     #verbose: # Optional
            //     #customCommand: # Required when command == Custom
            //     #customRegistry: 'useNpmrc' # Optional. Options: useNpmrc, useFeed
            //     #customFeed: # Required when customRegistry == UseFeed
            //     #customEndpoint: # Optional
            //     #publishRegistry: 'useExternalRegistry' # Optional. Options: useExternalRegistry, useFeed
            //     #publishFeed: # Required when publishRegistry == UseFeed
            //     #publishPackageMetadata: true # Optional
            //     #publishEndpoint: # Required when publishRegistry == UseExternalRegistry

            // Example:
            // - task: Npm@1
            //   displayName: 'npm install'
            //   inputs:
            //     command: install
            //     workingDir: src/angular7

            // - task: Npm@1
            //   displayName: 'Build Angular'
            //   inputs:
            //     command: custom
            //     customCommand: run build -- --prod
            //     workingDir: src/angular7


            // Going to:
            // run: npm publish --access public

            string command = GetStepInput(step, "command");
            string workingDir = GetStepInput(step, "workingDir");
            string customCommand = GetStepInput(step, "customCommand");

            if (command == "custom")
            {
                step.script = "npm " + customCommand;
            }
            else
            {
                step.script = "npm " + command;
            }

            if (string.IsNullOrEmpty(workingDir) == false)
            {
                step.script += " " + workingDir;
            }

            return CreateScriptStep(step);
        }

        private GitHubActions.Step CreateNodeToolStep(AzurePipelines.Step step)
        {
            //coming from:
            //- task: NodeTool@0
            //  inputs:
            //    versionSpec: '10.x'
            //  displayName: 'Install Node.js'

            //Going to:
            //- name: Use Node.js 10.x
            //  uses: actions/setup-node@v1
            //  with:
            //    node-version: 10.x

            string version = GetStepInput(step, "versionSpec");

            return new GitHubActions.Step
            {
                name = "Use Node.js " + version,
                uses = "actions/setup-node@v1",
                with = new OrderedDictionary
                {
                    { "node-version", version }
                }
            };
        }

        private GitHubActions.Step CreateUsePythonStep(AzurePipelines.Step step)
        {
            //coming from:
            //- task: UsePythonVersion@0
            //  inputs:
            //    versionSpec: '3.7'
            //    addToPath: true
            //    architecture: 'x64'

            //Going to:
            //- name: Setup Python 3.7
            //  uses: actions/setup-python@v1
            //  with:
            //    python-version: '3.7'

            string version = GetStepInput(step, "versionSpec");

            return new GitHubActions.Step
            {
                name = "Setup Python " + version,
                uses = "actions/setup-python@v1",
                with = new OrderedDictionary
                {
                    { "python-version", version }
                }
            };
        }

        private GitHubActions.Step CreateUseRubyStep(AzurePipelines.Step step)
        {
            //coming from:
            //# Use Ruby version: Use the specified version of Ruby from the tool cache, optionally adding it to the PATH
            //- task: UseRubyVersion@0
            //  inputs:
            //    #versionSpec: '>= 2.4' 
            //    #addToPath: true # Optional

            //Going to:
            //- uses: actions/setup-ruby@v1
            //  with:
            //    ruby-version: 2.6.x


            string version = GetStepInput(step, "versionSpec");

            return new GitHubActions.Step
            {
                name = "Setup Ruby " + version,
                uses = "actions/setup-ruby@v1",
                with = new OrderedDictionary
                {
                    { "ruby-version", version }
                }
            };
        }

        private GitHubActions.Step CreatePythonStep(AzurePipelines.Step step)
        {
            //coming from:
            //- task: PythonScript@0
            //  inputs:
            //    scriptSource: 'filePath'
            //    scriptPath: 'Python/Hello.py'

            //Going to:
            //- run: python Python/Hello.py

            var scriptPath = GetStepInput(step, "scriptPath");

            var pythonCommand = "python " + scriptPath;
            step.script = pythonCommand;

            var args = GetStepInput(step, "arguments");

            if (!string.IsNullOrEmpty(args))
            {
                step.script += $" {args}";
            }

            return CreateScriptStep(step);
        }

        private GitHubActions.Step CreatePublishTestResultsStep(AzurePipelines.Step step)
        {
            //coming from:
            //# Publish Test Results
            //- task: PublishTestResults@2
            //  inputs:
            //    #testResultsFormat: 'JUnit' # Options: JUnit, NUnit, VSTest, xUnit, cTest
            //    #testResultsFiles: '**/TEST-*.xml' 
            //    #searchFolder: '$(System.DefaultWorkingDirectory)' # Optional
            //    #mergeTestResults: false # Optional
            //    #failTaskOnFailedTests: false # Optional
            //    #testRunTitle: # Optional
            //    #buildPlatform: # Optional
            //    #buildConfiguration: # Optional
            //    #publishRunAttachments: true # Optional

            //TODO: Monitor this when a testing tab is finally added to GitHub
            //Going to:
            //- run: echo "This task equivalent does not yet exist in GitHub Actions"

            //string scriptPath = GetStepInput(step, "scriptPath");

            string command = @"echo ""This task equivalent does not yet exist in GitHub Actions""";
            step.script = command;

            GitHubActions.Step gitHubStep = CreateScriptStep(step);

            gitHubStep.step_message = "PublishTestResults@2 is a Azure DevOps specific task. There is no equivalent in GitHub Actions until there is a testing summary tab. See: https://github.community/t/publishing-test-results/16215";

            return gitHubStep;
        }

        private GitHubActions.Step CreateArchiveFilesStep(AzurePipelines.Step step)
        {
            //coming from:
            //- task: ArchiveFiles@2
            //  displayName: 'Archive files'
            //  inputs:
            //    rootFolderOrFile: '$(System.DefaultWorkingDirectory)/publish_output'
            //    includeRootFolder: false
            //    archiveType: zip
            //    archiveFile: $(Build.ArtifactStagingDirectory)/$(Build.BuildId).zip
            //    replaceExistingArchive: true

            //Going to: //https://github.com/marketplace/actions/create-zip-file
            //- uses: montudor/action-zip@v0.1.0
            //  with:
            //    args: zip -qq -r ./dir.zip ./dir

            string rootFolderOrFile = GetStepInput(step, "rootFolderOrFile");
            string archiveFile = GetStepInput(step, "archiveFile");

            string zipCommand = "zip -qq -r " + archiveFile + " " + rootFolderOrFile;

            return new GitHubActions.Step
            {
                uses = "montudor/action-zip@v0.1.0",
                with = new OrderedDictionary
                {
                    { "args", zipCommand }
                },
                step_message = "Note: This is a third party action: https://github.com/marketplace/actions/create-zip-file"
            };
        }

        private GitHubActions.Step CreateAzureAppServiceManageStep(AzurePipelines.Step step)
        {
            // https://docs.microsoft.com/en-us/azure/devops/pipelines/tasks/deploy/azure-app-service-manage?view=azure-devops
            // coming from:
            // - task: AzureAppServiceManage@0
            // displayName: 'Swap Slots: web service'
            // inputs:
            //   azureSubscription: 'connection to Azure Portal'
            //   WebAppName: $(WebServiceName)
            //   ResourceGroupName: $(ResourceGroupName)
            //   SourceSlot: 'staging'

            // Going to:
            // - name: Swap web service staging slot to production
            //   uses: Azure/cli@v1.0.0
            //   with:
            //     inlineScript: az webapp deployment slot swap --resource-group MyProjectRG --name featureflags-data-eu-service --slot staging --target-slot production

            string resourceGroup = GetStepInput(step, "resourcegroupname");
            string webAppName = GetStepInput(step, "webappname");
            string sourceSlot = GetStepInput(step, "sourceslot");
            string targetSlot = GetStepInput(step, "targetslot");

            if (string.IsNullOrEmpty(targetSlot))
            {
                targetSlot = "production";
            }

            // TODO: Add other properties for az webapp deployment

            string script = "az webapp deployment slot swap --resource-group " + resourceGroup +
                " --name " + webAppName +
                " --slot " + sourceSlot +
                " --target-slot " + targetSlot + "";

            return new GitHubActions.Step
            {
                uses = "Azure/cli@v1.0.0",
                with = new OrderedDictionary
                {
                    { "inlineScript", script }
                },
                DependsOn = GitHubActions.StepDependencies.AzureLogin
            };
        }

        private GitHubActions.Step CreateXamarinAndroidStep(AzurePipelines.Step step)
        {
            //coming from:
            //- task: XamarinAndroid@1
            //  inputs:
            //    projectFile: '**/*droid*.csproj'
            //    outputDirectory: '$(outputDirectory)'
            //    configuration: '$(buildConfiguration)'

            //Going to: https://levelup.gitconnected.com/using-github-actions-with-ios-and-android-xamarin-apps-693a93b48a61
            //- name: Android
            //  run: |
            //    cd Blank
            //    nuget restore
            //    cd Blank.Android
            //    msbuild '**/*droid*.csproj' /verbosity:normal /t:Rebuild /p:Configuration='$(buildConfiguration)'

            string projectFile = GetStepInput(step, "projectFile");
            string configuration = GetStepInput(step, "configuration");

            string script = "" +
            "cd Blank\n" +
            "nuget restore\n" +
            "cd Blank.Android\n" +
            "msbuild " + projectFile + " /verbosity:normal /t:Rebuild /p:Configuration=" + configuration;
            step.script = script;

            return CreateScriptStep(step);
        }

        private GitHubActions.Step CreateXamariniOSStep(AzurePipelines.Step step)
        {
            //coming from:
            //- task: XamariniOS@2
            //  inputs:
            //    solutionFile: '**/*.sln'
            //    configuration: 'Release'
            //    buildForSimulator: true
            //    packageApp: false

            //Going to: https://levelup.gitconnected.com/using-github-actions-with-ios-and-android-xamarin-apps-693a93b48a61
            //- name: iOS
            //  run: |
            //    cd Blank
            //    nuget restore
            //    msbuild Blank.iOS/Blank.iOS.csproj /verbosity:normal /t:Rebuild /p:Platform=iPhoneSimulator /p:Configuration=Debug

            string projectFile = GetStepInput(step, "projectFile");
            string configuration = GetStepInput(step, "configuration");

            string script = "" +
            "cd Blank\n" +
            "nuget restore\n" +
            "cd Blank.Android\n" +
            "msbuild " + projectFile + " /verbosity:normal /t:Rebuild /p:Platform=iPhoneSimulator /p:Configuration=" + configuration;
            step.script = script;

            return CreateScriptStep(step);
        }

        private GitHubActions.Step CreatePublishBuildArtifactsStep(AzurePipelines.Step step)
        {
            //There are 3 Azure DevOps variations
            //# Publish the artifacts
            //- task: PublishBuildArtifacts@1
            //  displayName: 'Publish Artifact'
            //  inputs:
            //    artifactName: drop
            //    PathtoPublish: '$(build.artifactstagingdirectory)'";

            //# Publishing pipeline artifacts is almost identical
            //- task: PublishPipelineArtifact@0
            //  displayName: Store artifact
            //  inputs:
            //    artifactName: 'MyProject'
            //    targetPath: 'MyProject/bin/release/netcoreapp2.2/publish/'

            //- publish: $(Build.ArtifactStagingDirectory)/$(Build.BuildId).zip
            //  artifact: drop

            //- name: publish build artifacts back to GitHub
            //  uses: actions/upload-artifact@v2
            //  with:
            //    name: console exe
            //    path: /home/runner/work/AzurePipelinesToGitHubActionsConverter/AzurePipelinesToGitHubActionsConverter/AzurePipelinesToGitHubActionsConverter/AzurePipelinesToGitHubActionsConverter.ConsoleApp/bin/Release/netcoreapp3.0

            string name = "";

            if (step.inputs != null && step.inputs.Contains("artifactname") == true)
            {
                name = GetStepInput(step, "artifactname");
            }

            string path = "";

            if (step.task?.ToUpper() == "PUBLISHBUILDARTIFACTS@1")
            {
                path = GetStepInput(step, "pathtopublish");
            }
            else if (step.task?.ToUpper() == "PUBLISHPIPELINEARTIFACT@0")
            {
                path = GetStepInput(step, "targetpath");
            }
            else if (step.task?.ToUpper() == "PUBLISHPIPELINEARTIFACT@1")
            {
                path = GetStepInput(step, "targetpath");
            }
            else if (step.publish != null)
            {
                name = step.artifact;
                path = step.publish;
            }

            GitHubActions.Step gitHubStep = new GitHubActions.Step
            {
                uses = "actions/upload-artifact@v2",
                with = new OrderedDictionary
                {
                    { "path", path }
                }
            };

            if (string.IsNullOrEmpty(name) == false)
            {
                gitHubStep.with.Add("name", name);
            }

            // In publish task, we need to delete any usage of build.artifactstagingdirectory variable as it's implied in github actions, and therefore not needed (Adding it adds the path twice)
            if (gitHubStep.with.Contains("path") && gitHubStep.with["path"] != null)
            {
                gitHubStep.with["path"] = (gitHubStep.with["path"].ToString()).Replace("$(build.artifactstagingdirectory)", "");
            }

            return gitHubStep;
        }

        private GitHubActions.Step CreateTemplateStep(AzurePipelines.Step step)
        {

            // There is no conversion for this: https://github.community/t5/GitHub-Actions/Call-an-action-from-another-action/td-p/45034
            //- template: templates/npm-build-steps.yaml
            //  parameters:
            //    extensionName: $(ExtensionName)



            //string name = "";
            //if (step.inputs != null && step.inputs.ContainsKey("artifactname") == true)
            //{
            //    name = GetStepInput(step, "artifactname");
            //}
            //string path = "";
            //if (step.task?.ToUpper() == "PUBLISHBUILDARTIFACTS@1")
            //{
            //    path = GetStepInput(step, "pathtopublish");
            //}
            //else if (step.task?.ToUpper() == "PUBLISHPIPELINEARTIFACT@0")
            //{
            //    path = GetStepInput(step, "targetpath");
            //}
            //else if (step.publish != null)
            //{
            //    name = step.artifact;
            //    path = step.publish;
            //}

            var gitHubStep = new GitHubActions.Step
            {
                run = "#" + step.template,
                step_message = "There is no conversion path for templates, currently there is no support to call other actions/yaml files from a GitHub Action"
            };

            string stepParameters = "";

            if (step.parameters != null)
            {
                foreach (DictionaryEntry item in step.parameters)
                {
                    stepParameters += item.Key + ": " + item.Value + System.Environment.NewLine;
                }
            }

            if (stepParameters != "")
            {
                gitHubStep.run += System.Environment.NewLine + stepParameters;
            }

            return gitHubStep;
        }

        // Safely extract the step input, if it exists
        private string GetStepInput(AzurePipelines.Step step, string name) => GetStepInput(step.inputs, name);

        private string GetStepInput(OrderedDictionary inputs, string name)
        {
            if (inputs != null && name != null)
            {
                var matchingInputs = inputs
                    .Cast<DictionaryEntry>()
                    .Where(i => i.StringKey().ToLower() == name.ToLower());

                if (matchingInputs.Any())
                {
                    return matchingInputs.First().StringValue();
                }
            }

            return null;
        }

        // Some pipelines need supporting steps as part of the processing. 
        // For example, if we are deploying to Azure, we need to add an Azure Login step
        public List<GitHubActions.Step> AddSupportingSteps(AzurePipelines.Step[] steps, VariablesProcessing variablesProcessing, OrderedDictionary variables, bool addCheckoutStep = true)
        {
            var newSteps = new List<GitHubActions.Step>();

            if (steps != null)
            {
                // Start by scanning all of the steps, to see if we need to insert additional tasks
                VariableGroup keyVaultGroup = null;
                (GitHubActions.Step step, int index) checkoutStep = default;

                // If the code needs a Checkout step, add it first
                if (addCheckoutStep)
                {
                    // Do we have checkout steps showing up as tasks in the job? If so, we'll remove these and map them to the checkout action
                    var checkoutSteps = steps
                        .Select((step, index) => (step, index))
                        .Where(cs => cs.step.task.ToUpper() == CheckoutStepId)
                        .ToList();
                    
                    // TODO: handle checkout steps for other repos?
                    if (checkoutSteps.Any())
                    {
                        // If a checkout step shows up here as a discrete step, let's grab the params to use for our GH checkout below
                        var mainCheckout = checkoutSteps.FirstOrDefault(c => GetStepInput(c.step, "repository") == "self");

                        if (mainCheckout != default)
                        {
                            checkoutStep = (CreateCheckoutStep(mainCheckout.step.inputs), mainCheckout.index);
                        }
                        else if (checkoutSteps.Any(c => GetStepInput(c.step, "repository") != "none")) // in the case the repo found is 'none', we will not add the checkout step
                        {
                            checkoutStep = (CreateCheckoutStep(), 0);
                        }

                        // Remove these discrete checkout steps from our step array
                        steps = steps.Except(checkoutSteps.Select(cs => cs.step)).ToArray();
                    }
                    else // no discrete checkout steps found, we'll assume we need a single checkout step at the front of the Job
                    {
                        checkoutStep = (CreateCheckoutStep(), 0);
                    }
                }

                // if the ADO pipeline has an Azure Key Vault-backed variable group, and it's used in the current set of vars, we'll add the steps needed to import the KV secrets
                keyVaultGroup = variablesProcessing.FindVariableGroup(variables, VariableGroup.KeyVaultGroupType);

                if (keyVaultGroup != null)
                {
                    newSteps.Add(CreateAzureKeyVaultStep(keyVaultGroup));
                }

                var newStepOffset = newSteps.Count;

                // Map new GH Actions steps from the ADO steps
                newSteps.AddRange(steps.Select(s => ProcessStep(s, variablesProcessing)));
                
                // if we're adding a checkout action, insert it at the index we identified above
                if (checkoutStep != default)
                {
                    newSteps.Insert(checkoutStep.index + newStepOffset, checkoutStep.step);
                }

                // find any steps that have dependencies and create those supporting steps here
                var supportingSteps = processStepDependencies(newSteps, steps);

                if (supportingSteps?.Count > 0)
                {
                    // order those in, supporting steps come first
                    newSteps.InsertRange(newStepOffset - 1, supportingSteps);
                }
            }

            return newSteps;
        }

        public List<GitHubActions.Step> processStepDependencies(List<GitHubActions.Step> steps, AzurePipelines.Step[] oldSteps)
        {
            var currentDependencies = GitHubActions.StepDependencies.None;
            var stepDependencies = new List<GitHubActions.Step>();
            var offset = steps.Count - oldSteps.Length;

            for (var i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                var oldStep = i >= offset ? oldSteps[i - offset] : null; // skip steps that have been frontloaded into the new steps list and won't exist in old steps

                if (checkDependency(step, ref currentDependencies, GitHubActions.StepDependencies.AzureLogin))
                {
                    stepDependencies.Add(CreateAzureLoginStep());
                }

                if (checkDependency(step, ref currentDependencies, GitHubActions.StepDependencies.JavaSetup))
                {
                    var javaVersion = GetStepInput(oldStep, "jdkVersionOption") ?? "1.8";
                    stepDependencies.Add(CreateSetupJavaStep(javaVersion));
                }

                if (checkDependency(step, ref currentDependencies, GitHubActions.StepDependencies.GradleSetup))
                {
                    stepDependencies.Add(CreateSetupGradleStep());
                }

                if (checkDependency(step, ref currentDependencies, GitHubActions.StepDependencies.MSBuildSetup))
                {
                    stepDependencies.Add(CreateMSBuildSetupStep());
                }
            }

            return stepDependencies;
        }

        bool checkDependency(GitHubActions.Step step, ref GitHubActions.StepDependencies currentDependencies, GitHubActions.StepDependencies newDependency)
        {
            if (step.IsDependentOn(newDependency) && (currentDependencies & newDependency) != newDependency)
            {
                currentDependencies = currentDependencies | newDependency;

                return true;
            }

            return false;
        }
    }
}
