using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GraphTestFunction;

public class SerializeGraphResults
{
    public static async Task<(Stream payload, Stream headers, bool isValid)> Serialize(HttpResponseMessage response)
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
