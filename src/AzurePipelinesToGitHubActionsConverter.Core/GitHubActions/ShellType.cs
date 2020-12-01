namespace AzurePipelinesToGitHubActionsConverter.Core.GitHubActions
{
    public class ShellType
    {
        public string Value { get; private set; }
        private ShellType(string type) { Value = type; }

        public static ShellType PowerShell = new ShellType("pwsh");
        public static ShellType Bash = new ShellType("bash");
        public static ShellType Cmd = new ShellType("cmd");
        public static ShellType Python = new ShellType("python");
        public static ShellType Sh = new ShellType("sh");
    }
}
