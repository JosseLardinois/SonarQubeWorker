using Dapper;
using Microsoft.Data.SqlClient;
using Sonarqube_API.Models;
using SonarQubeWorker.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarQubeWorker.DataAccess
{
    public class AzureSQLDataAccess : IAzureSQLDataAccess
    {
        private readonly string _connectionString;
        public AzureSQLDataAccess()
        {
            _connectionString = Environment.GetEnvironmentVariable("SQDBCS");
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task<bool> InsertSonarQubeResultsAsync(SonarQubeResults results)
        {
            const string query = @"
        INSERT INTO SonarQubeResults 
            (Id, Name, ScaleRating, SecurityReviewRating, ReliabilityRating, CodeSmells, Bugs, Vulnerabilities, Coverage, SecurityRating, SecurityHotspots, Complexity) 
        VALUES 
            (@Id, @Name, @ScaleRating, @SecurityReviewRating, @ReliabilityRating, @CodeSmells, @Bugs, @Vulnerabilities, @Coverage, @SecurityRating, @SecurityHotspots, @Complexity);";

            var affectedRows = await CreateConnection().ExecuteAsync(query, new
            {
                Id = Guid.NewGuid(), // Generates a new GUID for each entry
                Name = results.Name,
                ScaleRating = results.ScaleRating,
                SecurityReviewRating = results.SecurityReviewRating,
                ReliabilityRating = results.ReliabilityRating,
                CodeSmells = results.CodeSmells,
                Bugs = results.Bugs,
                Vulnerabilities = results.Vulnerabilities,
                Coverage = results.Coverage,
                SecurityRating = results.SecurityRating,
                SecurityHotspots = results.SecurityHotspots,
                Complexity = results.Complexity
            });

            return affectedRows > 0;
        }
    }
}
