using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using MongoDB.Driver;
using NetHope.SupportClasses;
using NetHope.ProactiveMessage;
using MongoDB.Bson;
using NetHope.Resources;

namespace NetHope.Dialogs
{
    internal class CommandDialog : IDialog<object>
    {
        private string language;
        private string gender;
        /*
         * Shows users the functions available to them and also instructs them how to change languages on the fly.
         * Language dependent and as such the buttons are provided in Eng / Arabic. 
         * If they're in English, WaitForSelection sends to Root, otherwise it's translated and sent to Luis
         */
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            /* The user is presented with different options that they can use to interact with Hakeem
             * These are provided in the form of buttons
             * My Preferences button is hidden at Dan's request for now
             */
            var activity = await result as Activity;
            gender = ConversationStarter.user.gender;
            language = ConversationStarter.user.PreferedLang; 

            await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_SwitchBetweenLanguages"));
            var selectOption = context.MakeMessage();
            if (language == StringResources.ar)
            {
                switch (gender)
                {
                    case "female":
                        selectOption.Text = StringResources.ar_CommandReminderFemale;
                        break;
                    case "male":
                        selectOption.Text = StringResources.ar_CommandReminderMale;
                        break;
                    default:
                        selectOption.Text = StringResources.ar_CommandReminderDefault;
                        break;
                }
            }
            else //English response, will need to be changed to 'else if' if other languages added
            {
                selectOption.Text = StringResources.en_CommandReminder1;
            }
            selectOption.Text += "\n\n" + StringResources.ResourceManager.GetString($"{language}_CommandReminder2");
            selectOption.Text += "\n\n" + StringResources.ResourceManager.GetString($"{language}_CommandReminder3");
            selectOption.Text += "\n\n" + StringResources.ResourceManager.GetString($"{language}_CommandReminder4");
            selectOption.Text += "\n\n" + StringResources.ResourceManager.GetString($"{language}_CommandReminder5");
            selectOption.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                    {
                        new CardAction() { Title = StringResources.ResourceManager.GetString($"{language}_AboutHakeem"), Type = ActionTypes.ImBack, Value = StringResources.ResourceManager.GetString($"{language}_About") },
                        new CardAction() { Title = StringResources.ResourceManager.GetString($"{language}_Commands"), Type = ActionTypes.ImBack, Value =  StringResources.ResourceManager.GetString($"{language}_Commands")},
                        new CardAction() { Title = StringResources.ResourceManager.GetString($"{language}_MyPreferences"), Type = ActionTypes.ImBack, Value = StringResources.ResourceManager.GetString($"{language}_Preferences") },
                        new CardAction() { Title = StringResources.ResourceManager.GetString($"{language}_BeginLearning") , Type = ActionTypes.ImBack, Value = StringResources.ResourceManager.GetString($"{language}_Continue") },
                    }
            };
            await context.PostAsync(selectOption);
            context.Wait(WaitForSelection);
        }

        private async Task WaitForSelection(IDialogContext context, IAwaitable<object> result)
        {
            /*If input is not "continue", determine language (translate if Arabic) and send Luis
             * Action taken depends on which of the commands a user chooses
             * LuisDialog() is used to determine what action to take
             * Continue starts the learning Dialog
             */
            Activity activity = await result as Activity;
            string text = activity.Text.Trim().ToLower();
            Debug.WriteLine(text);
            switch (text)
            {
                case "commands":
                    await context.Forward(new CommandReminder(), ResumeAfterKill, context.Activity, CancellationToken.None);
                    break;
                case "أوامر":
                    await context.Forward(new CommandReminder(), ResumeAfterKill, context.Activity, CancellationToken.None);
                    break;
                case "about":
                    await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_HakeemSpiel"));
                    await context.Forward(new LearningDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
                case "عن":
                    await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_HakeemSpiel"));
                    await context.Forward(new LearningDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
                case "preferences":
                    await context.Forward(new Preferences.DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "تفضيلاتي":
                    await context.Forward(new Preferences.DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "continue":
                    await context.Forward(new LearningDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
                case "استمرار":
                    await context.Forward(new LearningDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
                default:
                    if (language == StringResources.ar)
                    {
                        ConversationStarter.user.arabicText = activity.Text.Trim();
                        activity.Text = await Translate.Translator(activity.Text, StringResources.en);
                    }
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
            }
        }

        private async Task ResumeAfterKill(IDialogContext context, IAwaitable<object> result)
        {
            context.Done(true);
        }

    }

}

