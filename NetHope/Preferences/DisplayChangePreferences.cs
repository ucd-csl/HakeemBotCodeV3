using System;
using Microsoft.Bot.Builder.Dialogs;
using System.Threading.Tasks;
using NetHope.ProactiveMessage;
using Microsoft.Bot.Connector;
using System.Collections.Generic;
using NetHope.Controllers;
using NetHope.Resources;
using NetHope.SupportClasses;
using System.Threading;
using System.Diagnostics;
using NetHope.Dialogs;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Security.Cryptography;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Bot.Builder.ConnectorEx;
using System.Configuration;
using System.Security.Cryptography;

namespace NetHope.Preferences
{
    [Serializable]
    public class DisplayChangePreferences : IDialog<object>
    {
        private static string InterestsUrl = ConfigurationManager.AppSettings.Get("InterestsUrl");
        private static readonly IMongoCollection<UserDataCollection> UserDataCollection = SaveConversationData.GetReferenceToCollection<UserDataCollection>(ConfigurationManager.AppSettings.Get("UserCollection"));
        public static Dictionary<string, string> InterestList = new Dictionary<string, string>
            {
                {StringResources.en_Astronomy,StringResources.ar_Astronomy},
                {StringResources.en_Archaeology,StringResources.ar_Archaeology},
                {StringResources.en_Biology,StringResources.ar_Biology},
                {StringResources.en_Chemistry,StringResources.ar_Chemistry},
                {StringResources.en_Engineering,StringResources.ar_Engineering },
                {StringResources.en_Geography, StringResources.ar_Geography},
                {StringResources.en_Maths,StringResources.ar_Maths},
                {StringResources.en_Physics,StringResources.ar_Physics},
                {StringResources.en_Accounting,StringResources.ar_Accounting},
                {StringResources.en_Economics,StringResources.ar_Economics},
                {StringResources.en_Entrepreneurship,StringResources.ar_Entrepreneurship},
                {StringResources.en_Finance,StringResources.ar_Finance},
                {StringResources.en_Marketing,StringResources.ar_Marketing},
                {StringResources.en_Medicine,StringResources.ar_Medicine},
                {StringResources.en_Nutrition,StringResources.ar_Nutrition},
                {StringResources.en_Psychology,StringResources.ar_Psychology},
                {StringResources.en_Sociology,StringResources.ar_Sociology},
                {StringResources.en_Art, StringResources.ar_Art},
                {StringResources.en_History,StringResources.ar_History},
                {StringResources.en_Languages,StringResources.ar_Languages},
                {StringResources.en_Music,StringResources.ar_Music},
                {StringResources.en_Philosophy,StringResources.ar_Philosophy},
                {StringResources.en_Poetry,StringResources.ar_Poetry},
                {StringResources.en_Politics, StringResources.ar_Politics},
                {StringResources.en_Reading,StringResources.ar_Reading},
                {StringResources.en_Writing,StringResources.ar_Writing},
                {StringResources.en_Architecture,StringResources.ar_Architecture},
                {StringResources.en_Computers,StringResources.ar_Computers},
                {StringResources.en_Cooking,StringResources.ar_Cooking},
                {StringResources.en_Media,StringResources.ar_Media},
                {StringResources.en_Nature,StringResources.ar_Nature},
                {StringResources.en_Sports,StringResources.ar_Sports},
                {StringResources.en_VideoGames,StringResources.ar_VideoGames},
                {StringResources.en_BoardGames,StringResources.ar_BoardGames},
                {StringResources.en_Religion, StringResources.ar_Religion},
            };
        public static Dictionary<string, string> EducationList = new Dictionary<string, string>
        {
            {StringResources.en_PrimarySchool.ToLower(), StringResources.ar_PrimarySchool },
            {StringResources.en_EarlyHighSchool.ToLower(), StringResources.ar_EarlyHighSchool },
            {StringResources.en_LateHighSchool.ToLower(), StringResources.ar_LateHighSchool },
            {StringResources.en_University.ToLower(), StringResources.ar_University },
        };
        public static Dictionary<string, string> GenderList = new Dictionary<string, string>
        {
            {StringResources.en_Male.ToLower(), StringResources.ar_Male },
            {StringResources.en_Female.ToLower(), StringResources.ar_Female },
            {StringResources.en_PreferNotToSay.ToLower(), StringResources.ar_PreferNotToSay },
        };
        public static Dictionary<string, string> AccreditationList = new Dictionary<string, string>
        {
            {StringResources.en_Accredited.ToLower(), StringResources.ar_Accredited },
            {StringResources.en_NonAccredited.ToLower(), StringResources.ar_NonAccredited },
            {StringResources.en_Either.ToLower(), StringResources.ar_Either },
        };
        public static Dictionary<string, string> DeliveryList = new Dictionary<string, string>
        {
            {StringResources.en_Self_Paced.ToLower(), StringResources.ar_Self_Paced },
            {StringResources.en_Interval.ToLower(), StringResources.ar_Interval },
            {StringResources.en_Both.ToLower(), StringResources.ar_Both },
        };
        public static Dictionary<string, string> LanguageList = new Dictionary<string, string>
        {
            {StringResources.en_Arabic.ToLower(), StringResources.ar_Arabic },
            {StringResources.en_English.ToLower(), StringResources.ar_English },
            {StringResources.en_Both.ToLower(), StringResources.ar_Both }
        };
        
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            /* Method that displays a list of preferences changes that the user can make. 
             * Only users who has agreed to the privacy policy can edit personal information such as, name, education, gender, subjects etc.*/
            Activity activity = await result as Activity;
            Dictionary<string, string> options = new Dictionary<string, string>();
            var reply = context.MakeMessage();
            string gender = ConversationStarter.user.gender;
            string sessionLanguage = ConversationStarter.user.PreferedLang;
            string editConvLang = StringResources.ResourceManager.GetString($"{sessionLanguage}_EditConversationLang");
            string editDeliveryLang1 = StringResources.ResourceManager.GetString($"{sessionLanguage}_EditDeliveryLang1");
            string editDeliveryLang2 = StringResources.ResourceManager.GetString($"{sessionLanguage}_EditDeliveryLang2");
            string editNotification = StringResources.ResourceManager.GetString($"{sessionLanguage}_EditNotificationFreq");
            string editName = StringResources.ResourceManager.GetString($"{sessionLanguage}_EditName");
            string editInterests = StringResources.ResourceManager.GetString($"{sessionLanguage}_EditInterests");
            string editGender1 = StringResources.ResourceManager.GetString($"{sessionLanguage}_EditGender1");
            string editGender2 = StringResources.ResourceManager.GetString($"{sessionLanguage}_EditGender2");
            string editEducation1 = StringResources.ResourceManager.GetString($"{sessionLanguage}_EditEducation1");
            string editEducation2 = StringResources.ResourceManager.GetString($"{sessionLanguage}_EditEducation2");
            string editAccreditation1 = StringResources.ResourceManager.GetString($"{sessionLanguage}_EditAccreditation1");
            string editAccreditation2 = StringResources.ResourceManager.GetString($"{sessionLanguage}_EditAccreditation2");
            string requestData = StringResources.ResourceManager.GetString($"{sessionLanguage}_RequestData");
            string deleteUser1 = StringResources.ResourceManager.GetString($"{sessionLanguage}_DeleteUser1");
            string deleteUser2 = StringResources.ResourceManager.GetString($"{sessionLanguage}_DeleteUser2");
            string editDelivery1 = StringResources.ResourceManager.GetString($"{sessionLanguage}_EditDelivery1");
            string editDelivery2 = StringResources.ResourceManager.GetString($"{sessionLanguage}_EditDelivery2");
            string Continue = StringResources.ResourceManager.GetString($"{sessionLanguage}_Continue");

