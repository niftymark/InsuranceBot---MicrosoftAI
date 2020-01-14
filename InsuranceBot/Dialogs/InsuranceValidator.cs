using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InsuranceBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace InsuranceBot
{
    public static class InsuranceValidator
    {
        public static async Task<bool> CarMakeValidator(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var value = promptContext.Recognized.Value?.Trim() ?? string.Empty;
            if (value.Equals("foobar", StringComparison.OrdinalIgnoreCase))
            {
                await promptContext.Context.SendActivityAsync("Hmmm I don't recognize that make.").ConfigureAwait(false);
                return false;
            }
            else
            {
                promptContext.Recognized.Value = value;
                return true;
            }

        }

        public static async Task<bool> CarTypeValidator(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            var value = promptContext.Recognized.Value;
            if (value.Score > 0.5)
            {
                promptContext.Recognized.Value = value;
                return true;
            }
            else
            {
                await promptContext.Context.SendActivityAsync($"Car Type it's invalid, please make sure that you select a valid option.").ConfigureAwait(false);
                return false;
            }
        }

        public static async Task<bool> CarModelValidator(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var value = promptContext.Recognized.Value?.Trim() ?? string.Empty;
            if (value.Length >= 2)
            {
                promptContext.Recognized.Value = value;
                return true;
            }
            else
            {
                await promptContext.Context.SendActivityAsync($"Your car model should be at least 2 characters long.").ConfigureAwait(false);
                return false;
            }
        }

        public static async Task<bool> CarYearValidator(PromptValidatorContext<int> promptContext, CancellationToken cancellationToken)
        {
            var value = promptContext.Recognized.Value;
            if (value >= 1900 && value <= 2018)
            {
                promptContext.Recognized.Value = value;
                return true;
            }
            else
            {
                await promptContext.Context.SendActivityAsync($"Your car year should be between 1900 and 2018.").ConfigureAwait(false);
                return false;
            }
        }



        public static async Task<bool> CarPictureValidator(PromptValidatorContext<IList<Attachment>> promptContext, string carType)
        {
            // Add validation code here
            var computerVisionService = new ComputerVisionService();
            var detectResult = await computerVisionService.Detect(promptContext.Recognized.Value[0].ContentUrl);
            if (!detectResult.IsCar)
            {
                await promptContext.Context.SendActivityAsync($"That doesn't look like a car. It looks more like {detectResult.Description}.");
                return false;
            }

            // Add Custom Vision validation here
            var customVisionService = new CustomVisionService();
            var predictedCarType = await customVisionService.Analyze(promptContext.Recognized.Value[0].ContentUrl);
            var isRightCarType = string.Equals(predictedCarType, carType, StringComparison.OrdinalIgnoreCase);
            if (!isRightCarType)
            {
                await promptContext.Context.SendActivityAsync($"That doesn't look like a {carType}.");
                return false;
            }


            return true;
        }



        public static async Task<bool> UserFeedbackValidator(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var value = promptContext.Recognized.Value?.Trim() ?? string.Empty;
            if (value.Length > 2)
            {
                promptContext.Recognized.Value = value;
                return true;
            }
            else
            {
                await promptContext.Context.SendActivityAsync($"I need a little more feedback.").ConfigureAwait(false);
                return false;
            }
        }
    }
}
