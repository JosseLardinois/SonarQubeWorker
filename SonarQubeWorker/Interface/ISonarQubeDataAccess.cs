using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarQubeWorker.Interface
{
    public interface ISonarQubeDataAccess
    {
        Task CreateSonarQubeProject(string projectName);
        Task<string> GenerateSonarQubeToken(string projectName);
        Task ExecuteSonarScannerAndBuild(string projectKey, string sonarToken);
        Task<string> GetSonarqubeResults(string component);
    }
}