            if (sessionLanguage == StringResources.ar)
            {
                switch (gender)
                {
                    case "female":
                        reply.Text = StringResources.ResourceManager.GetString($"ar_ChangePreferencesFemale");
                        break;
                    case "male":
                        reply.Text = StringResources.ResourceManager.GetString($"ar_ChangeMale");
                        break;
                    default:
                        reply.Text = StringResources.ResourceManager.GetString($"ar_ChangePreferences");
                        break;
                }
            }
            else
            {
                reply.Text = StringResources.ResourceManager.GetString($"en_ChangePreferences");
                reply.Text += "\n\n" + StringResources.ResourceManager.GetString($"en_AltContinue");
            }

            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                    {
                        new CardAction(){ Title = editConvLang, Type = ActionTypes.ImBack, Value = editConvLang },
                        new CardAction(){ Title = editDeliveryLang1, Type = ActionTypes.ImBack, Value = editDeliveryLang2 },
                        new CardAction(){ Title = editNotification, Type = ActionTypes.ImBack, Value = editNotification },
                        new CardAction() { Title = editName, Type = ActionTypes.ImBack, Value= editName },
                        new CardAction() { Title = editInterests, Type = ActionTypes.ImBack, Value = editInterests },
                        new CardAction() { Title = editGender1, Type = ActionTypes.ImBack, Value = editGender2 },
                        new CardAction() { Title = editAccreditation1, Type = ActionTypes.ImBack, Value = editAccreditation2 },
                        new CardAction() { Title = editDelivery1, Type = ActionTypes.ImBack, Value = editDelivery2 },
                        new CardAction() { Title = editEducation1, Type = ActionTypes.ImBack, Value = editEducation2 },
                        new CardAction(){ Title = requestData, Type = ActionTypes.ImBack, Value = requestData },
                        new CardAction(){ Title = Continue, Type = ActionTypes.ImBack, Value = Continue },
                        new CardAction(){ Title = deleteUser1, Type = ActionTypes.ImBack, Value = deleteUser2 }
                    }
            };
            await context.PostAsync(reply);
            context.Wait(ChangePreference);
        }

        private async Task ChangePreference(IDialogContext context, IAwaitable<object> result)
        {
            /* Method that allows a user to change language, subject or notification preferences
             * Uses a switch statement to choose what subsequent methods to call
             * If none of these options are chosen then we use LUIS to determine the next course of action */
            Activity activity = await result as Activity;
            await ConversationStarter.CheckLanguage(activity.Text.Trim(), ConversationStarter.user._id);
            string language = ConversationStarter.user.PreferedLang;
            Dictionary<string, string> options = new Dictionary<string, string>();
            
            UserDataCollection user = ConversationStarter.user;
            List<string> interests = user.interests;
            List<string> interests_ar = new List<string>();
            if (language == StringResources.ar)
            {
                foreach (string interest in interests)
                {
                    interests_ar.Add(InterestList[interest]);
                }
            }
            
            switch (activity.Text.Trim().ToLower())
            {
                case "edit conversation language":
                    /* Change a user's preferred language */
                    await ShowChangeLanguageOptions(context,language);
                    context.Wait(SwitchLanguage);
                    break;

                case "تعديل لغة المحادثة":
                    await ShowChangeLanguageOptions(context, language);
                    context.Wait(SwitchLanguage);
                    break;

                case "edit course language":
                    /* Change a user's preferred language */
                    await ShowCourseLanguageOptions(context, language, user.language);
                    context.Wait(SaveLanguage);
                    break;

                case "تعديل لغة الدورة":
                    await ShowCourseLanguageOptions(context, language, user.language);
                    context.Wait(SaveLanguage);
                    break;

                case "edit interests":
                    /* Change a user's interests */
                    string interest = language == StringResources.ar ? string.Join(", ", interests_ar.ToArray()) : string.Join(", ", interests.ToArray());
                    await ShowInterestOptions(context, language, interest);
                    context.Wait(ConfirmInterestChange);
                    break;

                case "تعديل الأهتمامات":
                    string ints = language == StringResources.ar ? string.Join(", ", interests_ar.ToArray()) : string.Join(", ", interests.ToArray());
                    await ShowInterestOptions(context, language, ints);
                    context.Wait(ConfirmInterestChange);
                    break;


                case "edit notification frequency":
                    /* Change a user's preferred notification frequency */
                    await ShowNotificationOptions(context, language, user.Notification);
                    context.Wait(ChangeNotification);
                    break;

                case "تعديل تردد التنبيهات":
                    /* Change a user's preferred notification frequency */
                    await ShowNotificationOptions(context, language, user.Notification);
                    context.Wait(ChangeNotification);
                    break;

                case "edit gender":
                    /* Change a user's preferred gender to identify as */
                    await ShowGenderOptions(context, language, user.gender);
                    context.Wait(ChangeGender);
                    break;

                case "تعديل الجنس":
                    await ShowGenderOptions(context, language, user.gender);
                    context.Wait(ChangeGender);
                    break;

                case "edit accreditation":
                    /* Change a user's preferred course accreditation setting */
                    await ShowAccreditationOptions(context, language, user.accreditation, user.gender);
                    context.Wait(ChangeAccreditation);
                    break;

                case "تعديل تفضيلات الاعتماد":
                    await ShowAccreditationOptions(context, language, user.accreditation, user.gender);
                    context.Wait(ChangeAccreditation);
                    break;

                case "edit delivery":
                    /* Change a user's preferred course delivery setting */
                    await ShowDeliveryOptions(context, language, user.delivery);
                    context.Wait(ChangeDelivery);
                    break;

                case "تعديل تفضيلات التقديم":
                    /* Change a user's preferred course delivery setting */
                    await ShowDeliveryOptions(context, language, user.delivery);
                    context.Wait(ChangeDelivery);
                    break;

                case "edit education":
                    /* Change a user's preferred course delivery setting */
                    await ShowEducationOptions(context, language, user.education);
                    context.Wait(ChangeEducation);
                    break;

                case "تعديل تفضيلات التعلم":
                    await ShowEducationOptions(context, language, user.education);
                    context.Wait(ChangeEducation);
                    break;

                case "edit name":
                    /* Change a user's preferred course delivery setting */
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_NamePref1"), user.Name));
                    await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_NamePref2"));
                    context.Wait(ChangeName);
                    break;

                case "تعديل الأسم":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_NamePref1"), user.Name));
                    await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_NamePref2"));
                    context.Wait(ChangeName);
                    break;

                case "continue":
                    /* Bring the user back to the main dialog */
                    await context.Forward(new LearningDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;

                case "استمرار":
                    await context.Forward(new LearningDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;

                case "delete user":
                    /* Delete the user's data from the database */
                    await ShowDeleteOptions(context, language);
                    context.Wait(DeleteUserData);
                    break;

                case "الغاء بيانات المستخدم":
                    await ShowDeleteOptions(context, language);
                    context.Wait(DeleteUserData);
                    break;

                case "request data":
                    /*Allows user to request a portable copy of the data we hold on them */
                    await ShowDataOptions(context,language, user.gender);
                    context.Wait(GiveRequestedData);
                    break;

                case "طلب بيانات":
                    await ShowDataOptions(context, language, user.gender);
                    context.Wait(GiveRequestedData);
                    break;

                default:
                    /* If none of the options provided are chosen we use LUIS to see what the user wants to do */
                    if (language == StringResources.ar)
                    {
                        ConversationStarter.user.arabicText = activity.Text.Trim();
                        activity.Text = await Translate.Translator(activity.Text, StringResources.en);
                    }
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
            }
        }

        public static async Task ShowChangeLanguageOptions(IDialogContext context, String language)
        {
            var switchLanguage = context.MakeMessage();
            if (language == StringResources.ar)
            {
                string goBack = StringResources.ar_GoBack;
                switchLanguage.Text = StringResources.ar_SwitchLang;
                switchLanguage.SuggestedActions = new SuggestedActions()
                {
                    Actions = new List<CardAction>()
                        {
                            new CardAction(){ Title = StringResources.ar_SwitchToOther, Type = ActionTypes.ImBack, Value = StringResources.ar_English },
                            new CardAction(){ Title = goBack, Type = ActionTypes.ImBack, Value = goBack },
                        }
                };
            }
            else
            {
                string goBack = StringResources.en_GoBack;
                switchLanguage.Text = StringResources.en_SwitchLang;
                switchLanguage.SuggestedActions = new SuggestedActions()
                {
                    Actions = new List<CardAction>()
                        {
                            new CardAction(){ Title = StringResources.en_SwitchToOther, Type = ActionTypes.ImBack, Value = StringResources.en_Arabic },
                            new CardAction(){ Title = goBack, Type = ActionTypes.ImBack, Value = goBack },
                        }
                };
            }
            await context.PostAsync(switchLanguage);
        }

        private static async Task ShowCourseLanguageOptions(IDialogContext context, String language, String delLanguage)
        {
            string goBack = StringResources.ResourceManager.GetString($"{language}_GoBack");
            string English = StringResources.ResourceManager.GetString($"{language}_English");
            string Arabic = StringResources.ResourceManager.GetString($"{language}_Arabic");
            string Both = StringResources.ResourceManager.GetString($"{language}_Both");
            var selectLanguage = context.MakeMessage();
            if (language == StringResources.ar) { 
                switch (delLanguage) { 
                    case "English":
                        delLanguage = English;
                        break;
                    case "Arabic":
                        delLanguage = Arabic;
                        break;
                    case "Both":
                        delLanguage = Both;
                        break;
                }
            }
            await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_CourseLangPref1"), delLanguage));
            selectLanguage.Text = StringResources.ResourceManager.GetString($"{language}_CourseLangPref2");
            selectLanguage.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                        {
                            new CardAction(){ Title = English, Type = ActionTypes.ImBack, Value = English },
                            new CardAction(){ Title = Arabic, Type = ActionTypes.ImBack, Value = Arabic },
                            new CardAction(){ Title = Both, Type = ActionTypes.ImBack, Value = Both },
                            new CardAction(){ Title = goBack, Type = ActionTypes.ImBack, Value = goBack },
                        }
            };
            await context.PostAsync(selectLanguage);
        }

        private static async Task ShowInterestOptions(IDialogContext context, String language, String interest)
        {
            string Yes = StringResources.ResourceManager.GetString($"{language}_Yes");
            var changeInterests = context.MakeMessage();
            await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_InterestsPref1"), interest));
            changeInterests.Text = StringResources.ResourceManager.GetString($"{language}_InterestsPref2");
            changeInterests.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                        {
                            new CardAction(){ Title = Yes, Type = ActionTypes.ImBack, Value = Yes },
                            new CardAction(){ Title = StringResources.ResourceManager.GetString($"{language}_NoGoBack"), Type = ActionTypes.ImBack, Value = StringResources.ResourceManager.GetString($"{language}_GoBack") },

                        }
            };
            await context.PostAsync(changeInterests);
        }

        private static async Task ShowNotificationOptions(IDialogContext context, String language, long frequency)
        {
            string goBack = StringResources.ResourceManager.GetString($"{language}_GoBack");
            var selectNotificationFrequency = context.MakeMessage();
            List<CardAction> actions = new List<CardAction>();
            if (frequency == 0)
            {
                await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_NotificationsOff1"));
                selectNotificationFrequency.Text = StringResources.ResourceManager.GetString($"{language}_NotificationsOff2");
                actions.Add(new CardAction() { Title = StringResources.ResourceManager.GetString($"{language}_TurnOnNotifications"), Type = ActionTypes.ImBack, Value = StringResources.ResourceManager.GetString($"{language}_TurnOn") });
            }
            else
            {
                await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_NotificationsOn1"), frequency));
                selectNotificationFrequency.Text = StringResources.ResourceManager.GetString($"{language}_NotificationsOn2");
                actions.Add(new CardAction() { Title = StringResources.ResourceManager.GetString($"{language}_TurnOffNotifications"), Type = ActionTypes.ImBack, Value = StringResources.ResourceManager.GetString($"{language}_TurnOff") });
            }
            actions.Add(new CardAction() { Title = goBack, Type = ActionTypes.ImBack, Value = goBack });

            selectNotificationFrequency.SuggestedActions = new SuggestedActions() { Actions = actions };
            await context.PostAsync(selectNotificationFrequency);
        }

        private static async Task ShowGenderOptions(IDialogContext context, String language, String gender)
        {
            string goBack = StringResources.ResourceManager.GetString($"{language}_GoBack");
            string Male = StringResources.ResourceManager.GetString($"{language}_Male");
            string Female = StringResources.ResourceManager.GetString($"{language}_Female");
            string PreferNotToSay = StringResources.ResourceManager.GetString($"{language}_PreferNotToSay");
            var selectGender = context.MakeMessage();
            if (language == StringResources.ar)
            {
                switch (gender.ToLower())
                {
                    case "male":
                        gender = Male;
                        break;
                    case "female":
                        gender = Female;
                        break;
                    case "prefer not to say":
                        gender = PreferNotToSay;
                        break;
                    default:
                        break;
                }
            }
            await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_GenderPref1"), gender));
            selectGender.Text = StringResources.ResourceManager.GetString($"{language}_GenderPref2");
            selectGender.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                        {
                            new CardAction(){ Title = Male, Type=ActionTypes.ImBack, Value= Male },
                            new CardAction(){ Title = Female, Type=ActionTypes.ImBack, Value= Female },
                            new CardAction(){ Title = PreferNotToSay, Type=ActionTypes.ImBack, Value= PreferNotToSay },
                            new CardAction(){ Title = goBack, Type=ActionTypes.ImBack, Value= goBack },
                        },
            };
            await context.PostAsync(selectGender);
        }

        private static async Task ShowAccreditationOptions(IDialogContext context, String language, String accreditation, String gender)
        {
            string Accredited = StringResources.ResourceManager.GetString($"{language}_Accredited");
            string NonAccredited = StringResources.ResourceManager.GetString($"{language}_NonAccredited");
            string Either = StringResources.ResourceManager.GetString($"{language}_Either");
            string goBack = StringResources.ResourceManager.GetString($"{language}_GoBack");
            await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_AccredPref1"), accreditation));
            var selectAccred = context.MakeMessage();
            if (language == StringResources.ar)
            {
                switch (gender.ToLower())
                {
                    case "female":
                        selectAccred.Text = StringResources.ar_AccredPrefFemale;
                        break;
                    case "male":
                        selectAccred.Text = StringResources.ar_AccredPrefMale;
                        break;
                    default:
                        selectAccred.Text = StringResources.ar_AccredPref2;
                        break;
                }
                switch (accreditation.ToLower())
                {
                    case "accredited":
                        accreditation = Accredited;
                        break;
                    case "non-accredited":
                        accreditation = NonAccredited;
                        break;
                    case "either":
                        accreditation = Either;
                        break;
                    default:
                        break;
                }
            }
            else
            {
                selectAccred.Text = StringResources.en_AccredPref2;
            }
            selectAccred.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                        {
                            new CardAction(){ Title = Accredited, Type=ActionTypes.ImBack, Value= Accredited },
                            new CardAction(){ Title = NonAccredited, Type=ActionTypes.ImBack, Value= NonAccredited },
                            new CardAction(){ Title = Either, Type=ActionTypes.ImBack, Value= Either },
                            new CardAction(){ Title = goBack, Type=ActionTypes.ImBack, Value= goBack },
                        },
            };
            await context.PostAsync(selectAccred);
        }

        private static async Task ShowDeliveryOptions(IDialogContext context, String language, String delivery)
        {
            string Either = StringResources.ResourceManager.GetString($"{language}_Either");
            string SelfPaced = StringResources.ResourceManager.GetString($"{language}_Self_Paced"); ;
            string Interval = StringResources.ResourceManager.GetString($"{language}_Interval");
            string goBack = StringResources.ResourceManager.GetString($"{language}_GoBack");
            var selectDelivery = context.MakeMessage();
            if (language == StringResources.ar)
            {
                switch (delivery.ToLower())
                {
                    case "self-paced":
                        delivery = SelfPaced;
                        break;
                    case "interval":
                        delivery = Interval;
                        break;
                    case "either":
                        delivery = Either;
                        break;
                    default: break;
                }
            }
            await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_DeliveryPref1"), delivery));
            selectDelivery.Text = StringResources.ResourceManager.GetString($"{language}_DeliveryPref2");
            selectDelivery.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                        {
                            new CardAction(){ Title = SelfPaced, Type=ActionTypes.ImBack, Value= SelfPaced },
                            new CardAction(){ Title = Interval, Type=ActionTypes.ImBack, Value= Interval },
                            new CardAction(){ Title = Either, Type=ActionTypes.ImBack, Value= Either },
                            new CardAction(){ Title = goBack, Type=ActionTypes.ImBack, Value= goBack },
                        },
            };
            await context.PostAsync(selectDelivery);
        }

        private static async Task ShowEducationOptions(IDialogContext context, String language, String education)
        {
            string EarlyHighSchool = StringResources.ResourceManager.GetString($"{language}_EarlyHighSchool");
            string LateHighSchool = StringResources.ResourceManager.GetString($"{language}_LateHighSchool");
            string PrimarySchool = StringResources.ResourceManager.GetString($"{language}_PrimarySchool");
            string University = StringResources.ResourceManager.GetString($"{language}_University");
            string goBack = StringResources.ResourceManager.GetString($"{language}_GoBack");
            var selectEducation = context.MakeMessage();
            if (language == StringResources.ar)
            {
                switch (education.ToLower())
                {
                    case "primary school":
                        education = PrimarySchool;
                        break;
                    case "early high school":
                        education = EarlyHighSchool;
                        break;
                    case "late high school":
                        education = LateHighSchool;
                        break;
                    case "university":
                        education = University;
                        break;
                    default: break;
                }
            }
            await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_EducationPref1"), education));
            selectEducation.Text = StringResources.ResourceManager.GetString($"{language}_EducationPref2");
            selectEducation.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                        {
                            new CardAction(){ Title = PrimarySchool, Type=ActionTypes.ImBack, Value= PrimarySchool},
                            new CardAction(){ Title = EarlyHighSchool, Type=ActionTypes.ImBack, Value= EarlyHighSchool},
                            new CardAction(){ Title = LateHighSchool, Type=ActionTypes.ImBack, Value= LateHighSchool},
                            new CardAction(){ Title = University, Type = ActionTypes.ImBack, Value= University},
                            new CardAction(){ Title = goBack, Type=ActionTypes.ImBack, Value= goBack },
                        },
            };
            await context.PostAsync(selectEducation);
        }

        private static async Task ShowDataOptions(IDialogContext context, String language, String gender)
        {
            string ViewData = StringResources.ResourceManager.GetString($"{language}_ViewData");
            string GetCopy = StringResources.ResourceManager.GetString($"{language}_GetCopy");
            string goBack = StringResources.ResourceManager.GetString($"{language}_GoBack");
            var getData = context.MakeMessage();
            if (language == StringResources.ar)
            {
                switch (gender.ToLower())
                {
                    case "female":
                        getData.Text = StringResources.ar_RequestDataPrefFemale;
                        break;
                    case "male":
                        getData.Text = StringResources.ar_RequestDataPrefMale;
                        break;
                    default:
                        getData.Text = StringResources.ar_RequestDataPref;
                        break;
                }
            }
            else
            {
                getData.Text = StringResources.en_RequestDataPref;
            }
            getData.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                        {
                            new CardAction(){ Title = ViewData, Type=ActionTypes.ImBack, Value= ViewData},
                            new CardAction(){ Title = GetCopy, Type=ActionTypes.ImBack, Value= GetCopy},
                            new CardAction(){ Title = goBack, Type=ActionTypes.ImBack, Value= goBack },
                        },
            };
            await context.PostAsync(getData);
        }

        private static async Task ShowDeleteOptions(IDialogContext context, String language)
        {
            string DeleteData = StringResources.ResourceManager.GetString($"{language}_DeleteData");
            string goBack = StringResources.ResourceManager.GetString($"{language}_GoBack");
            var deleteUser = context.MakeMessage();
            deleteUser.Text = StringResources.ResourceManager.GetString($"{language}_ConfirmDelete");
            deleteUser.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                        {
                            new CardAction(){ Title = DeleteData, Type=ActionTypes.ImBack, Value= DeleteData},
                            new CardAction(){ Title = goBack, Type=ActionTypes.ImBack, Value= goBack },
                        },
            };
            await context.PostAsync(deleteUser);
        }

        private async Task ChangeNotification(IDialogContext context, IAwaitable<object> result)
        {
            /* Allows a user to change how frequently they will receive notifications
             * Calls DisplayChangePreferences again in case the user wants to amend any other preferences */

            Activity activity = await result as Activity;
            string language = ConversationStarter.user.PreferedLang;
            Regex regex_ar = new Regex(@"^[\u0621-\u064A\u0660-\u0669 ]+$");
            Regex regex = new Regex(@"^\d+$");
            string text = activity.Text.Trim();
            var cosmosID = ConversationStarter.user._id;
            switch (text.ToLower())
            {
                case "go back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "exit":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "الرجوع":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "turn off":
                    await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_NotificationsDisabled"));
                    await SaveConversationData.SaveNotification(0, cosmosID);
                    ConversationStarter.user.Notification = 0;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "إيقاف تشغيل":
                    await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_NotificationsDisabled"));
                    await SaveConversationData.SaveNotification(0, cosmosID);
                    ConversationStarter.user.Notification = 0;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "turn on":
                    await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_DefaultNotifications"));
                    await SaveConversationData.SaveNotification(7, cosmosID);
                    ConversationStarter.user.Notification = 7;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "شغله":
                    await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_DefaultNotifications"));
                    await SaveConversationData.SaveNotification(7, cosmosID);
                    ConversationStarter.user.Notification = 7;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                default:
                    if (regex.IsMatch(text))
                    {
                        try
                        {
                            long reminder = Convert.ToInt32(await Translate.Translator(activity.Text, StringResources.en));
                            await SaveConversationData.SaveNotification(reminder, cosmosID);
                            ConversationStarter.user.Notification = Convert.ToInt32(reminder);
                            await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_SpecifiedNotifications"), text));
                            await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                        }
                        catch
                        {
                            await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                        }
                    }
                    else
                    {
                        if (language == StringResources.ar)
                        {
                            ConversationStarter.user.arabicText = text;
                            activity.Text = await Translate.Translator(text, StringResources.en);
                        }
                        await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    }
                    break;
            }
        }

        public async Task SwitchLanguage(IDialogContext context, IAwaitable<object> result)
        {
            /* Allows a user to change the conversation language
            * Calls DisplayChangePreferences again in case the user wants to amend any other preferences */
            var activity = await result as Activity;
            string text = activity.Text.Trim();
            await ConversationStarter.CheckLanguage(activity.Text.Trim(), ConversationStarter.user._id);
            string language = ConversationStarter.user.PreferedLang;
            switch (text.ToLower())
            {
                case "go back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "exit":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "الرجوع":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "arabic":
                    await context.PostAsync(StringResources.ar_Language_Selected);
                    ConversationStarter.user.PreferedLang = StringResources.ar;
                    await SaveConversationData.UpdateInputLanguage(ConversationStarter.user._id, StringResources.ar);
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "عربى":
                    await context.PostAsync(StringResources.ar_Language_Selected);
                    ConversationStarter.user.PreferedLang = StringResources.ar;
                    await SaveConversationData.UpdateInputLanguage(ConversationStarter.user._id, StringResources.ar);
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "english":
                    await context.PostAsync(StringResources.en_Language_Selected);
                    ConversationStarter.user.PreferedLang = StringResources.en;
                    await SaveConversationData.UpdateInputLanguage(ConversationStarter.user._id, StringResources.en);
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "إنجليزي":
                    await context.PostAsync(StringResources.en_Language_Selected);
                    ConversationStarter.user.PreferedLang = StringResources.en;
                    await SaveConversationData.UpdateInputLanguage(ConversationStarter.user._id, StringResources.en);
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                default:
                    if (language == StringResources.ar)
                    {
                        ConversationStarter.user.arabicText = activity.Text;
                        activity.Text = await Translate.Translator(activity.Text, StringResources.en);
                    }
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
            }
        }

        private async Task SaveLanguage(IDialogContext context, IAwaitable<object> result)
        {
            /* Allows a user to change their preferred language for course delivery
             * Calls DisplayChangePreferences again in case the user wants to amend any other preferences */
            var activity = await result as Activity;
            string text = activity.Text.Trim();
            await ConversationStarter.CheckLanguage(text, ConversationStarter.user._id);
            string language = ConversationStarter.user.PreferedLang;
            string inputLang = await Translate.Detect(activity.Text);
            var cosmosID = ConversationStarter.user._id;
            switch (text.ToLower())
            {
                case "go back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "exit":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "الرجوع":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "english":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_CourseLanguageConfirm"), text.ToLower()));
                    await SaveConversationData.SaveLanguagePreference(text, cosmosID);
                    ConversationStarter.user.language = text;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "arabic":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_CourseLanguageConfirm"), text.ToLower()));
                    await SaveConversationData.SaveLanguagePreference(text, cosmosID);
                    ConversationStarter.user.language = text;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "both":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_CourseLanguageConfirm"), text.ToLower()));
                    await SaveConversationData.SaveLanguagePreference(text, cosmosID);
                    ConversationStarter.user.language = text;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "إنجليزي":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_CourseLanguageConfirm"), text.ToLower()));
                    await SaveConversationData.SaveLanguagePreference(StringResources.en_English, cosmosID);
                    ConversationStarter.user.language = StringResources.en_English;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "عربي":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_CourseLanguageConfirm"), text.ToLower()));
                    await SaveConversationData.SaveLanguagePreference(StringResources.en_Arabic, cosmosID);
                    ConversationStarter.user.language = StringResources.en_Arabic;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "كلاهما":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_CourseLanguageConfirm"), text.ToLower()));
                    await SaveConversationData.SaveLanguagePreference(StringResources.en_Both, cosmosID);
                    ConversationStarter.user.language = StringResources.en_Both;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                default:
                    if (language == StringResources.ar)
                    {
                        ConversationStarter.user.arabicText = text;
                        activity.Text = await Translate.Translator(text, StringResources.en);
                    }
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
            }
        }

        private async Task ChangeGender(IDialogContext context, IAwaitable<object> result)
        {
            /* Allows a user to change their preferred gender
             * Calls DisplayChangePreferences again in case the user wants to amend any other preferences */
            Activity activity = await result as Activity;
            string text = activity.Text.Trim();
            await ConversationStarter.CheckLanguage(text, ConversationStarter.user._id);
            string language = ConversationStarter.user.PreferedLang;
            var cosmosID = ConversationStarter.user._id;
            switch (text.ToLower())
            {
                case "go back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "exit":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "الرجوع":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "male":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_GenderUpdated"), text));
                    ConversationStarter.user.gender = text;
                    await SaveConversationData.SaveGenderPreference(text, cosmosID);
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "female":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_GenderUpdated"), text));
                    ConversationStarter.user.gender = text;
                    await SaveConversationData.SaveGenderPreference(text, cosmosID);
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "prefer not to say":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_GenderUpdated"), text));
                    ConversationStarter.user.gender = text;
                    await SaveConversationData.SaveGenderPreference(text, cosmosID);
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "ذكر":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_GenderUpdated"), text));
                    ConversationStarter.user.gender = StringResources.en_Male;
                    await SaveConversationData.SaveGenderPreference(StringResources.en_Male, cosmosID);
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "أنثى":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_GenderUpdated"), text));
                    ConversationStarter.user.gender = StringResources.en_Female;
                    await SaveConversationData.SaveGenderPreference(StringResources.en_Female, cosmosID);
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "افضل عدم القول":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_GenderUpdated"), text));
                    ConversationStarter.user.gender = StringResources.en_PreferNotToSay;
                    await SaveConversationData.SaveGenderPreference(StringResources.en_PreferNotToSay, cosmosID);
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                default:
                    if (language == StringResources.ar)
                    {
                        ConversationStarter.user.arabicText = text;
                        activity.Text = await Translate.Translator(text, StringResources.en);
                    }
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
            }
        }

        private async Task ChangeAccreditation(IDialogContext context, IAwaitable<object> result)
        {
            /* Allows a user to change their accreditation preferences
             * Calls DisplayChangePreferences again in case the user wants to amend any other preferences */
            Activity activity = await result as Activity;
            string text = activity.Text.Trim();
            await ConversationStarter.CheckLanguage(text, ConversationStarter.user._id);
            string language = ConversationStarter.user.PreferedLang;
            var cosmosID = ConversationStarter.user._id;
            switch (text.ToLower())
            {
                case "go back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "exit":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "الرجوع":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "accredited":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_AccreditationUpdated"), text));
                    await SaveConversationData.SaveAccreditationPreference(text, cosmosID);
                    ConversationStarter.user.accreditation = text;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "معتمدة":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_AccreditationUpdated"), text));
                    await SaveConversationData.SaveAccreditationPreference(StringResources.en_Accredited, cosmosID);
                    ConversationStarter.user.accreditation = StringResources.en_Accredited;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "non-accredited":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_AccreditationUpdated"), text));
                    await SaveConversationData.SaveAccreditationPreference(text, cosmosID);
                    ConversationStarter.user.accreditation = text;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "غير معتمدة":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_AccreditationUpdated"), text));
                    await SaveConversationData.SaveAccreditationPreference(StringResources.en_NonAccredited, cosmosID);
                    ConversationStarter.user.accreditation = StringResources.en_NonAccredited;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "either":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_AccreditationUpdated"), text));
                    await SaveConversationData.SaveAccreditationPreference(text, cosmosID);
                    ConversationStarter.user.accreditation = text;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "كلاهما":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_AccreditationUpdated"), text));
                    await SaveConversationData.SaveAccreditationPreference(StringResources.en_Either, cosmosID);
                    ConversationStarter.user.accreditation = StringResources.en_Either;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                default:
                    if (language == StringResources.ar)
                    {
                        ConversationStarter.user.arabicText = text;
                        activity.Text = await Translate.Translator(text, StringResources.en);
                    }
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
            }
        }

        private async Task ChangeDelivery(IDialogContext context, IAwaitable<object> result)
        {
            /* Allows a user to change their course delivery preferences
         * Calls DisplayChangePreferences again in case the user wants to amend any other preferences */
            Activity activity = await result as Activity;
            string text = activity.Text.Trim();
            await ConversationStarter.CheckLanguage(text, ConversationStarter.user._id);
            string language = ConversationStarter.user.PreferedLang;
            var cosmosID = ConversationStarter.user._id;
            switch (text.ToLower())
            {
                case "go back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "exit":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "الرجوع":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "self-paced":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_CourseDeliveryUpdated"), text));
                    await SaveConversationData.SaveDeliveryPreference(text, cosmosID);
                    ConversationStarter.user.delivery = text;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "السرعة الذاتية":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_CourseDeliveryUpdated"), text));
                    await SaveConversationData.SaveDeliveryPreference(StringResources.en_Self_Paced, cosmosID);
                    ConversationStarter.user.delivery = StringResources.en_Self_Paced;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "interval":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_CourseDeliveryUpdated"), text));
                    await SaveConversationData.SaveDeliveryPreference(text, cosmosID);
                    ConversationStarter.user.delivery = text;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "فترات منتظمة":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_CourseDeliveryUpdated"), text));
                    await SaveConversationData.SaveDeliveryPreference(StringResources.en_Interval, cosmosID);
                    ConversationStarter.user.delivery = StringResources.en_Interval;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "either":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_CourseDeliveryUpdated"), text));
                    await SaveConversationData.SaveDeliveryPreference(text, cosmosID);
                    ConversationStarter.user.delivery = text;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "كلاهما":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_CourseDeliveryUpdated"), text));
                    await SaveConversationData.SaveDeliveryPreference(StringResources.en_Either, cosmosID);
                    ConversationStarter.user.delivery = StringResources.en_Either;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                default:
                    if (language == StringResources.ar)
                    {
                        ConversationStarter.user.arabicText = text;
                        activity.Text = await Translate.Translator(text, StringResources.en);
                    }
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
            }
        }

        private async Task ChangeEducation(IDialogContext context, IAwaitable<object> result)
        {
            /* Allows a user to change their education level
            * Calls DisplayChangePreferences again in case the user wants to amend any other preferences */
            Activity activity = await result as Activity;
            string text = activity.Text.Trim();
            await ConversationStarter.CheckLanguage(text, ConversationStarter.user._id);
            string language = ConversationStarter.user.PreferedLang;
            var cosmosID = ConversationStarter.user._id;
            switch (text.ToLower())
            {
                case "go back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "exit":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "الرجوع":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "primary school":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_EducationUpdated"), text));
                    await SaveConversationData.SaveEducationPreference(text, cosmosID);
                    ConversationStarter.user.education = text;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "التعليم الابتدائي":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_EducationUpdated"), text));
                    await SaveConversationData.SaveEducationPreference(StringResources.en_PrimarySchool, cosmosID);
                    ConversationStarter.user.education = StringResources.en_PrimarySchool;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "early high school":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_EducationUpdated"), text));
                    await SaveConversationData.SaveEducationPreference(text, cosmosID);
                    ConversationStarter.user.education = text;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "بداية المرحلة الثانوية":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_EducationUpdated"), text));
                    await SaveConversationData.SaveEducationPreference(StringResources.en_EarlyHighSchool, cosmosID);
                    ConversationStarter.user.education = StringResources.en_EarlyHighSchool;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "late high school":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_EducationUpdated"), text));
                    await SaveConversationData.SaveEducationPreference(text, cosmosID);
                    ConversationStarter.user.education = text;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "نهاية المرحلة الثانوية":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_EducationUpdated"), text));
                    await SaveConversationData.SaveEducationPreference(StringResources.en_LateHighSchool, cosmosID);
                    ConversationStarter.user.education = StringResources.en_LateHighSchool;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "university":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_EducationUpdated"), text));
                    await SaveConversationData.SaveEducationPreference(text, cosmosID);
                    ConversationStarter.user.education = text;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "الجامعة":
                    await context.PostAsync(String.Format(StringResources.ResourceManager.GetString($"{language}_EducationUpdated"), text));
                    await SaveConversationData.SaveEducationPreference(StringResources.en_University, cosmosID);
                    ConversationStarter.user.education = StringResources.en_University;
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                default:
                    if (language == StringResources.ar)
                    {
                        ConversationStarter.user.arabicText = activity.Text;
                        activity.Text = await Translate.Translator(activity.Text, StringResources.en);
                    }
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
            }
        }

        private async Task DeleteUserData(IDialogContext context, IAwaitable<object> result)
        {
            /* Allows a user to delete their user data from hakeem database
            * Calls DisplayChangePreferences again in case the user wants to amend any other preferences */
            Activity activity = await result as Activity;
            string text = activity.Text.Trim();
            await ConversationStarter.CheckLanguage(text, ConversationStarter.user._id);
            string language = ConversationStarter.user.PreferedLang;
            var cosmosID = ConversationStarter.user._id;
            switch (text.ToLower())
            {
                case "go back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "exit":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "الرجوع":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "delete data":
                    await SaveConversationData.DeleteConvoData(activity.Conversation.Id);
                    ConversationStarter.user = new UserDataCollection();
                    await SaveConversationData.DeleteUserData(activity.From.Id);
                    await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_DataDeleted"));
                    await context.Forward(new ConversationStarter(), null, activity, CancellationToken.None);
                    break;
                case "حذف البيانات":
                    await SaveConversationData.DeleteConvoData(activity.Conversation.Id);
                    ConversationStarter.user = new UserDataCollection();
                    await SaveConversationData.DeleteUserData(activity.From.Id);
                    await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_DataDeleted"));
                    await context.Forward(new ConversationStarter(), null, activity, CancellationToken.None);
                    break;
                default:
                    if (language == StringResources.ar)
                    {
                        ConversationStarter.user.arabicText = activity.Text;
                        activity.Text = await Translate.Translator(activity.Text, StringResources.en);
                    }
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
            }
        }

        private async Task ConfirmInterestChange(IDialogContext context, IAwaitable<object> result)
        {
            /* Method to confirm desire to chnage interests
             * Calls DisplayChangePreferences again in case the user wants to amend any other preferences */
            Activity activity = await result as Activity;
            string text = activity.Text.Trim();
            await ConversationStarter.CheckLanguage(text, ConversationStarter.user._id);
            string language = ConversationStarter.user.PreferedLang;
            var cosmosID = ConversationStarter.user._id;
            switch (text.ToLower())
            {
                case "go back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "exit":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "الرجوع":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "yes":
                    await ChangeInterests(context, cosmosID);
                    await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_InterestsUpdated"));
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "نعم":
                    await ChangeInterests(context, cosmosID);
                    await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_InterestsUpdated"));
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                default:
                    if (language == StringResources.ar)
                    {
                        ConversationStarter.user.arabicText = activity.Text;
                        activity.Text = await Translate.Translator(activity.Text, StringResources.en);
                    }
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
            }
        }

        private async Task ChangeInterests(IDialogContext context, ObjectId iD)
        {
            /* Allows a user to change their interests
             * Calls DisplayChangePreferences again in case the user wants to amend any other preferences */
            string language = ConversationStarter.user.PreferedLang;
            var link = context.MakeMessage();
            link.Text=StringResources.ResourceManager.GetString($"{language}_ChangeInterests");
            UserDataCollection user2 = UserDataCollection.Find(x => x._id == iD).FirstOrDefault();
            Object userId = user2._id;
            List<string> interests = user2.interests;

            Aes myAes = Aes.Create();
            string encrypted = ConversationStarter.EncryptString(userId.ToString(), myAes.Key, myAes.IV);
            string cryptic = encrypted;
            string key = Convert.ToBase64String(myAes.Key);
            string iv = Convert.ToBase64String(myAes.IV);
            string title;
            title = StringResources.ResourceManager.GetString($"{language}_InterestsForm");
            List<CardAction> Actions = new List<CardAction>()
                {
                    new CardAction() { Title = title, Type = ActionTypes.OpenUrl, Value =  InterestsUrl + cryptic + "," + key + "," + iv + ","+language},
                };
            HeroCard Card = new HeroCard()
            {
                Buttons = Actions
            };
            Attachment attachment = Card.ToAttachment();
            link.Attachments.Add(attachment);
            int timeout = 0;
            await context.PostAsync(link);
            while (interests.SequenceEqual(user2.interests) || user2.interests == null)
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
            ConversationStarter.user.interests = user2.interests;
        }

        private async Task ChangeName(IDialogContext context, IAwaitable<object> result)
        {
            /* Allows a user to change their name
            * Calls DisplayChangePreferences again in case the user wants to amend any other preferences */
            Activity activity = await result as Activity;
            string language = ConversationStarter.user.PreferedLang;
            string text = activity.Text.Trim();
            switch (text.ToLower())
            {
                case "go back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "exit":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "الرجوع":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "start over":
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
                case "إبدأ من جديد":
                    activity.Text = StringResources.en_StartOver;
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
                case "about":
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
                case "عن":
                    activity.Text = StringResources.en_About;
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
                case "about hakeem":
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
                case "حول حكيم":
                    activity.Text = StringResources.en_AboutHakeem.ToLower();
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
                case "commands":
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
                case "أوامر":
                    activity.Text = StringResources.en_Commands;
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
                case "preferences":
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
                case "my preferences":
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
                case "تفضيلاتي":
                    activity.Text = StringResources.en_Preferences;
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
                default:
                    var cosmosID = ConversationStarter.user._id;
                    string name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(activity.Text.ToLower());
                    await SaveConversationData.UpdateUserName(name, cosmosID);
                    ConversationStarter.user.Name = name;
                    await context.PostAsync(string.Format(StringResources.ResourceManager.GetString($"{language}_NameUpdated"), name));
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
            }
        }

        private async Task GiveRequestedData(IDialogContext context, IAwaitable<object> result)
        {
            /* Method to confirm whether user wants to download or view data
            * Calls DisplayChangePreferences again in case the user wants to amend any other preferences */
            Activity activity = await result as Activity;
            string text = activity.Text.Trim();
            await ConversationStarter.CheckLanguage(text, ConversationStarter.user._id);
            string language = ConversationStarter.user.PreferedLang;
            var cosmosID = ConversationStarter.user._id;
            UserDataCollection user = UserDataCollection.Find(x => x._id == cosmosID).FirstOrDefault();
            switch (text.ToLower())
            {
                case "go back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "exit":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "back":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "الرجوع":
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "view data":
                    await ShowData(context, language);
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "عرض البيانات":
                    await ShowData(context, language);
                    await context.Forward(new DisplayChangePreferences(), null, activity, CancellationToken.None);
                    break;
                case "get a copy":
                    await LinkToData(context, language);
                    await context.Forward(new DisplayChangePreferences(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
                case "الحصول على نسخة":
                    await LinkToData(context, language);
                    await context.Forward(new DisplayChangePreferences(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
                default:
                    if (language == StringResources.ar)
                    {
                        ConversationStarter.user.arabicText = activity.Text;
                        activity.Text = await Translate.Translator(activity.Text, StringResources.en);
                    }
                    await context.Forward(new LuisDialog(), ResumeAfterKill, activity, CancellationToken.None);
                    break;
            }
        }

        private static async Task ShowData(IDialogContext context, String language)
        {
            UserDataCollection user = ConversationStarter.user;
            string ar = StringResources.ar;
            string subjects = "";
            string interests = "";
            string pastCourses = "";
            string gender = language == ar ? GenderList[user.gender.ToLower()] : user.gender;
            string education = language == ar ? EducationList[user.education.ToLower()] : user.education;
            string accreditation = language == ar ? AccreditationList[user.accreditation.ToLower()] : user.accreditation;
            string deliveryLanguage = language == ar ? LanguageList[user.language.ToLower()] : user.language;
            string delivery = language == ar ? DeliveryList[user.delivery.ToLower()] : user.delivery;
            string convLang = user.PreferedLang == ar ? StringResources.ar_Arabic : StringResources.en_English;
            await context.PostAsync(StringResources.ResourceManager.GetString($"{language}_UsersData"));
            //if (user.PreferedSub != null)
            //{
            //    foreach (var subject in user.PreferedSub)
            //    {
            //        subjects += subject + " ";
            //    }
            //}
            if (user.interests != null)
            {
                foreach (var interest in user.interests)
                {
                    interests += language == ar ? InterestList[interest] + " " :  interest + " ";
                }
            }
            if (user.PastCourses != null)
            {
                for(int i = 0; i < user.PastCourses.Count; i++)
                {
                    pastCourses += language == ar ? user.PastCourses[i].NameArabic : user.PastCourses[i].Name;
                }
            }
            string userInfo = String.Format(StringResources.ResourceManager.GetString($"{language}_DisplayName"), user.Name);
            userInfo += "\n\n" + String.Format(StringResources.ResourceManager.GetString($"{language}_DisplayConvLang"), convLang);
            userInfo += "\n\n" + String.Format(StringResources.ResourceManager.GetString($"{language}_DisplayGender"), gender);
            userInfo += "\n\n" + String.Format(StringResources.ResourceManager.GetString($"{language}_DisplayEducation"), education);
            //userInfo += "\n\n" + String.Format(StringResources.ResourceManager.GetString($"{language}_PreferredSubjects"), subjects);
            userInfo += "\n\n" + String.Format(StringResources.ResourceManager.GetString($"{language}_DisplayInterests"), interests);
            userInfo += "\n\n" + String.Format(StringResources.ResourceManager.GetString($"{language}_DisplayPastCourses"), pastCourses);
            userInfo += "\n\n" + String.Format(StringResources.ResourceManager.GetString($"{language}_DisplayAccreditationPref"), accreditation);
            userInfo += "\n\n" + String.Format(StringResources.ResourceManager.GetString($"{language}_DisplayCourseLangPref"), deliveryLanguage);
            userInfo += "\n\n" + String.Format(StringResources.ResourceManager.GetString($"{language}_DisplayCourseDeliveryPref"), delivery);
            userInfo += "\n\n" + String.Format(StringResources.ResourceManager.GetString($"{language}_PrivacyPolicyVersion"), user.privacy_policy_version.ToString());
            userInfo += "\n\n" + String.Format(StringResources.ResourceManager.GetString($"{language}_DisplayNotifications"), user.Notification);
            await context.PostAsync(userInfo);
        }

        private static async Task LinkToData(IDialogContext context, string language)
        {
            var message = context.MakeMessage();
            string url = ConfigurationManager.AppSettings["UserDataURL"] + UserToCrypt(language);
            Debug.WriteLine(url);
            message.Text = StringResources.ResourceManager.GetString($"{language}_DataLink");
            List<CardAction> Actions = new List<CardAction>()
            {
                new CardAction() { Title = StringResources.ResourceManager.GetString($"{language}_DataDownload"), Type = ActionTypes.OpenUrl, Value = url },
            };
            HeroCard Card = new HeroCard() { Buttons = Actions };
            DateTime timeNow = DateTime.UtcNow.ToLocalTime();
            string timeStamp = language == StringResources.ar ? timeNow.ToString("MMMM dd, yyyy", CultureInfo.CreateSpecificCulture("ar-AE")) : timeNow.ToString("MMMM dd, yyyy");
            Card.Text = string.Format(StringResources.ResourceManager.GetString($"{language}_DataTimestamp"), timeStamp);
            Attachment linkAttachment = Card.ToAttachment();
            message.Attachments.Add(linkAttachment);
            //message.SuggestedActions = new SuggestedActions() { Actions = Actions };
            await context.PostAsync(message);
        }
        
        private static string UserToCrypt(string language)
        {
            UserDataCollection temp = ConversationStarter.user;
            string userData = "";
            if (language == StringResources.ar)
            {
                string Interests = "[ ";
                for (int i = 0; i < temp.interests.Count; i++)
                {
                    Interests += InterestList[temp.interests[i]] + " ";
                }
                Interests += InterestList[temp.interests[temp.interests.Count - 1]] + "]";
                string pastCourses = "[ ";
                if (temp.PastCourses.Count > 0)
                {
                    for(int i = 0; i < temp.PastCourses.Count - 1; i++)
                    {
                        pastCourses += "'" + temp.PastCourses[i].NameArabic + "', ";
                    }
                    pastCourses += temp.PastCourses[temp.PastCourses.Count - 1].NameArabic;
                }
                pastCourses += " ]";
                userData += temp.Name + "||" + GenderList[temp.gender.ToLower()] + "||" + EducationList[temp.education.ToLower()] + "||" + StringResources.ar_Arabic + "||" + LanguageList[temp.language.ToLower()] + "||" + AccreditationList[temp.accreditation.ToLower()] + "||" + DeliveryList[temp.delivery.ToLower()] + "||";
                userData += Interests + "||" + pastCourses + "||" + temp.Notification + "||" + temp.privacy_policy_version;
            }
            else
            {
                string Interests = "[ ";
                for (int i = 0; i < temp.interests.Count-1; i++)
                {
                    Interests += temp.interests[i] + ", ";
                }
                Interests += temp.interests[temp.interests.Count - 1]+"]";
                string pastCourses = "[ ";
                if (temp.PastCourses.Count > 0)
                {
                    for (int i = 0; i < temp.PastCourses.Count-1; i++)
                    {
                        pastCourses += "'" + temp.PastCourses[i].Name + "', ";
                    }
                    pastCourses += temp.PastCourses[temp.PastCourses.Count - 1].Name;
                }
                pastCourses += " ]";
                userData += temp.Name + "||" + temp.gender + "||" + temp.education + "||" + StringResources.en_English + "||" + temp.language + "||" + temp.accreditation + "||" + temp.delivery + "||";
                userData += Interests + "||" + pastCourses + "||" + temp.Notification + "||" + temp.privacy_policy_version;
            }
            Aes myAes = Aes.Create();
            string encrypted = ConversationStarter.EncryptString(userData, myAes.Key, myAes.IV);
            string cryptic = encrypted;
            string key = Convert.ToBase64String(myAes.Key);
            string iv = Convert.ToBase64String(myAes.IV);
            return cryptic+ "," +key + "," + iv + "," + language;
        }
        
        private async Task ResumeAfterSubmit(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = await result as Activity;
            await context.Forward(new LearningDialog(), ResumeAfterKill, activity, CancellationToken.None);
        }

        private async Task ResumeAfterKill(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = await result as Activity;
            context.Done(true);
        }

    }
}