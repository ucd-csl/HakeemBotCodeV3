using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NetHope.Controllers;
using NetHope.Resources;
using NetHope.ProactiveMessage;
using NetHope.Dialogs;
using MongoDB.Bson;
using NetHope.Preferences;
using NetHope.SupportClasses;
using System.Linq;
using MongoDB.Driver;
using System.Text.RegularExpressions;

namespace NetHope.Dialogs
{
    [Serializable]
    public class LuisDialog : LuisDialog<object>
    {
        private string language;
        private static readonly IMongoCollection<UserDataCollection> UserDataCollection = SaveConversationData.GetReferenceToCollection<UserDataCollection>(ConfigurationManager.AppSettings.Get("UserCollection"));
        public LuisDialog() : base(new LuisService(GetLuisModelAttribute()))
        {
        }

        private static LuisModelAttribute GetLuisModelAttribute()
        {
            LuisModelAttribute attribute = new LuisModelAttribute(ConfigurationManager.AppSettings.Get("LuisModelId"), ConfigurationManager.AppSettings.Get("LuisKey"), domain: ConfigurationManager.AppSettings.Get("CognitiveDomain"));
            attribute.BingSpellCheckSubscriptionKey = ConfigurationManager.AppSettings.Get("BingSpellCheckKey");
            attribute.SpellCheck = true;
            return attribute;
        }

        /*LUIS Queries return with the following constants. 
         * The return value results in the appropriate function being called, all of which are contained in this file
         * If new Entities/ Intents are added to LUIS they must have a corresponding command in this file.
         * The output of each entity/ intent can be modified within this file although modification in other files further along the conversation path may also be required
         */
        // CONSTANTS        
        // Entities
        public const string Entity_Preference = "preferences";
        public const string Entity_StartOver = "start over";
        public const string Entity_Commands = "commands";
        public const string Entity_subject = "subject";
        // Intents
        public const string Intent_learning = "learning";
        public const string Intent_None = "None";
        public const string Intent_Greeting = "greeting";
        public const string Intent_Question = "Question";
        public const string Intent_Gibberish = "Gibberish";
        public const string Intent_Clarification = "Clarification";
        public const string Intent_Command = "command";
        public const string Intent_End = "end_conversation";
        public const string Intent_Preferences = "Preferences";
        public const string Intent_Restart = "Restart";
        public const string Intent_Suggestions = "suggestion";
        public const string Intent_How = "How_are_you";
        public const string Intent_How_Response = "How_are_you_response";
        public const string Intent_Ignore = "Ignore";
        public const string Intent_ChangeLanguage = "ChangeLanguage";
        public const string Intent_Delete_and_Restart = "Delete_and_Restart";
        public const string Intent_Arabic = "Arabic";

        /*
         *Default intent
         *If no intent is detected, the user is asked to clarify
         */
        [LuisIntent(Intent_None)]
        public async Task None(IDialogContext context, LuisResult result)
        {
            Debug.WriteLine("None intent");
            await AskForClarification(context);
            //string language = ConversationStarter.user.PreferedLang;
            //string arabicText = ConversationStarter.user.arabicText;
            //if (arabicText == StringResources.ar_Commands)
            //{
            //    await context.Forward(new CommandReminder(), ResumeAfterKill, context.Activity, CancellationToken.None);
            //}
            //else
            //{
            //    string entity;
            //    if (ExtractEntity(result) != null) {
            //        entity = ExtractEntity(result);
            //    }
            //    else
            //    {
            //        if (result.AlteredQuery != null)
            //        {
            //            entity = result.AlteredQuery;
            //        }
            //        else
            //        {
            //            entity = result.Query;
            //        }
            //    }
            //    if (entity.Length <= 3)
            //    {
            //        await AskForClarification(context);
            //    }
            //    else
            //    {
            //        List<string> sub_topics = SaveConversationData.GetAllUniqueSubTopics(entity, language).Result;
            //        List<CourseList> courses_from_direct_sub = SaveConversationData.MatchCourseBySubTopic(entity, StringResources.en);
            //        List<CourseList> courses_by_name = SaveConversationData.GetCoursesBySimilarName(entity);
            //        if (sub_topics.Count > 0 || courses_from_direct_sub.Count > 0 || courses_by_name.Count > 0)
            //        {
            //            await Learning(context, result);
            //        }
            //        else
            //        {
            //            await AskForClarification(context);
            //        }
            //    }
            //}
        }

        /* If the intent is determined to be gibberish, the user is asked to clarify */
        [LuisIntent(Intent_Gibberish)]
        public async Task Gibberish(IDialogContext context, LuisResult result)
        {
            await AskForClarification(context);
        }

