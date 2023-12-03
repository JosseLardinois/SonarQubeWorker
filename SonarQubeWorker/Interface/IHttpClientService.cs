using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonarQubeWorker.Interface
{
    public interface IHttpClientService
    {
        Task SendRequest(Guid scanId);
    }
}
