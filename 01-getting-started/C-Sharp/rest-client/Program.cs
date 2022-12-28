using System;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;
using System.Linq;

namespace rest_client
{
    class Program
    {
        private static string cogSvcEndpoint;
        private static string cogSvcKey;
        static async Task Main(string[] args)
        {
            try
            {
                // Get config settings from AppSettings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                cogSvcEndpoint = configuration["CognitiveServicesEndpoint"];
                cogSvcKey = configuration["CognitiveServiceKey"];

                // Set console encoding to unicode
                Console.InputEncoding = Encoding.Unicode;
                Console.OutputEncoding = Encoding.Unicode;

                // Get user input (until they enter "quit")
                List<string> allLines = new List<string>();
                string userText = "";

                while (userText.ToLower() != "quit")
                {
                    Console.WriteLine("Enter some text ('quit' to stop)");
                    userText = Console.ReadLine();

                    bool exit = userText.ToLower() == "quit";
                    if (exit) break;

                    if (userText.ToLower() != "stop")
                    {
                        allLines.Add(userText);
                    }
                    else
                    {
                        await GetLanguage(allLines);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        static async Task GetLanguage(List<string> inputs)
        {
            try
            {
                var inputJarray = inputs
                    .Select((inputLine, id) => new JObject()
                    {
                       new JProperty("id", id),
                       new JProperty("text", inputLine)
                    }).ToArray();

                var jarray = new JArray(inputJarray);

                JObject jsonBody = new JObject(new JProperty("documents", jarray));
                
                // Encode as UTF-8
                UTF8Encoding utf8 = new UTF8Encoding(true, true);
                byte[] encodedBytes = utf8.GetBytes(jsonBody.ToString());
                
                // Let's take a look at the JSON we'll send to the service
                Console.WriteLine(utf8.GetString(encodedBytes, 0, encodedBytes.Length));

                // Make an HTTP request to the REST interface
                var client = new HttpClient();
                var queryString = HttpUtility.ParseQueryString(string.Empty);

                // Add the authentication key to the header
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", cogSvcKey);

                // Use the endpoint to access the Text Analytics language API
                var uri = cogSvcEndpoint + "text/analytics/v3.1/languages?" + queryString;

                // Send the request and get the response
                HttpResponseMessage response;
                using (var content = new ByteArrayContent(encodedBytes))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    response = await client.PostAsync(uri, content);
                }

                // If the call was successful, get the response
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Display the JSON response in full (just so we can see it)
                    string responseContent = await response.Content.ReadAsStringAsync();
                    JObject results = JObject.Parse(responseContent);
                    Console.WriteLine(results.ToString());

                    // Extract the detected language name for each document
                    foreach (JObject document in results["documents"])
                    {
                        Console.WriteLine("\nLanguage: " + (string)document["detectedLanguage"]["name"]);
                    }
                }
                else
                {
                    // Something went wrong, write the whole response
                    Console.WriteLine(response.ToString());
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
    }
}
