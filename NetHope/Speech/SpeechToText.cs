using Microsoft.Bot.Connector;
//using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using NetHope.Controllers;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Bot.Builder.Dialogs;
using NetHope.ProactiveMessage;
using System.Diagnostics;
using System.Configuration;

namespace NetHope.Speech
{
    public class SpeechToText
    {
        private static readonly string requestUri = @""+ ConfigurationManager.AppSettings.Get("SpeechToTextUrl"); // API URL
        private static HttpWebRequest request;
        private static string SpeechTranslationKey = ConfigurationManager.AppSettings.Get("SpeechTranslationKey");
        /// <summary>
        /// Takes in a WAV stream when a WAV file is passed through to the bot as an attachment
        /// </summary>
        /// <param name ="stream">The stream of the WAV audio file</param>
        /// <returns>the parsed text of the audio file</returns>
        public async static Task<string> Speech(Stream stream, string userId)
        {
            PopulateRequest(userId); // call the static method to populate the request headers

            /*
            * Open a request stream and write 1024 byte chunks in the stream one at a time.
            */
            byte[] buffer = null;
            int bytesRead = 0;


            using (Stream requestStream = request.GetRequestStream())
            {
                buffer = new Byte[checked((uint)Math.Min(1024, stream.Length))];

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    requestStream.Write(buffer, 0, bytesRead);
                }
                requestStream.Flush();
            }

            return ParseResponse(request);
        }

        /// <summary>
        /// Takes in the location of the file storing a WAV file when a M4A file was passed through to the bot
        /// </summary>
        /// <param name="FName"> the filename of a file that was created to convert a M4A file to a WAV</param>
        /// <returns>The text parsed from the audio file </returns>
        public async static Task<string> Speech(string FName, string userId)
        {
            PopulateRequest(userId); // call the static method to populate the request headers

            using (FileStream fs = new FileStream(FName, FileMode.Open, FileAccess.Read))
            {

                /*
                * Open a request stream and write 1024 byte chunks in the stream one at a time.
                */
                byte[] buffer = null;
                int bytesRead = 0;
                using (Stream requestStream = request.GetRequestStream())
                {
                    /*
                    * Read 1024 raw bytes from the input audio file.
                    */
                    buffer = new Byte[checked((uint)Math.Min(1024, (int)fs.Length))];
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        requestStream.Write(buffer, 0, bytesRead);
                    }

                    // Flush
                    requestStream.Flush();
                }
            }
            return ParseResponse(request);
        }

        /// <summary>
        /// Method to populate all the headers of the HttpRequest
        /// </summary>
        private static void PopulateRequest(string userId)
        {
            request = (HttpWebRequest)WebRequest.Create(requestUri + SaveConversationData.GetLanguageForAPI(userId));
            request.SendChunked = true;
            request.Accept = @"application/json;text/xml";
            request.Method = "POST";
            request.ProtocolVersion = HttpVersion.Version11;
            request.ContentType = @"audio; codec=audio/pcm; samplerate=16000";
            request.Headers["Ocp-Apim-Subscription-Key"] = SpeechTranslationKey;
        }

        /// <summary>
        /// Takes the request and interprets the response and parses it  
        /// </summary>
        /// <param name="request">the request to be parsed</param>
        /// <returns>the string parsed from the request</returns>
        private static string ParseResponse(HttpWebRequest request)
        {

            string responseString;
            using (WebResponse response = request.GetResponse())
            using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                responseString = sr.ReadToEnd();

            //return responseString; // this is the string of the enture json returned from the API

            var responseJson = (JObject)JsonConvert.DeserializeObject(responseString);
            return responseJson["DisplayText"].Value<string>().ToLower();
        }
    }
}