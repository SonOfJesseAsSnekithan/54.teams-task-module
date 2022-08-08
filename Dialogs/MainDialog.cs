using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using TeamsTaskModule.Bots;

namespace TeamsTaskModule.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        protected readonly ILogger Logger;
        protected UserState UserState;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(ILogger<MainDialog> logger, UserState userState)
            : base(nameof(MainDialog))
        {
            Logger = logger;
            UserState = userState;
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new UserProfileDialog(UserState));
            AddDialog(new UserProfileTaskDialog(UserState));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var weekLaterDate = DateTime.Now.AddDays(7).ToString("MMMM d, yyyy");
            var messageText = stepContext.Options?.ToString() ?? $"What can I help you with today?\nSay something like \"get profile\" or \"get profile2\" or \"test\"";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context?.Activity?.Text.ToLower() == "get profile")
            {
                // LUIS is not configured, we just run the BookingDialog path with an empty BookingDetailsInstance.
                return await stepContext.BeginDialogAsync(nameof(UserProfileDialog), null, cancellationToken);
            }else if (stepContext.Context?.Activity?.Text.ToLower() == "get profile2")
            {
                return await stepContext.BeginDialogAsync(nameof(UserProfileTaskDialog), null, cancellationToken);
            } else if (stepContext.Context?.Activity?.Text.ToLower() == "test")
            {
                var reply = MessageFactory.Attachment(new[] { DialogAndWelcomeBot<MainDialog>.GetTaskModuleHeroCardOptions() });

                //var reply = MessageFactory.Attachment(new[] { GetTaskModuleHeroCardOptions(), GetTaskModuleAdaptiveCardOptions() });
                await stepContext.Context.SendActivityAsync(reply, cancellationToken);
            }



            return await stepContext.NextAsync(null, cancellationToken);
        }

        
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Restart the main dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}

