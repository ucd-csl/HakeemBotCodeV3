using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using NetHope.Dialogs;
using NetHope.Resources;
using NetHope.SupportClasses;
using NetHope.Preferences;
using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Configuration;

namespace NetHope.ProactiveMessage
{
    internal class ConversationStarter : IDialog<object>
    {
        /* This class aims to allow the user to continue on from where they left off last time
         * The user is prompted to see who they are and then asked about the previous course they took
         * to see how they are getting on/got on with it.
         * (Currently the user can pick one of three 'mock-up' users, with Fatima as the default user
         * This will likely change in the future)
         */
        private static readonly IMongoCollection<UserDataCollection> UserDataCollection = SaveConversationData.GetReferenceToCollection<UserDataCollection>(ConfigurationManager.AppSettings.Get("UserCollection"));
        private static string LuisEndpoint = ConfigurationManager.AppSettings.Get("LuisEndpoint") + StringResources.luisEndpointExtra;
        private static string PreferencesUrl = ConfigurationManager.AppSettings.Get("PreferencesUrl");
        public static UserDataCollection user = new UserDataCollection();
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(CheckUserExists);
            return Task.CompletedTask;
        }

        private static async Task CheckUserExists(IDialogContext context, IAwaitable<object> result)
        {
            /* 
             * Hakeem checks if the user has already created an account
             * If not, the user is brought to the React App and enters some personal information
             * */
            Activity activity = await result as Activity;
            if (!SaveConversationData.CheckUserExists(activity.From.Id))
            {
                await context.PostAsync(String.Format(StringResources.en_HakeemIntroduction + "{0}" + StringResources.ar_HakeemIntroduction, "\n\n"));
                var response = context.MakeMessage();
                response.Text = String.Format(StringResources.en_ChooseLanguage + "{0}" + StringResources.ar_ChooseLanguage, "\n\n");
                response.SuggestedActions = new SuggestedActions()
                {
                    Actions = new List<CardAction>()
                  {
                    new CardAction() { Title = StringResources.ar_ContinueInArabic, Value = StringResources.ar_Arabic, Type = ActionTypes.ImBack },
                    new CardAction() { Title = StringResources.en_ContinueInEnglish, Value = StringResources.en_English, Type = ActionTypes.ImBack },
                  }
                };
                await context.PostAsync(response);
                context.Wait(SaveLanguageSelection);
            }
            else
            {
                user = await SaveConversationData.GetUserDataCollection(activity.From.Id);
                string preferredLanguage = user.PreferedLang.Substring(0,2).ToLower();
                await SaveConversationData.UpdateInputLanguage(user._id, preferredLanguage);
                string name = user.Name;
                Debug.WriteLine(preferredLanguage + " " + name + " " + StringResources.ResourceManager.GetString($"{preferredLanguage}_HowAreYouName"));
                await context.PostAsync(string.Format(StringResources.ResourceManager.GetString($"{preferredLanguage}_HowAreYouName"), name));
                context.Wait(HowResponse);
            }
        }

        private static async Task SaveLanguageSelection(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            /* 
             * This function detects the language the user replies in and saves the language in context under convoLanguage (the language the user wants to converse with Hakeem in)
             * Language is saved here until the user profile in the database is created.
             * If a language other than Arabic or English is specified the language is defaulted to English
             */
            var activity = await result as Activity;
            string language = await Translate.Detect(activity.Text);
            switch (activity.Text.Trim().ToLower())
            {
                case "arabic":
                    user.PreferedLang = StringResources.ar;
                    await context.PostAsync(StringResources.ar_Language_Selected);
                    break;
                case "عربي":
                    user.PreferedLang = StringResources.ar;
                    await context.PostAsync(StringResources.ar_Language_Selected);
                    break;
                case "english":
                    user.PreferedLang = StringResources.en;
                    await context.PostAsync(StringResources.en_Language_Selected);
                    break;
                case "الإنجليزية":
                    user.PreferedLang = StringResources.en;
                    await context.PostAsync(StringResources.en_Language_Selected);
                    break;
                default:
                    if (language == StringResources.en)
                    {
                        user.PreferedLang = StringResources.en;
                        await context.PostAsync(StringResources.en_UsingEnglish);
                    }
                    else if (language == StringResources.ar)
                    {
                        user.PreferedLang = StringResources.ar;
                        await context.PostAsync(StringResources.ar_UsingArabic);
                    }
                    else
                    {
                        user.PreferedLang = StringResources.en;
                        await context.PostAsync(StringResources.en_LanguageNotSupported);
                    }
                    break;
            }
            if (!SaveConversationData.CheckUserExists(activity.From.Id))
            {
                await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_AskForName"));
                context.Wait(MakeNewUser);
            }
        }

