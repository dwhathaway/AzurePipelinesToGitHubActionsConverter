using System;
using System.Collections;
using System.Text.RegularExpressions;

namespace AzurePipelinesToGitHubActionsConverter.Core.Extensions
{
    public static class Helpers
    {
        public static string[] Split(this string input, string sep)
        {
            return input.Split(new[] { sep }, StringSplitOptions.None);
        }

        public static string ReplaceAnyCase(this string value, string pattern, string replacement)
        {
            return Regex.Replace(value, Regex.Escape(pattern), replacement.Replace("$", "$$"), RegexOptions.IgnoreCase);
        }

        public static string StringKey(this DictionaryEntry de)
        {
            return de.Key as string;
        }

        public static string StringValue(this DictionaryEntry de)
        {
            return de.Value as string;
        }
    }
}
