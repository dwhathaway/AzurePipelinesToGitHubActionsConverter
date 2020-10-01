using CommandLine;

namespace AzurePipelinesToGitHubActionsConverter.ConsoleApp.Commands.Options
{
    [Verb("convert", HelpText = "Add file contents to the index.")]
    public class ConvertFileOptions
    {
        [Option('f', "filePath", Required = true, HelpText = "The file path of the source YAML file")]
        public string FilePath { get; set; }

        [Option('o', "outputPath", Required = false, HelpText = "The folder path to output the generate")]
        public string OutputPath { get; set; }
    }
}