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
                UserDataCollection user = await SaveConversationData.GetUserDataCollection(activity.From.Id);
                Debug.WriteLine(user.ToJson());
                await MapUserToContext(user, context);
                
                string preferredLanguage = context.UserData.GetValue<string>("PreferedLang").Substring(0,2).ToLower();
                BsonObjectId iD = new BsonObjectId(new ObjectId(context.UserData.GetValue<string>("_id")));
                await SaveConversationData.UpdateInputLanguage(iD, preferredLanguage);
                string name = context.UserData.GetValue<string>("Name");
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
                    context.UserData.SetValue("PreferedLang", StringResources.ar);
                    await context.PostAsync(StringResources.ar_Language_Selected);
                    break;
                case "عربي":
                    context.UserData.SetValue("PreferedLang", StringResources.ar);
                    await context.PostAsync(StringResources.ar_Language_Selected);
                    break;
                case "english":
                    context.UserData.SetValue("PreferedLang", StringResources.en);
                    await context.PostAsync(StringResources.en_Language_Selected);
                    break;
                case "الإنجليزية":
                    context.UserData.SetValue("PreferedLang", StringResources.en);
                    await context.PostAsync(StringResources.en_Language_Selected);
                    break;
                default:
                    if (language == StringResources.en)
                    {
                        context.UserData.SetValue("PreferedLang", StringResources.en);
                        await context.PostAsync(StringResources.en_UsingEnglish);
                    }
                    else if (language == StringResources.ar)
                    {
                        context.UserData.SetValue("PreferedLang", StringResources.ar);
                        await context.PostAsync(StringResources.ar_UsingArabic);
                    }
                    else
                    {
                        context.UserData.SetValue("PreferedLang", StringResources.en);
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
            context.UserData.SetValue("Name", name);
            string language = context.UserData.GetValue<string>("PreferedLang");
            //await SaveConversationData.SaveConversationAsync(activity.From.Name, activity.Recipient.Id, activity.Recipient.Name, activity.ServiceUrl, activity.ChannelId, activity.Conversation.Id);
            ConversationReference reference = new ConversationReference
            {
                ActivityId = activity.From.Id,
                Bot = activity.Recipient,
                ChannelId = activity.ChannelId,
                ServiceUrl = activity.ServiceUrl,
                User = activity.From,
            };
            await SaveConversationData.SaveNewUser(activity.From.Id, name, language, reference);
            UserDataCollection user = await SaveConversationData.GetUserDataCollection(activity.From.Id);

            await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_NiceToMeetYou"), name)+ "\U0001F642 ");
            user = await SendUserForm(context, user._id);

            await MapUserToContext(user, context);

            await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_ThanksForInfo"));
            await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_WeeklyNotifications"), name);
            await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_HowAreYouName"), name));
            context.UserData.SetValue("firstUsage", "true");
            context.Wait(HowResponse);
        }
        
        private static async Task HowResponse(IDialogContext context, IAwaitable<object> result)
        {
            /* 
             * Uses sentiment analysis to determine the appropriate response to how the user is doing
             */
            Activity activity = await result as Activity;
            UserDataCollection user = await SaveConversationData.GetUserDataCollection(activity.From.Id);

            await CheckLanguage(activity.Text.Trim(), context);
            string language = context.UserData.GetValue<string>("PreferedLang");
            string gender = context.UserData.GetValue<string>("gender");
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
                context.UserData.SetValue("firstUsage", "false");
                await context.Forward(new LearningDialog(), ResumeAfterKill, activity, CancellationToken.None);
            }
            else if (context.UserData.GetValue<string>("firstUsage") == "true")
            {
                await SaveConversationData.UpdateFirstUsage(user._id);
                context.UserData.SetValue("firstUsage", "false");
                await context.Forward(new CommandDialog(), ResumeAfterKill, activity, CancellationToken.None);
            }
            else
            {
                context.UserData.SetValue("firstUsage", "false");
                await context.Forward(new LearningDialog(), ResumeAfterKill, activity, CancellationToken.None);
            }
        }

        private static async Task ResumeAfterKill(IDialogContext context, IAwaitable<object> result)
        {
            context.Done(true);
        }

        private static async Task<UserDataCollection> SendUserForm(IDialogContext context, BsonObjectId iD)
        {
            /*
             * Function that sends the user a link to a preference form
             * Conversation is paused until preference form is filled out
             */
            UserDataCollection user = UserDataCollection.Find(x => x._id == iD).FirstOrDefault();
            Object userId = user._id;
            string language = user.PreferedLang;
            List<string> interests = user.interests;

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
            while (user.interests == null || interests.SequenceEqual(user.interests))
            {
                user = UserDataCollection.Find(x => x._id == iD).FirstOrDefault();
                Thread.Sleep(1000);
                timeout += 1;
                if (timeout >= 18000)
                {
                    UserDataCollection.DeleteMany(x => x._id == iD);
                    await context.Forward(new ConversationStarter(), ResumeAfterKill, context.Activity, CancellationToken.None);
                }
            }
            return user;
        }

        public static async Task CheckLanguage(string input, IDialogContext context)
        {
            string inputLang = await Translate.Detect(input);
            BsonObjectId iD = new BsonObjectId(new ObjectId(context.UserData.GetValue<string>("_id")));
            if (inputLang != context.UserData.GetValue<string>("PreferedLang"))
            {
                if (inputLang == "fa") inputLang = StringResources.ar;
                await SaveConversationData.UpdateInputLanguage(iD, inputLang);
                context.UserData.SetValue("PreferedLang", inputLang);
            }
        }

        public static async Task MapUserToContext(UserDataCollection user, IDialogContext context)
        {
            context.UserData.SetValue("_id", user._id.ToString());
            context.UserData.SetValue("Name", user.Name == null ? "" : user.Name);
            context.UserData.SetValue("Accreditation", user.accreditation == null ? "" : user.accreditation);
            context.UserData.SetValue("PreferedLang", user.PreferedLang == null? context.UserData.GetValue<string>("PreferedLang") : user.PreferedLang);
            context.UserData.SetValue("PastCourses", user.PastCourses == null ? new List<UserCourse>() : user.PastCourses);
            context.UserData.SetValue("preferencesSet", user.preferencesSet);
            context.UserData.SetValue("arabicText", user.arabicText == null ? "" : user.arabicText);
            context.UserData.SetValue("chosenCourse", user.chosenCourse == null ? new CourseList() : user.chosenCourse);
            context.UserData.SetValue("conversationReference", user.conversationReference == null ? new ConversationReference() : user.conversationReference);
            context.UserData.SetValue("courseList", user.courseList == null ? new List<UserCourse>() : user.courseList);
            context.UserData.SetValue("currentCourse", user.currentCourse == null ? new UserCourse() : user.currentCourse);
            context.UserData.SetValue("currentSubTopic", user.currentSubTopic == null ? "" : user.currentSubTopic);
            context.UserData.SetValue("currentTopic", user.currentTopic == null ? "": user.currentTopic);
            context.UserData.SetValue("gender", user.gender == null ? "" : user.gender);
            context.UserData.SetValue("interests", user.interests == null ? new List<string>() : user.interests);
            context.UserData.SetValue("langauge", user.language == null ? "" : user.language);
            context.UserData.SetValue("lastActive", user.lastActive == null ? new DateTime() : user.lastActive);
            context.UserData.SetValue("lastNotified", user.lastNotified);
            context.UserData.SetValue("delivery", user.delivery == null ? "" : user.delivery);
            context.UserData.SetValue("firstUsage", user.firstUsage.ToString() == null ? "true" : user.firstUsage.ToString());
            context.UserData.SetValue("education", user.education == null ? "" : user.education);
            context.UserData.SetValue("messageStack", user.messageStack == null ? new Stack<string>() : user.messageStack);
            context.UserData.SetValue("privacy_policy_version", user.privacy_policy_version);
            context.UserData.SetValue("Notification", user.Notification);
            context.UserData.SetValue("User_id", user.User_id == null ? context.Activity.From.Id : user.User_id);
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