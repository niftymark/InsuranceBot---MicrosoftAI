using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InsuranceBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace InsuranceBot.Middlewares
{
    public class TranslationMiddleware : IMiddleware
    {
        private const string DefaultLanguage = "en";
        private readonly IStatePropertyAccessor<string> _languageStateProperty;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationMiddleware"/> class.
        /// </summary>
        /// <param name="languageStateProperty">State property for current language.</param>
        public TranslationMiddleware(IStatePropertyAccessor<string> languageStateProperty)
        {
            _languageStateProperty = languageStateProperty ?? throw new ArgumentNullException(nameof(languageStateProperty));
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(turnContext.Activity.Text))
            {
                await next(cancellationToken);
                return;
            }

            var language = await _languageStateProperty.GetAsync(turnContext, () => null);
            if (string.IsNullOrEmpty(language))
            {
                language = await TranslatorService.Detect(turnContext.Activity.Text);
                await _languageStateProperty.SetAsync(turnContext, language);
            }

            if (!language.StartsWith("en"))
            {
                // If the language in the message is not english we will translate it to english before continue
                var translatedText = await TranslatorService.Translate(turnContext.Activity.Text, language, "en");
                turnContext.Activity.Text = translatedText;
            }

            turnContext.OnSendActivities(async (newContext, activities, nextSend) =>
            {
                var userLanguage = await _languageStateProperty.GetAsync(turnContext, () => DefaultLanguage) ?? DefaultLanguage;
                var shouldTranslate = userLanguage != DefaultLanguage;

                // Translate messages sent to the user to user language
                if (shouldTranslate)
                {
                    var tasks = new List<Task>();
                    foreach (Activity currentActivity in activities.Where(a => a.Type == ActivityTypes.Message))
                    {
                        tasks.Add(TranslateMessageActivityAsync(currentActivity, userLanguage));
                    }

                    if (tasks.Any())
                    {
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                }

                return await nextSend();
            });

            await next(cancellationToken);
        }

        private async Task TranslateMessageActivityAsync(IMessageActivity activity, string targetLocale, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (activity.Type == ActivityTypes.Message)
            {
                activity.Text = await TranslatorService.Translate(activity.Text, DefaultLanguage, targetLocale);
            }
        }
    }
}
