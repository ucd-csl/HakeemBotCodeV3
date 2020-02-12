using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using NetHope.SupportClasses;
using NetHope.ProactiveMessage;
using NetHope.Dialogs;
using NetHope.Resources;

namespace NetHope.Dialogs
{
    internal class RecommendDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(StartRecommendation);
            return Task.CompletedTask;
        }

        private async Task StartRecommendation(IDialogContext context, IAwaitable<object> result)
        {
            /*
             * Function that takes the interests of the user and the subjects of past courses and checks for courses that suit their preferences (pace, accredditation, language)
             * If No courses are found given the current preferences 'restrictions' are lifted i.e. pace / accreditation / education level are removed when searching for suitable courses
             */
            Activity activity = await result as Activity;
            string user_id = activity.From.Id;
            string language = ConversationStarter.user.PreferedLang;

            List<CourseList> course = new List<CourseList>();
            UserDataCollection user = ConversationStarter.user;
            List<string> interest = user.interests;
            List<UserCourse> past_courses = user.PastCourses;

            if (past_courses.Count > 0)
            {
                string[] course_to_sub = CourseToInterest(past_courses);
                interest.AddRange(course_to_sub);
            }

            Random rnd = new Random();

            int restictive = 0;
            List<CourseList> course_current = new List<CourseList>();
            // make sure all interests have a chance to be checked for courses
            // we will cull the list down later at random so it doesn't get too long for the user
            for (int i = 0; i < interest.Count; i++)
            {

                int index = rnd.Next(i, interest.Count);

                string current_interest = interest[index];
                interest.Insert(0, interest[index]);
                interest.RemoveAt(index + 1);

                course_current = SaveConversationData.TryMatchCourse(current_interest, user.accreditation, user.delivery, true, user.language, user.education, restictive);

                course.AddRange(course_current);

                if (i == interest.Count - 1 && course.Count == 0)
                {
                    if (restictive == 3)
                    {
                        // we've had 3 failed passes, give up without finding any courses
                        // later we should instead recommend the most popular course (collabartive filtering)

                        break;
                    }
                    // first 'pass' has failed to find any courses, start a new 'pass' with less restrictive parameters
                    i = 0;
                    i--;
                    restictive++;
                }
            }


            await PresentRecommendation(course, activity, context, restictive);

        }

        private async Task PresentRecommendation(List<CourseList> courses, Activity activity, IDialogContext context, int restrictive)
        {
            /*
             * Function to display the course recommendations to the User
             * If no courses are found, an error message is displayed. (Implement a RecommendMostPopular() to be implemented in future.
             * Waits for Users action
             */
            courses = ReduceLength(courses);
            string language = ConversationStarter.user.PreferedLang;
            if (courses.Count == 0)
            {
                await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_RecommendFail"));
                context.Done(true);
            }
            else
            {
                if (restrictive != 0)
                {
                    await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_RestrictedRecommendation"));
                    switch (restrictive)
                    {
                        case 1:
                            await context.PostAsync("\u2022"+ StringResources.ResourceManager.GetString($"{language}_EducationLevel"));
                            goto case 2;
                        case 2:
                            await context.PostAsync("\u2022"+ StringResources.ResourceManager.GetString($"{language}_PreferredLanguage"));
                            goto case 3;
                        case 3:
                            await context.PostAsync("\u2022" + StringResources.ResourceManager.GetString($"{language}_CourseDelivery"));
                            break;
                        default:
                            break;

                    }
                }
                
                var reply = context.MakeMessage();
                List<CardAction> course_suggestions = new List<CardAction>();
                for (int i = 0; i < courses.Count; i++)
                {
                    if (courses[i] == null)
                    {
                        break;
                    }
                    string name = language == StringResources.en ? courses[i].courseName : courses[i].courseNameArabic;
                    course_suggestions.Add(new CardAction() { Title = name, Type = ActionTypes.ImBack, Value = name });
                }
                reply.Text = StringResources.ResourceManager.GetString($"{language}_PrefRecommend");
                string startOver = StringResources.ResourceManager.GetString($"{language}_StartOver");
                course_suggestions.Add(new CardAction() { Title = startOver, Type = ActionTypes.ImBack, Value = startOver });
                reply.SuggestedActions = new SuggestedActions()
                {
                    Actions = course_suggestions
                };
                await context.PostAsync(reply);
                context.Wait(WaitForChoice);
            }
        }

        private async Task WaitForChoice(IDialogContext context, IAwaitable<object> result)
        {
            /*
             * If start over if selected LearningDialog is called
             * If User wishes to take course -course information and a link to course is presented
             * Otherwise LUIS is called
             */
            Activity activity = await result as Activity;
            string course = activity.Text;
            await ConversationStarter.CheckLanguage(activity.Text.Trim(), context);
            UserDataCollection user = context.UserData.GetValue<UserDataCollection>("UserObject");
            string language = user.PreferedLang;
            if (course.ToLower() == StringResources.en_StartOver.ToLower() || course == StringResources.ar_StartOver)
            {
                await context.Forward(new LearningDialog(), ResumeAfter, activity, CancellationToken.None);
            }
            else
            {
                CourseList course_by_name = SaveConversationData.GetCourseByName(course);
                var options = context.MakeMessage();

                if (course_by_name == null)
                {
                    if (language == StringResources.ar)
                    {
                        user.arabicText = activity.Text.Trim();
                        context.UserData.SetValue("UserObject", user);
                        activity.Text = await Translate.Translator(activity.Text, StringResources.en);
                    }
                    await context.Forward(new LuisDialog(), ResumeAfter, activity, CancellationToken.None);
                }
                else
                {
                    string gender = user.gender;
                    string courseName = language == StringResources.en ? course_by_name.courseName : course_by_name.courseNameArabic;
                    string takeCourse = StringResources.ResourceManager.GetString($"{language}_TakeCourse");
                    string startOver = StringResources.ResourceManager.GetString($"{language}_StartOver");
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_CourseInfo"), courseName));
                    string askTakeCourse = "";
                    if (language == StringResources.ar)
                    {
                        switch (gender)
                        {
                            case "female":
                                askTakeCourse = StringResources.ResourceManager.GetString($"ar_TakeCourseFemale");
                                break;
                            case "male":
                                askTakeCourse = StringResources.ResourceManager.GetString($"ar_TakeCourseMale");
                                break;
                            default:
                                askTakeCourse = StringResources.ResourceManager.GetString($"ar_TakeCourse");
                                break;
                        }
                    }
                    else
                    {
                        askTakeCourse = StringResources.ResourceManager.GetString($"en_TakeCourse");
                    }
                    string courseInfo = "";
                    if (course_by_name.selfPaced)
                    {
                        courseInfo += String.Format(StringResources.ResourceManager.GetString($"{language}_SelfPaced"), course_by_name.approxDuration.ToString());
                    }
                    else
                    {
                        courseInfo += String.Format(StringResources.ResourceManager.GetString($"{language}_Scheduled"), course_by_name.approxDuration.ToString());
                    }
                    if (course_by_name.accreditationOption)
                    {
                        courseInfo += "\n\n" + StringResources.ResourceManager.GetString($"{language}_IsAccredited");
                    }
                    else
                    {
                        courseInfo += "\n\n" + StringResources.ResourceManager.GetString($"{language}_NotAccredited");
                    }
                    if (course_by_name.financialAid)
                    {
                        courseInfo += "\n\n" + StringResources.ResourceManager.GetString($"{language}_FinanceAvailable");
                    }
                    else
                    {
                        courseInfo += "\n\n" + StringResources.ResourceManager.GetString($"{language}_NoFinanceAvailable");
                    }
                    if (course_by_name.courseTrailer != null)
                    {
                        courseInfo += "\n\n" + String.Format(StringResources.ResourceManager.GetString($"{language}_CourseTrailer"), course_by_name.courseTrailer);
                    }
                    courseInfo += "\n\n" + StringResources.ResourceManager.GetString($"{language}_CourseDescription");
                    courseInfo += "\n\n" + course_by_name.description;
                    user.chosenCourse = course_by_name;
                    context.UserData.SetValue("UserObject", user);
                    await context.PostAsync(courseInfo);
                    options.SuggestedActions = new SuggestedActions()
                    {
                        Actions = new List<CardAction>()
                            {
                                new CardAction(){ Title = takeCourse, Type = ActionTypes.ImBack, Value = takeCourse },
                                new CardAction(){ Title = startOver, Type = ActionTypes.ImBack, Value = startOver}
                            }
                    };
                    
                    await context.PostAsync(options);
                    context.Wait(PrintCourse);
                }
            }

        }

        private async Task PrintCourse(IDialogContext context, IAwaitable<object> result)
        {
            /*
             * If users opts to take course - course is added to past courses in User Database
             * Start over calls the learning Dialog 
             * All unexpected input is sent to LUIS
             */
            Activity activity = await result as Activity;
            string choice = activity.Text;
            await ConversationStarter.CheckLanguage(activity.Text.Trim(), context);
            UserDataCollection user = context.UserData.GetValue<UserDataCollection>("UserObject");
            string language = user.PreferedLang;
            var reply = context.MakeMessage();
            var response = context.MakeMessage();
            if (choice.ToLower() == StringResources.en_TakeCourse.ToLower() || choice == StringResources.ar_TakeCourse)
            {
                CourseList course = user.chosenCourse;

                UserCourse user_course = new UserCourse()
                {
                    Name = course.courseName,
                    NameArabic = course.courseNameArabic,
                    Complete = false,
                    Date = DateTime.Now,
                    InProgress = false,
                    Queried = false,
                    Rating = 0,
                    Taken = false
                };
                user.PastCourses.Add(user_course);
                context.UserData.SetValue("UserObject", user);
                await SaveConversationData.SaveUserCourse(user._id, user_course);
                string courseName = language == StringResources.en ? course.courseName : course.courseNameArabic;
                string startOver = StringResources.ResourceManager.GetString($"{language}_StartOver");
                
                response.Text = String.Format(StringResources.ResourceManager.GetString($"{language}_CourseLink"), courseName);

                List<CardAction> link = new List<CardAction>();
                link.Add(new CardAction() { Title = course.courseName, Type = ActionTypes.OpenUrl, Value = course.url });
                HeroCard Card = new HeroCard(){Buttons = link};
                Attachment linkAttachment = Card.ToAttachment();
                response.Attachments.Add(linkAttachment);
                reply.Text = StringResources.ResourceManager.GetString($"{language}_SearchNew");
                reply.SuggestedActions = new SuggestedActions()
                {
                    Actions = new List<CardAction>()
                    {
                        new CardAction(){Title = startOver, Type = ActionTypes.ImBack, Value = startOver }
                    }
                };
                await context.PostAsync(response);
                await context.PostAsync(reply);
                context.Wait(SendBack);
            }
            else if (choice.ToLower() == StringResources.en_StartOver || choice == StringResources.ar_StartOver)
            {
                await context.Forward(new LearningDialog(), null, activity, CancellationToken.None);
            }
            else
            {
                if (language == StringResources.ar)
                {
                    user.arabicText = choice;
                    context.UserData.SetValue("UserObject", user);
                    activity.Text = await Translate.Translator(activity.Text, StringResources.en);
                }
                await context.Forward(new LuisDialog(), ResumeAfter, choice, CancellationToken.None);
            }
        }

        private async Task SendBack(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = await result as Activity;
            await context.Forward(new LearningDialog(), ResumeAfter, activity, CancellationToken.None);
        }

        private List<CourseList> ReduceLength(List<CourseList> courses)
        {
            if (courses.Count <= 5)
            {
                return courses;
            }

            Random rnd = new Random();
            while (courses.Count > 5)
            {
                int rand = rnd.Next(0, courses.Count);
                courses.RemoveAt(rand);
            }

            return courses;
        }

        //private void RecommendMostPopular() {}

        private string[] CourseToInterest(List<UserCourse> past_courses)
        {
            string[] subjects = new string[past_courses.Count];
            dynamic[] time = new dynamic[past_courses.Count];
            for (int i = 0; i < past_courses.Count; i++)
            {
                string sub = SaveConversationData.CourseToSubject(past_courses[i].Name);
                subjects[i] = sub;
                time[i] = past_courses[i].Date;
            }

            bool sorted = false;
            bool swap = false;
            while (!sorted)
            {
                swap = false;
                dynamic last = time[0];
                for (int i = 1; i < subjects.Length; i++)
                {
                    if (time[i] < time[i - 1])
                    {
                        dynamic tmp = time[i];
                        time[i] = time[i - 1];
                        time[i - 1] = tmp;
                        tmp = subjects[i];
                        subjects[i] = subjects[i - 1];
                        subjects[i - 1] = tmp;
                        swap = true;
                        break;
                    }
                }

                if (!swap) { sorted = true; }
            }

            return subjects;
        }

        private async Task ResumeAfter(IDialogContext context, IAwaitable<object> result)
        {
            context.Done(true);
        }

        private Task TailoredRecommendation()
        {
            return Task.CompletedTask;
        }
    }
}