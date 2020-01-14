using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace InsuranceBot.Services
{
    public class ComputerVisionService
    {
        private static readonly HttpClient Client = new HttpClient();

        // Add ComputerVisionClient definition here
        private readonly ComputerVisionClient _computerVisionApi = new ComputerVisionClient(
         new ApiKeyServiceClientCredentials("f75052eb026b4479985bc0ff54e2a3ba"),
         new System.Net.Http.DelegatingHandler[] { });

        public ComputerVisionService()
        {
            // Set the endpoint here
            _computerVisionApi.Endpoint = "https://westus.api.cognitive.microsoft.com";
        }

        public async Task<DetectResult> Detect(string imageUrl)
        {
            var image = await Client.GetByteArrayAsync(imageUrl);
            var results = await _computerVisionApi.AnalyzeImageInStreamAsync(
                new MemoryStream(image),
                new List<VisualFeatureTypes> { VisualFeatureTypes.Tags, VisualFeatureTypes.Description, VisualFeatureTypes.Categories });

            return new DetectResult
            {
                IsCar = results.Tags.Any(x => x.Name == "car") || results.Categories.Any(x => x.Name.Contains("trans_car")),
                Description = results.Description.Captions.First().Text,
            };

        }
    }

    public class DetectResult
    {
        public bool IsCar { get; set; }

        public string Description { get; set; }
    }
}
