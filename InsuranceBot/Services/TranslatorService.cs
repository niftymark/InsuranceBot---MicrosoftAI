using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using InsuranceBot.Models;
using Newtonsoft.Json;

namespace InsuranceBot.Services
{
    public class TranslatorService
    {
        private const string TranslatorEndpoint = "https://api.cognitive.microsofttranslator.com/{0}?api-version=3.0";
        private static readonly HttpClient Client = new HttpClient();

        public static async Task<string> Detect(string input)
        {
            var uri = string.Format(TranslatorEndpoint, "detect");
            var body = new object[] { new { Text = input } };
            var requestBody = JsonConvert.SerializeObject(body);

            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            message.Headers.Add("Ocp-Apim-Subscription-Key", "f75052eb026b4479985bc0ff54e2a3ba");
            message.Headers.Add("Ocp-Apim-Subscription-Region", "westus");
            message.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            message.Headers.Add("X-ClientTraceId", Guid.NewGuid().ToString());

            var response = await Client.SendAsync(message);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<List<TranslatorDetectResponse>>(responseBody);
            var languageDetected = result.FirstOrDefault()?.Language;

            return languageDetected;
        }

        public static async Task<string> Translate(string input, string from, string to)
        {
            var uri = $"{string.Format(TranslatorEndpoint, "translate")}&from={from}&to={to}";
            var body = new object[] { new { Text = input } };
            var requestBody = JsonConvert.SerializeObject(body);

            var message = new HttpRequestMessage(HttpMethod.Post, uri);
            message.Headers.Add("Ocp-Apim-Subscription-Key", "f75052eb026b4479985bc0ff54e2a3ba");
            message.Headers.Add("Ocp-Apim-Subscription-Region", "westus");
            message.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            message.Headers.Add("X-ClientTraceId", Guid.NewGuid().ToString());

            var response = await Client.SendAsync(message);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<List<Dictionary<string, List<Dictionary<string, string>>>>>(responseBody);
            var translations = result[0]["translations"];
            var translation = translations[0]["text"];

            return translation;
        }
    }
}
