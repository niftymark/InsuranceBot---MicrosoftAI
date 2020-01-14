using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace InsuranceBot.Services
{
    public class CustomVisionService
    {

        private static readonly HttpClient Client = new HttpClient();

        // Add PredictionEndpoint definition here
        private static readonly CustomVisionPredictionClient _predictionClient =
          new CustomVisionPredictionClient()
          {
              ApiKey = "7f80027c98084fd9999f9f2643a6c231",
              Endpoint = "https://eastus.api.cognitive.microsoft.com/"

          };

        public async Task<string> Analyze(string imageUrl)
        {
            var image = await Client.GetByteArrayAsync(imageUrl);

            var result = _predictionClient.ClassifyImage(
              projectId: new Guid("efdba214-6789-43ed-aece-098e8d0bb54a"),
              publishedName: "minilitwareLab",
              imageData: new MemoryStream(image));

            return result.Predictions.OrderByDescending(x => x.Probability).First().TagName;
        }

       

    }


 
}
