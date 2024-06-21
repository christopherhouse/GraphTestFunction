using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace GraphTestFunction;

public class GraphEmailClient
{
    private static readonly string clientId = Environment.GetEnvironmentVariable("ClientId");
    private static readonly string clientSecret = Environment.GetEnvironmentVariable("ClientSecret");
    private static readonly string tenantId = Environment.GetEnvironmentVariable("TenantId");
    private static readonly Uri graphQueryUri = new Uri(Environment.GetEnvironmentVariable("GraphQueryUri"));
    private static readonly string Scopes = Environment.GetEnvironmentVariable("Scopes");

    private static HttpClient httpClient = new HttpClient();

    public static async Task FetchEmailsAsync(IBinder binder, ILogger logger)
    {
        var responseBody = string.Empty;
        var statusCode = "UNKNOWN";
        HttpResponseMessage response = null;
        var startTime = DateTime.UtcNow;

        try
        {
            var message = new HttpRequestMessage(HttpMethod.Get, graphQueryUri);
            message.Headers.Add("Authorization", $"Bearer {await GetAccessTokenAsync()}");
            response = await httpClient.SendAsync(message);
            responseBody = await response.Content.ReadAsStringAsync();
            statusCode = Convert.ToString((int)response.StatusCode);
        }
        catch (HttpRequestException hex)
        {
            statusCode = Convert.ToString((int)hex.StatusCode);
            responseBody = $"HTTP request exception encountered while calling Graph API: {hex}";
            logger.LogError(hex, responseBody);
        }
        catch (Exception ex)
        {
            statusCode = "UNKNOWN";
            responseBody = $"Exception encountered while calling Graph API: {ex}";
            logger.LogError(ex, responseBody);
        }
        finally
        {
            if (response != null)
            {
                var output = await SerializeResponse(response);
                await OutputStreams(output, binder, startTime, statusCode);
                await StoreHash(binder, responseBody, output.isValid, startTime, statusCode);
            }
        }
    }

    private static async Task StoreHash(IBinder binder, string responseBody, bool isValid, DateTime startTime, string statusCode)
    {
        var hashFileName = isValid ? $"graphresponses/{startTime:yyyy-MM-ddTHH:mm:ssZ}-hash-{statusCode}.txt" : $"graphresponses/invalid/{startTime:yyyy-MM-ddTHH:mm:ssZ}-hash-{statusCode}.txt";

        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(responseBody));

        var hashBytes = md5.ComputeHash(hash);

        // Convert the byte array to a hexadecimal string
        var sb = new StringBuilder();
        foreach (var b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
        await stream.FlushAsync();
        stream.Position = 0;

        await using var writer = binder.Bind<Stream>(new BlobAttribute(hashFileName, FileAccess.Write));
        await stream.CopyToAsync(writer);
    }

    private static async Task OutputStreams((Stream payload, Stream headers, bool isValid) input, IBinder binder, DateTime startTime, string statusCode)
    {
        var payloadFileName = input.isValid ? $"graphresponses/{startTime:yyyy-MM-ddTHH:mm:ssZ}-payload-{statusCode}.json" : $"graphresponses/invalid/{startTime:yyyy-MM-ddTHH:mm:ssZ}-payload-{statusCode}.json";
        var headerFileName = input.isValid ? $"graphresponses/{startTime:yyyy-MM-ddTHH:mm:ssZ}-headers-{statusCode}.json" : $"graphresponses/invalid/{startTime:yyyy-MM-ddTHH:mm:ssZ}-headers-{statusCode}.json";

        await using var writer = binder.Bind<Stream>(new BlobAttribute(payloadFileName, FileAccess.Write));
        await input.payload.CopyToAsync(writer);
        await writer.FlushAsync();

        await using var headerWriter = binder.Bind<Stream>(new BlobAttribute(headerFileName, FileAccess.Write));
        await input.headers.CopyToAsync(headerWriter);
        await headerWriter.FlushAsync();
    }

    private static async Task<string> GetAccessTokenAsync()
    {
        var app = ConfidentialClientApplicationBuilder.Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
            .Build();

        var scopes = Scopes.Split(',');

        var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();

        return result.AccessToken;
    }

    public static async Task<(Stream payload, Stream headers, bool isValid)> SerializeResponse(HttpResponseMessage response)
    {
        var bodyStream = new MemoryStream();
        var headerStream = new MemoryStream();
        var isValid = false;

        var builder = new StringBuilder();
        var responseJson = await response.Content.ReadAsStringAsync();

        foreach (var header in response.Headers)
        {
            builder.Append(header.Key).Append(": ").AppendJoin(", ", header.Value).Append(Environment.NewLine);
        }

        builder.AppendLine();

        var headerBytes = Encoding.UTF8.GetBytes(builder.ToString());
        var bodyBytes = Encoding.UTF8.GetBytes(responseJson);

        await headerStream.WriteAsync(headerBytes, 0, headerBytes.Length);
        await headerStream.FlushAsync();
        headerStream.Position = 0;

        await bodyStream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
        await bodyStream.FlushAsync();
        bodyStream.Position = 0;

        try
        {
            JObject.Parse(responseJson);

            isValid = true;
        }
        catch (JsonReaderException e)
        {
            Console.WriteLine(e);
        }



        return (bodyStream, headerStream, isValid);
    }
}