        private static async Task MakeNewUser(IDialogContext context, IAwaitable<object> result)
        {
            /*
             *Store the user's name in the DB
             *Call the React App, allowing the user to provide some personal information
             */
            Activity activity = await result as Activity;
            string name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(activity.Text.ToLower());
            string language = user.PreferedLang;
            await SaveConversationData.SaveConversationAsync(activity.From.Name, activity.Recipient.Id, activity.Recipient.Name, activity.ServiceUrl, activity.ChannelId, activity.Conversation.Id);
            ConversationReference reference = new ConversationReference
            {
                ActivityId = activity.From.Id,
                Bot = activity.Recipient,
                ChannelId = activity.ChannelId,
                ServiceUrl = activity.ServiceUrl,
                User = activity.From,
            };
            await SaveConversationData.SaveNewUser(activity.From.Id, name, language, reference);
            user = await SaveConversationData.GetUserDataCollection(activity.From.Id);
            //UserDataCollection.Find(x => x.User_id == activity.From.Id && x.Name.ToLower() == name.ToLower()).FirstOrDefault();

            await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_NiceToMeetYou"), name)+ "\U0001F642 ");
            user = await SendUserForm(context, user._id);
            await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_ThanksForInfo"));
            await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_WeeklyNotifications"), name);
            await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_HowAreYouName"), name));
            user.firstUsage = true; 
            context.Wait(HowResponse);
        }
        
        private static async Task HowResponse(IDialogContext context, IAwaitable<object> result)
        {
            /* 
             * Uses sentiment analysis to determine the appropriate response to how the user is doing
             */
            Activity activity = await result as Activity;
            string language = "";
            //string inputLang = await Translate.Detect(activity.Text);
            //if (inputLang != user.PreferedLang)
            //{
            //    activity.Text = inputLang == StringResources.ar ? StringResources.en_UseArabic : StringResources.en_UseEnglish;
            //    Debug.WriteLine(activity.Text);
            //    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
            //}
            //else
            //{
                await CheckLanguage(activity.Text.Trim(), user._id);
                language = user.PreferedLang;
                string gender = user.gender.ToLower();
                string query = activity.Text.Trim();
                if (language == StringResources.ar)
                {
                    await Translate.Translator(query, StringResources.en);
                };
                string endpoint_query = LuisEndpoint + query;
                string sentiment = "";
                using (WebClient client = new WebClient())
                {
                    string response = client.DownloadString(endpoint_query);
                    QueryResponse output = new QueryResponse(response);
                    sentiment = output.sentimentAnalysis.sentiment;
                }
                Debug.WriteLine(sentiment);
                if (sentiment == StringResources.en_Positive)
                {
                    await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_PositiveSentiment"));
                }
                else if (sentiment == StringResources.en_Negative && language == StringResources.ar)
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
                else if (sentiment == StringResources.en_Negative)
                {
                    await context.PostAsync(StringResources.en_NegativeSentiment);
                }
                if (user.GetType().GetProperty("firstUsage") == null)
                {
                    await SaveConversationData.UpdateFirstUsage(user._id);
                    user.firstUsage = false;
                    await context.Forward(new LearningDialog(), ResumeAfterKill, activity, CancellationToken.None);
                }
                else if (user.firstUsage)
                {
                    await SaveConversationData.UpdateFirstUsage(user._id);
                    user.firstUsage = false;
                    await context.Forward(new CommandDialog(), ResumeAfterKill, activity, CancellationToken.None);
                }
                else
                {
                    await context.Forward(new LearningDialog(), ResumeAfterKill, activity, CancellationToken.None);
                }
            //}
        }

