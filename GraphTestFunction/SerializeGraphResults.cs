using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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
        var headers = ConvertHeadersToDictionary(response.Headers);
        var headersJson = JObject.Parse(JsonConvert.SerializeObject(headers));
        var responseJson = await response.Content.ReadAsStringAsync();
        byte[] jsonBytes = null;

        try
        {
            var bodyJson = JObject.Parse(responseJson);
            var output = new JObject
            {
                ["headers"] = headersJson,
                ["body"] = bodyJson
            };

            var bodyAndHeaders = output.ToString();

            jsonBytes = Encoding.UTF8.GetBytes(bodyAndHeaders);
            isValid = true;
        }
        catch (JsonReaderException e)
        {
            jsonBytes = Encoding.UTF8.GetBytes(CreateOutputForInvalidJson(responseJson, headersJson.ToString()));
            Console.WriteLine(e);
        }

        await serializedStream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
        await serializedStream.FlushAsync();
        serializedStream.Position = 0;
        
        return (serializedStream, isValid);
    }

    static string CreateOutputForInvalidJson(string body, string headers) => $"{headers}{Environment.NewLine}{body}";

    static Dictionary<string, string> ConvertHeadersToDictionary(HttpResponseHeaders httpResponseHeaders)
    {
        var headers = new Dictionary<string, string>();

        foreach (var header in httpResponseHeaders)
        {
            if (header.Value != null)
            {
                var value = string.Join(" ", header.Value)
                    .TrimEnd(" ".ToCharArray());

                headers.Add(header.Key, value);
            }   
        }

        return headers;
    }
}
