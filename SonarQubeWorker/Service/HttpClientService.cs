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
            client.BaseAddress = new Uri("https://sonarqubeapi.b2gwfvg6c2gddqcd.westeurope.azurecontainer.io/");
            var url = $"GetProjectResults?projectName={scanId}";
            var response = await client.GetAsync(url);
        }
    }
}
