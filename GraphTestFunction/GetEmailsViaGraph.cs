using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Tables;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;


namespace GraphTestFunction;

public static class GetEmailsViaGraph
{
    private static readonly string clientId = Environment.GetEnvironmentVariable("ClientId");
    private static readonly string clientSecret = Environment.GetEnvironmentVariable("ClientSecret");
    private static readonly string tenantId = Environment.GetEnvironmentVariable("TenantId");
    private static readonly Uri graphQueryUri = new Uri(Environment.GetEnvironmentVariable("GraphQueryUri"));
    private static readonly string Scopes = Environment.GetEnvironmentVariable("Scopes");

    [FunctionName("GetEmailsViaGraph")]
    [return: Table("graphresults")]
    public static async Task<GraphResult> Run([TimerTrigger("%CronExpression%")] TimerInfo myTimer, ILogger log)
    {
        log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        
        var app = ConfidentialClientApplicationBuilder.Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
            .Build();
        
        var scopes = Scopes.Split(',');

        var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();

        var statusCode = string.Empty;
        var responseBody = string.Empty;

        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {result.AccessToken}");

            var response = await httpClient.GetAsync(graphQueryUri);
            statusCode = Convert.ToString((int)response.StatusCode);
            responseBody = await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException hex)
        {
            statusCode = Convert.ToString((int)hex.StatusCode);
            responseBody = $"HTTP request exception encountered while calling Graph API: {hex}";
            log.LogError(hex, responseBody);
        }
        catch (Exception ex)
        {
            statusCode = "UNKNOWN";
            responseBody = $"Exception encountered while calling Graph API: {ex}";
            log.LogError(ex, responseBody);
        }

        string hashString = string.Empty;
        using (var md5 = MD5.Create())
        {
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(responseBody));
            hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        var requestResult = new GraphResult
        {
            PartitionKey = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            RowKey = statusCode,
            Result = responseBody,
            ResultHash = hashString
        };
        
        return requestResult;
    }
}


public class GraphResult
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public string Result { get; set; }
    public string ResultHash { get; set; }
}