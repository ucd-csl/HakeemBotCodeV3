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
using NetHope.Speech;
using NetHope.TrackConversation;
using Autofac;
using Microsoft.Bot.Builder.Dialogs.Internals;
using System.Configuration;
using Json;
using Newtonsoft.Json;

namespace NetHope.Controllers
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        public static Dictionary<string, ConversationThread> userConversations = new Dictionary<string, ConversationThread>(); // This stores messages in a local stack, not a DB

        public string appID = ConfigurationManager.AppSettings["MicrosoftAppId"];
        public string appPassword = ConfigurationManager.AppSettings["MicrosoftAppPassword"];
        // This is where each new connection comes in, whether its a message or a connection
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            using (ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl), new MicrosoftAppCredentials(appID, appPassword)))
            
            //if it is a message sent by the user it is sent to root here
            //First check various conditions which will dictate whether hakeem welcomes the use back or continues as usual
            if (activity.Type == ActivityTypes.Message)
            {
                string conversationId = activity.Conversation.Id;
                string channel = activity.ChannelId;
                var url = HttpContext.Current.Request.Url;
                

                if (!SaveConversationData.CheckUserExists(activity.From.Id))
                {
                    //await SaveConversationData.SaveBasicConversationAsync(activity.From.Id, activity.ServiceUrl, activity.ChannelId, activity.Conversation.Id, true);
                    await Conversation.SendAsync(activity, () => new ExceptionHandlerDialog<object>(new ConversationStarter(), displayException: false));
                }  
                //check if conversation is stale
                //else if (activity.Type == ActivityTypes.Message && SaveConversationData.GetLastMessage(activity.Conversation.Id).AddMinutes(1) < DateTime.Now.ToUniversalTime())
                //{
                //    Debug.WriteLine("stale");
                //    await SaveConversationData.UpdateConvTime(conversationId);
                //    await Conversation.SendAsync(activity, () => new LearningDialog());
                //}
                //check if the user has previously ended the conversation using the end conversation command
                //else if (!SaveConversationData.CheckConvoStatus(conversationId))
                //{

                //    await SaveConversationData.RestartConversation(conversationId);
                //    await Conversation.SendAsync(activity, () => new ConversationStarter());
                //}

                //if none of these conditions pass, send to root dialog as normal
                else
                {
                    ConversationReference cookie = new ConversationReference
                    {
                        ActivityId = activity.From.Id,
                        Bot = activity.Recipient,
                        ChannelId = activity.ChannelId,
                        ServiceUrl = activity.ServiceUrl,
                        User = activity.From,
                        Conversation = activity.Conversation
                    };
                    await SaveConversationData.SaveCookieAsync(activity.From.Id, cookie);
                    await SaveConversationData.UpdateLastActive(activity.From.Id, DateTime.Now.ToUniversalTime());
                    if (!userConversations.ContainsKey(conversationId))
                    {
                        userConversations[conversationId] = new ConversationThread(conversationId);
                    }
                        //<add_id_to_dictionary>

                        //<audio_to_text> if audio clip sent, extract it and then parse it as text
                        var audioAttachment = activity.Attachments?.FirstOrDefault(a => a.ContentType.Equals("audio/wav") || a.ContentType.Equals("audio/x-m4a") || a.ContentType.Equals("audio") || a.ContentType.Equals("audio/mp3") || a.ContentType.Equals("audio/mp4"));

                        if (audioAttachment != null)
                        {
                            try
                            {
                                string responseFromAPI = await SpeechToTextAuth.DealWithAttachment(audioAttachment, connector, conversationId); // get the text from attachment
                                activity.Text = responseFromAPI.Replace(".", string.Empty); //update the user text input from null to the audio text
                                await connector.Conversations.ReplyToActivityAsync(activity.CreateReply(activity.Text)); // print the text
                            }
                            catch (Exception e)
                            {
                                await connector.Conversations.ReplyToActivityAsync(activity.CreateReply(e.ToString())); // print the error
                            }
                            userConversations[conversationId].UserMessage("AUDIO"); // if the clip was audio we save audio to the conversation stack
                                                                                    //TODO update the conversation stack to store activities not strings, would allow for the replaying of audioclips
                                                                                    //</audio_to_text>
                        }
                        else
                        {
                            userConversations[conversationId].UserMessage(activity.Text); // if the input was just text, we add that text to the conversation stack
                    }
                    await Conversation.SendAsync(activity, () => new ExceptionHandlerDialog<object>(new ConversationStarter(), displayException: false)); // pass the typed text (or the parsed text) to the main dialog
                }
            }
            else//if it is not a message it is sent to the message handlers here
            {
                await HandleSystemMessage(activity); // input is not the type message
            }
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        //if the system message isnt a message we handle it here
        private async Task HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData) // not implemented
            {
            }
            //handle addition and deletion of contacts here            
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                IContactRelationUpdateActivity contactupdate = message;
                //handle using deleting bot from contacts
                if (contactupdate.Action.Equals("remove"))
                {
                    //await SaveConversationData.DeleteConvoData(message.From.Id);
                    //await SaveConversationData.DeleteSavedMessagesData(message.Conversation.Id);
                    await SaveConversationData.DeleteUserData(ConversationStarter.user._id);

                }
            }
            //if a new user adds the bot as a contact we say hello here
            else if (message.Type == ActivityTypes.ConversationUpdate || message.Type == ActivityTypes.ContactRelationUpdate)
            {
                Activity activity = message;

                //await SaveConversationData.SaveConversationAsync(activity.From.Id, activity.From.Name, // feature to update the latest interaction from a user
                //activity.Recipient.Id, activity.Recipient.Name, activity.ServiceUrl, activity.ChannelId, activity.Conversation.Id, true);
                
                //await SaveConversationData.SaveBasicConversationAsync(activity.From.Id, activity.ServiceUrl, activity.ChannelId, activity.Conversation.Id, true);

                using (ConnectorClient client = new ConnectorClient(new Uri(message.ServiceUrl), new MicrosoftAppCredentials(appID, appPassword)))

                    //if (!SaveConversationData.CheckUserExists(message.From.Id))
                    if (message.MembersAdded.Any(o => o.Id == message.Recipient.Id))
                    {
                        await Conversation.SendAsync(message, () => new ConversationStarter());
                    }
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate) // not implemented
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing) // not implemented
            {
                // Handle knowing that the user is typing
            }
        }
    }
}