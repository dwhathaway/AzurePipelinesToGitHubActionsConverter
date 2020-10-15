using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzurePipelinesToGitHubActionsConverter.ConsoleApp.Services
{
    public class AzureDevOpsService
    {
        // Hard code the ADO API version to ensure that we don't inadvertently introduce bugs from upstream
        // API changes in new release of ADO
        private readonly string _apiVersion = "5.1";

        private readonly HttpClient _httpClient;

        public AzureDevOpsService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<JObject> GetBuildDefinition(string baseAddress, string organizationName, string projectName,
            string personalAccessToken, long? definitionId, string? continuationToken = null, string apiVersion = "")
        {
            // Query the details of a give build definition
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Scheme = "https";
            uriBuilder.Host = baseAddress;
            uriBuilder.Path = $"{organizationName}/{projectName}/_apis/build/definitions/{definitionId}";

            // Check to see if the caller has provided an API Version, if not, use the global default version
            apiVersion = string.IsNullOrWhiteSpace(apiVersion) ? _apiVersion : apiVersion;

            List<string> queryParams = new List<string>()
            {
                $"api-version={apiVersion}"
            };

            // Append a continuationToken if provided
            if (!string.IsNullOrWhiteSpace(continuationToken))
                queryParams.Add($"continuationToken={continuationToken}");

            uriBuilder.Query = string.Join("&", queryParams);

            var result = await SendAsync(HttpMethod.Get, uriBuilder.Uri, string.Empty, encodeAuthToken(personalAccessToken), true);

            var json = await result.Content.ReadAsStringAsync();

            var responseObject = JObject.Parse(json);

            // Look for the continuation token in the header and if not empty, add to the returning JObject
            if (result.Headers.Contains("x-ms-continuationtoken"))
            {
                continuationToken = result.Headers.GetValues("x-ms-continuationtoken").FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(continuationToken))
                    responseObject.Add("continuationToken", continuationToken);
            }

            return responseObject;
        }

        public async Task<JObject> GetBuildDefinitions(string baseAddress, string organizationName, string projectName,
            string personalAccessToken, List<long>? definitionIds = default(List<long>), string? yamlFilename = "", int top = 0,
            string? continuationToken = null, string apiVersion = "")
        {
            // Query for a list of Builds > a given date
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Scheme = "https";
            uriBuilder.Host = baseAddress;
            uriBuilder.Path = $"{organizationName}/{projectName}/_apis/build/definitions";

            // Check to see if the caller has provided an API Version, if not, use the global default version
            apiVersion = string.IsNullOrWhiteSpace(apiVersion) ? _apiVersion : apiVersion;

            List<string> queryParams = new List<string>()
            {
                $"api-version={apiVersion}"
            };

            // If value is specified, set the Yaml file anme to search for builds
            if (!string.IsNullOrWhiteSpace(yamlFilename))
                queryParams.Add($"yamlFilename={yamlFilename}");

            // If value is specified, limit the number of items to return
            if (top > 0)
                queryParams.Add($"$top={top}");

            if (definitionIds != null && definitionIds.Count > 0)
                queryParams.Add($"definitionIds={string.Join(",", definitionIds)}");

            // Append a continuationToken if provided
            if (!string.IsNullOrWhiteSpace(continuationToken))
                queryParams.Add($"continuationToken={continuationToken}");

            // Make sure to include all the properties
            queryParams.Add($"includeAllProperties=true");

            uriBuilder.Query = string.Join("&", queryParams);

            var result = await SendAsync(HttpMethod.Get, uriBuilder.Uri, string.Empty, encodeAuthToken(personalAccessToken), true);

            var json = await result.Content.ReadAsStringAsync();

            var responseObject = JObject.Parse(json);

            // Look for the continuation token in the header and if not empty, add to the returning JObject
            // Need to make sure this is the right place to look for the token - it's in different places in different
            // calls, and it's not clearly documented here - https://docs.microsoft.com/en-us/rest/api/azure/devops/build/definitions/list?view=azure-devops-rest-6.1
            if (result.Headers.Contains("x-ms-continuationtoken"))
            {
                continuationToken = result.Headers.GetValues("x-ms-continuationtoken").FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(continuationToken))
                    responseObject.Add("continuationToken", continuationToken);
            }

            return responseObject;
        }

        public async Task<string> GetPipelineYaml(string baseAddress, string organizationName, string projectName,
            string personalAccessToken, long pipelineId, string branchName = "", string apiVersion = "")
        {
            var responseObject = await RunPipeline(baseAddress, organizationName, projectName, personalAccessToken,
                pipelineId, branchName, null, true, apiVersion);

            var finalYaml = responseObject.ContainsKey("finalYaml")
                ? responseObject["finalYaml"].Value<string>()
                : string.Empty;

            return finalYaml;
        }

        public async Task<JObject> RunPipeline(string baseAddress, string organizationName, string projectName,
            string personalAccessToken, long pipelineId, string branchName = "", Dictionary<string, object> variables = null, bool previewRun = false, string apiVersion = "" )
        {
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Scheme = "https";
            uriBuilder.Host = baseAddress;
            uriBuilder.Path = $"{organizationName}/{projectName}/_apis/pipelines/{pipelineId}/runs";

            // Check to see if the caller has provided an API Version, if not, use the global default version
            apiVersion = string.IsNullOrWhiteSpace(apiVersion) ? _apiVersion : apiVersion;

            List<string> queryParams = new List<string>()
            {
                $"api-version={apiVersion}"
            };

            uriBuilder.Query = string.Join("&", queryParams);

            var requestBody = new JObject();

            // If there are build variables, add them to the body
            if (variables != null && variables.Count > 0)
            {
                JArray variablesArray = new JArray();

                // ToDo: Make sure this is the correct format for the request body
                foreach (var variable in variables)
                {
                    variablesArray.Add(new JObject() { variable.Key, variable.Value.ToString() });
                }

                requestBody.Add(new JObject() { "variables", variablesArray });
            }

            if(previewRun)
                requestBody.Add(new JProperty("previewRun", true));

            if (!string.IsNullOrEmpty(branchName))
            {
                // Build up the resources node
                requestBody.Add(new JProperty("resources",
                    new JObject(new JProperty("repositories",
                        new JObject(new JProperty("self",
                            new JObject( new JProperty("refName", $"refs/heads/{branchName}")
                            ))
                        ))
                    ))
                );
            }

            var result = await SendAsync(HttpMethod.Post, uriBuilder.Uri, requestBody.ToString(Formatting.None), encodeAuthToken(personalAccessToken), false);

            var json = await result.Content.ReadAsStringAsync();

            var responseObject = JObject.Parse(json);

            // For cases where previewRun == true, we're calling it to get the YAML, so we want to do 1 additional check
            if (previewRun && result.StatusCode == HttpStatusCode.BadRequest)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error requesting YAML for Pipeline Id '{pipelineId}'");
                if(responseObject.ContainsKey("message"))
                    Console.WriteLine($"Message: {responseObject["message"].Value<string>()}");
                Console.ResetColor();
            }

            return responseObject;
        }

        public async Task<JObject> GetBuilds(string baseAddress, string organizationName, string projectName,
            string personalAccessToken, List<long>? definitionIds = default(List<long>), string? minTime = null,
            string? maxTime = null, int top = 0, string? continuationToken = null, string apiVersion = "")
        {
            // Query for a list of Builds > a given date
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Scheme = "https";
            uriBuilder.Host = baseAddress;
            uriBuilder.Path = $"{organizationName}/{projectName}/_apis/build/builds";

            // Check to see if the caller has provided an API Version, if not, use the global default version
            apiVersion = string.IsNullOrWhiteSpace(apiVersion) ? _apiVersion : apiVersion;

            List<string> queryParams = new List<string>()
            {
                $"api-version={apiVersion}"
            };

            // If value is specified, set the min date/time to search for builds
            if (!string.IsNullOrWhiteSpace(minTime))
                queryParams.Add($"minTime={minTime}");

            // If value is specified, set the min date/time to search for builds
            if (!string.IsNullOrWhiteSpace(maxTime))
                queryParams.Add($"maxTime={maxTime}");

            // If value is specified, limit the number of items to return
            if (top > 0)
                queryParams.Add($"$top={top}");

            if (definitionIds != null && definitionIds.Count > 0)
                queryParams.Add($"definitions={string.Join(",", definitionIds)}");

            // Append a continuationToken if provided
            if (!string.IsNullOrWhiteSpace(continuationToken))
                queryParams.Add($"continuationToken={continuationToken}");

            uriBuilder.Query = string.Join("&", queryParams);

            var result = await SendAsync(HttpMethod.Get, uriBuilder.Uri, string.Empty, encodeAuthToken(personalAccessToken), true);

            var json = await result.Content.ReadAsStringAsync();

            var responseObject = JObject.Parse(json);

            // Look for the continuation token in the header and if not empty, add to the returning JObject
            if (result.Headers.Contains("x-ms-continuationtoken"))
            {
                continuationToken = result.Headers.GetValues("x-ms-continuationtoken").FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(continuationToken))
                    responseObject.Add("continuationToken", continuationToken);
            }

            return responseObject;
        }

        public async Task<JObject> GetBuildTimelineRecords(string baseAddress, string organizationName, string projectName, string buildId, string personalAccessToken, string apiVersion = "")
        {
            // Query for a list of Builds > a given date
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Scheme = "https";
            uriBuilder.Host = baseAddress;
            uriBuilder.Path = $"{organizationName}/{projectName}/_apis/build/builds/{buildId}/timeline";

            // Check to see if the caller has provided an API Version, if not, use the global default version
            apiVersion = string.IsNullOrWhiteSpace(apiVersion) ? _apiVersion : apiVersion;

            List<string> queryParams = new List<string>()
            {
                $"api-version={apiVersion}"
            };

            uriBuilder.Query = string.Join("&", queryParams);

            var result = await SendAsync(HttpMethod.Get, uriBuilder.Uri, string.Empty, encodeAuthToken(personalAccessToken), true);

            var json = await result.Content.ReadAsStringAsync();

            // Check to see if the response has a body - for some builds, this will be empty
            var responseObject = string.IsNullOrWhiteSpace(json) ? new JObject() : JObject.Parse(json);

            return responseObject;
        }

        public async Task<JObject> GetWorkItems(string baseAddress, string organizationName, string projectName, string teamName, string wiqlQuery, string personalAccessToken, int top = 0, string apiVersion = "")
        {
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Scheme = "https";
            uriBuilder.Host = baseAddress;

            if(!string.IsNullOrEmpty(teamName))
            {
                uriBuilder.Path = $"{organizationName}/{projectName}/{teamName}/_apis/wit/wiql";
            }
            else
            {
                uriBuilder.Path = $"{organizationName}/{projectName}/_apis/wit/wiql";
            }

            // Check to see if the caller has provided an API Version, if not, use the global default version
            apiVersion = string.IsNullOrWhiteSpace(apiVersion) ? _apiVersion : apiVersion;

            List<string> queryParams = new List<string>()
            {
                "timePrecision=true",
                $"api-version={apiVersion}"
            };

            // If value is specified, limit the number of items to return
            if (top > 0)
                queryParams.Add($"$top={top}");

            uriBuilder.Query = string.Join("&", queryParams);

            // ToDo - Fix this body syntax
            var requestBody = JsonConvert.SerializeObject(wiqlQuery);

            var result = await SendAsync(HttpMethod.Post, uriBuilder.Uri, requestBody, encodeAuthToken(personalAccessToken), false);

            var json = await result.Content.ReadAsStringAsync();

            JObject queryResult = new JObject();

            // This happens when the request has more results than WIQL queries allow
            if (result.StatusCode == HttpStatusCode.BadRequest)
            {
                JObject badRequestResult = JObject.Parse(json);

                var ex = new Exception(badRequestResult["message"].ToString());

                var exParams = new Dictionary<string, string>()
                {
                    {"innerException", badRequestResult["innerException"].ToString()},
                    {"typeName", badRequestResult["typeName"].ToString()},
                    {"typeKey", badRequestResult["typeKey"].ToString()},
                    {"errorCode", badRequestResult["errorCode"].ToString()},
                    {"eventId", badRequestResult["eventId"].ToString()}
                };
            }
            else
            {
                queryResult = JObject.Parse(json);
            }

            return queryResult;
        }

        public async Task<string> GetWorkItem(string baseAddress, string organizationName, string projectName, long workItemId, string personalAccessToken, string apiVersion = "")
        {
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Scheme = "https";
            uriBuilder.Host = baseAddress;
            uriBuilder.Path = $"{organizationName}/{projectName}/_apis/wit/workItems/{workItemId}";

            // Check to see if the caller has provided an API Version, if not, use the global default version
            apiVersion = string.IsNullOrWhiteSpace(apiVersion) ? _apiVersion : apiVersion;

            List<string> queryParams = new List<string>()
            {
                "$expand=All",
                $"api-version={apiVersion}"
            };

            uriBuilder.Query = string.Join("&", queryParams);

            var result = await SendAsync(HttpMethod.Get, uriBuilder.Uri, string.Empty, encodeAuthToken(personalAccessToken), true);

            var json = await result.Content.ReadAsStringAsync();

            return json;
        }

        public async Task<JObject> GetWorkItemLinks(string baseAddress, string organizationName,
            string projectName, string personalAccessToken, string? startDateTime = null, string? continuationToken = null, string apiVersion = "")
        {
            // Query for a list of WorkItemLinks > a given date
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Scheme = "https";
            uriBuilder.Host = baseAddress;
            uriBuilder.Path = $"{organizationName}/{projectName}/_apis/wit/reporting/workitemlinks";

            // Check to see if the caller has provided an API Version, if not, use the global default version
            apiVersion = string.IsNullOrWhiteSpace(apiVersion) ? _apiVersion : apiVersion;

            List<string> queryParams = new List<string>()
            {
                $"api-version={apiVersion}"
            };

            // If value is specified, limit the number of items to return
            if (!string.IsNullOrEmpty(startDateTime))
                queryParams.Add($"startDateTime={startDateTime}");

            // Append a continuationToken if provided
            if(!string.IsNullOrEmpty(continuationToken))
                queryParams.Add($"continuationToken={continuationToken}");


            uriBuilder.Query = string.Join("&", queryParams);

            var result = await SendAsync(HttpMethod.Get, uriBuilder.Uri, string.Empty, encodeAuthToken(personalAccessToken), true);

            var json = await result.Content.ReadAsStringAsync();

            var responseObject = JObject.Parse(json);

            return responseObject;
        }

        public async Task<JObject> GetProjects(string baseAddress, string organizationName, string personalAccessToken, string? continuationToken = null, string apiVersion = "")
        {
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Scheme = "https";
            uriBuilder.Host = baseAddress;
            uriBuilder.Path = $"{organizationName}/_apis/projects";

            // Check to see if the caller has provided an API Version, if not, use the global default version
            apiVersion = string.IsNullOrWhiteSpace(apiVersion) ? _apiVersion : apiVersion;

            List<string> queryParams = new List<string>()
            {
                $"api-version={apiVersion}"
            };

            // Append a continuationToken if provided
            if (!string.IsNullOrWhiteSpace(continuationToken))
                queryParams.Add($"continuationToken={continuationToken}");

            uriBuilder.Query = string.Join("&", queryParams);

            var result = await SendAsync(HttpMethod.Get, uriBuilder.Uri, string.Empty, encodeAuthToken(personalAccessToken), true);

            HttpHeaders headers = result.Headers;
            IEnumerable<string> values;
            string token = string.Empty;
            if (headers.TryGetValues("x-ms-continuationtoken", out values))
            {
                token = values.First();
            }

            var json = await result.Content.ReadAsStringAsync();

            var responseObject = JObject.Parse(json);

            responseObject.Add("continuationToken", token);

            return responseObject;
        }

        public async Task<JObject> GetTestPlans(string baseAddress, string organizationName, string projectName, string personalAccessToken, string apiVersion = "")
        {
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Scheme = "https";
            uriBuilder.Host = baseAddress;
            uriBuilder.Path = $"{organizationName}/{projectName}/_apis/test/plans";

            // Check to see if the caller has provided an API Version, if not, use the global default version
            apiVersion = string.IsNullOrWhiteSpace(apiVersion) ? _apiVersion : apiVersion;

            List<string> queryParams = new List<string>()
            {
                $"filterActivePlans=true",
                $"api-version={apiVersion}"
            };

            uriBuilder.Query = string.Join("&", queryParams);

            var result = await SendAsync(HttpMethod.Get, uriBuilder.Uri, string.Empty, encodeAuthToken(personalAccessToken), true);

            var json = await result.Content.ReadAsStringAsync();

            var responseObject = JObject.Parse(json);

            return responseObject;
        }

        public async Task<JObject> GetTestSuites(string baseAddress, string organizationName, string projectName, string personalAccessToken,
            long planId, string? continuationToken = null, string apiVersion = "")
        {
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Scheme = "https";
            uriBuilder.Host = baseAddress;
            uriBuilder.Path = $"{organizationName}/{projectName}/_apis/testplan/Plans/{planId}/suites";

            // Check to see if the caller has provided an API Version, if not, use the global default version
            apiVersion = string.IsNullOrWhiteSpace(apiVersion) ? _apiVersion : apiVersion;

            List<string> queryParams = new List<string>()
            {
                $"api-version={apiVersion}"
            };

            // Append a continuationToken if provided
            if (!string.IsNullOrWhiteSpace(continuationToken))
                queryParams.Add($"continuationToken={continuationToken}");

            uriBuilder.Query = string.Join("&", queryParams);

            var result = await SendAsync(HttpMethod.Get, uriBuilder.Uri, string.Empty, encodeAuthToken(personalAccessToken), true);

            HttpHeaders headers = result.Headers;
            IEnumerable<string> values;
            string token = string.Empty;
            if (headers.TryGetValues("x-ms-continuationtoken", out values))
            {
                token = values.First();
            }

            var json = await result.Content.ReadAsStringAsync();

            var responseObject = JObject.Parse(json);

            responseObject.Add("continuationToken", token);

            return responseObject;
        }

        public async Task<JObject> GetTestPoints(string baseAddress, string organizationName, string projectName, string personalAccessToken,
            long planId, long suiteId, string? continuationToken = null, string apiVersion = "")
        {
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Scheme = "https";
            uriBuilder.Host = baseAddress;
            uriBuilder.Path = $"{organizationName}/{projectName}/_apis/test/Plans/{planId}/Suites/{suiteId}/points";

            // Check to see if the caller has provided an API Version, if not, use the global default version
            apiVersion = string.IsNullOrWhiteSpace(apiVersion) ? _apiVersion : apiVersion;

            List<string> queryParams = new List<string>()
            {
                $"includePointDetails=true",
                $"api-version={apiVersion}"
            };

            // Append a continuationToken if provided
            if (!string.IsNullOrWhiteSpace(continuationToken))
                queryParams.Add($"continuationToken={continuationToken}");

            uriBuilder.Query = string.Join("&", queryParams);

            var result = await SendAsync(HttpMethod.Get, uriBuilder.Uri, string.Empty, encodeAuthToken(personalAccessToken), true);

            HttpHeaders headers = result.Headers;
            IEnumerable<string> values;
            string token = string.Empty;
            if (headers.TryGetValues("x-ms-continuationtoken", out values))
            {
                token = values.First();
            }

            var json = await result.Content.ReadAsStringAsync();

            var responseObject = JObject.Parse(json);

            responseObject.Add("continuationToken", token);

            return responseObject;
        }

        private AuthenticationHeaderValue encodeAuthToken(string personalAccessToken)
        {
            var token64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + personalAccessToken));
            var auth = new AuthenticationHeaderValue("Basic", token64);
            return auth;
        }

        protected async Task<HttpResponseMessage> SendAsync(
            HttpMethod requestType,
            Uri requestUri,
            string json,
            AuthenticationHeaderValue authToken,
            bool throwOnException,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage(requestType, requestUri);

            if (json != null)
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = authToken;

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (throwOnException)
                response.EnsureSuccessStatusCode();

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.EnsureSuccessStatusCode();
            }

            return response;
        }
    }
}