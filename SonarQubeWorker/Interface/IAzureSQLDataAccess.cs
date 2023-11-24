using Sonarqube_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarQubeWorker.Interface
{
    public interface IAzureSQLDataAccess
    {
        Task<bool> InsertSonarQubeResultsAsync(SonarQubeResults results);
    }
}