        private static async Task ResumeAfterKill(IDialogContext context, IAwaitable<object> result)
        {
            context.Done(true);
        }

        private static async Task<UserDataCollection> SendUserForm(IDialogContext context, ObjectId iD)
        {
            /*
             * Function that sends the user a link to a preference form
             * Conversation is paused until preference form is filled out
             */
            UserDataCollection user2 = UserDataCollection.Find(x => x._id == iD).FirstOrDefault();
            Object userId = user2._id;
            string language = user2.PreferedLang;
            List<string> interests = user2.interests;

            Aes myAes = Aes.Create();
            string encrypted = EncryptString(userId.ToString(), myAes.Key, myAes.IV);
            string cryptic = encrypted;
            string key = Convert.ToBase64String(myAes.Key);
            string iv = Convert.ToBase64String(myAes.IV);
            var link = context.MakeMessage();
            string warningMessage;
            
            link.Text = StringResources.ResourceManager.GetString($"{language}_PreferenceFormLink");
            List<CardAction> Actions = new List<CardAction>()
            {
                new CardAction() { Title = StringResources.ResourceManager.GetString($"{language}_PreferencesForm"), Type = ActionTypes.OpenUrl, Value = PreferencesUrl  + cryptic + "," + key + "," + iv + ","+language },
            };
            HeroCard Card = new HeroCard()
            {
                Buttons = Actions
            };
            Attachment linkAttachment = Card.ToAttachment();
            link.Attachments.Add(linkAttachment);
            warningMessage = StringResources.ResourceManager.GetString($"{language}_CompleteFormWarning");
            
            int timeout = 0;
            await context.PostAsync(link);
            await context.PostAsync(warningMessage);
            while (user2.interests == null || interests.SequenceEqual(user2.interests))
            {
                user2 = UserDataCollection.Find(x => x._id == iD).FirstOrDefault();
                Thread.Sleep(1000);
                timeout += 1;
                if (timeout >= 18000)
                {
                    UserDataCollection.DeleteMany(x => x._id == iD);
                    await context.Forward(new ConversationStarter(), ResumeAfterKill, context.Activity, CancellationToken.None);
                }
            }
            return user2;
        }

        public static async Task CheckLanguage(string input, BsonObjectId userId)
        {
            string inputLang = await Translate.Detect(input);
            if (inputLang != user.PreferedLang)
            {
                if (inputLang == "fa") inputLang = StringResources.ar;
                await SaveConversationData.UpdateInputLanguage(userId, inputLang);
                user.PreferedLang = inputLang;
            }
        }

        public static string EncryptString(string plainText, byte[] key, byte[] iv)
        {
            Aes encryptor = Aes.Create();
            encryptor.Mode = CipherMode.CBC;

            byte[] aesKey = new byte[32];
            Array.Copy(key, 0, aesKey, 0, 32);
            encryptor.Key = aesKey;
            encryptor.IV = iv;

            MemoryStream memoryStream = new MemoryStream();
            ICryptoTransform aesEncryptor = encryptor.CreateEncryptor();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, aesEncryptor, CryptoStreamMode.Write);

            byte[] plainBytes = ASCIIEncoding.UTF8.GetBytes(plainText);
            cryptoStream.Write(plainBytes, 0, plainBytes.Length);
            cryptoStream.FlushFinalBlock();
            byte[] cipherBytes = memoryStream.ToArray();

            memoryStream.Close();
            cryptoStream.Close();

            string cipherText = Convert.ToBase64String(cipherBytes, 0, cipherBytes.Length);
            return cipherText;
        }

    }
}