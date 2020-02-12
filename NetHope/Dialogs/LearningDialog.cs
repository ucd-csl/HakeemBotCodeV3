using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using NetHope.Controllers;
using NetHope.Dialogs;
using NetHope.Resources;
using NetHope.SupportClasses;
using NetHope.ProactiveMessage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Configuration;

namespace NetHope.Dialogs
{

    [Serializable]
    public class LearningDialog : IDialog<object>
    {
        private static readonly IMongoCollection<UserDataCollection> UserDataCollection = SaveConversationData.GetReferenceToCollection<UserDataCollection>(ConfigurationManager.AppSettings.Get("UserCollection"));

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(CheckCourses);
            return Task.CompletedTask;
        }
        private async Task CheckCourses(IDialogContext context, IAwaitable<object> result)
        {
            /*
             * Function to query the user about courses presented to them in the past
             * If no recent courses to be queried Learning dialog is started by presenting topics button
             */
            Activity activity = await result as Activity;
            UserDataCollection user = context.UserData.GetValue<UserDataCollection>("UserObject");
            user.messageStack = new Stack<string>();
            var response = context.MakeMessage();
            List<UserCourse> courses = user.PastCourses;
            UserCourse current_course = new UserCourse();
            Random rnd = new Random();
            int start = user.PastCourses.Count - 1;
            int random = 0;
            bool found = false;
            while (start >= 0)
            {
                if (found) break;
                random = rnd.Next(0, start);
                current_course = courses[random];
                Debug.WriteLine(current_course.Name);
                courses.RemoveAt(random);
                courses.Add(current_course);
                start--;
                DateTime now = DateTime.UtcNow;
                TimeSpan diff = now - current_course.Date;
                Debug.WriteLine(diff.TotalDays.ToString());
                if (!current_course.InProgress && !current_course.Taken && !current_course.Queried && diff.TotalDays >= 7)
                {
                    found = true;
                    user.currentCourse = current_course;
                }
                else if (current_course.InProgress && current_course.Taken && current_course.Queried && diff.TotalDays >= 7)
                {
                    found = true;
                    user.currentCourse = current_course;
                }
            }
            string language = user.PreferedLang;
            if (found && !current_course.Taken)
            {
                Debug.WriteLine("Found");
                await context.Forward(new CheckCourse(), LetsLearn, activity, CancellationToken.None);
            }
            else if (found && current_course.Taken)
            {
                Debug.WriteLine("ReFound");
                await context.Forward(new ReCheckCourse(), LetsLearn, activity, CancellationToken.None);
            }
            else
            {
                await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_LetsLearn"));
                response.Text = StringResources.ResourceManager.GetString($"{language}_RespondTopics");
                response.SuggestedActions = new SuggestedActions()
                {
                    Actions = new List<CardAction>()
                {
                    new CardAction(){Title=StringResources.ResourceManager.GetString($"{language}_Topics"), Type=ActionTypes.ImBack, Value=StringResources.ResourceManager.GetString($"{language}_Topics")},
                    new CardAction(){Title=StringResources.ResourceManager.GetString($"{language}_StartOver"), Type=ActionTypes.ImBack, Value=StringResources.ResourceManager.GetString($"{language}_StartOver")}
                }
                };
                await context.PostAsync(response);
                context.Wait(FindTopic);
            }
        }
        public static async Task LetsLearn(IDialogContext context, IAwaitable<object> result)
        {
            var response = context.MakeMessage();
            UserDataCollection user = context.UserData.GetValue<UserDataCollection>("UserObject");
            string language = user.PreferedLang;
            await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_LetsLearn"));
            response.Text = StringResources.ResourceManager.GetString($"{language}_RespondTopics");
            response.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                    new CardAction(){Title=StringResources.ResourceManager.GetString($"{language}_Topics"), Type=ActionTypes.ImBack, Value=StringResources.ResourceManager.GetString($"{language}_Topics")},
                    new CardAction(){Title=StringResources.ResourceManager.GetString($"{language}_StartOver"), Type=ActionTypes.ImBack, Value=StringResources.ResourceManager.GetString($"{language}_StartOver")}
                }
            };
            await context.PostAsync(response);
            context.Wait(FindTopic);
        }

        public static async Task FindTopic(IDialogContext context, IAwaitable<object> result)
        {
            /*
             * Function handles user input after learning dialog started
             * If topics is selected - subtopics are displayed
             * If User inputs text other than 'topics' LUIS is called
             */
            Activity activity = await result as Activity;
            var reply = context.MakeMessage();
            await ConversationStarter.CheckLanguage(activity.Text.Trim(), context);
            UserDataCollection user = context.UserData.GetValue<UserDataCollection>("UserObject");
            string language = user.PreferedLang;
            
            List<CardAction> childSuggestions = new List<CardAction>();
            if (activity.Text == StringResources.ar_Topics || activity.Text.ToLower() == StringResources.en_Topics.ToLower())
            {
                await DisplayTopics(context, language);
                context.Wait(FindSubTopic);
            }
            else if (language == StringResources.ar && SaveConversationData.GetUniqueTopicsArabic().Contains(activity.Text.Trim()))
            {
                ConversationStarter.user.currentTopic = activity.Text.Trim();
                await DisplaySubTopics(context, language, SaveConversationData.GetUniqueSubTopicsArabic(activity.Text.Trim()));
                context.Wait(FindCourses);
            }
            else if (language == StringResources.ar && SaveConversationData.GetAllSubTopicsArabic().Contains(activity.Text.Trim()))
            {
                List<CourseList> course_by_sub = SaveConversationData.MatchCourseBySubTopic(activity.Text.Trim(), language);
                var topic = SaveConversationData.GetTopicBySubtopic(activity.Text.Trim(), language);
                ConversationStarter.user.currentTopic = topic;
                ConversationStarter.user.currentSubTopic = activity.Text.Trim();
                List<dynamic> temp = FilterByPreferences(context, language, course_by_sub);
                string removed = "";
                if (temp[0].Count < course_by_sub.Count)
                {
                    course_by_sub = temp[0];
                }
                if (temp[1].Count > 0)
                {
                    foreach (string preference in temp[1])
                    {
                        removed += "\n\n \u2022" + preference;
                    }
                }
                await DisplayCourse(context, language, course_by_sub, removed);
                context.Wait(FinalCourse);
            }
            else if (language == StringResources.en && SaveConversationData.GetUniqueTopics().Contains(activity.Text.Trim().ToLower()))
            {
                ConversationStarter.user.currentTopic = activity.Text.Trim().ToLower();
                await DisplaySubTopics(context, language, SaveConversationData.GetUniqueSubTopics(activity.Text.Trim()));
                context.Wait(FindCourses);
            }
            else if (language == StringResources.en && SaveConversationData.GetAllSubTopics().Contains(activity.Text.Trim().ToLower()))
            {
                List<CourseList> course_by_sub = SaveConversationData.MatchCourseBySubTopic(activity.Text.Trim(), language);
                var topic = SaveConversationData.GetTopicBySubtopic(activity.Text.Trim(), language);
                ConversationStarter.user.currentTopic = topic;
                ConversationStarter.user.currentSubTopic = activity.Text.Trim();
                List<dynamic> temp = FilterByPreferences(context, language, course_by_sub);
                string removed = "";
                if (temp[0].Count < course_by_sub.Count)
                {
                    course_by_sub = temp[0];
                }
                if (temp[1].Count > 0)
                {
                    foreach (string preference in temp[1])
                    {
                        removed += "\n\n \u2022" + preference;
                    }
                }
                await DisplayCourse(context, language, course_by_sub, removed);
                context.Wait(FinalCourse);
            }
            else
            {
                if (language == StringResources.ar)
                {
                    string arabic = activity.Text.Trim();
                    ConversationStarter.user.arabicText = arabic;
                    activity.Text = await Translate.Translator(activity.Text.Trim(), StringResources.en);
                }
                await context.Forward(new LuisDialog(), ResumeAfterLuisDialog, activity, CancellationToken.None);
            }
        }

        public static async Task DisplayTopics(IDialogContext context, string language)
        {
            /*
             * Function to display topics as suggested actions
             */
            var reply = context.MakeMessage();
            UserDataCollection user = context.UserData.GetValue<UserDataCollection>("UserObject");
            string gender = user.gender;
            List<CardAction> childSuggestions = new List<CardAction>();
            if (language == StringResources.ar)
            {
                List<dynamic> unique_topics = SaveConversationData.GetUniqueTopicsArabic();
                switch (gender)
                {
                    case "female":
                        reply.Text = StringResources.ar_ShowTopicsFemale;
                        break;
                    case "male":
                        reply.Text = StringResources.ar_ShowTopicsMale;
                        break;
                    default:
                        reply.Text = StringResources.ar_ShowTopics;
                        break;
                }
                foreach (string topic in unique_topics)
                {
                    childSuggestions.Add(new CardAction() { Title = topic, Type = ActionTypes.ImBack, Value = topic });
                }
            }
            else
            {
                List<dynamic> unique_topics = SaveConversationData.GetUniqueTopics();
                reply.Text = StringResources.en_ShowTopics;
                foreach (string topic in unique_topics)
                {
                    childSuggestions.Add(new CardAction() { Title = topic, Type = ActionTypes.ImBack, Value = topic });
                }
            }
            childSuggestions.Add(new CardAction() { Title = StringResources.ResourceManager.GetString($"{language}_GoBack"), Type = ActionTypes.ImBack, Value = StringResources.ResourceManager.GetString($"{language}_GoBack"), DisplayText = "AS" });

            reply.SuggestedActions = new SuggestedActions() { Actions = childSuggestions };
            await context.PostAsync(reply);
        }

        public static async Task FindSubTopic(IDialogContext context, IAwaitable<object> result)
        {
            /*
             * Function handles user input after learning dialog started
             * If User opts to go back - Learning Dialog is restarted
             * Otherwise subtopics are displayed for chosen topic
             * If no subtopics can be found - LUIS is called
             */
            Activity activity = await result as Activity;
            string choice = activity.Text.Trim();
            await ConversationStarter.CheckLanguage(activity.Text.Trim(), context);
            UserDataCollection user = context.UserData.GetValue<UserDataCollection>("UserObject");
            string language = user.PreferedLang;

            if (choice.ToLower() == StringResources.en_GoBack.ToLower() || choice.ToLower() == StringResources.en_Back || choice == StringResources.ar_GoBack)
            {
                await context.Forward(new LearningDialog(), ResumeAfterLuisDialog, activity, CancellationToken.None);
            }
            else
            {
                List<CardAction> childSuggestions = new List<CardAction>();
                var reply = context.MakeMessage();
                List<dynamic> unique_subtopics;
                if (language == StringResources.ar)
                {
                    unique_subtopics = SaveConversationData.GetUniqueSubTopicsArabic(choice);
                }
                else
                {
                    unique_subtopics = SaveConversationData.GetUniqueSubTopics(choice.ToLower());
                }
                if (unique_subtopics.Count > 0)
                {
                    user.currentTopic = choice;
                    context.UserData.SetValue("UserObject", user);
                    await DisplaySubTopics(context, language, unique_subtopics);
                    context.Wait(FindCourses);
                }
                else
                {
                    if (language == StringResources.ar)
                    {
                        user.arabicText = activity.Text.Trim();
                        activity.Text = await Translate.Translator(activity.Text, StringResources.en);
                    }
                    await context.Forward(new LuisDialog(), ResumeAfterLuisDialog, activity, CancellationToken.None);
                }
            }
        }

        public static async Task DisplaySubTopics(IDialogContext context, string language, List<dynamic> unique_subtopics)
        {
            /*
             * Function to display subtopics as suggested actions
             */
            List<CardAction> childSuggestions = new List<CardAction>();
            UserDataCollection user = context.UserData.GetValue<UserDataCollection>("UserObject");
            string gender = user.gender;
            string goBack = StringResources.ResourceManager.GetString($"{language}_GoBack");
            var reply = context.MakeMessage();
            var chosenTopic = user.currentTopic;
            if (language == StringResources.ar)
            {
                switch (gender)
                {
                    case "female":
                        reply.Text = String.Format(StringResources.ar_ShowSubTopicsFemale, chosenTopic);
                        break;
                    case "male":
                        reply.Text = String.Format(StringResources.ar_ShowSubTopicsMale, chosenTopic);
                        break;
                    default:
                        reply.Text = String.Format(StringResources.ar_ShowSubTopics, chosenTopic);
                        break;
                }
            }
            else
            {
                reply.Text = String.Format(StringResources.en_ShowSubTopics, chosenTopic);
            }
            foreach (string topic in unique_subtopics)
            {
                childSuggestions.Add(new CardAction() { Title = topic, Type = ActionTypes.ImBack, Value = topic });
            }
            childSuggestions.Add(new CardAction() { Title = goBack, Type = ActionTypes.ImBack, Value = goBack });
            reply.SuggestedActions = new SuggestedActions() { Actions = childSuggestions };
            await context.PostAsync(reply);
        }

        public static async Task FindCourses(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = await result as Activity;
            await ConversationStarter.CheckLanguage(activity.Text.Trim(), context);
            UserDataCollection user = context.UserData.GetValue<UserDataCollection>("UserObject");
            string language = user.PreferedLang;
            string choice = activity.Text.ToLower().Trim();
            if (choice.ToLower() == StringResources.en_GoBack.ToLower() || choice.ToLower() == StringResources.en_Back || choice == StringResources.ar_GoBack)
            {
                await DisplayTopics(context, language);
                context.Wait(FindSubTopic);
            }
            else
            {
                List <CourseList> course_by_sub = SaveConversationData.MatchCourseBySubTopic(choice, language);
                if (course_by_sub.Count != 0)
                {
                    user.currentSubTopic = choice;
                    context.UserData.SetValue("UserObject", user);
                    List<dynamic> temp = FilterByPreferences(context, language, course_by_sub);
                    string removed = "";
                    if(temp[0].Count < course_by_sub.Count)
                    {
                        course_by_sub = temp[0];
                    }
                    if (temp[1].Count > 0)
                    {
                        foreach(string preference in temp[1])
                        {
                            removed += "\n\n \u2022" + preference ;
                        }
                    }
                    await DisplayCourse(context, language, course_by_sub, removed);
                    context.Wait(FinalCourse);
                }
                else
                {
                    Debug.WriteLine(choice);
                    if (language == StringResources.ar)
                    {
                        user.arabicText = activity.Text.Trim();
                        context.UserData.SetValue("UserObject", user);
                        activity.Text = await Translate.Translator(activity.Text, StringResources.en);
                    }
                    await context.Forward(new LuisDialog(), ResumeAfterLuisDialog, activity, CancellationToken.None);
                }
            }
        }

        public static async Task DisplayCourse(IDialogContext context, string language, List<CourseList> course_by_sub, string removed)
        {
            List<CardAction> childSuggestions = new List<CardAction>();
            var reply = context.MakeMessage();
            UserDataCollection user = context.UserData.GetValue<UserDataCollection>("UserObject");
            string subject = Grammar.Capitalise(user.currentSubTopic);
            string goBack = StringResources.ResourceManager.GetString($"{language}_GoBack");
            if (removed == "")
            {
                reply.Text = String.Format(StringResources.ResourceManager.GetString($"{language}_ShowCourses"),subject);
            }
            else
            {
                reply.Text = String.Format(StringResources.ResourceManager.GetString($"{language}_ShowRestrictedCourses"),removed, subject);
            }
            foreach (CourseList course in course_by_sub)
            {
                string courseName = language == StringResources.en ? course.courseName : course.courseNameArabic;
                childSuggestions.Add(new CardAction() { Title = courseName, Type = ActionTypes.ImBack, Value = courseName });
            }
            childSuggestions.Add(new CardAction() { Title = goBack, Type = ActionTypes.ImBack, Value = goBack });
            reply.SuggestedActions = new SuggestedActions() { Actions = childSuggestions };
            await context.PostAsync(reply);
        }

        public static List<dynamic> FilterByPreferences(IDialogContext context, string language, List<CourseList> courses)
        {
            var cosmosID = context.UserData.GetValue<UserDataCollection>("UserObject")._id;
            UserDataCollection user = UserDataCollection.Find(x => x._id == cosmosID).FirstOrDefault();
            if(user.education == StringResources.en_LateHighSchool)
            {
                user.education = StringResources.en_Intermediate;
            }
            else if (user.education == StringResources.en_University)
            {
                user.education = StringResources.en_Advanced;
            }
            else
            {
                user.education = StringResources.en_Beginner;
            }
            List<string> removed = new List<string>();
            if(user.delivery != StringResources.en_Either)
            {
                List<CourseList> temp = FilterByPacing(user.delivery, courses);
                if (temp.Count > 0)
                {
                    courses = temp;
                }
                else
                {
                    removed.Add(StringResources.ResourceManager.GetString($"{language}_CourseDelivery"));
                }
            }
            if (user.accreditation != StringResources.en_Either)
            {
                List<CourseList> temp = FilterByAccreditation(user.accreditation, courses);
                if (temp.Count > 0)
                {
                    courses = temp;
                }
                else
                {
                    removed.Add(StringResources.ResourceManager.GetString($"{language}_CourseAccreditation"));
                }
            }
            if (user.language != StringResources.en_Both)
            {
                List<CourseList> temp = FilterByLanguage(user.language, courses);
                if (temp.Count > 0)
                {
                    courses = temp;
                }
                else
                {
                    removed.Add(StringResources.ResourceManager.GetString($"{language}_PreferredLanguage"));
                }
            }
            List<CourseList> education = FilterByEducation(user.education, courses);
            if (education.Count > 0)
            {
                courses = education;
            }
            else
            {
                removed.Add(StringResources.ResourceManager.GetString($"{language}_EducationLevel"));
            }
            List<dynamic> arrays = new List<dynamic>
            {
                { courses },
                { removed }
            };
            return arrays;
        }

        public static List<CourseList> FilterByLanguage(string language, List<CourseList> courses)
        {
            List<CourseList> results = courses.Where(x => x.languageDelivered.Contains(language)).ToList();
            return results;
        }
        public static List<CourseList> FilterByEducation(string education, List<CourseList> courses)
        {
            if (education == StringResources.en_Advanced)
            {
                return courses;
            }
            List<CourseList> results = new List<CourseList>();
            if (education == StringResources.en_Intermediate)
            {
                results = courses.Where(x => x.level.Contains(StringResources.en_Beginner) || x.level.Contains(StringResources.en_Intermediate)).ToList();
            }
            else
            {
                results = courses.Where(x => x.level.Contains(StringResources.en_Beginner)).ToList();
            }
            return results;
        }
        public static List<CourseList> FilterByAccreditation(string accreditation, List<CourseList> courses)
        {
            List<CourseList> results = new List<CourseList>();
            if (accreditation == StringResources.en_Accredited)
            {
                results = courses.Where(x => x.accreditationOption == true).ToList();
            }
            else
            {
                results = courses.Where(x => x.accreditationOption == false).ToList();
            }
            return results;
        }
        public static List<CourseList> FilterByPacing(string pacing, List<CourseList> courses)
        {
            List<CourseList> results = new List<CourseList>();
            if (pacing == StringResources.en_Self_Paced)
            {
                results = courses.Where(x => x.selfPaced == true).ToList();
            }
            else
            {
                results = courses.Where(x => x.selfPaced == false).ToList();
            }
            return results;
        }

        public static async Task FinalCourse(IDialogContext context, IAwaitable<object> result)
        {
            /*
             * Method that takes the chosen course and displays the course info
             * If no course is found with input name - LUIS is called
             */
            Activity activity = await result as Activity;
            string choice;
            await ConversationStarter.CheckLanguage(activity.Text.Trim(), context);
            UserDataCollection user = context.UserData.GetValue<UserDataCollection>("UserObject");
            string language = user.PreferedLang; 
            if (activity.Text.ToLower() == StringResources.en_GoBack.ToLower() || activity.Text.ToLower() == StringResources.en_Back || activity.Text.Trim() == StringResources.ar_GoBack)
            {
                choice = user.currentTopic;
                List<dynamic> unique_subtopics;
                if (language == StringResources.ar)
                {
                    unique_subtopics = SaveConversationData.GetUniqueSubTopicsArabic(choice);
                }
                else
                {
                    unique_subtopics = SaveConversationData.GetUniqueSubTopics(choice);
                }
                await DisplaySubTopics(context, language, unique_subtopics);
                context.Wait(FindCourses);
            }
            else
            {
                CourseList course = SaveConversationData.GetCourseByName(activity.Text.Trim());
                if (course != null)
                {
                    await PresentCourses(context, course);
                    context.Wait(AwaitCourseChoice);
                }
                else
                {
                    if (language == StringResources.ar)
                    {
                        user.arabicText = activity.Text;
                        context.UserData.SetValue("UserObject", user);
                        activity.Text = await Translate.Translator(activity.Text, StringResources.en);
                    }
                    await context.Forward(new LuisDialog(), ResumeAfterLuisDialog, activity, CancellationToken.None);
                }
            }

        }

        public static async Task PresentCourses(IDialogContext context, CourseList course)
        {
            var options = context.MakeMessage();
            UserDataCollection user = context.UserData.GetValue<UserDataCollection>("UserObject");
            string gender = user.gender; 
            string language = user.PreferedLang;
            string courseName = (language == StringResources.en ? course.courseName : course.courseNameArabic);
            string takeCourse = StringResources.ResourceManager.GetString($"{language}_TakeCourse");
            string goBack = StringResources.ResourceManager.GetString($"{language}_GoBack");
            await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_CourseInfo"), courseName));
            string askTakeCourse = "";
            if (language == StringResources.ar)
            {
                switch (gender)
                {
                    case "female":
                        askTakeCourse = StringResources.ar_AskTakeCourseFemale;
                        break;
                    case "male":
                        askTakeCourse = StringResources.ar_AskTakeCourseMale;
                        break;
                    default:
                        askTakeCourse = StringResources.ar_AskTakeCourse;
                        break;
                }
            }
            else
            {
                askTakeCourse = StringResources.en_AskTakeCourse;
            }
            string courseInfo = "";
            if (course.selfPaced)
            {
                courseInfo += String.Format(StringResources.ResourceManager.GetString($"{language}_SelfPaced"), course.approxDuration.ToString());
            }
            else
            {
                courseInfo += String.Format(StringResources.ResourceManager.GetString($"{language}_Scheduled"), course.approxDuration.ToString());
            }
            if (course.accreditationOption)
            {
                courseInfo += "\n\n" + StringResources.ResourceManager.GetString($"{language}_IsAccredited");
            }
            else
            {
                courseInfo += "\n\n" + StringResources.ResourceManager.GetString($"{language}_NotAccredited");
            }
            if (course.financialAid)
            {
                courseInfo += "\n\n" + StringResources.ResourceManager.GetString($"{language}_FinanceAvailable");
            }
            else
            {
                courseInfo += "\n\n" + StringResources.ResourceManager.GetString($"{language}_NoFinanceAvailable");
            }
            if (course.courseTrailer != null)
            {
                courseInfo += "\n\n" + String.Format(StringResources.ResourceManager.GetString($"{language}_CourseTrailer"), course.courseTrailer);
            }
            if (language == StringResources.ar)
            {
                for(int i = 0; i < course.languageDelivered.Length; i++)
                {
                    if (course.languageDelivered[i] == StringResources.en_English)
                    {
                        course.languageDelivered[i] = StringResources.ar_English;
                    }
                    if (course.languageDelivered[i] == StringResources.en_Arabic)
                    {
                        course.languageDelivered[i] = StringResources.ar_Arabic;
                    }
                } 
            }
            courseInfo += "\n\n" + String.Format(StringResources.ResourceManager.GetString($"{language}_CourseDeliveryLanguage"), string.Join(", ", course.languageDelivered.ToArray()));
            courseInfo += "\n\n" + StringResources.ResourceManager.GetString($"{language}_CourseDescription");
            courseInfo += "\n\n";
            courseInfo += language == StringResources.ar ? course.descriptionArabic : course.description;
            user.chosenCourse = course;
            context.UserData.SetValue("UserObject", user);
            await context.PostAsync(courseInfo);
            options.Text = askTakeCourse;
            options.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                        {
                            new CardAction(){ Title = takeCourse, Type = ActionTypes.ImBack, Value = takeCourse},
                            new CardAction(){ Title = goBack, Type = ActionTypes.ImBack, Value = goBack }
                        }
            };
            await context.PostAsync(options);
        }
        
        public static async Task AwaitCourseChoice(IDialogContext context, IAwaitable<object> result)
        {
            /*
             * This method handles the Users choice to take the course presented to them or go back. 
             * If the user does not select to "go back" or "take course" the user is redirected to LUIS
             * User is also redirected to LUIS upon return to conversation if they choose to take course
             */
            Activity activity = await result as Activity;
            string choice = activity.Text;
            await ConversationStarter.CheckLanguage(activity.Text.Trim(), context);
            UserDataCollection user = context.UserData.GetValue<UserDataCollection>("UserObject");
            string language = ConversationStarter.user.PreferedLang; 
            var reply1 = context.MakeMessage();
            var reply = context.MakeMessage();
            if (choice.ToLower() == StringResources.en_GoBack.ToLower() || choice.ToLower() == "back" || choice == StringResources.ar_GoBack)
            {
                List<CourseList> course_by_sub = SaveConversationData.MatchCourseBySubTopic(ConversationStarter.user.currentSubTopic, language);
                List<dynamic> temp = FilterByPreferences(context, language, course_by_sub);
                string removed = "";
                if (temp[0].Count < course_by_sub.Count)
                {
                    course_by_sub = temp[0];
                }
                if (temp[1].Count > 0)
                {
                    foreach (string preference in temp[1])
                    {
                        removed += "\n\n \u2022" + preference;
                    }
                }
                await DisplayCourse(context, language, course_by_sub,removed);
                context.Wait(FinalCourse);
            }
            else if (choice.ToLower() == StringResources.en_TakeCourse.ToLower() || choice == StringResources.ar_TakeCourse)
            {
                CourseList course = user.chosenCourse;
                UserCourse user_course = new UserCourse()
                {
                    Name = course.courseName,
                    NameArabic = course.courseNameArabic,
                    Complete = false,
                    Date = DateTime.UtcNow,
                    InProgress = false,
                    Queried = false,
                    Rating = 0,
                    Taken = false
                };
                List<CardAction> link = new List<CardAction>();
                string courseName = language == StringResources.en ? course.courseName : course.courseNameArabic;
                bool alreadyTaken = false;
                for(int i = 0; i < .user.PastCourses.Count; i++)
                {
                    if (user.PastCourses[i].Name == user_course.Name)
                    {
                        user.PastCourses[i] = user_course;
                        alreadyTaken = true;
                        context.UserData.SetValue("UserObject", user);
                        await SaveConversationData.UpdatePastCourses(user._id, user.PastCourses);
                    }
                }
                if (!alreadyTaken)
                {
                    user.PastCourses.Add(user_course);
                    context.UserData.SetValue("UserObject", user);
                    await SaveConversationData.SaveUserCourse(activity.From.Id, user_course);
                }
                reply.Text = String.Format(StringResources.ResourceManager.GetString($"{language}_CourseLink"), courseName);
                link.Add(new CardAction() { Title = language == StringResources.ar? course.courseNameArabic: course.courseName, Type = ActionTypes.OpenUrl, Value = course.url });
                HeroCard Card = new HeroCard(){Buttons = link};
                Attachment linkAttachment = Card.ToAttachment();
                reply1.Attachments.Add(linkAttachment);
                string startOver = StringResources.ResourceManager.GetString($"{language}_StartOver");
                reply.Text = StringResources.ResourceManager.GetString($"{language}_SearchNew");
                reply.SuggestedActions = new SuggestedActions()
                {
                    Actions = new List<CardAction>()
                    {
                        new CardAction(){Title = startOver, Type = ActionTypes.ImBack, Value = startOver }
                    }
                };
                await context.PostAsync(reply1);
                await context.PostAsync(reply);
                context.Wait(FinalSelection);
            }
            else
            {
                if (language == StringResources.ar)
                {
                    user.arabicText = activity.Text.Trim();
                    context.UserData.SetValue("UserObject", user);
                    activity.Text = await Translate.Translator(activity.Text, StringResources.en);
                }
                await context.Forward(new LuisDialog(), ResumeAfterLuisDialog, activity, CancellationToken.None);
            }
        }

        public static async Task FinalSelection(IDialogContext context, IAwaitable<object> result)
        {
            /*
             * Function that takes input after user is presented with course and sends to LUIS
             */
            Activity activity = await result as Activity;
            await ConversationStarter.CheckLanguage(activity.Text.Trim(), context);
            UserDataCollection user = context.UserData.GetValue<UserDataCollection>("UserObject");
            string language = user.PreferedLang;
            string text = activity.Text.Trim().ToLower();
            if (text == StringResources.en_StartOver || text == StringResources.ar_StartOver)
            {
                await context.Forward(new LearningDialog(), ResumeAfterLuisDialog, activity, CancellationToken.None);
            }
            else
            {
                if (language == StringResources.ar)
                {
                    user.arabicText = activity.Text;
                    context.UserData.SetValue("UserObject", user);
                    activity.Text = await Translate.Translator(activity.Text, StringResources.en);
                }
                await context.Forward(new LuisDialog(), ResumeAfterLuisDialog, activity, CancellationToken.None);
            }
        }

        private async Task ResumeAfterKill(IDialogContext context, IAwaitable<object> result)
        {
            /*
             *Method that kills stack after context has been passed on and returned
             */
            await result;
            context.Done(true);
        }

        private static async Task ResumeAfterLuisDialog(IDialogContext context, IAwaitable<object> result)
        {
            /* 
             * After LUIS understanding, the next message comes back to here
             */
            context.Done(true);
        }
    }
}