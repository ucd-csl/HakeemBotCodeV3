﻿using System;
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
using MongoDB.Driver;
using System.Configuration;

namespace NetHope.Dialogs
{
    [Serializable]
    public class ReCheckCourse : IDialog<object>
    {
        private static readonly IMongoCollection<UserDataCollection> UserDataCollection = SaveConversationData.GetReferenceToCollection<UserDataCollection>(ConfigurationManager.AppSettings.Get("UserCollection"));

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
            await ConversationStarter.CheckLanguage(activity.Text.Trim(), context);
            string language = context.UserData.GetValue<string>("PreferedLang");
            UserCourse current_course = context.UserData.GetValue<UserCourse>("currentCourse");
            string checkUp = StringResources.ResourceManager.GetString($"{language}_ReCheck");
            string checkStarted = StringResources.ResourceManager.GetString($"{language}_StillTaking");
            string yes = StringResources.ResourceManager.GetString($"{language}_Yes");
            string no = StringResources.ResourceManager.GetString($"{language}_No");
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
                            new CardAction(){Title=yes, Type=ActionTypes.ImBack, Value=yes},
                            new CardAction(){Title=no, Type=ActionTypes.ImBack, Value=no},
                        }
            };
            await context.PostAsync(reply);
            context.Wait(StillTakingCourse);
        }

        private async Task StillTakingCourse(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = await result as Activity;
            await ConversationStarter.CheckLanguage(activity.Text.Trim(), context);
            string language = context.UserData.GetValue<string>("PreferedLang");
            UserCourse current_course = context.UserData.GetValue<UserCourse>("currentCourse");
            List<UserCourse> pastCourses = context.UserData.GetValue<List<UserCourse>>("PastCourses");
            BsonObjectId iD = new BsonObjectId(new ObjectId(context.UserData.GetValue<string>("_id")));
            if (activity.Text.ToLower() == StringResources.en_Yes.ToLower() || activity.Text == StringResources.ar_Yes)
            {
                current_course.Date = DateTime.UtcNow;
                for (int i = 0; i < pastCourses.Count; i++)
                {
                    if (pastCourses[i].Name == current_course.Name)
                    {
                        pastCourses[i] = current_course;
                    }
                }
                await SaveConversationData.UpdatePastCourses(iD, pastCourses);
                context.UserData.SetValue("PastCourses", pastCourses);
                await context.PostAsync("Ok, I will check in again in a week");
                context.Done(true);
            }
            else if(activity.Text.ToLower() == StringResources.en_No.ToLower() || activity.Text == StringResources.ar_No)
            {
                var reply = context.MakeMessage();
                string yes = StringResources.ResourceManager.GetString($"{language}_Yes");
                string no = StringResources.ResourceManager.GetString($"{language}_No");
                reply.Text = "Did you finish taking this course?";
                reply.SuggestedActions = new SuggestedActions()
                {
                    Actions = new List<CardAction>()
                        {
                            new CardAction(){Title=yes, Type=ActionTypes.ImBack, Value=yes},
                            new CardAction(){Title=no, Type=ActionTypes.ImBack, Value=no},
                        }
                };
                await context.PostAsync(reply);
                context.Wait(CourseStopped);
            }
            else
            {
                if (language == StringResources.ar)
                {
                    string arabic = activity.Text.Trim();
                    await SaveConversationData.UpdateArabicText(iD, arabic);
                    context.UserData.SetValue("arabicText", arabic);
                    activity.Text = await Translate.Translator(activity.Text, StringResources.en);
                }
                await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
            }
        }

        private async Task CourseStopped(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = await result as Activity;
            await ConversationStarter.CheckLanguage(activity.Text.Trim(), context);
            string language = context.UserData.GetValue<string>("PreferedLang");
            UserCourse current_course = context.UserData.GetValue<UserCourse>("currentCourse");
            List<UserCourse> pastCourses = context.UserData.GetValue<List<UserCourse>>("PastCourses");
            BsonObjectId iD = new BsonObjectId(new ObjectId(context.UserData.GetValue<string>("_id")));

            if (activity.Text.ToLower() == StringResources.en_Yes.ToLower() || activity.Text == StringResources.ar_Yes)
            {
                current_course.InProgress = false;
                current_course.Complete = true;
            }
            else if (activity.Text.ToLower() == StringResources.en_No.ToLower() || activity.Text == StringResources.ar_No)
            {
                current_course.InProgress = false;
            }
            else
            {
                if (language == StringResources.ar)
                {
                    string arabic = activity.Text.Trim();
                    await SaveConversationData.UpdateArabicText(iD, arabic);
                    context.UserData.SetValue("arabicText", arabic);
                    activity.Text = await Translate.Translator(activity.Text, StringResources.en);
                }
                await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);

            }
            for (int i = 0; i < pastCourses.Count; i++)
            {
                if (pastCourses[i].Name == current_course.Name)
                {
                    pastCourses[i] = current_course;
                }
            }
            await SaveConversationData.UpdatePastCourses(iD, pastCourses);
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