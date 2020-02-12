using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Net;
using System.Diagnostics;
using MongoDB.Driver;
using NetHope.ProactiveMessage;
using MongoDB.Bson;
using NetHope.SupportClasses;
using System.Configuration;
using NetHope.Resources;

namespace NetHope.Dialogs
{
    /* Users can ask Hakeem how he is. This function is responsible for interpreting that question and providing the appropriate response
     * Hakeem will then ask the user the same question and perform sentiment analysis on the answer. This sentiment will determine Hakeem's response to the user
     */
    [Serializable]
    public class HowAreYou : IDialog<object>
    {
        private string userLanguage;
        private string gender;
        private string endpoint = ConfigurationManager.AppSettings.Get("LuisEndpoint")+ StringResources.luisEndpointExtra;
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            /* Thanks the user for asking about Hakeem and asks how they are doing
             */
            Activity activity = await result as Activity;
            await ConversationStarter.CheckLanguage(activity.Text.Trim(), context);
            UserDataCollection user = context.UserData.GetValue<UserDataCollection>("UserObject");
            userLanguage = user.PreferedLang; 
            
            string greetingResponse = StringResources.ResourceManager.GetString($"{userLanguage}_HowAreYouResponse");
            string howAreYou = StringResources.ResourceManager.GetString($"{userLanguage}_HowAreYou");
            
            await context.PostAsync(greetingResponse);
            await context.PostAsync(howAreYou);
            context.Wait(UserResponse);
        }

        private async Task UserResponse(IDialogContext context, IAwaitable<object> result)
        {
            /* Function responsible for sentiment analysis
             * Switch statement is used to provide the appropriate response based on the users input
             */
            Activity activity = await result as Activity;
            await ConversationStarter.CheckLanguage(activity.Text.Trim(), context);
            UserDataCollection user = context.UserData.GetValue<UserDataCollection>("UserObject");
            gender = user.gender;
            userLanguage = user.PreferedLang; 

            string endpoint_query = endpoint + activity.Text;
            string sentiment = "";
            using (WebClient client = new WebClient())
            {
                string response = client.DownloadString(endpoint_query);
                QueryResponse output = new QueryResponse(response);
                sentiment = output.sentimentAnalysis.sentiment;
            }
            switch (sentiment)
            {
                case "positive":
                    await context.PostAsync(StringResources.ResourceManager.GetString($"{userLanguage}_PositiveSentiment"));
                    break;
                case "negative":
                    if (userLanguage == StringResources.ar)
                    {
                        switch (gender)
                        {
                            case "female":
                                await context.PostAsync(StringResources.ar_NegativeSentimentFemale);
                                break;
                            case "male":
                                await context.PostAsync(StringResources.ar_NegativeSentimentMale);
                                break;
                            default:
                                await context.PostAsync(StringResources.ar_NegativeSentimentDefault);
                                break;
                        }

                    }
                    else
                    {
                        await context.PostAsync(StringResources.en_NegativeSentiment);
                    }
                    break;
                default:
                    await context.PostAsync(StringResources.ResourceManager.GetString($"{userLanguage}_LetsLearn"));
                    break;
            }
            context.Done(true);
        }

        private async Task ResumeAfterKill(IDialogContext context, IAwaitable<object> result)
        {
            context.Done(true);
        }
    }
}