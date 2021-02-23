using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AzurePipelinesToGitHubActionsConverter.Core.Conversion
{
    public static class ConditionsProcessing
    {
        static Dictionary<string, string> operatorMapping = new Dictionary<string, string>()
        {
            { "eq", "==" },
            { "ne", "!=" },
            { "le", "<=" },
            { "lt", "<" },
            { "ge", ">=" },
            { "gt", ">" },
            { "and", "&&" },
            { "or", "||" },
            { "not", "!" }
        };

        static Dictionary<string, string> functionMapping = new Dictionary<string, string>()
        {
            { "startswith", "startsWith" },
            { "endswith", "endsWith" },
            { "containsvalue", "containsValue" },
            { "contains", "contains" },
            { "format ", "format" },
            { "coalesce ", "coalesce" }, // ? don't think this actually exists in Actions
            { "join ", "join" }
        };

        public static string TranslateConditions(string condition, VariablesProcessing vp, int depth = 0, object context = null)
        {
            // track recursive depth
            depth++;

            if (condition == null)
            {
                return null;
            }

            // Sometimes conditions are spread over multiple lines, we are going to compress this to one line to make the processing easier
            condition = condition.Replace("\r\n", "");

            string processedCondition = "";

            // Get the condition. split the key word from the contents
            List<string> contentList = FindBracketedContentsInString(condition);

            // Examine the contents for last set of contents, the most complete piece of the contents, to get the keywords, recursively, otherwise, convert the contents to GitHub
            string contents = contentList[contentList.Count - 1];
            string conditionKeyWord = condition.Replace("(" + contents + ")", "").Trim();

            if (contents.IndexOf("(") >= 0)
            {
                // Need to count the number "(" brackets. If it's > 1, then iterate until we get to one.
                int bracketsCount = CountCharactersInString(contents, ')');

                if (bracketsCount >= 1)
                {
                    // Split the strings by "," - but also respecting brackets
                    List<string> innerContents = SplitContents(contents);
                    string innerContentsProcessed = "";

                    for (int i = 0; i < innerContents.Count; i++)
                    {
                        string innerContent = innerContents[i];
                        innerContentsProcessed += TranslateConditions(innerContent, vp, depth, context);

                        if (i != innerContents.Count - 1)
                        {
                            innerContentsProcessed += ",";
                        }
                    }

                    contents = innerContentsProcessed;
                }
            }

            // Join the pieces back together again
            processedCondition += ProcessCondition(conditionKeyWord, contents, context);

            // Translate any system variables
            processedCondition = ProcessVariables(processedCondition);

            // change conditions to use env[''] syntax instead of variables['']
            processedCondition = vp.ProcessIndexedVariables(processedCondition);

            depth--;

            // returning our compound condition - if we're top level, we'll remove any grouping ()
            if (depth == 0 && processedCondition.StartsWith("(") && processedCondition.EndsWith(")"))
            {
                return processedCondition.Substring(1, processedCondition.Length - 2);
            }

            return processedCondition;
        }

        // TODO: Add more variables. Note that this format (variables['name']) is conditions specific.
        private static string ProcessVariables(string condition)
        {
            if (condition.IndexOf("variables['Build.SourceBranch']") >= 0)
            {
                condition = condition.Replace("variables['Build.SourceBranch']", "github.ref");
            }
            else if (condition.IndexOf("eq(variables['Build.SourceBranchName']") >= 0)
            {
                condition = condition.Replace("eq(variables['Build.SourceBranchName']", "endsWith(github.ref");
            }

            return condition;
        }

        private static string ProcessCondition(string condition, string contents, object context = null)
        {
            condition = condition.Trim();

            switch (condition.ToLower())
            {
                // Job/step status check functions:
                // Azure DevOps: https://docs.microsoft.com/en-us/azure/devops/pipelines/process/expressions?view=azure-devops#job-status-functions
                // GitHub Actions: https://help.github.com/en/actions/reference/context-and-expression-syntax-for-github-actions#job-status-check-functions
                case "always":
                case "canceled":
                    return condition + "(" + contents + ")";
                case "failed":
                    return "failure(" + contents + ")";
                case "succeeded":
                    return "success(" + contents + ")";
                case "succeededorfailed": // Essentially the same as "always", but not cancelled

                    // Job level conditions do not allow access to the job context, oddly enough
                    if (context is AzurePipelines.Job)
                    {
                        return "(success() || failure())";
                    }
                    else
                    {
                        return "job.status != 'cancelled'";
                    }

                // Functions: 
                // Azure DevOps: https://docs.microsoft.com/en-us/azure/devops/pipelines/process/expressions?view=azure-devops#functions
                // GitHub Actions: https://help.github.com/en/actions/reference/context-and-expression-syntax-for-github-actions#functions
                case "eq": // eq
                case "ne": // ne
                case "le": // le
                case "lt": // lt
                case "ge": // ge
                case "gt": // gt
                    return translateOperator(operatorMapping[condition], contents, 2);
                case "not": // not
                    return $"{ operatorMapping[condition] }({ contents })"; // i.e. !(inner condition(s))
                case "and": // and
                case "or": // or
                    return translateOperator(operatorMapping[condition], contents, group: true);
                case "startswith": // startsWith
                case "endswith": // endsWith
                case "contains": // contains( search, item )
                case "coalesce": // coalesce
                case "containsvalue": // containsValue
                case "format": // format
                case "in": // in
                case "join": // oin
                case "notin": // notin
                case "xor": // xor
                case "counter": // counter

                    functionMapping.TryGetValue(condition, out string actionsFunction);
                    return $"{ actionsFunction ?? condition }({ contents })";

                default:
                    return "";
            }
        }

        private static string translateOperator(string op, string contents, int operandsNeeded = 0, bool group = false)
        {
            var comparisonParts = SplitContents(contents);

            if (operandsNeeded > 0 && comparisonParts.Count != operandsNeeded)
            {
                throw new Exception($"Expected {operandsNeeded} parts but found {comparisonParts.Count}");
            }

            var sb = new StringBuilder(group ? "(" : string.Empty);

            for (int i = 0; i < comparisonParts.Count; i++)
            {
                if (i != comparisonParts.Count - 1)
                {
                    sb.AppendFormat("{0} {1} ", comparisonParts[i].Trim(), op);
                }
                else
                {
                    sb.AppendFormat("{0}{1}", comparisonParts[i].Trim(), group ? ")" : string.Empty);
                }
            }

            return sb.ToString().Trim();
        }

        // Public so that it can be unit tested
        public static List<string> FindBracketedContentsInString(string text)
        {
            IEnumerable<string> results = Nested(text);
            List<string> list = results.ToList<string>();

            // Remove the last item - that is the current item we don't need
            if (list.Count > 1)
            {
                list.RemoveAt(list.Count - 1);
            }

            return list;
        }

        private static IEnumerable<string> Nested(string value)
        {
            // From: https://stackoverflow.com/questions/38479148/separate-nested-parentheses-in-c-sharp
            if (string.IsNullOrEmpty(value))
            {
                yield break; // or throw exception
            }

            Stack<int> brackets = new Stack<int>();

            for (int i = 0; i < value.Length; ++i)
            {
                char ch = value[i];

                if (ch == '(')
                {
                    brackets.Push(i);
                }
                else if (ch == ')')
                {
                    // i.e. stack has values: if (!brackets.Any()) throw ...
                    int openBracket = brackets.Pop();

                    yield return value.Substring(openBracket + 1, i - openBracket - 1);
                }
            }

            // Possible Future enhancement: you may want to check here if there're too many '('
            // i.e. stack still has values: if (brackets.Any()) throw ... 
            yield return value;
        }

        private static int CountCharactersInString(string text, char character)
        {
            return text.Count(x => x == character);
        }

        // Public so that it can be unit tested
        // Takes a string, and splits it by commas, respecting (). For example,
        // the string "eq('ABCDE', 'BCD'), ne(0, 1)", is split into "eq('ABCDE', 'BCD')" and "ne(0, 1)"
        // This was originally RegEx, but RegEx cannot handle nested brackets, so we wrote our own simple parser
        public static List<string> SplitContents(string text)
        {
            char splitCharacter = ',';
            List<string> list = new List<string>();
            StringBuilder sb = new StringBuilder();
            int openBracketCount = 0;

            foreach (char nextChar in text)
            {
                // If we have no open brackets, and the split character has been found, add the current string to the list 
                if (openBracketCount == 0 && nextChar == splitCharacter)
                {
                    list.Add(sb.ToString());
                    sb = new StringBuilder();
                }
                else if (nextChar == '(')
                {
                    // We found a open bracket - this is nested, but track it
                    openBracketCount++;
                    sb.Append(nextChar);
                }
                else if (nextChar == ')')
                {
                    // We found a closed bracket - if this is 0, we are not tracking anymore.
                    openBracketCount--;
                    sb.Append(nextChar);
                }
                else
                {
                    // Otherwise, append the character
                    sb.Append(nextChar);
                }
            }

            list.Add(sb.ToString());

            return list;
        }
    }
}
