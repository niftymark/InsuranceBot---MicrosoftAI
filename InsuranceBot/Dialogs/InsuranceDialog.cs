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
using Microsoft.Extensions.Logging;

namespace InsuranceBot.Dialogs
{
    public class InsuranceDialog : ComponentDialog
    {
        // Dialog IDs
        private const string ProfileDialog = "profileDialog";

        private const string Site = "https://insurance.litwaredemos.com/images";

        public InsuranceDialog(
            IStatePropertyAccessor<InsuranceState> insuranceStateAccessor,
            ILoggerFactory loggerFactory)
            : base(nameof(InsuranceDialog))
        {
            InsuranceStateAccessor = insuranceStateAccessor ?? throw new ArgumentNullException(nameof(insuranceStateAccessor));

            // Add control flow dialogs
            var waterfallSteps = new WaterfallStep[]
            {
                InitializeStateStepAsync,
                AskInsuranceTypeStep,
                FinishInsuranceTypeStep,
            };

            AddDialog(new WaterfallDialog(PromptStep.GatherInsuranceType, waterfallSteps));
            AddDialog(new TextPrompt(PromptStep.InsuranceTypePrompt));

            // Add control flow dialogs
            var gatherInfoWaterfallSteps = new WaterfallStep[]
            {
                PromptForCarTypeStepAsync,
                PromptForCarMakeStepAsync,
                PromptForCarModelStepAsync,
                PromptForCarYearStepAsync,
                PromptForCarPictureStepAsync,
                PromptForUserFeedbackStepAsync,
                FinalStep,
            };

            AddDialog(new WaterfallDialog(PromptStep.GatherInfo, gatherInfoWaterfallSteps));
            AddDialog(new TextPrompt(PromptStep.CarTypePrompt));
            AddDialog(new TextPrompt(PromptStep.CarMakePrompt, InsuranceValidator.CarMakeValidator));
            AddDialog(new TextPrompt(PromptStep.CarModelPrompt, InsuranceValidator.CarModelValidator));
            AddDialog(new NumberPrompt<int>(PromptStep.CarYearPrompt, InsuranceValidator.CarYearValidator));
            // Add picture prompt here
            AddDialog(new AttachmentPrompt(PromptStep.CarPicturePrompt, async (promptValidatorContext, cancellationToken) =>
            {
                var ineedInsuranceState = await InsuranceStateAccessor.GetAsync(promptValidatorContext.Context);
                return await InsuranceValidator.CarPictureValidator(promptValidatorContext, ineedInsuranceState.CarType);
            }));
            AddDialog(new TextPrompt(PromptStep.UserFeedbackPrompt, InsuranceValidator.UserFeedbackValidator));
        }

        public IStatePropertyAccessor<InsuranceState> InsuranceStateAccessor { get; }

        private async Task<DialogTurnResult> InitializeStateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var ineedInsuranceState = await InsuranceStateAccessor.GetAsync(stepContext.Context, () => null);
            if (ineedInsuranceState == null)
            {
                var ineedInsuranceStateOpt = stepContext.Options as InsuranceState;
                if (ineedInsuranceStateOpt != null)
                {
                    await InsuranceStateAccessor.SetAsync(stepContext.Context, ineedInsuranceStateOpt);
                }
                else
                {
                    await InsuranceStateAccessor.SetAsync(stepContext.Context, new InsuranceState());
                }
            }

            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> AskInsuranceTypeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var ineedInsuranceState = await InsuranceStateAccessor.GetAsync(stepContext.Context);

            // If we already have the insurance type move to next step
            if (!string.IsNullOrEmpty(ineedInsuranceState.InsuranceType))
            {
                return await stepContext.NextAsync();
            }

            // Since we don't have the type we need to ask it
            var actions = new[]
            {
                new CardAction(type: ActionTypes.ImBack, title: "Car", value: "Car", image: $"{Site}/auto_600x400.png"),
                new CardAction(type: ActionTypes.ImBack, title: "Property", value: "Property", image: $"{Site}/property_600x400.jpg"),
                new CardAction(type: ActionTypes.ImBack, title: "Life", value: "Life", image: $"{Site}/life_600x400.jpg"),
            };
            var heroCard = new HeroCard(buttons: actions);

            // Add the cards definition with images
            var cards = actions
                .Select(x => new HeroCard
                {
                    Images = new List<CardImage> { new CardImage(x.Image) },
                    Buttons = new List<CardAction> { x },
                }.ToAttachment())
                .ToList();


            // Replace the following line to show carousel with images
            var activity = (Activity)MessageFactory.Carousel(cards, "What kind of insurance do you need?");

