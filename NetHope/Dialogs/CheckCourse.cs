using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Bot.Builder.ConnectorEx;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.PersonalityChat.Core;
using Microsoft.Bot.Connector;
using NetHope.ProactiveMessage;
using NetHope.Resources;
using NetHope.SupportClasses;
using MongoDB.Bson;

namespace NetHope.Dialogs
{
    [Serializable]
    public class CheckCourse : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(this.MessageReceivedAsync);
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            /*
             * Function to handle courses user has previously viewed.
             */
            Activity activity = await result as Activity;
            
            UserCourse current_course = context.UserData.GetValue<UserCourse>("currentCourse");
            await ConversationStarter.CheckLanguage(activity.Text.Trim(), context);
            string language = context.UserData.GetValue<string>("PreferedLang"); 

            string checkUp = StringResources.ResourceManager.GetString($"{language}_CheckUp");
            string checkStarted = StringResources.ResourceManager.GetString($"{language}_CheckCourseStarted");
            string yes = StringResources.ResourceManager.GetString($"{language}_Yes");
            string no = StringResources.ResourceManager.GetString($"{language}_No");
            string startedCourse = StringResources.ResourceManager.GetString($"{language}_CourseStarted");
            string courseNotTaken = StringResources.ResourceManager.GetString($"{language}_CourseNotStarted");
            var reply = context.MakeMessage();
            if (language == StringResources.ar)
            {
                await context.PostAsync(String.Format(checkUp, current_course.NameArabic));
            }
            else
            {
                await context.PostAsync(String.Format(checkUp, current_course.Name));
            }
            reply.Text = checkStarted;
            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                        {
                            new CardAction(){Title=startedCourse, Type=ActionTypes.ImBack, Value=yes},
                            new CardAction(){Title=courseNotTaken, Type=ActionTypes.ImBack, Value=no},
                        }
            };
            await context.PostAsync(reply);
            context.Wait(HaveTakenCourse);
        }

        private async Task HaveTakenCourse(IDialogContext context, IAwaitable<object> result)
        {
            /*
             * Function to handle User input
             * If User confirms taking course - course is flagged as taken and in progress
             * If user selects 'no' to taking course course is flagged not in progress and not taken
             * If answer is not 'yes' or 'no' LUIS is called
             */
            Activity activity = await result as Activity;
            UserCourse current_course = context.UserData.GetValue<UserCourse>("currentCourse");
            await ConversationStarter.CheckLanguage(activity.Text.Trim(), context);
            string language = context.UserData.GetValue<string>("PreferedLang");
            BsonObjectId cosmosID = new BsonObjectId(new ObjectId(context.UserData.GetValue<string>("_id")));
            current_course.Queried = true;
            List<UserCourse> pastCourses = context.UserData.GetValue<List<UserCourse>>("PastCourses");

            if (activity.Text.ToLower() == StringResources.en_Yes.ToLower() || activity.Text == StringResources.ar_Yes)
            {
                current_course.Date = DateTime.UtcNow;
                current_course.Taken = true;
                current_course.InProgress = true;
            }
            else if (activity.Text.ToLower() == StringResources.en_No.ToLower() || activity.Text == StringResources.ar_No)
            {
                current_course.Taken = false;
                current_course.InProgress = false;
            }
            else
            {
                if (language == StringResources.ar)
                {
                    await SaveConversationData.UpdateArabicText(cosmosID, activity.Text.Trim());
                    context.UserData.SetValue("arabicText", activity.Text.Trim());
                    activity.Text = await Translate.Translator(activity.Text, StringResources.en);
                }
                await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);

            }
            for(int i =0; i < pastCourses.Count; i++)
            {
                if(pastCourses[i].Name == current_course.Name)
                {
                    pastCourses[i] = current_course;
                }
            }
            await SaveConversationData.UpdatePastCourses(cosmosID, pastCourses);
            context.UserData.SetValue("PastCourses", pastCourses);
            await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_NoteMade"));
            context.Done(true);
        }

        private async Task ResumeAfterKill(IDialogContext context, IAwaitable<object> result)
        {
            context.Done(true);
        }
    }
}