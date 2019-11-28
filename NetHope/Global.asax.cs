
using System.Web.Http;
using System.Net;
using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Builder.Azure;
using System.Configuration;
using System.Reflection;
using Microsoft.Bot.Builder.Dialogs.Internals;
using System;
using Microsoft.Bot.Connector;
using Autofac.Integration.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using NetHope.SupportClasses;

namespace NetHope
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        CancellationTokenSource _getTokenAsyncCancellation = new CancellationTokenSource();

        protected void Application_Start(object sender, EventArgs e)
        {
            if (ServicePointManager.SecurityProtocol.HasFlag(SecurityProtocolType.Tls12) == false)
            {
                ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol | SecurityProtocolType.Tls12;
            }
            
            Conversation.UpdateContainer(
                builder =>
                {
                    builder.RegisterModule(new AzureModule(Assembly.GetExecutingAssembly()));
                    builder.RegisterModule(new DefaultExceptionMessageOverrideModule());
                    var uri = new Uri(ConfigurationManager.AppSettings["DocumentDbUrl"]);
                    var key = ConfigurationManager.AppSettings["DocumentDbKey"];
                    var store = new DocumentDbBotDataStore(uri, key);
                    builder.Register(c => store)
                        .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                        .AsSelf()
                        .SingleInstance();
       
                });
            GlobalConfiguration.Configure(WebApiConfig.Register);
            this.RegisterBotModules();

            var appID = ConfigurationManager.AppSettings["MicrosoftAppId"];
            var appPassword = ConfigurationManager.AppSettings["MicrosoftAppPassword"];
            if (!string.IsNullOrEmpty(appID) && !string.IsNullOrEmpty(appPassword))
            {
                var credentials = new MicrosoftAppCredentials(appID, appPassword);
                Task.Factory.StartNew(async () =>
                {
                    while (!_getTokenAsyncCancellation.IsCancellationRequested)
                    {
                        try
                        {
                            var token = await credentials.GetTokenAsync().ConfigureAwait(false);
                        }
                        catch (MicrosoftAppCredentials.OAuthException ex)
                        {
                            Trace.TraceError(ex.ToString());
                        }

                        await Task.Delay(TimeSpan.FromMinutes(30), _getTokenAsyncCancellation.Token).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }
        }

        private void RegisterBotModules()
        {
            Conversation.UpdateContainer(builder =>
            {
                builder.RegisterModule(new ReflectionSurrogateModule());
                builder.RegisterModule(new DefaultExceptionMessageOverrideModule());
            });
        }

        protected void Application_End()
        {
            _getTokenAsyncCancellation.Cancel();
        }
    }
}