            return await stepContext.PromptAsync(PromptStep.InsuranceTypePrompt, new PromptOptions { Prompt = activity }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinishInsuranceTypeStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var context = stepContext.Context;
            var ineedInsuranceState = await InsuranceStateAccessor.GetAsync(context);
            // If the insurance type is not set yet
            if (string.IsNullOrEmpty(ineedInsuranceState.InsuranceType))
            {
                ineedInsuranceState.InsuranceType = stepContext.Result as string;
            }
            
            if (string.Equals(ineedInsuranceState.InsuranceType, "car", StringComparison.OrdinalIgnoreCase))
            {
                return await stepContext.ReplaceDialogAsync(PromptStep.GatherInfo);
            }
            else
            {
                await context.SendActivityAsync($"Right now I can only help with car insurance: {ineedInsuranceState.InsuranceType}");
                return await stepContext.EndDialogAsync();
            }
        }

        private async Task<DialogTurnResult> PromptForCarTypeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var actions = new[]
            {
                new CardAction(type: ActionTypes.ImBack, title: "Sedan", value: "Sedan"),
                new CardAction(type: ActionTypes.ImBack, title: "SUV", value: "SUV"),
                new CardAction(type: ActionTypes.ImBack, title: "Sports car", value: "Sports car"),
            };

            var heroCard = new HeroCard(buttons: actions);
            var activity = (Activity)MessageFactory.Carousel(new[] { heroCard.ToAttachment() }, "Please select a car type.");
            return await stepContext.PromptAsync(PromptStep.CarTypePrompt, new PromptOptions { Prompt = activity }, cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForCarMakeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var ineedInsuranceState = await InsuranceStateAccessor.GetAsync(stepContext.Context);
            ineedInsuranceState.CarType = stepContext.Result as string;
            await InsuranceStateAccessor.SetAsync(stepContext.Context, ineedInsuranceState);

            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Type = ActivityTypes.Message,
                    Text = "What make of car do you want to insure?",
                },
            };
            return await stepContext.PromptAsync(PromptStep.CarMakePrompt, opts);
        }

        private async Task<DialogTurnResult> PromptForCarModelStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var ineedInsuranceState = await InsuranceStateAccessor.GetAsync(stepContext.Context);
            ineedInsuranceState.CarMake = stepContext.Result as string;
            await InsuranceStateAccessor.SetAsync(stepContext.Context, ineedInsuranceState);

            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Type = ActivityTypes.Message,
                    Text = "And the model?",
                },
            };
            return await stepContext.PromptAsync(PromptStep.CarModelPrompt, opts);
        }

        private async Task<DialogTurnResult> PromptForCarYearStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var ineedInsuranceState = await InsuranceStateAccessor.GetAsync(stepContext.Context);
            ineedInsuranceState.CarModel = stepContext.Result as string;
            await InsuranceStateAccessor.SetAsync(stepContext.Context, ineedInsuranceState);

            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Type = ActivityTypes.Message,
                    Text = "And the year?",
                },
            };
            return await stepContext.PromptAsync(PromptStep.CarYearPrompt, opts);
        }

        private async Task<DialogTurnResult> PromptForCarPictureStepAsync(WaterfallStepContext stepContext, CancellationToken cancellation)
        {
            var ineedInsuranceState = await InsuranceStateAccessor.GetAsync(stepContext.Context);
            ineedInsuranceState.CarYear = (int)stepContext.Result;
            await InsuranceStateAccessor.SetAsync(stepContext.Context, ineedInsuranceState);

            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Type = ActivityTypes.Message,
                    Text = "Please upload an image of your car to continue.",
                },
            };
            return await stepContext.PromptAsync(PromptStep.CarPicturePrompt, opts);
        }

        private async Task<DialogTurnResult> PromptForUserFeedbackStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync("We can insure your new car for just $116.25 per month.This includes coverage for your whole family and a 10 % discount given your existing policy with us.");
            await Task.Delay(2000);

            var opts = new PromptOptions
            {
                Prompt = new Activity
                {
                    Type = ActivityTypes.Message,
                    Text = "What do you think? If this sounds good, we can start your coverage right now!",
                },
            };
            return await stepContext.PromptAsync(PromptStep.UserFeedbackPrompt, opts);
        }

        private async Task<DialogTurnResult> FinalStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var ineedInsuranceState = await InsuranceStateAccessor.GetAsync(stepContext.Context);
            var feedback = stepContext.Result as string;
            var sentiment = await TextSentimentService.GetTextSentiment(feedback);
            if (sentiment < 0.5)
            {
                await stepContext.Context.SendActivityAsync("I understand. We really want to make it work. Let me see if a customer service agent is available to review this in more detail.");
            }
            else
            {
                await stepContext.Context.SendActivityAsync("Great! We are going to prepare everything for you.");
            }

            return await stepContext.EndDialogAsync();
        }
    }

    public static class PromptStep
    {
        public const string GatherInsuranceType = "gatherInsuranceType";
        public const string InsuranceTypePrompt = "insuranceTypePrompt";

        public const string GatherInfo = "gatherInfo";
        public const string CarTypePrompt = "carTypePrompt";
        public const string CarMakePrompt = "carMakePrompt";
        public const string CarModelPrompt = "carModelPrompt";
        public const string CarYearPrompt = "carYearPrompt";
        public const string CarPicturePrompt = "carPicturePrompt";
        public const string UserFeedbackPrompt = "userFeedbackPrompt";
    }
}