        /* Input is ignored by LUIS */
        [LuisIntent(Intent_Ignore)]
        public async Task Ignore(IDialogContext context, LuisResult result)
        {
            await AskForClarification(context);
        }

        /* Asks what the user would like to do with the inputed term
         * This method currently only suggests finding a course, but as funtionality returns we can suggest more options */
        [LuisIntent(Intent_Clarification)]
        public async Task Unclear(IDialogContext context, LuisResult result)
        {
            language = ConversationStarter.user.PreferedLang;
            Activity response = ((Activity)context.Activity).CreateReply(StringResources.ResourceManager.GetString($"{language}_Clarify_input").Replace("{0}", result.Query));
            response.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                    {
                        new CardAction(){ Title = StringResources.ResourceManager.GetString($"{language}_AvailableCourses"), Type=ActionTypes.ImBack, Value=StringResources.ResourceManager.GetString($"{language}_Courses") }
                    }
            };
            await context.PostAsync(await MessagesController.userConversations[context.Activity.Conversation.Id].BotMessage(response));
            context.Wait(WhatToDo);
        }

        /* Intent to suggest learning content */
        [LuisIntent(Intent_Suggestions)]
        public async Task suggestion(IDialogContext context, LuisResult result)
        {
            await context.Forward(new RecommendDialog(), SendToRoot, context.Activity, CancellationToken.None);
        }

        /* User is asking how Hakeem is. Calls HowAreYou() */
        [LuisIntent(Intent_How)]
        public async Task How_are_you(IDialogContext context, LuisResult result)
        {
            await context.Forward(new HowAreYou(), SendToRoot, context.Activity, CancellationToken.None);
        }

        /* User has requested to delete their data */
        [LuisIntent(Intent_Delete_and_Restart)]
        public async Task Delete_and_Restart(IDialogContext context, LuisResult result)
        {
            List<CardAction> childSuggestions = new List<CardAction>();
            var reply = context.MakeMessage();
            string language = ConversationStarter.user.PreferedLang;
            string Yes = StringResources.ResourceManager.GetString($"{language}_Yes");
            string No = StringResources.ResourceManager.GetString($"{language}_No");
            reply.Text = StringResources.ResourceManager.GetString($"{language}_DeleteQuery");
            childSuggestions.Add(new CardAction() { Title = Yes, Type = ActionTypes.ImBack, Value = Yes });
            childSuggestions.Add(new CardAction() { Title = No, Type = ActionTypes.ImBack, Value = No });
            await context.PostAsync(reply);
            context.Wait(ConfirmDelete);
            
        }

        public async Task ConfirmDelete(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = await result as Activity;
            language = ConversationStarter.user.PreferedLang;
            if (activity.Text.ToLower() == StringResources.en_Yes.ToLower() || activity.Text == StringResources.ar_Yes)
            {
                //await SaveConversationData.DeleteConvoData(activity.Conversation.Id);
                await SaveConversationData.DeleteUserData(ConversationStarter.user._id);
                await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_DataDeleted"));
                await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_NewConvo"));
                await context.Forward(new ConversationStarter(), SendToRoot, context.Activity, CancellationToken.None);
            }
            else
            {
                await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_DataKept"));
                await context.Forward(new LearningDialog(), SendToRoot, context.Activity, CancellationToken.None);
            }
        }

        //Somehow Arabic text gets sent to Luis so just redirect to Clarification (for now at least - need to figure out how this is happening
        [LuisIntent(Intent_Arabic)]
        public async Task Arabic(IDialogContext context, LuisResult result)
        {
            await AskForClarification(context);
        }

