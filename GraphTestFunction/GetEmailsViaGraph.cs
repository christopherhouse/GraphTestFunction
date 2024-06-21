using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;


namespace GraphTestFunction;

public static class GetEmailsViaGraph
{

    [FunctionName("GetEmailsViaGraph")]
    public static async Task Run(
        [TimerTrigger("%CronExpression%", RunOnStartup = false)] TimerInfo myTimer, ILogger log,
        IBinder binder)
    {
        log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        await GraphEmailClient.FetchEmailsAsync(binder, log);
    }
}