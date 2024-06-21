using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GraphTestFunction
{
    public static class GetEmailsViaGraphHttp
    {
        [FunctionName("GetEmailsViaGraphHttp")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log,
            IBinder binder)
        {
            await GraphEmailClient.FetchEmailsAsync(binder, log);

            return new OkResult();
        }
    }
}
