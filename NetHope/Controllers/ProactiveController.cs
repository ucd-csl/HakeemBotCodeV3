using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web;
using System.Diagnostics;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using NetHope.Dialogs;
using NetHope.ProactiveMessage;
using NetHope.SupportClasses;
using System.Configuration;
using Newtonsoft.Json;
using NetHope.Resources;

namespace NetHope.Controllers
{
    //This controller exists just to prove that we can trigger that proactive conversation even from compeltely outside the bot's code
    //Let's say you have a web service or some backend system that needs to trigger it, this is how you would do that
    //[BotAuthentication]
    public class CustomWebAPIController : ApiController
    {
        public string appID = ConfigurationManager.AppSettings["MicrosoftAppId"];
        public string appPassword = ConfigurationManager.AppSettings["MicrosoftAppPassword"];

        [Route("api/ProactiveAPI")]
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            UserDataCollection user = await SaveConversationData.GetUserDataCollection(activity.From.Id);
            ConversationReference reference = user.conversationReference;
            ConnectorClient connector = new ConnectorClient(new Uri(reference.ServiceUrl), new MicrosoftAppCredentials(appID, appPassword));
            string language = user.PreferedLang.ToLower();
            string Failed = StringResources.ResourceManager.GetString($"{language}_FailedProactive");
            string Topics = StringResources.ResourceManager.GetString($"{language}_Topics");
            string Recommend  = StringResources.ResourceManager.GetString($"{language}_Recommendation");
            string StartOver = StringResources.ResourceManager.GetString($"{language}_StartOver");
            string WhatToLearn = StringResources.ResourceManager.GetString($"{language}_WhatShouldILearn");
            string NewCourse = StringResources.ResourceManager.GetString($"{language}_NewCourseFound");
            string WantToLearn = StringResources.ResourceManager.GetString($"{language}_IWantToLearnTopic");
            string ShowMe = StringResources.ResourceManager.GetString($"{language}_ShowMe");
            string ShowCourse = StringResources.ResourceManager.GetString($"{language}_DoYouWantToSeeCourses");
            Address address = new Address(reference.Bot.Id, reference.ChannelId, reference.User.Id, reference.Conversation.Id, reference.ServiceUrl);
            ResumptionCookie resumptionCookie = new ResumptionCookie(address, reference.User.Name, false, activity.Locale);
            var message = (Activity)resumptionCookie.GetMessage();
            var reply = message.CreateReply();
            if (activity.Text == StringResources.en_Fail)
            {
                reply.Text = String.Format(Failed, user.Notification);
                List<CardAction> course = new List<CardAction>();
                course.Add(new CardAction() { Title = Topics, Type = ActionTypes.ImBack, Value = Topics });
                course.Add(new CardAction() { Title = Recommend, Type = ActionTypes.ImBack, Value = WhatToLearn });
                course.Add(new CardAction() { Title = StartOver, Type = ActionTypes.ImBack, Value = StartOver });
                reply.SuggestedActions = new SuggestedActions() { Actions = course };
            }
            else
            {
                string[] topics = activity.Text.Split('$');
                string topic = language == StringResources.ar ? topics[3] : topics[2];
                string subTopic = language == StringResources.ar ? topics[1] : topics[0];
                await connector.Conversations.ReplyToActivityAsync(message.CreateReply(string.Format(NewCourse,subTopic)));
                reply.Text = string.Format(ShowCourse, subTopic, topic);
                List<CardAction> course = new List<CardAction>();
                course.Add(new CardAction() { Title = string.Format(ShowMe, subTopic), Type = ActionTypes.ImBack, Value = string.Format(WantToLearn,subTopic) });
                course.Add(new CardAction() { Title = StartOver, Type = ActionTypes.ImBack, Value = StartOver });
                reply.SuggestedActions = new SuggestedActions() { Actions = course };
            }
            await connector.Conversations.ReplyToActivityAsync(reply);
            
            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}