using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.AI.Luis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InsuranceBot.Dialogs;
using InsuranceBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace InsuranceBot
{
    public class InsuranceBot : IBot
    {
        // Supported LUIS Intents
        public const string INeedInsuranceIntent = "INeedInsurance";

        private readonly InsuranceBotAccessors _accessors;
        private readonly ILogger _logger;

        private QnAMaker QnA { get; } = null;

        // Add LUIS Recognizer
        private LuisRecognizer _luis;



        public InsuranceBot(InsuranceBotAccessors accessors, ILoggerFactory loggerFactory, LuisRecognizer luisRecognizer, QnAMaker qna)

        {
            _accessors = accessors ?? throw new ArgumentNullException(nameof(accessors));

            QnA = qna ?? throw new ArgumentNullException(nameof(qna));

            // The DialogSet needs a DialogState accessor, it will call it when it has a turn context.
            _dialogs = new DialogSet(accessors.ConversationDialogState);

            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            // Initialize the LUIS recognizer
            _luis = luisRecognizer;


            _dialogs.Add(new InsuranceDialog(_accessors.GetInsuranceState, loggerFactory));

            _logger = loggerFactory.CreateLogger<InsuranceBot>();
            _logger.LogTrace("Turn start.");
        }

        private DialogSet _dialogs { get; set; }

        /// <summary>
        /// Every conversation turn for our Rennovations Bot will call this method.
        /// There are no dialogs used, since it's "single turn" processing, meaning a single
        /// request and response.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        /// <seealso cref="IMiddleware"/>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            var activity = turnContext.Activity;

            // Create a dialog context
            var dc = await _dialogs.CreateContextAsync(turnContext);

            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            if (activity.Type == ActivityTypes.Message)
            {
                // Continue the current dialog
                var dialogResult = await dc.ContinueDialogAsync();

                // if no one has responded,
                if (!dc.Context.Responded)
                {
                    // examine results from active dialog
                    switch (dialogResult.Status)
                    {
                        case DialogTurnStatus.Empty:
                            // Replace with LUIS handler here
                            // Perform a call to LUIS to retrieve results for the current activity message.
                            var luisResults = await _luis.RecognizeAsync(dc.Context, cancellationToken).ConfigureAwait(false);
                            var topScoringIntent = luisResults?.GetTopScoringIntent();
                            var topIntent = topScoringIntent.Value.intent;

                            // Your code goes here
                            switch (topIntent)
                            {
                                case INeedInsuranceIntent:
                                    await INeedInsuranceHandler(turnContext, dc, luisResults);
                                    break;

                                default:
                                    var answers = await this.QnA.GetAnswersAsync(dc.Context);

                                    if (answers is null || answers.Count() == 0)
                                    {
                                        await dc.Context.SendActivityAsync("Sorry, I didn't understand that.");
                                    }
                                    else if (answers.Any())
                                    {
                                        // If the service produced one or more answers, send the first one.
                                        await dc.Context.SendActivityAsync(answers[0].Answer);
                                    }
                                    break;
                            }

                            break;

                        case DialogTurnStatus.Waiting:
                            // The active dialog is waiting for a response from the user, so do nothing.
                            break;

                        case DialogTurnStatus.Complete:
                            await dc.EndDialogAsync();
                            break;

                        default:
                            await dc.CancelAllDialogsAsync();
                            break;
                    }
                }

                // Get the conversation state from the turn context.
                var state = await _accessors.GetInsuranceState.GetAsync(turnContext, () => new InsuranceState());

                // Set the property using the accessor.
                await _accessors.GetInsuranceState.SetAsync(turnContext, state);

                // Save the new state into the conversation state.
                await _accessors.ConversationState.SaveChangesAsync(turnContext);
                await _accessors.UserState.SaveChangesAsync(turnContext);
            }
            else if (activity.Type == ActivityTypes.ConversationUpdate)
            {
                // Add code for welcome message here
                if (activity.MembersAdded.Any())
                {
                    // Iterate over all new members added to the conversation.
                    foreach (var member in activity.MembersAdded)
                    {
                        if (string.Equals(member.Id, activity.Recipient.Id, StringComparison.InvariantCultureIgnoreCase))
                        {
                            await dc.Context.SendActivityAsync("Hi! How can I help you today?");
                        }
                    }
                }
            }
        }

        private async Task INeedInsuranceHandler(ITurnContext turnContext, DialogContext dialogContext, RecognizerResult result)
        {
            var type = (string)result.Entities["InsuranceType"]?[0];
            if (!string.IsNullOrEmpty(type))
            {
                var state = await _accessors.GetInsuranceState.GetAsync(turnContext, () => new InsuranceState());
                state.InsuranceType = type;
                await _accessors.GetInsuranceState.SetAsync(turnContext, state);

                // Save the new state into the conversation state.
                await _accessors.ConversationState.SaveChangesAsync(turnContext);
                await _accessors.UserState.SaveChangesAsync(turnContext);
            }

            await dialogContext.BeginDialogAsync(nameof(InsuranceDialog));
        }
    }
}
