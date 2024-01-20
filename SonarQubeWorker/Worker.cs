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
        private readonly IMapper _mapper;
        private readonly ILogger<Worker> _logger;
        private readonly string _serviceBusConnectionString;
        private readonly string _topicName;
        private readonly string _subscriptionName;

        public Worker(ILogger<Worker> logger, IAzureBlobDataAccess azureBlobDataAccess, ISonarQubeDataAccess sonarQubeDataAccess, IAzureSQLDataAccess azureSQLDataAccess, IMapper mapper)
        {
            _azureBlobDataAccess = azureBlobDataAccess;
            _sonarQubeDataAccess = sonarQubeDataAccess;
            _azureSQLDataAccess = azureSQLDataAccess;
            _logger = logger;
            _serviceBusConnectionString = Environment.GetEnvironmentVariable("SQServiceBusCS");
            _topicName = Environment.GetEnvironmentVariable("SQTopicName");
            _subscriptionName = Environment.GetEnvironmentVariable("SQSubscriptionName");
            _mapper = mapper;
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

        public async Task<string> DownloadSourceCodeLocally(string scanId, string userId)
        {
            try
            {
                // Download file
                RetrieveSourceCodeResponse? file = await _azureBlobDataAccess.DownloadAsyncInstantDownload(scanId, userId);
                if (file == null)
                {
                    // Was not, return error message to client
                    return $"File {scanId} could not be downloaded.";
                }

                //Unzip source code file
                var sourcefolder = await UnzipFolder(scanId);

                //Remove the .zip extension
                var cleanScanId = scanId.Replace(".zip", "");

                //Execute SonarQube scan
                //create project
                await _sonarQubeDataAccess.CreateSonarQubeProject(cleanScanId);

                //generate sonarqube project token
                var token = await _sonarQubeDataAccess.GenerateSonarQubeToken(cleanScanId);


                //Go into directory and execute the sonarscanner commands
                await _sonarQubeDataAccess.ExecuteSonarScannerAndBuild(cleanScanId, token);

                await Task.Delay(5000);
                var results = await _sonarQubeDataAccess.GetSonarqubeResults(cleanScanId);

                var mappedResults = await _mapper.MapToResults(results);

                var isInserterd = await _azureSQLDataAccess.InsertSonarQubeResultsAsync(mappedResults);

                return "ok";
            }
            catch (Exception ex)
            {
                return $"An error occurred: {ex.Message}";
            }
        }



        private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
        {
            try
            {
                string messageBody = args.Message.Body.ToString();
                _logger.LogInformation($"Received message: {messageBody}");
                if (!IsValidMessage(messageBody, out string projectlanguage, out string scanid, out string userid))
                {
                    Console.WriteLine("Invalid message format or missing arguments.");
                    await args.AbandonMessageAsync(args.Message);
                    return;
                }
                dynamic parsedMessage = JsonConvert.DeserializeObject(messageBody);
                string scanId = parsedMessage.scanid;
                string userId = parsedMessage.userid;


                await DownloadSourceCodeLocally(scanId, userId);
                await args.CompleteMessageAsync(args.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred while processing the message: {ex.Message}");
                await args.AbandonMessageAsync(args.Message);
            }
        }

        private Task ProcessErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError($"Exception occurred while receiving message: {args.Exception.Message}");
            return Task.CompletedTask;
        }

        private bool IsValidMessage(string messageBody, out string projectlanguage, out string scanId, out string userId)
        {
            try
            {
                dynamic parsedMessage = JsonConvert.DeserializeObject(messageBody);

                projectlanguage = parsedMessage.projectlanguage;
                scanId = parsedMessage.scanid;
                userId = parsedMessage.userid;
                if (string.IsNullOrEmpty(scanId) || string.IsNullOrEmpty(userId) || projectlanguage != "c#")
                {
                    return false;
                }

                return true;
            }
            catch
            {
                projectlanguage = null;
                scanId = null;
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
                throw new Exception($"Error extracting zip file '{filename}': {ex.Message}");
            }
        }
    }
}
