using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using NetHope.ProactiveMessage;
using NetHope.Resources;

namespace NetHope.Dialogs
{
    internal class CommandReminder : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;
            await OutputCommands(context);
            await context.Forward(new LearningDialog(), ResumeAfterKill, activity, CancellationToken.None);
        }

        private async Task ResumeAfterKill(IDialogContext context, IAwaitable<object> result)
        {
            context.Done(true);
        }

        public static async Task OutputCommands(IDialogContext context)
        {
            string language = ConversationStarter.user.PreferedLang;
            string gender = ConversationStarter.user.gender;
            await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_CommandList1"));
            string List = "";
            if (language == StringResources.ar)
            {
                switch (gender)
                {
                    case "female":
                        List += StringResources.ar_CommandListFemale;
                        break;
                    case "male":
                        List += StringResources.ar_CommandListMale;
                        break;
                    default:
                        List += StringResources.ar_CommandListDefault;
                        break;
                }
            }
            List += "\n\n" + StringResources.ResourceManager.GetString($"{language}_CommandList2");
            List += "\n\n" + StringResources.ResourceManager.GetString($"{language}_CommandList3");
            List += "\n\n" + StringResources.ResourceManager.GetString($"{language}_CommandList4");
            List += "\n\n" + StringResources.ResourceManager.GetString($"{language}_CommandList5");
            await context.PostAsync(List);
        }
    }
}
