using Newtonsoft.Json;
using SonarQubeWorker.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SonarQubeWorker.Service
{
    public class HttpClientService : IHttpClientService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public HttpClientService(IHttpClientFactory httpClientFactory) {
            _httpClientFactory = httpClientFactory;
        }

        public async Task SendRequest(Guid scanId)
        {
            var client = _httpClientFactory.CreateClient();
            // Set the base address for HTTP requests
            client.BaseAddress = new Uri("http://sonarqubeapi.b2gwfvg6c2gddqcd.westeurope.azurecontainer.io/");

            // Set basic authentication header with admin credentials

            // Construct URL with component and metrics
            var url = $"GetProjectResults?projectName={scanId}";

            // Send a GET request to the SonarQube API
            var response = await client.GetAsync(url);
        }
    }
}
