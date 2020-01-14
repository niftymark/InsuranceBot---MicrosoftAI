
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rest;

namespace InsuranceBot.Services
{
    public class TextSentimentService
    {
        public static async Task<double> GetTextSentiment(string input)
        {
            ITextAnalyticsClient client = new TextAnalyticsClient(new ApiKeyServiceClientCredentials())
            {
                Endpoint = "https://westus.api.cognitive.microsoft.com",
            };

            var language = await client.DetectLanguageAsync(languageBatchInput: new LanguageBatchInput(documents: new List<LanguageInput>() { new LanguageInput(id: "1", text: input) }));

            var langCode = language.Documents[0].DetectedLanguages[0].Iso6391Name;

            var sentiment = await client.SentimentAsync(multiLanguageBatchInput: new MultiLanguageBatchInput(new List<MultiLanguageInput>() { new MultiLanguageInput(id: "1", language: langCode, text: input) }));

            return sentiment.Documents[0].Score.GetValueOrDefault();

        }

        /// <summary>
        /// Container for subscription credentials. Make sure to enter your valid key.
        /// </summary>
        private class ApiKeyServiceClientCredentials : ServiceClientCredentials
        {
            public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Add("Ocp-Apim-Subscription-Key", "f75052eb026b4479985bc0ff54e2a3ba");
                return base.ProcessHttpRequestAsync(request, cancellationToken);
            }
        }
    }
}