        /* User wishes to change the language that they speak to Hakeem in */
        [LuisIntent(Intent_ChangeLanguage)]
        public async Task ChangeLanguage(IDialogContext context, LuisResult result)
        {
            Debug.WriteLine("Change");
            var cosmosID = ConversationStarter.user._id;
            string current_lang = ConversationStarter.user.PreferedLang.ToLower(); 
            string entity = ExtractEntity(result);
            entity = entity.Trim();
            if (entity.ToLower() == StringResources.en_Arabic.ToLower())
            {
                await context.PostAsync(StringResources.ar_SpeakArabic);
                ConversationStarter.user.PreferedLang = StringResources.ar;
                await SaveConversationData.UpdateInputLanguage(cosmosID, StringResources.ar);
                await context.Forward(new LearningDialog(), ResumeAfterKill, context.Activity, CancellationToken.None);
            }
            else if (entity.ToLower() == StringResources.en_English.ToLower())
            {
                await context.PostAsync(StringResources.en_SpeakEnglish);
                ConversationStarter.user.PreferedLang = StringResources.en;
                await SaveConversationData.UpdateInputLanguage(cosmosID, StringResources.en);
                await context.Forward(new LearningDialog(), ResumeAfterKill, context.Activity, CancellationToken.None);
            }
            else if (current_lang.ToLower() == StringResources.en_English.ToLower())
            {
                await context.PostAsync(StringResources.ar_SpeakArabic);
                ConversationStarter.user.PreferedLang = StringResources.ar;
                await SaveConversationData.UpdateInputLanguage(cosmosID, StringResources.ar);
                await context.Forward(new LearningDialog(), ResumeAfterKill, context.Activity, CancellationToken.None);
            }
            else if (current_lang.ToLower() == StringResources.en_Arabic.ToLower())
            {
                await context.PostAsync(StringResources.en_SpeakEnglish);
                ConversationStarter.user.PreferedLang = StringResources.en;
                await SaveConversationData.UpdateInputLanguage(cosmosID, StringResources.en);
                await context.Forward(new LearningDialog(), ResumeAfterKill, context.Activity, CancellationToken.None);
            }
        }
        
        /* User wishes to cease communicating with Hakeem */
        [LuisIntent(Intent_End)]
        public async Task end_conversation(IDialogContext context, LuisResult result)
        {
            string language = ConversationStarter.user.PreferedLang.ToLower();
            await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_Goodbye"));
            //await SaveConversationData.EndConversation(context.Activity.Conversation.Id);
            context.Done(true);
        }

        public async Task Topics(IDialogContext context, LuisResult result)
        {
            await context.Forward(new LearningDialog(), ResumeAfterKill, context.Activity, CancellationToken.None);
        }

        /* The user wishes to now what commands they can use with Hakeem. Calls CommandReminder() */
        [LuisIntent(Intent_Command)]
        public async Task Command(IDialogContext context, LuisResult result)
        {
            await context.Forward(new CommandReminder(), ResumeAfterKill, context.Activity, CancellationToken.None);
        }

        /* The user wishes to save their personal information/preferences. Calls DisplayChangePreferences() */
        [LuisIntent(Intent_Preferences)]
        public async Task Preferences(IDialogContext context, LuisResult result)
        {
            await context.Forward(new DisplayChangePreferences(), SendToRoot, context.Activity, CancellationToken.None);
        }

        /* The user wishes to restart the conversation with Hakeem. Calls CommandDialog() which begins the dialog from the beginning */

        [LuisIntent(Intent_Restart)]
        public async Task Restart(IDialogContext context, LuisResult result)
        {
            await context.Forward(new LearningDialog(), ResumeAfterKill, context.Activity, CancellationToken.None);
        }

        /*responds to answer for clarify from the None intent
        At the moment only offers the option to search for related courses, but opens the door for more functionality
        if anything else is entered other than courses, it sends it to the root dialog*/
        private async Task WhatToDo(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var res = await result;
            string input = res.Text.ToLower();
            switch (input)
            {
                case "courses":
                    await context.Forward(new LearningDialog(), ResumeAfterKill, context.Activity, CancellationToken.None);
                    break;
                default:
                    await context.Forward(new LearningDialog(), ResumeAfterKill, context.Activity, CancellationToken.None);
                    break;
            }
        }

        /* Response to any form of greeting from the user */
        [LuisIntent(Intent_Greeting)]
        public async Task Greeting(IDialogContext context, LuisResult result)
        {
            //Activity activity = new Activity();
            //await context.Forward(new TestInputDialog(), ResumeAfterKill, activity, CancellationToken.None);
            string language = ConversationStarter.user.PreferedLang;
            string userName = ConversationStarter.user.Name;
            if (language == "")
            {
                await context.Forward(new ConversationStarter(), ResumeAfterKill, context.Activity, CancellationToken.None);
            }
            else
            {
                if (userName != "")
                {
                    await context.PostAsync(await MessagesController.userConversations[context.Activity.Conversation.Id].BotMessage(((Activity)context.Activity).CreateReply(StringResources.ResourceManager.GetString($"{language}_Greeting_1"))));
                    await context.Forward(new LearningDialog(), ResumeAfterKill, context.Activity, CancellationToken.None);
                }
                else
                {
                    await context.Forward(new ConversationStarter(), ResumeAfterKill, context.Activity, CancellationToken.None);
                }
            }
        }

