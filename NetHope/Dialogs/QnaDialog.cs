using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.CognitiveServices.QnAMaker;
using Microsoft.Bot.Builder.Dialogs;
using System.Configuration;
using NetHope.Resources;

namespace NetHope.Dialogs
{
    [Serializable]
    public class QnaDialog
    {

        static string host = ConfigurationManager.AppSettings["CognitiveDomain"];
        static string service = StringResources.qnaMakerService;
        static string baseRoute = StringResources.qnaMakerKnowledge;
        static string endpointService = StringResources.qnaMakerPath;
        static string key = ConfigurationManager.AppSettings.Get("LuisKey");
        static string kbid = ConfigurationManager.AppSettings.Get("KnowledgeBaseId");
        static string endpoint_host = ConfigurationManager.AppSettings.Get("QnAHost");
        static string endpoint_key = ConfigurationManager.AppSettings.Get("QnAKey");
        public string question = "";
        public IDialogContext context;

        public QnaDialog(string question, IDialogContext context)
        {
            this.context = context;
            this.question = question;
        }

        static string PrettyPrint(string s)
        {
            return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(s), Formatting.Indented);
        }

        public struct Response
        {
            public HttpResponseHeaders headers;
            public string response;
            public Response(HttpResponseHeaders headers, string response)
            {
                this.headers = headers;
                this.response = response;
            }
        }

        async static Task<Response> Get(string uri)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Get;
                request.RequestUri = new Uri(uri);
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                return new Response(response.Headers, responseBody);
            }
        }
        async static Task GetKnowledgeBaseHostNameDetails()
        {
            var uri = host + service + baseRoute + "/" + kbid;
            var response = await Get(uri);

            // the endpoint key is the same endpoint key for all KBs associated with this QnA Maker service key
            var details = JsonConvert.DeserializeObject<KBDetails>(response.response);
            endpoint_host = details.hostName;
        }

        async static Task GetEndpointKeys()
        {
            // notice that this uri doesn't change based on KB ID
            var uri = host + service + StringResources.qnaMakerEndpointkeys;
            var response = await Get(uri);

            // the endpoint key is the same endpoint key for all KBs associated with this QnA Maker service key
            var fields = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.response);
            endpoint_key = fields[StringResources.qnaMakerPrimaryEndpointkey];
        }

        public async Task<string> GetAnswer(string question)
        {

            var uri = endpoint_host + endpointService + baseRoute + "/" + kbid + StringResources.qnaMakerGenerateAnswer;
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                question = question.Replace("'", "");
                request.Content = new StringContent("{question:'" + question + "'}", Encoding.UTF8, "application/json");

                // NOTE: The value of the header contains the string/text 'EndpointKey ' with the trailing space

                request.Headers.Add(StringResources.qnaMakerAuthorization,StringResources.qnaMakerEndpointKey + endpoint_key);
                var response = await client.SendAsync(request);
                string responseJson = await response.Content.ReadAsStringAsync();
                var responseBody = await response.Content.ReadAsAsync<RootObject>();
                return responseBody.answers[0].answer;
            }
        }
    }

    public class Answer
    {
        public List<string> questions { get; set; }
        public string answer { get; set; }
        public double score { get; set; }
        public int id { get; set; }
        public string source { get; set; }
        public List<object> metadata { get; set; }
    }

    public class RootObject
    {
        public List<Answer> answers { get; set; }
    }

    public class KBDetails
    {
        public string id;
        public string hostName;
        public string lastAccessedTimestamp;
        public string lastChangedTimestamp;
        public string lastPublishedTimestamp;
        public string name;
        public string userId;
        public string[] urls;
        public string[] sources;
    }
}