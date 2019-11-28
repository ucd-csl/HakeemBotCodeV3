using System;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NetHope.Resources;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NetHope.SupportClasses
{
    class Translate
    {
        //Global Variables for each function
        static string host = ConfigurationManager.AppSettings["TranslateHost"];
        static string key = ConfigurationManager.AppSettings["TranslateKey"];

        //translate
        static string transPath = StringResources.translatePath;
        //static string params_ = "&to=ar";
        static string transUri = host + transPath; // + params_;

        //detect
        static string detectPath = StringResources.detectPath;
        static string detectUri = host + detectPath;

        /*Function responsible for translating any messages sent in any language other than English             
         Currently only used to translate to Classic Arabic but could be expanded to include other languages
         */
        public async static Task<string> Translator(string text, string language)
        {
            // Takes in the language we want the text translated to and then calls detect to figure out which language we need translating from
            Object[] body = new Object[] { new { Text = text } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(transUri + "&to=" + language);//combine URI with the context language for translation

                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                //var result = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(responseBody), Formatting.Indented);

                var result = JArray.Parse(responseBody)[0]["translations"][0]["text"];//returns JSON text, relevant value is extracted here

                return result.ToString();
            }
        }

        /*Function to detect the language the message was sent in
         Uses same key and api as translator function above
         Useful for when user changes language mid conversation- Hakeem can then respond in same language
         Almost identical to translator function in terms of structure*/
        public async static Task<string> Detect(string text)
        {
            System.Object[] body = new System.Object[] { new { Text = text } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(detectUri);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JArray.Parse(responseBody)[0]["language"];
                return result.ToString();
            }
        }
    }
}