        /* Responds to any request for learning courses
         * Passes context to the CallLearning Dialog if the content is specified
         * If the content is not specified, clarification is asked for */
        [LuisIntent(Intent_learning)]
        public async Task Learning(IDialogContext context, LuisResult result)
        {
            string entity = "";
            string language = ConversationStarter.user.PreferedLang;
            string gender = ConversationStarter.user.gender;
            entity = ExtractLearningEntity(result).ToLower();
            string engEntity = entity;
            if (entity != "" && entity != null)
            {
                entity = entity;
            }
            else
            {
                if (result.AlteredQuery != null)
                {
                    entity = result.AlteredQuery;
                }
                else
                {
                    entity = result.Query;
                }
            }

            var response = context.MakeMessage();
            List<CourseList> courses_from_direct_sub = SaveConversationData.MatchCourseBySubTopic(entity, StringResources.en);
            List<CourseList> courses_by_name = SaveConversationData.GetCoursesBySimilarName(entity);
            List<string> sub_topics = SaveConversationData.GetAllUniqueSubTopics(entity, language).Result;
            Debug.WriteLine("hey");
            entity = language == StringResources.ar ? SaveConversationData.GetArabicSubTopic(entity) : entity.ToLower();
            Debug.WriteLine(engEntity + " = "+ entity);
            if (sub_topics.Count > 0)
            {
                ConversationStarter.user.currentTopic = entity;
                List<CardAction> childSuggestions = new List<CardAction>();
                foreach (string topic in sub_topics)
                {
                    string title = topic;
                    childSuggestions.Add(new CardAction() { Title = title, Type = ActionTypes.ImBack, Value = title });
                }
                response.SuggestedActions = new SuggestedActions() { Actions = childSuggestions };
                if (language == StringResources.ar)
                {
                    switch (gender)
                    {
                        case "female":
                            response.Text = String.Format(StringResources.ar_ShowSubTopicsFemale, entity);
                            break;
                        case "male":
                            response.Text = String.Format(StringResources.ar_ShowSubTopicsMale, entity);
                            break;
                        default:
                            response.Text = String.Format(StringResources.ar_ShowSubTopics, entity);
                            break;
                    }
                }
                else
                {
                    response.Text = String.Format(StringResources.en_ShowSubTopics, Grammar.Capitalise(entity));
                }
                await context.PostAsync(response);
                ConversationStarter.user.messageStack = new Stack<string>();
                context.Wait(LearningDialog.FindSubTopic);
            }
            else if (courses_from_direct_sub.Count > 0 || courses_by_name.Count > 0)
            {
                List<CourseList> courses;
                if (courses_from_direct_sub.Count != 0)
                {
                    courses = courses_from_direct_sub;
                    var topic = SaveConversationData.GetTopicBySubtopic(entity, language);
                    Debug.WriteLine(topic);
                    ConversationStarter.user.currentTopic = topic;
                    ConversationStarter.user.currentSubTopic = entity.ToLower();
                }
                else
                {
                    courses = courses_by_name;
                    var subtopic = SaveConversationData.GetSubtopicByCourse(entity, language);
                    var topic = SaveConversationData.GetTopicBySubtopic(subtopic, language);
                    ConversationStarter.user.currentTopic = topic;
                    ConversationStarter.user.currentSubTopic = subtopic;
                }
                List<dynamic> temp = LearningDialog.FilterByPreferences(context, language, courses);
                string removed = "";
                if (temp[0].Count < courses.Count)
                {
                    courses = temp[0];
                }
                if (temp[1].Count > 0)
                {
                    foreach (string preference in temp[1])
                    {
                        removed += "\n\n \u2022" + preference;
                    }
                }
                Debug.WriteLine("Got Here");
                await LearningDialog.DisplayCourse(context, language, courses, removed);
                context.Wait(LearningDialog.FinalCourse);
            }
            else
            {
                await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_NoCourseFound"));
                await context.Forward(new LearningDialog(), ResumeAfterKill, context.Activity, CancellationToken.None);
            }
        }

        public async Task ResumeAfterLearning(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = await result as Activity;
            await context.Forward(new LearningDialog(), ResumeAfterKill, activity, CancellationToken.None);
        }

        /* Called if the user asks for help and asks what they want to learn about */
        //[LuisIntent(Intent_Help)]
        public async Task Help(IDialogContext context, LuisResult result)
        {
            await AskForClarification(context);
        }

        /* Method called when there is no entity in learning, or when user enters 'help' */
        public async Task AskForClarification(IDialogContext context)
        {
            string language = ConversationStarter.user.PreferedLang;
            await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_DidntUnderstand"));
            await context.Forward(new LearningDialog(), ResumeAfterKill, context.Activity, CancellationToken.None);
        }

