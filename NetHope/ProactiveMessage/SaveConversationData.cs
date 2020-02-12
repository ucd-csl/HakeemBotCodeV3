using Microsoft.Bot.Connector;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Authentication;
using System.Diagnostics;
using Microsoft.Bot.Builder.Dialogs;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System.Configuration;
using NetHope.Resources;

namespace NetHope.ProactiveMessage
{
    public class SaveConversationData
    {
        //private static readonly IMongoCollection<ConversationObject> UserCollection = GetReferenceToCollection<ConversationObject>(ConfigurationManager.AppSettings["ConversationCollection"]);
        private static readonly IMongoCollection<UserDataCollection> UserDataCollection = GetReferenceToCollection<UserDataCollection>(ConfigurationManager.AppSettings["UserCollection"]);
        private static readonly IMongoCollection<CourseList> CoursesCollection = GetReferenceToCollection<CourseList>(ConfigurationManager.AppSettings["CourseCollection"]);

        //Get language stored in conversation object using conversation id, if null default to English
        internal static string GetLanguage(string userId)
        {
            string result = UserDataCollection.Find(x => x.User_id == userId).FirstOrDefault().PreferedLang;
            return result;
        }

        //API needs language code for speech to text, obtained from conversation, gets from Conversation obj using ID
        internal static string GetLanguageForAPI(string userId)
        {
            switch (GetLanguage(userId).ToLower())
            {
                case "english":
                    return StringResources.en_US;
                case "arabic":
                    return StringResources.ar_EG;
                case "en":
                    return StringResources.en_US;
                case "ar":
                    return StringResources.ar_EG;
                default:
                    return StringResources.en_US;
            }
        }

        internal static string GetLanguageForString(string userId)
        {
            switch (GetLanguage(userId).ToLower())
            {
                case "en":
                    return StringResources.en_English;
                case "ar":
                    return StringResources.ar_Arabic;
                case "english":
                    return StringResources.en_English;
                case "arabic":
                    return StringResources.ar_Arabic;
                default:
                    return StringResources.en_English;
            }
        }

        internal async static Task<UserDataCollection> GetUserDataCollection(string user_id)
        {
            UserDataCollection user = UserDataCollection.Find(x => x.User_id == user_id).FirstAsync().Result;
            return user;
        }

        //internal async static Task<ConversationObject> GetConversationObject(string conversationId)
        //{
        //    try
        //    {
        //        return await UserCollection.Find(_ => _.ConversationId == conversationId).SingleAsync();
        //    }
        //    catch (Exception)
        //    {
        //        return null;
        //    }
        //}

        internal static async Task UpdateInputLanguage(BsonObjectId id, string language)
        {
            Debug.WriteLine(language);
            var update = Builders<UserDataCollection>.Update.Set("PreferedLang", language);
            UserDataCollection.FindOneAndUpdate(x => x._id == id, update);
        }

        /* Called by root dialog to save new conversation IDs*/
        //internal static async Task<bool> SaveBasicConversationAsync(string fromId, string serviceUrl, string channelId, string conversationId, bool status)
        //{
        //    var exists = UserCollection.AsQueryable().FirstOrDefault(x => x.ConversationId == conversationId) != null;

        //    if (!exists)
        //    {

        //        await UserCollection.InsertOneAsync(
        //            new ConversationObject
        //            {
        //                FromId = fromId,
        //                ServiceUrl = serviceUrl,
        //                ConversationId = conversationId,
        //                Date = DateTime.Now,
        //                Status = status,
        //                FromName = "",
        //                ToId = "",
        //                ToName = "",
        //                ChannelId = "",
        //                Language = "",

        //            });

        //        return true;
        //    }
        //    else // updating the time of already existing attribute
        //    {
        //        var filter = Builders<ConversationObject>.Filter.Eq(s => s.ConversationId, conversationId);
        //        ConversationObject results = UserCollection.Find(filter).FirstOrDefault();
        //        DateTime previousDate = results.Date;
        //        results.Date = DateTime.Now;
                
        //        var result = await UserCollection.ReplaceOneAsync(filter, results);
        //    }
        //    return false;
        //}

