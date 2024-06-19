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
    public static async Task<(Stream payload, bool isValid)> Serialize(HttpResponseMessage response)
    {
        var serializedStream = new MemoryStream();
        var isValid = false;

        var builder = new StringBuilder();
        var responseJson = await response.Content.ReadAsStringAsync();

        foreach (var header in response.Headers)
        {
            builder.Append(header.Key).Append(": ").AppendJoin(", ", header.Value).Append(Environment.NewLine);
        }

        builder.AppendLine();
        builder.AppendLine(responseJson);
        var outputBytes = Encoding.UTF8.GetBytes(builder.ToString());

        try
        {
            JObject.Parse(responseJson);

            isValid = true;
        }
        catch (JsonReaderException e)
        {
            Console.WriteLine(e);
        }
        
        await serializedStream.WriteAsync(outputBytes, 0, outputBytes.Length);
        await serializedStream.FlushAsync();
        serializedStream.Position = 0;
        
        return (serializedStream, isValid);
    }
}
