using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using Sonarqube_API.Models;
using SonarQubeWorker.Interface;
using System.IO.Compression;

namespace SonarQubeWorker
{
    public class Worker : BackgroundService
    {
        private readonly ISonarQubeDataAccess _sonarQubeDataAccess;
        private readonly IAzureBlobDataAccess _azureBlobDataAccess;
        private readonly IAzureSQLDataAccess _azureSQLDataAccess;
        private readonly ILogger<Worker> _logger;
        private readonly string _serviceBusConnectionString;
        private readonly string _topicName;
        private readonly string _subscriptionName;

        public Worker(ILogger<Worker> logger, IAzureBlobDataAccess azureBlobDataAccess, ISonarQubeDataAccess sonarQubeDataAccess, IAzureSQLDataAccess azureSQLDataAccess)
        {
            _azureBlobDataAccess = azureBlobDataAccess;
            _sonarQubeDataAccess = sonarQubeDataAccess;
            _azureSQLDataAccess = azureSQLDataAccess;
            _logger = logger;
            _serviceBusConnectionString = Environment.GetEnvironmentVariable("SQServiceBusCS");
            _topicName = Environment.GetEnvironmentVariable("SQTopicName");
            _subscriptionName = Environment.GetEnvironmentVariable("SQSubscriptionName");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await using var client = new ServiceBusClient(_serviceBusConnectionString);

            // Create a processor to process messages from the topic subscription
            var processor = client.CreateProcessor(_topicName, _subscriptionName, new ServiceBusProcessorOptions());

            // Add handlers to process messages and errors
            processor.ProcessMessageAsync += ProcessMessageAsync;
            processor.ProcessErrorAsync += ProcessErrorAsync;

            // Start processing
            await processor.StartProcessingAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(10000, stoppingToken);
                Console.WriteLine("Waiting for message...");
            }

            // Stop processing
            await processor.StopProcessingAsync();
        }

        public async Task<string> DownloadSourceCodeLocally(string filename, string userId)
        {

            try
            {
                // Download file
                RetrieveSourceCodeResponse? file = await _azureBlobDataAccess.DownloadAsyncInstantDownload(filename, userId);
                if (file == null)
                {
                    // Was not, return error message to client
                    return $"File {filename} could not be downloaded.";
                }

                //Unzip source code file
                var sourcefolder = await UnzipFolder(filename);



                //Execute SonarQube scan
                //create project
                await _sonarQubeDataAccess.CreateSonarQubeProject(filename);

                //generate sonarqube project token
                var token = await _sonarQubeDataAccess.GenerateSonarQubeToken(filename);


                //Go into directory and execute the sonarscanner commands
                await _sonarQubeDataAccess.ExecuteSonarScannerAndBuild(filename, token);



                await Task.Delay(5000);
                var results = await _sonarQubeDataAccess.GetSonarqubeResults(filename);

                var mappedResults = await MapToResults(results);

                var isInserterd = await _azureSQLDataAccess.InsertSonarQubeResultsAsync(mappedResults);

                return "ok";
            }
            catch (Exception ex)
            {
                // Handle other exceptions here if needed, and send an appropriate response to the client.
                // You can also log the error if needed.
                return $"An error occurred: {ex.Message}";
            }
        }
        //to SonarQubeDataAccess Layer



        private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
        {
            try
            {
                // Process the message
                string messageBody = args.Message.Body.ToString();
                _logger.LogInformation($"Received message: {messageBody}");
                if (!IsValidMessage(messageBody, out string projectlanguage, out string filename, out string userid))
                {
                    Console.WriteLine("Invalid message format or missing arguments.");
                    await args.AbandonMessageAsync(args.Message);
                    return;
                }
                dynamic parsedMessage = JsonConvert.DeserializeObject(messageBody);
                string fileName = parsedMessage.filename;
                string userId = parsedMessage.userid;


                await DownloadSourceCodeLocally(filename, userId);
                //call the scan function using the gained data and execute the scan 

                // Complete the message to remove it from the subscription
                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur while processing the message
                _logger.LogError($"Error occurred while processing the message: {ex.Message}");

                // Abandon the message to let the Service Bus retry processing it
                await args.AbandonMessageAsync(args.Message);
            }
        }

        private Task ProcessErrorAsync(ProcessErrorEventArgs args)
        {
            // Handle any exceptions that occur during the message handler execution
            _logger.LogError($"Exception occurred while receiving message: {args.Exception.Message}");
            return Task.CompletedTask;
        }

        private bool IsValidMessage(string messageBody, out string projectlanguage, out string filename, out string userId)
        {
            try
            {
                dynamic parsedMessage = JsonConvert.DeserializeObject(messageBody);

                projectlanguage = parsedMessage.projectlanguage;
                filename = parsedMessage.filename;
                userId = parsedMessage.userid;

                // Validate if filename and userId exist in the parsed message
                if (string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(userId) || projectlanguage != "c#")
                {
                    return false;
                }

                return true;
            }
            catch
            {
                projectlanguage = null;
                filename = null;
                userId = null;
                return false;

            }
        }

        private async Task<string> UnzipFolder(string filename)
        {
            string foldername = filename.Replace(".zip", "");
            string sourcePath = filename;
            string destinationPath = foldername;
            try
            {
                Console.WriteLine("Unzipping");
                await Task.Run(() => ZipFile.ExtractToDirectory(sourcePath, destinationPath));
                Console.WriteLine("extracted to directory");
                return destinationPath;
            }
            catch (IOException ex) when (ex.Message.Contains("already exists"))
            {
                throw new Exception($"The file '{filename}' has already been scanned.");
            }
            catch (Exception ex)
            {
                // You can also log the error if needed.
                throw new Exception($"Error extracting zip file '{filename}': {ex.Message}");
            }
        }

        private async Task<SonarQubeResults> MapToResults(string json)
        {
            var response = JsonConvert.DeserializeObject<SonarQubeResponse>(json);
            var result = new SonarQubeResults();

            if (response?.Component != null)
            {
                result.Name = response.Component.Name;

                foreach (var measure in response.Component.Measures)
                {
                    switch (measure.Metric)
                    {
                        case "sqale_rating":
                            result.ScaleRating = double.Parse(measure.Value);
                            break;
                        case "security_review_rating":
                            result.SecurityReviewRating = double.Parse(measure.Value);
                            break;
                        case "reliability_rating":
                            result.ReliabilityRating = double.Parse(measure.Value);
                            break;
                        case "code_smells":
                            result.CodeSmells = int.Parse(measure.Value);
                            break;
                        case "bugs":
                            result.Bugs = int.Parse(measure.Value);
                            break;
                        case "vulnerabilities":
                            result.Vulnerabilities = int.Parse(measure.Value);
                            break;
                        case "coverage":
                            result.Coverage = double.Parse(measure.Value);
                            break;
                        case "security_rating":
                            result.SecurityRating = double.Parse(measure.Value);
                            break;
                        case "security_hotspots":
                            result.SecurityHotspots = int.Parse(measure.Value);
                            break;
                        case "complexity":
                            result.Complexity = int.Parse(measure.Value);
                            break;
                            // Add additional cases here if there are more metrics.
                    }
                }
            }

            return result;
        }
    }
}