        //internal static async Task<bool> SaveConversationAsync(string fromName, string toId, string toName, string serviceUrl, string channelId, string conversationId)
        //{
        //    if (channelId != StringResources.directline) // updating the time of already existing attribute
        //    {
        //        var filter = Builders<ConversationObject>.Filter.Eq(s => s.ConversationId, conversationId);
        //        ConversationObject results = UserCollection.Find(filter).FirstOrDefault();
        //        results.FromName = fromName;
        //        results.ToId = toId;
        //        results.ToName = toName;
        //        results.ChannelId = channelId;
        //        var x = await UserCollection.ReplaceOneAsync(filter, results);
        //        return true;
        //    }
        //    return false;
        //}
        
        internal static async Task SaveCookieAsync(string userId, ConversationReference cookie)
        {
            var filter = Builders<UserDataCollection>.Filter.Eq(s => s.User_id, userId);
            UserDataCollection results = UserDataCollection.Find(filter).FirstOrDefault();
            results.conversationReference = cookie;
            await UserDataCollection.ReplaceOneAsync(filter, results);
        }

        internal static async Task<ConversationReference> GetReferenceAsync(BsonObjectId userId)
        {
            var filter = Builders<UserDataCollection>.Filter.Eq(s => s._id, userId);
            UserDataCollection results = UserDataCollection.Find(filter).FirstOrDefault();
            return results.conversationReference;
        }

        //internal async static Task EndConversation(string convoId) //sets conversation as finished in DB
        //{
        //    var update = Builders<ConversationObject>.Update.Set("Status", false);
        //    await UserCollection.FindOneAndUpdateAsync(x => x.ConversationId == convoId, update);
        //}

        //internal async static Task RestartConversation(string convoId)
        //{
        //    var update = Builders<ConversationObject>.Update.Set("Status", true);
        //    await UserCollection.FindOneAndUpdateAsync(x => x.ConversationId == convoId, update);
        //}

        //internal static bool CheckConvoStatus(string convoId)
        //{
        //    return UserCollection.Find(x => x.ConversationId == convoId).FirstOrDefault().Status;
        //}

        //internal static DateTime GetLastMessage(string conversationId)
        //{
        //    var filter = Builders<ConversationObject>.Filter.Eq(s => s.ConversationId, conversationId);
        //    ConversationObject results = UserCollection.Find(filter).FirstOrDefault();
        //    return results.Date != null ? results.Date : DateTime.Now.ToUniversalTime();
        //}

        /* Keep track of the time since the last message the user sent*/
        //internal async static Task UpdateConvTime(string conversationId)
        //{
        //    var filter = Builders<ConversationObject>.Filter.Eq(s => s.ConversationId, conversationId);
        //    ConversationObject results = UserCollection.Find(filter).FirstOrDefault();
        //    DateTime previousDate = results.Date;
        //    results.Date = DateTime.Now.ToUniversalTime();
        //    var result = await UserCollection.ReplaceOneAsync(filter, results);
        //}

        internal static async Task UpdateLastActive(string userId, DateTime date)
        {
            var filter = Builders<UserDataCollection>.Filter.Eq(s => s.User_id, userId);
            UserDataCollection result = UserDataCollection.Find(filter).FirstOrDefault();
            result.lastActive = date;
            await UserDataCollection.ReplaceOneAsync(filter, result);
        }

        //internal static bool CheckConvoExist(string conversationId)
        //{
        //    return UserCollection.Find(x => x.ConversationId == conversationId).FirstOrDefault() == null;
        //}

        /* Method that takes the name of a collection as a parameter and returns a reference to that collection */
        public static IMongoCollection<T> GetReferenceToCollection<T>(string collectionName)
        {
            String connectionString = @"" + ConfigurationManager.AppSettings["CosmosMainEndpoint"] + StringResources.cosmosEndpointExtra;
            MongoClientSettings settings = MongoClientSettings.FromUrl(
              new MongoUrl(connectionString)
            );
            settings.SslSettings =
              new SslSettings() { EnabledSslProtocols = SslProtocols.Tls12 };
            var mongoClient = new MongoClient(settings);
            IMongoDatabase _database = mongoClient.GetDatabase(ConfigurationManager.AppSettings["CosmosMainDB"]);
            return _database.GetCollection<T>(collectionName);
        }