        /* If the user wants to learn but doesnt specify the content, this method deals with the response to 'what would you like to learn?'
         * Passes the context to the calllearning dialog once the content is established */
        private async Task AfterClarification(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var res = await result;
            string input = res.Text;
            await context.Forward(new LearningDialog(), ResumeAfterKill, context.Activity, CancellationToken.None);
        }

        /* The QnA is used to answer questions from the user. RootDialog() is called once the answer has been provided */
        [LuisIntent(Intent_Question)]
        public async Task Question(IDialogContext context, LuisResult result)
        {
            string language = ConversationStarter.user.PreferedLang;
            string query = "";
            if (result.AlteredQuery != null)
            {
                query = result.AlteredQuery;
            }
            else
            {
                query = result.Query;
            }
            QnaDialog qna = new QnaDialog(query, context);
            var answer = await qna.GetAnswer(query);

            if (language == StringResources.ar)
            {
                answer = await Translate.Translator(answer, StringResources.ar);
            }
            await context.PostAsync(answer);
            await context.Forward(new LearningDialog(), ResumeAfterKill, context.Activity, CancellationToken.None);
        }

        /* Used to continue the conversation after the QnA has been used */
        public async Task resumeAfterQnA(IDialogContext context, IAwaitable<object> result)
        {
            await context.Forward(new LearningDialog(), ResumeAfterKill, context.Activity, CancellationToken.None);
        }

        /* After any context is passed on and finished, this kills the stack */
        private async Task ResumeAfterKill(IDialogContext context, IAwaitable<object> result)
        {
            context.Done(true);
        }

        private async Task ResumeAfterLuisInput(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = await result as Activity;
        }

        /* Pass context to RootDialog() */
        private async Task SendToRoot(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = await result as Activity;
            await context.Forward(new LearningDialog(), ResumeAfterKill, context.Activity, CancellationToken.None);
        }

        /* Returns a string of the highest matched entity */
        public string ExtractEntity(LuisResult result)
        {
            Debug.WriteLine(result.ToJson());
            var Entity = "";
            if (result.Entities.Count > 0)
            {
                Entity = result.Entities[0].Entity;
                return Entity;
            }
            else
            {
                return Entity;
            }
        }

        public string ExtractLearningEntity(LuisResult result)
        {
            Debug.WriteLine(result.ToJson());
            var Entity = "";
            if (result.Entities.Count > 0)
            {
                try
                {
                    Boolean hasRes = result.Entities[0].GetType().GetProperty("Resolution") != null;
                    if (hasRes)
                    {
                        Entity = Regex.Replace(result.Entities[0].Resolution.Values.ToJson(), "[^A-Za-z0-9 _]", "");
                    }
                    else
                    {
                        Entity = result.Entities[0].Entity;
                    }
                    return Entity;
                }
                catch (NullReferenceException error)
                {
                    Debug.WriteLine("NullReferenceException " + error.ToString());
                }
                catch (ArgumentOutOfRangeException error)
                {
                    Debug.WriteLine("ArgumentOutOfRangeException " + error.ToString());
                }
                return null;
            }
            else
            {
                return Entity;
            }
        }

        /* Returns a string of the highest matched entity */
        public string IntentRecognition(LuisResult result)
        {
            IList<IntentRecommendation> listOfIntentsFound = result.Intents;
            StringBuilder IntentResults = new StringBuilder();
            foreach (IntentRecommendation item in listOfIntentsFound)
            {
                IntentResults.Append(item.Intent);
            }
            return IntentResults.ToString();
        }
    }
}

public class Entity
{
    public Entity(string type, float score)
    {
        entityType = type;
        entityScore = score;
    }
    public string entityType { get; set; }
    public float entityScore { get; set; }
}

public class SentimentAnalysis
{
    public SentimentAnalysis(string sentimentType, float score)
    {
        sentiment = sentimentType;
        sentimentScore = score;
    }
    public string sentiment { get; set; }
    public float sentimentScore { get; set; }
}

public class QueryResponse
{
    public QueryResponse(string json)
    {
        JObject jobject = JObject.Parse(json);
        query = jobject["query"].ToString();
        TopScoreIntent = new Entity(jobject["topScoringIntent"]["intent"].ToString(), jobject["topScoringIntent"]["score"].ToObject<float>());
        entities = jobject["entities"].ToObject<List<string>>();
        var sentiment_an = jobject["sentimentAnalysis"];
        sentimentAnalysis = new SentimentAnalysis(sentiment_an["label"].ToString(), sentiment_an["score"].ToObject<float>());
    }
    public string query { get; set; }
    public Entity TopScoreIntent { get; set; }
    public List<string> entities { get; set; }
    public SentimentAnalysis sentimentAnalysis { get; set; }
}
