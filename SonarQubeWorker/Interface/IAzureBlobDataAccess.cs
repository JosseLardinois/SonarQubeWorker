using Sonarqube_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarQubeWorker.Interface
{
    public interface IAzureBlobDataAccess
    {
        Task<RetrieveSourceCodeResponse> DownloadAsyncInstantDownload(string blobFilename, string userId);
    }
}