        public static async Task DeleteUserData(BsonObjectId id)
        {
            /* Delete the current user's data from the database */

            UserDataCollection.FindOneAndDelete(x => x._id == id);
        }

        //public static async Task DeleteConvoData(string conversation_id)
        //{
        //    /* Delete data about the current conversation from the database */
        //    UserCollection.FindOneAndDelete(x => x.ConversationId == conversation_id);
        //}
        
        public static async Task SaveUserCourse(BsonObjectId user_id, UserCourse course)
        {
            UserDataCollection user = UserDataCollection.Find(x => x._id == user_id).FirstOrDefault();
            List<UserCourse> courses = user.PastCourses;
            courses.Add(course);
            var update = Builders<UserDataCollection>.Update.Set("PastCourses", courses);
            UserDataCollection.FindOneAndUpdate(x => x._id == user_id, update);
        }

        public static string CourseToSubject(string course_name)
        {
            CourseList course = CoursesCollection.Find(x => x.courseName == course_name).FirstOrDefault();
            if (course != null)
            {
                return course.subTopic;
            }
            else
            {
                return "";
            }
        }

        public static List<CourseList> MatchCourseByTopic(string topic)
        {
            return CoursesCollection.Find(x => x.topic.ToLower() == topic.ToLower() || x.topicArabic == topic).ToList();
        }

        public static List<CourseList> MatchCourseBySubTopic(string topic, string language)
        {
            List<CourseList> courses = new List<CourseList>();
            if (language == StringResources.ar)
            {
                courses = CoursesCollection.Find(x => x.subTopicArabic == topic).ToList();
            }
            else
            {
                courses = CoursesCollection.Find(x => x.subTopic.ToLower() == topic).ToList();
            }
            return courses;
        }

        public static List<dynamic> GetUniqueTopics()
        {
            var filter = new BsonDocument();
            List<dynamic> result = CoursesCollection.Distinct<dynamic>("topic", filter).ToList();
            return result;
        }

        public static List<dynamic> GetUniqueTopicsArabic()
        {
            var filter = new BsonDocument();
            List<dynamic> result = CoursesCollection.Distinct<dynamic>("topicArabic", filter).ToList();
            return result;
        }

        public static List<dynamic> GetAllSubTopics()
        {
            var filter = new BsonDocument();
            List<dynamic> result = CoursesCollection.Distinct<dynamic>("subTopic", filter).ToList();
            return result;
        }

        public static List<dynamic> GetAllSubTopicsArabic()
        {
            var filter = new BsonDocument();
            List<dynamic> result = CoursesCollection.Distinct<dynamic>("subTopicArabic", filter).ToList();
            return result;
        }

        public static string GetTopicBySubtopic(string subtopic, string language)
        {
            string result = "";
            Debug.WriteLine("Subtopic: " + subtopic);
            if (language == StringResources.ar)
            {
                result = CoursesCollection.Find(x => x.subTopicArabic == subtopic).FirstOrDefault().topicArabic;
            }
            else
            {
                result = CoursesCollection.Find(x => x.subTopic.ToLower() == subtopic.ToLower()).FirstOrDefault().topic;
            }
            Debug.WriteLine(result);
            return result;
        }

        public static string GetSubtopicByCourse(string name, string language)
        {
            string result;
            if (language == StringResources.ar)
            {
                result = CoursesCollection.Find(x => x.courseNameArabic.Contains(name)).FirstOrDefault().subTopicArabic;
            }
            else
            {
                result = CoursesCollection.Find(x => x.courseName.ToLower().Contains(name.ToLower())).FirstOrDefault().subTopic;
            }
            return result;
        }
        
        public static List<dynamic> GetUniqueSubTopics(string topic)
        {
            FilterDefinition<CourseList> filter = new BsonDocument("topic", SupportClasses.Grammar.Capitalise(topic).TrimStart().TrimEnd());
            List<dynamic> result = CoursesCollection.Distinct<dynamic>("subTopic", filter).ToList();
            return result;
        }

