using AzurePipelinesToGitHubActionsConverter.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzurePipelinesToGitHubActionsConverter.ConsoleApp.Commands.Options;
using AzurePipelinesToGitHubActionsConverter.ConsoleApp.Services;
using AzurePipelinesToGitHubActionsConverter.Core.Conversion;
using YamlDotNet.Serialization;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace AzurePipelinesToGitHubActionsConverter.ConsoleApp
{
    class Program
    {
        static readonly IDeserializer _yamlDeserializer = new Deserializer();

        static readonly ISerializer  _yamlSerializer = new Serializer();

        static List<string> _missingConverters = new List<string>();

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
            string pipelineYaml = CleanPrNode(File.ReadAllText(opts.FilePath));

            FileInfo inputFileInfo = new FileInfo(opts.FilePath);

            string outputBasePath = opts.OutputFolder;

            // Check if an output path was provided.  If not, default one and notify the user
            if (string.IsNullOrWhiteSpace(outputBasePath))
            {
                outputBasePath = inputFileInfo.DirectoryName;
                Console.WriteLine($"'outputPath' not provided, defaulting to '{outputBasePath}'");
            }

            try
            {
                // Run the converter
                Conversion conversion = new Conversion();

                var result = conversion.ConvertAzurePipelineToGitHubAction(pipelineYaml);

                var outputFolder = Path.Combine(outputBasePath, ".github", "workflows");

                // Create the repo-specific .github/workflows folder
                if (!Directory.Exists(outputFolder))
                {
                    Console.WriteLine($"Creating folder '{outputFolder}, it did not exist");
                    Directory.CreateDirectory(outputFolder);
                }

                if (result.comments != null && result.comments.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;

                    Console.WriteLine($"Pipeline {inputFileInfo.Name} partially converted");

                    result.comments.ForEach((comment) =>
                    {
                        Console.WriteLine(comment);

                        if(comment.Contains("#Note: Error! This step does not have a conversion path yet")
                            && !_missingConverters.Contains(comment))
                            _missingConverters.Add(comment);
                    });

                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;

                    Console.WriteLine($"Successfully converted {inputFileInfo.Name}");

                    Console.ResetColor();
                }

                // Write the output file
                File.WriteAllText(Path.Combine(outputFolder, inputFileInfo.Name), result.actionsYaml);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine($"An error occured converting your pipeline");
                Console.WriteLine(e.ToString());
                Console.ResetColor();

                retVal = -1;
            }

            if(_missingConverters.Count > 0)
            {
                Console.WriteLine("The following converters required by these pipelines are:");
                _missingConverters.ForEach((missingConverter) => Console.Write(missingConverter));
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
            if (string.IsNullOrWhiteSpace(outputBasePath))
            {
                outputBasePath = Directory.GetCurrentDirectory();
                Console.WriteLine($"'outputPath' not provided, defaulting to '{outputBasePath}'");
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
                            p["repository"]["properties"]["shortName"].Value<string>().ToLower() ==
                            opts.RepositoryName.ToLower())
                        .ToList();
                }

                if(!string.IsNullOrEmpty(opts.PipelineFolderName))
                {
                    // Filter the list of pipelines by the repository name
                    yamlPipelines = yamlPipelines.Where((p) =>
                            p["path"].Value<string>().ToLower() ==
                            $"{opts.PipelineFolderName.ToLower()}")
                        .ToList();
                }

                foreach (var yamlPipeline in yamlPipelines)
                {
                    var pipelineId = yamlPipeline["id"].Value<long>();
                    var repositoryName = yamlPipeline["repository"]["properties"]["shortName"].Value<string>();

                    var task1 = adoService.GetPipelineYaml(baseUrl, opts.Account, opts.Project,
                        opts.PersonalAccessToken, pipelineId, "6.1-preview.1");

                    Task.WaitAll(task1);

                    var pipelineYaml = CleanPrNode(task1.Result);

                    if(!string.IsNullOrWhiteSpace(pipelineYaml))
                    {
                        var yamlFullFilename = yamlPipeline["process"]["yamlFilename"].Value<string>();
                        var yamlFilename = yamlFullFilename.Split('/').Last();

                        // This is completely optional, but it's possible that someone duplicated
                        // the workflow name elsewhere in the repo
                        if (opts.IncludeIdInFilename.Value)
                            yamlFilename = $"{pipelineId.ToString()}_{yamlFilename}";

                        var outputFolder = Path.Combine(outputBasePath, repositoryName, ".github", "workflows");

                        // Create the repo-specific .github/workflows folder
                        if (!Directory.Exists(outputFolder))
                        {
                            Console.WriteLine($"Creating folder '{outputFolder}, it did not exist");
                            Directory.CreateDirectory(outputFolder);
                        }

                        try
                        {
                            // Run the converter
                            Conversion conversion = new Conversion();

                            var result = conversion.ConvertAzurePipelineToGitHubAction(pipelineYaml);

                            if (result.comments != null && result.comments.Count > 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;

                                Console.WriteLine($"Pipeline {yamlFullFilename} partially converted");

                                result.comments.ForEach((comment) =>
                                {
                                    Console.WriteLine(comment);

                                    if(comment.Contains("#Note: Error! This step does not have a conversion path yet")
                                       && !_missingConverters.Contains(comment))
                                        _missingConverters.Add(comment);
                                });

                                Console.ResetColor();
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Green;

                                Console.WriteLine($"Successfully converted {yamlFullFilename}");

                                Console.ResetColor();
                            }

                            // Write the output file
                            File.WriteAllText(Path.Combine(outputFolder, yamlFilename), result.actionsYaml);
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

            if(_missingConverters.Count > 0)
            {
                Console.WriteLine("The following converters required by these pipelines are:");
                _missingConverters.ForEach((missingConverter) => Console.Write(missingConverter));
            }

            return retVal;
        }

        /// <summary>
        /// Fixes a case where the call to the previewRun API returns pr: enabled: false instead of pr: none
        /// </summary>
        /// <param name="pipelineYaml">Pipeline YAML</param>
        /// <returns></returns>
        private static string CleanPrNode(string pipelineYaml)
        {
            string fixedPipelineYaml = string.Empty;

            if(!string.IsNullOrEmpty(pipelineYaml))
            {
                // Hack alert - pipelines that define the pr trigger as pr: none
                // returns as pr: enabled: false from this call - we need to fix this for transformation purposes
                using (StringReader s = new StringReader(pipelineYaml))
                {
                    // Do a bunch of stuff to convert the YAML to JSON cuz json is easier to work with
                    Dictionary<object, object> yamlObject =
                        _yamlDeserializer.Deserialize<Dictionary<object, object>>(s);

                    var serializer = new SerializerBuilder()
                        .JsonCompatible()
                        .Build();

                    var json = serializer.Serialize(yamlObject);

                    var jsonObject = JObject.Parse(json);

                    if(jsonObject.ContainsKey("pr"))
                    {
                        if (!jsonObject["pr"]["enabled"].Value<bool>())
                            jsonObject["pr"] = "none";
                    }

                    json = JsonConvert.SerializeObject(jsonObject);

                    var expConverter = new ExpandoObjectConverter();
                    dynamic deserializedObject = JsonConvert.DeserializeObject<ExpandoObject>(json, expConverter);

                    // Convert it back to YAML and return
                    var yamlSerializer = new YamlDotNet.Serialization.Serializer();
                    fixedPipelineYaml = yamlSerializer.Serialize(deserializedObject);
                }
            }

            return fixedPipelineYaml;
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
