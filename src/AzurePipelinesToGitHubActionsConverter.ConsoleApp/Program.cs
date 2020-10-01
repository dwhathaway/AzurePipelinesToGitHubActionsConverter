using AzurePipelinesToGitHubActionsConverter.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzurePipelinesToGitHubActionsConverter.ConsoleApp.Commands.Options;
using AzurePipelinesToGitHubActionsConverter.ConsoleApp.Services;
using AzurePipelinesToGitHubActionsConverter.Core.Conversion;
using YamlDotNet.Serialization;
using CommandLine;
using Newtonsoft.Json.Linq;

namespace AzurePipelinesToGitHubActionsConverter.ConsoleApp
{
    class Program
    {
        static int Main(string[] args) {
            return Parser.Default.ParseArguments<ConvertFileOptions, ExtractAndConvertOptions>(args)
                .MapResult(
                    (ConvertFileOptions opts) => ConvertFile(opts),
                    (ExtractAndConvertOptions opts) => ExtractAndConvert(opts),
                    errs => 1);
        }

        private static int ConvertFile(ConvertFileOptions opts)
        {
            int retVal = 0;

            // Make sure the source file path actually exists before trying to process
            if (!File.Exists(opts.FilePath))
            {
                Console.WriteLine($"Invalid input file, file '{opts.FilePath}' does not exist");
                return 2; // Return error and stop processing
            }

            // Read the YAML file contents
            string pipelineYaml = File.ReadAllText(opts.FilePath);

            string outputBasePath = opts.OutputPath;

            FileInfo inputFileInfo = new FileInfo(opts.FilePath);

            // Check if an output path was provided.  If not, default one and notify the user
            if (string.IsNullOrWhiteSpace(opts.OutputPath))
            {
                outputBasePath = Path.Combine(inputFileInfo.DirectoryName, ".github", "workflows");

                Console.WriteLine($"'outputPath' not provided, defaulting to '{outputBasePath}'");
            }
            else
            {
                outputBasePath = Path.Combine(opts.OutputPath, ".github", "workflows");
            }

            if (!Directory.Exists(outputBasePath))
            {
                Console.WriteLine($"Creating folder '{outputBasePath}, it did not exist");
                Directory.CreateDirectory(outputBasePath);
            }

            try
            {
                // Run the converter
                Conversion conversion = new Conversion();

                var result = conversion.ConvertAzurePipelineToGitHubAction(pipelineYaml);

                // Write the output file
                File.WriteAllText(Path.Combine(outputBasePath, inputFileInfo.Name), result.actionsYaml);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine($"An error occured converting your pipeline");
                Console.WriteLine(e.ToString());
                Console.ResetColor();

                retVal = -1;
            }

            return retVal;
        }