        public static List<dynamic> GetUniqueSubTopicsArabic(string topic)
        {
            FilterDefinition<CourseList> filter = new BsonDocument("topicArabic", topic);
            List<dynamic> result = CoursesCollection.Distinct<dynamic>("subTopicArabic", filter).ToList();
            return result;
        }

        internal async static Task<List<string>> GetAllUniqueSubTopics(string topic, string language)
        {
            // there is a functionality to get all distinct values of a field from a query but I couldn't get it to work
            var result = CoursesCollection.Find(y => y.topic.ToLower() == topic.ToLower() || y.topicArabic == topic).ToList();
            HashSet<string> distinct = new HashSet<string>();
            for (int i = 0; i < result.Count; i++)
            {
                string subTopic = language == StringResources.ar ? result[i].subTopicArabic : result[i].subTopic;
                distinct.Add(subTopic);
            }
            return distinct.ToList();
        }

        public static List<CourseList> TryMatchCourse(string sub_topic, string accreditation, string self_paced, bool financial_aid, string language, string level, int restrictive)
        {
            // capitalise first letter
            language = language.First().ToString().ToUpper() + language.ToLower().Substring(1);

            bool accreditation_new = accreditation == StringResources.en_Accredited ? true : false;
            string education = "";
            if (level == StringResources.en_LateHighSchool || level == StringResources.en_EarlyHighSchool)
            {
                education = StringResources.en_Intermediate;
            }
            else if (level == StringResources.en)
            {
                education = StringResources.en_Advanced;
            }
            else
            {
                education = StringResources.en_Beginner;
            }


            bool both_language = language.ToLower() == StringResources.en_Both.ToLower() ? true : false;
            bool both_pace = self_paced.ToLower() == StringResources.en_Both.ToLower() ? true : false;
            bool self_paced_bool = self_paced.ToLower() == StringResources.en_Self_Paced.ToLower() ? true : false;
            bool level_bool = false;

            // restrictive refers to how stringent the search parameters are for the courses
            // restrictive = 0 ==> full restrictions
            // restrictive = 1 ==> ignore the user's education level when searching
            // restrictive = 2 ==> ignore language
            // restrictive = 3 ==> ignore self_paced
            if (restrictive == 1)
            {
                level_bool = true;
            }
            if (restrictive == 2)
            {
                both_language = true;
            }
            if (restrictive == 3)
            {
                both_pace = true;
            }


            return CoursesCollection.Find(x => (x.languageDelivered.Contains(language) || both_language) && (both_pace || x.selfPaced == self_paced_bool) && (x.accreditationOption == accreditation_new) && (x.level == "" || x.level == education || level_bool) && (x.topic.ToLower() == sub_topic.ToLower() || x.subTopic.ToLower() == sub_topic.ToLower())).ToList();


        }

