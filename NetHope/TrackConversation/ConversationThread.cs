using Microsoft.Bot.Connector;
using NetHope.Speech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

/* Class that saves the context of the conversation so that it can be started back up later
 * The conversation data isn't being stored currently though 17/4/19  */

namespace NetHope.TrackConversation
{
    [Serializable]
    public class ConversationThread
    {
        private readonly string conversationId;
        private Stack<string> userString;
        private Stack<Activity> botString;

        public ConversationThread(string conversationId)
        {
            this.conversationId = conversationId;
            userString = new Stack<string>();
            botString = new Stack<Activity>();
        }

        public void UserMessage(string message)
        {
            //SaveConversationData.SaveInteraction(conversationId, message, "user"); // Not saving to DB now
            userString.Push(message);
        }

        public bool SpokeAudioLast()
        {
            return userString.Peek() == "AUDIO";
        }

        public async Task<Activity> BotMessage(Activity message)
        {
            //SaveConversationData.SaveInteraction(conversationId, message.Text, "bot"); // Not saving to DB now
            botString.Push(message);
            if (!SpokeAudioLast())
            {
                return message;
            }
            else
            {
                return await GenerateSpeech.CreateSpeech(message);
            }
        }

        public Activity LastBotMessage()
        {
            return botString.Peek();
        }

        public int TotalUtterances()
        {
            return userString.Count + botString.Count;
        }
    }
}