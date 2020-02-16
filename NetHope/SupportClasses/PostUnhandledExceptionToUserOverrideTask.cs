using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using NetHope.Resources;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Connector;
using NetHope.ProactiveMessage;

namespace NetHope.SupportClasses
{
    public class PostUnhandledExceptionToUserOverrideTask : IPostToBot
    {
        private readonly ResourceManager resources;
        private readonly IPostToBot inner;
        private readonly IBotToUser botToUser;
        private readonly TraceListener trace;

        public PostUnhandledExceptionToUserOverrideTask(IPostToBot inner, IBotToUser botToUser, ResourceManager resources, TraceListener trace)
        {
            SetField.NotNull(out this.inner, nameof(inner), inner);
            SetField.NotNull(out this.botToUser, nameof(botToUser), botToUser);
            SetField.NotNull(out this.resources, nameof(resources), resources);
            SetField.NotNull(out this.trace, nameof(trace), trace);
        }

        public async Task PostAsync(IActivity activity, CancellationToken token)
        {
            UserDataCollection user = await SaveConversationData.GetUserDataCollection(activity.From.Id);
            string language = user == null ? "" :user.PreferedLang ;
            if(language != StringResources.en && language != StringResources.ar)
            {
                language = StringResources.en;
            }
            try
            {
                await inner.PostAsync(activity, token);
            }
            catch (Exception)
            {
                try
                {
                    await botToUser.PostAsync(StringResources.ResourceManager.GetString($"{language}_BotIssue"), cancellationToken: token);
                }
                catch (Exception inner)
                {
                    trace.WriteLine(inner);
                }

                throw;
            }
        }
    }
}