        public static bool CheckUserExists(string user_id)
        {
            /* Check for the user's Skype ID in the database */
            List<UserDataCollection> results = UserDataCollection.Find(x => x.User_id == user_id).ToList();
            if (results.Count >= 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static CourseList GetCourseByName(string name)
        {
            CourseList course = CoursesCollection.Find(x => x.courseName.ToLower() == name.ToLower() || x.courseNameArabic == name).FirstOrDefault();
            return course;
        }

        public static string GetArabicSubTopic(string name)
        {
            Debug.WriteLine(name);
            CourseList subTopic = CoursesCollection.Find(x => x.subTopic.ToLower() == name || x.topic.ToLower() == name).FirstOrDefault();
            Debug.WriteLine(subTopic.subTopic);
            return subTopic.subTopicArabic;
        }

        public static List<CourseList> GetCoursesBySimilarName(string name)
        {
            return CoursesCollection.Find(x => x.courseName.ToLower().Contains(name.ToLower()) || x.courseNameArabic.Contains(name)).ToList();
        }

        public static async Task SaveNewUser(string user_id, string name, string language, ConversationReference reference)
        {
            /* Saves a new user to the database who has accepted the privacy policy
             * Name and Skype ID are saved and the other fields are populated with default values
             * Preferences app is used to update these other values */
            await UserDataCollection.InsertOneAsync(new UserDataCollection
            {
                Name = name,
                Notification = 7,
                PreferedLang = language,
                PreferedSub = new List<string>(),
                PastCourses = new List<UserCourse>(),
                User_id = user_id,
                interests = new List<string>(),
                conversationReference = reference,
                lastNotified = 0,
                lastActive = DateTime.Now.ToUniversalTime(),
                preferencesSet = false,
                firstUsage = true
            }); ;
        }

        public static async Task UpdatePastCourses(BsonObjectId user_id, List<UserCourse> courses)
        {
            var update = Builders<UserDataCollection>.Update.Set("PastCourses", courses);
            UserDataCollection.FindOneAndUpdate(x => x._id == user_id, update);
            Debug.WriteLine("hi");
        }

        public static async Task SaveLanguagePreference(string language, ObjectId id)
        {
            /* Update a user's preferred learning language in the database */

            if (language == StringResources.ar_English)
            {
                language = StringResources.en_English;
            }
            else if (language == StringResources.ar_Arabic)
            {
                language = StringResources.en_Arabic;
            }
            else if (language == StringResources.ar_Both)
            {
                language = StringResources.en_Both;
            }
            var update = Builders<UserDataCollection>.Update.Set("language", language);
            UserDataCollection.FindOneAndUpdate(x => x._id == id, update);
        }

        public static async Task SaveSubjectPreferenceDelete(List<string> subjects, string id)
        {
            /* Remove a user's preferred subject from the database */

            var update = Builders<UserDataCollection>.Update.Set("PreferedSub", subjects);
            await UserDataCollection.FindOneAndUpdateAsync(x => x.User_id == id, update);
        }

        public static async Task SaveNotification(long reminder, ObjectId id)
        {
            /* Update a user's notification frequency in the database */
            var update = Builders<UserDataCollection>.Update.Set("Notification", reminder);
            UserDataCollection.FindOneAndUpdate(x => x._id == id, update);
        }

        public static async Task SaveGenderPreference(string gender, ObjectId id)
        {
            /* Update a user's preferred gender in the database */
            gender = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(gender);
            var update = Builders<UserDataCollection>.Update.Set("gender", gender);
            UserDataCollection.FindOneAndUpdate(x => x._id == id, update);
        }

        public static async Task SaveAccreditationPreference(string accreditation, ObjectId id)
        {
            /* Update a user's preferred accreditation setting in the database */
            accreditation = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(accreditation);
            var update = Builders<UserDataCollection>.Update.Set("accreditation", accreditation);
            UserDataCollection.FindOneAndUpdate(x => x._id == id, update);
        }

        public static async Task SaveDeliveryPreference(string delivery, ObjectId id)
        {
            /* Update a user's preferred accreditation setting in the database */
            delivery = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(delivery);
            var update = Builders<UserDataCollection>.Update.Set("delivery", delivery);
            UserDataCollection.FindOneAndUpdate(x => x._id == id, update);
        }

        public static async Task SaveEducationPreference(string education, ObjectId id)
        {
            /* Update a user's preferred accreditation setting in the database */
            education = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(education);
            var update = Builders<UserDataCollection>.Update.Set("education", education);
            UserDataCollection.FindOneAndUpdate(x => x._id == id, update);
        }

        public static async Task UpdateUserName(string name, ObjectId id)
        {
            /* Update a user's name in the database */
            name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
            var update = Builders<UserDataCollection>.Update.Set("Name", name);
            UserDataCollection.FindOneAndUpdate(x => x._id == id, update);
        }

        public static async Task UpdateFirstUsage(BsonObjectId id)
        {
            var update = Builders<UserDataCollection>.Update.Set("firstUsage", false);
            UserDataCollection.FindOneAndUpdate(x => x._id == id, update);
        }
    }

    [Serializable]
    [BsonIgnoreExtraElements]
    public class CourseList
    {
        //public Int32 __v { get; set; }
        public ObjectId _id { get; set; }
        public string courseName { get; set; }
        public string courseNameArabic { get; set; }
        public string topic { get; set; }
        public string subTopic { get; set; }
        public string topicArabic { get; set; }
        public string subTopicArabic { get; set; }
        public string[] languageDelivered { get; set; }
        public string level { get; set; }
        public bool accreditationOption { get; set; }
        public bool selfPaced { get; set; }
        public bool financialAid { get; set; }
        public long approxDuration { get; set; }
        public string approxDurationArabic { get; set; }
        public Uri url { get; set; }
        public string description { get; set; }
        public string descriptionArabic { get; set; }
        public bool prerequisites { get; set; }
        public string prerequisiteInfo { get; set; }
        public string lastUpdated { get; set; }
        public string courseProvider { get; set; }
        public string courseTrailer { get; set; }
    }

    public class UserCourse
    {
        public string _t { get; set; }
        public string Name { get; set; }
        public string NameArabic { get; set; }
        public DateTime Date { get; set; }
        public Boolean Taken { get; set; }
        public Boolean InProgress { get; set; }
        public Boolean Complete { get; set; }
        public Boolean Queried { get; set; }
        public Int16 Rating { get; set; }
    }

    [Serializable]
    public class MessageCollection
    {
        public ObjectId _id { get; set; }
        public string ConversationId { get; set; }
        public List<string> Message { get; set; }
        public bool Status { get; set; }
    }


    [Serializable]
    public class ConversationObject
    {
        public ObjectId _id { get; set; }
        public string FromId { get; set; }
        public string FromName { get; set; }
        public string ToId { get; set; }
        public string ToName { get; set; }
        public string ServiceUrl { get; set; }
        public string ChannelId { get; set; }
        public string ConversationId { get; set; }
        public DateTime Date { get; set; }
        public string Language { get; set; }
        public bool Status { get; set; }
    }

    [Serializable]
    [BsonIgnoreExtraElements]
    public class UserDataCollection
    {
        [BsonId]
        public ObjectId _id { get; set; }
        public string Name { get; set; }
        public string PreferedLang { get; set; }
        public string language { get; set; }
        public List<string> PreferedSub { get; set; }
        public int Notification { get; set; }
        public List<UserCourse> PastCourses { get; set; }
        public string User_id { get; set; }
        public List<string> interests { get; set; }
        public string gender { get; set; }
        public string accreditation { get; set; }
        public string delivery { get; set; }
        public string education { get; set; }
        public int privacy_policy_version { get; set; }
        public ConversationReference conversationReference { get; set; }
        public BsonCookie cookie { get; set; }
        public DateTime lastActive { get; set; }
        public int lastNotified { get; set; }
        public bool preferencesSet { get; set; }
        public string arabicText { get; set; }
        public string currentTopic { get; set; }
        public string currentSubTopic { get; set; }
        public UserCourse currentCourse { get; set; }
        public CourseList chosenCourse { get; set; }
        public Stack<string> messageStack { get; set; }
        public List<UserCourse> courseList { get; set; }
        public bool firstUsage { get; set; }
    }

    [Serializable]
    [BsonIgnoreExtraElements]
    [BsonDiscriminator("Address")]
    public class BsonAddress
    {
        public BsonAddress(string botId, string channel, string userId, string conversationId, string url)
        {
            this.BotId = botId;
            this.ChannelId = channel;
            this.UserId = userId;
            this.ConversationId = conversationId;
            this.ServiceUrl = url;
        }

        public string BotId { get; set; }
        public string ChannelId { get; set; }
        public string UserId { get; set; }
        public string ConversationId { get; set; }
        public string ServiceUrl { get; set; }
    }

    [BsonIgnoreExtraElements]
    [BsonDiscriminator("BsonCookie")]
    public class BsonCookie
    {

        public BsonCookie(ResumptionCookie cookie)
        {
            this.Address = new BsonAddress(cookie.Address.BotId, cookie.Address.ChannelId, cookie.Address.UserId, cookie.Address.ConversationId, cookie.Address.ServiceUrl);
            this.UserName = cookie.UserName;
            this.IsTrustedServiceUrl = cookie.IsTrustedServiceUrl;
            this.IsGroup = cookie.IsGroup;
            this.Locale = cookie.Locale;
        }
        public BsonAddress Address { get; set; }
        public string UserName { get; set; }
        public bool IsTrustedServiceUrl { get; set; }
        public bool IsGroup { get; set; }
        public string Locale { get; set; }
    }
}