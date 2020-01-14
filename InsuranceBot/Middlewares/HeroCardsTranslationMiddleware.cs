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
    public class HeroCardsTranslationMiddleware : IMiddleware
    {
        private readonly IStatePropertyAccessor<string> _languageStateProperty;

        /// <summary>
        /// Initializes a new instance of the <see cref="HeroCardsTranslationMiddleware"/> class.
        /// </summary>
        /// <param name="languageStateProperty">State property for current language.</param>
        public HeroCardsTranslationMiddleware(IStatePropertyAccessor<string> languageStateProperty)
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

            turnContext.OnSendActivities(async (newContext, activities, nextSend) =>
            {
                foreach (var attachment in activities.SelectMany(x => x.Attachments ?? new List<Attachment>()))
                {
                    if (attachment.Content is HeroCard heroCard)
                    {
                        heroCard.Buttons = await TranslateCardActions(language, heroCard.Buttons);
                    }
                }

                return await nextSend();
            });

            await next(cancellationToken);
        }

        private async Task<IList<CardAction>> TranslateCardActions(string targetLanguage, IEnumerable<CardAction> actions)
        {
            if (targetLanguage?.StartsWith("en") ?? true)
            {
                return actions.ToList();
            }

            var translatedActions = new List<CardAction>();
            foreach (var action in actions)
            {
                var title = await TranslatorService.Translate(action.Title, "en", targetLanguage);
                var value = await TranslatorService.Translate(action.Value as string, "en", targetLanguage);
                translatedActions.Add(new CardAction(action.Type, title, action.Image, value: value));
            }

            return translatedActions;
        }
    }
}