        private static int ExtractAndConvert(ExtractAndConvertOptions opts)
        {
            int retVal = 0;

            // Check for a provided baseUrl.  If not provided, default
            string baseUrl = !string.IsNullOrWhiteSpace(opts.BaseUrl)
                ? opts.BaseUrl
                : "dev.azure.com";

            AzureDevOpsService adoService = new AzureDevOpsService();

            string continuationToken = string.Empty;

            string outputBasePath = opts.OutputFolder;

            // Check if an output path was provided.  If not, default one and notify the user
            if (string.IsNullOrWhiteSpace(opts.OutputFolder))
            {
                outputBasePath = Path.Combine(Directory.GetCurrentDirectory(), ".github", "workflows");

                Console.WriteLine($"'outputPath' not provided, defaulting to '{outputBasePath}'");
            }
            else
            {
                outputBasePath = Path.Combine(opts.OutputFolder, ".github", "workflows");
            }

            if (!Directory.Exists(outputBasePath))
            {
                Console.WriteLine($"Creating folder '{outputBasePath}, it did not exist");
                Directory.CreateDirectory(outputBasePath);
            }


            // Execute a loop
            do
            {
                // Extract each of the pipelines from the ADO project
                var task = adoService.GetBuildDefinitions(baseUrl, opts.Account, opts.Project,
                    opts.PersonalAccessToken, opts.PipelineIds.ToList(), opts.YamlFilename, 0, continuationToken);

                Task.WaitAll(task);

                var response = task.Result;

                var pipelines = response["value"] as JArray;

                var yamlPipelines = pipelines.Where((p) => ((JObject)p["process"]).ContainsKey("yamlFilename")).ToList();

                if (!string.IsNullOrEmpty(opts.RepositoryName))
                {
                    // Filter the list of pipelines by the repository name
                    yamlPipelines = yamlPipelines.Where((p) =>
                            p["repository"]["name"].Value<string>().ToLower() ==
                            $"{opts.Project.ToLower()}/{opts.RepositoryName.ToLower()}")
                        .ToList();
                }

                if(!string.IsNullOrEmpty(opts.PipelineFolderPrefix))
                {
                    // Filter the list of pipelines by the repository name
                    yamlPipelines = yamlPipelines.Where((p) =>
                            p["path"].Value<string>().ToLower() ==
                            $"{opts.PipelineFolderPrefix.ToLower()}")
                        .ToList();
                }

                foreach (var yamlPipeline in yamlPipelines)
                {
                    var pipelineId = yamlPipeline["id"].Value<long>();

                    var task1 = adoService.GetPipelineYaml(baseUrl, opts.Account, opts.Project,
                        opts.PersonalAccessToken, pipelineId, "6.1-preview.1");

                    Task.WaitAll(task1);

                    var pipelineYaml = task1.Result;

                    if(!string.IsNullOrWhiteSpace(pipelineYaml))
                    {
                        var yamlFullFilename = yamlPipeline["process"]["yamlFilename"].Value<string>();
                        var yamlFilename = yamlFullFilename.Split('/').Last();

                        if (opts.IncludeIdInFilename.Value)
                            yamlFilename = $"{pipelineId.ToString()}_{yamlFilename}";

                        try
                        {
                            // Run the converter
                            Conversion conversion = new Conversion();

                            var result = conversion.ConvertAzurePipelineToGitHubAction(pipelineYaml);

                            // ToDo: output comments here

                            Console.WriteLine($"Successfully converted {yamlFullFilename}");

                            // Write the output file
                            File.WriteAllText(Path.Combine(outputBasePath, yamlFilename), result.actionsYaml);
                        }
                        catch (Exception e)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;

                            Console.WriteLine($"An error occured converting your pipeline");
                            Console.WriteLine(e.ToString());
                            Console.ResetColor();

                            retVal = -1;
                        }
                    }
                }

                // After processing each build, check in the response to see if there's a continuationToken, if there is, call
                // the REST API again, passing the token to get the next set of responses.  Repeat in a do/while loop until
                // the continuationToken is no longer populated in the header
                continuationToken = response.ContainsKey("continuationToken") ? response["continuationToken"].ToString() : string.Empty;
            } while (!string.IsNullOrWhiteSpace(continuationToken));

            // For

            return retVal;
        }

        //Read in a YAML file and convert it to a T object
        private static T ReadYamlFile<T>(string yaml)
        {
            IDeserializer deserializer = new DeserializerBuilder().Build();
            T yamlObject = deserializer.Deserialize<T>(yaml);

            return yamlObject;
        }

        //Write a YAML file using the T object
        private static string WriteYAMLFile<T>(T obj)
        {
            //Convert the object into a YAML document
            ISerializer serializer = new SerializerBuilder()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull) //New as of YamlDotNet 8.0.0: https://github.com/aaubry/YamlDotNet/wiki/Serialization.Serializer#configuredefaultvalueshandlingdefaultvalueshandling
                .Build();
            string yaml = serializer.Serialize(obj);

            return yaml;
        }
    }
}
