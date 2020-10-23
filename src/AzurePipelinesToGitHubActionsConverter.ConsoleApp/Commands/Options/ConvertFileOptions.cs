using CommandLine;

namespace AzurePipelinesToGitHubActionsConverter.ConsoleApp.Commands.Options
{
    [Verb("convert", HelpText = "Add file contents to the index.")]
    public class ConvertFileOptions
    {
        [Option('f', "filePath", Required = true, HelpText = "The file path of the source YAML file")]
        public string FilePath { get; set; }

        [Option('o', "outputFolder", Required = false, HelpText = "The folder path where the .github/workflows folder should be created")]
        public string OutputFolder { get; set; }
    }
}
