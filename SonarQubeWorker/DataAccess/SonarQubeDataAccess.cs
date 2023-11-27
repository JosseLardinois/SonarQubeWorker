using Newtonsoft.Json.Linq;
using SonarQubeWorker.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SonarQubeWorker.DataAccess
{
    public class SonarQubeDataAccess : ISonarQubeDataAccess
    {
        private readonly string _sonarQubeUrl;
        private readonly string _adminUsername;
        private readonly string _organizationName;


        public SonarQubeDataAccess()
        {
            _sonarQubeUrl = "https://sonarcloud.io";
            _adminUsername = Environment.GetEnvironmentVariable("sonarToken");
            _organizationName = Environment.GetEnvironmentVariable("organizationName");
        }

        public async Task<string> GenerateSonarQubeToken(string projectName)
        {
            using (var client = new HttpClient())
            {
                // Set the base address and authentication headers
                client.BaseAddress = new Uri(_sonarQubeUrl);
                var byteArray = System.Text.Encoding.ASCII.GetBytes($"{_adminUsername}:");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                // Prepare the content to be sent (token name)
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("name", projectName + "Token"),
                    new KeyValuePair<string, string>("organization", _organizationName)
                });

                // Send a POST request to the SonarQube API
                HttpResponseMessage response = await client.PostAsync("/api/user_tokens/generate", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    // Assuming the response contains a JSON with the token
                    // Extract the token from the response and return it
                    var token = ExtractTokenFromResponse(responseContent); // Implement this method based on your SonarQube version's response format
                    return token;
                }
                else
                {
                    throw new Exception("Failed to generate SonarQube token: " + response.StatusCode);
                }
            }
        }

        public async Task CreateSonarQubeProject(string projectName)
        {
            var projectKey = projectName;
            using (var client = new HttpClient())
            {
                // Set the base address for HTTP requests
                client.BaseAddress = new Uri(_sonarQubeUrl);

                // Set basic authentication header with admin credentials
                var byteArray = System.Text.Encoding.ASCII.GetBytes($"{_adminUsername}:");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                // Prepare content to be sent (project key and name)
                var content = new FormUrlEncodedContent(new[]
                {
            new KeyValuePair<string, string>("project", projectKey),
            new KeyValuePair<string, string>("name", projectName),
            new KeyValuePair<string, string>("organization", _organizationName)
        });

                // Send a POST request to the SonarQube API
                HttpResponseMessage response = await client.PostAsync("/api/projects/create", content);

                // Check response status
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Project created successfully.");
                }
                else
                {
                    Console.WriteLine("Error occurred: " + response.StatusCode);
                }
            }
        }

        public async Task<string> GetSonarqubeResults(string component)
        {
            using (var client = new HttpClient())
            {
                // Set the base address for HTTP requests
                client.BaseAddress = new Uri(_sonarQubeUrl);

                // Set basic authentication header with admin credentials
                var byteArray = System.Text.Encoding.ASCII.GetBytes($"{_adminUsername}:");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                // Construct URL with component and metrics
                string url = $"/api/measures/component?component={component}&metricKeys=bugs,new_bugs,code_smells,vulnerabilities,new_vulnerabilities,coverage,sqale_rating,reliability_rating,security_rating,security_review_rating,security_hotspots,coverage,complexity&organization={_organizationName}";

                // Send a GET request to the SonarQube API
                HttpResponseMessage response = await client.GetAsync(url);

                // Check response status
                if (response.IsSuccessStatusCode)
                {

                    var i = response.Content;
                    var y = response.Content.ReadAsStringAsync(); ;
                    Console.WriteLine(i);
                    // Read and return the response content as a string
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Console.WriteLine(response);
                    throw new HttpRequestException($"Request failed with status code: {response.StatusCode}");
                }
            }
        }


        public async Task ExecuteSonarScannerAndBuild(string projectKey, string sonarToken)
        {
            var foldername = projectKey.Replace(".zip", "");
            var solutionPath = FindSolutionPath(foldername);

            try
            {
                // Execute SonarScanner Begin
                var sonarBeginCommand = $"dotnet-sonarscanner begin /k:'{projectKey}' /o:'{_organizationName}' /d:sonar.host.url='{_sonarQubeUrl}' /d:sonar.login='{_adminUsername}'";
                await ExecuteCommand("/bin/bash", $"-c \"{sonarBeginCommand}\"", solutionPath);

                // Execute dotnet build
                var buildCommand = "dotnet build";
                await ExecuteCommand("/bin/bash", $"-c \"{buildCommand}\"", solutionPath);

                // Execute SonarScanner End
                var sonarEndCommand = $"dotnet-sonarscanner end /d:sonar.login='{_adminUsername}'";
                await ExecuteCommand("/bin/bash", $"-c \"{sonarEndCommand}\"", solutionPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred during execution: {ex.Message}");
            }
        }

        private async Task ExecuteCommand(string fileName, string arguments, string workingDirectory)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WorkingDirectory = workingDirectory;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string errors = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (!string.IsNullOrEmpty(errors))
                {
                    Console.WriteLine($"Error during command execution ({fileName} {arguments}): {errors}");
                }
                else
                {
                    Console.WriteLine($"Command executed successfully ({fileName} {arguments}). Output: {output}");
                }
            }
        }

        private string ExtractTokenFromResponse(string responseContent)
        {
            // Parse the JSON response
            var jsonResponse = JObject.Parse(responseContent);

            // Extract the token. This assumes the JSON structure has a field 'token'
            // The exact field name and structure may vary depending on your SonarQube version
            var token = jsonResponse["token"]?.ToString();

            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("Token not found in the response.");
            }

            return token;
        }
        private static string FindSolutionPath(string startDirectory)
        {
            // Check if the startDirectory is valid
            if (string.IsNullOrEmpty(startDirectory) || !Directory.Exists(startDirectory))
            {
                throw new InvalidOperationException("Invalid start directory.");
            }

            // Call the recursive search method
            return FindSolutionInDirectory(new DirectoryInfo(startDirectory));
        }

        private static string FindSolutionInDirectory(DirectoryInfo directory)
        {
            // Search for .sln files in the current directory
            var solutionFiles = directory.GetFiles("*.sln");
            if (solutionFiles.Length > 0)
            {
                // Return the directory path of the first found solution file
                return solutionFiles[0].DirectoryName;
            }

            // Recursively search in subdirectories
            foreach (var subDirectory in directory.GetDirectories())
            {
                try
                {
                    var solutionPath = FindSolutionInDirectory(subDirectory);
                    if (!string.IsNullOrEmpty(solutionPath))
                    {
                        return solutionPath;
                    }
                }
                catch (FileNotFoundException)
                {
                    continue;
                }
            }

            throw new FileNotFoundException($"Solution file not found in or below: {directory.FullName}");
        }
    }
}
