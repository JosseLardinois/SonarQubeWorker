using Newtonsoft.Json;
using Sonarqube_API.Models;
using SonarQubeWorker.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarQubeWorker.Mapper
{
    public class Mapper : IMapper
    { private readonly ILogger _logger;
        public Mapper(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<SonarQubeResults> MapToResults(string json)
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
