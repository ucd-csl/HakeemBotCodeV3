using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using NAudio.Wave;
using NetHope.Resources;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace NetHope.Speech
{
    public class SpeechToTextAuth
    {
        public async static Task<string> DealWithAttachment(Attachment attachment, ConnectorClient connector, string conversationId)
        {
            string responseFromAPI;
            Stream stream = await DealWithAudio(connector, attachment); // get a stream from whatever clip is sent
            byte[] bytes = StreamToBytes(stream);
            MemoryStream ms = new MemoryStream(bytes); // convert the stream to bytes and back to memory stream so its an object with length

            if (attachment.ContentType.Equals("audio/wav")) // this is the filetype the API accepts so needs no modification
            {
                responseFromAPI = await SpeechToText.Speech(ms, conversationId);
            }
            else if (attachment.ContentType.Equals("audio") || attachment.ContentType.Equals("audio/x-m4a") || attachment.ContentType.Equals("audio/mp4") || attachment.ContentType.Equals("audio/mp3"))
            {
                string fullPath = await StreamToFileName(ms);
                responseFromAPI = await SpeechToText.Speech(fullPath, conversationId); // pass the file to the API calling function
                File.Delete(fullPath); // delete the file in memory to clear space
            }
            else
            {
                responseFromAPI = StringResources.en_Error;
            }

            return responseFromAPI;

        }

        public static async Task<Stream> DealWithAudio(ConnectorClient connector, Attachment audioAttachment)
        {
            using (var httpClient = new HttpClient())
            {
                var uri = new Uri(audioAttachment.ContentUrl);

                if ((uri.Host.EndsWith("skype.com") || uri.Host.EndsWith("trafficmanager.net")) && uri.Scheme == "https")
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(connector));
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
                }

                return await httpClient.GetStreamAsync(uri);
            }
        }

        /// <summary>
        /// Gets the JwT token of the bot. 
        /// </summary>
        /// <param name="connector"></param>
        /// <returns>JwT token of the bot</returns>
        public static async Task<string> GetTokenAsync(ConnectorClient connector)
        {
            var credentials = connector.Credentials as MicrosoftAppCredentials;
            if (credentials != null)
            {
                return await credentials.GetTokenAsync();
            }

            return null;
        }

        /// Converts Stream into byte[].
        /// <param name="input">Input stream</param>
        /// <returns>Output byte[]</returns>
        public static byte[] StreamToBytes(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public async static Task<string> StreamToFileName(MemoryStream stream)
        {
            try
            {
                string fileName = Guid.NewGuid().ToString(); // randomly generate a unique filename to use temporarily
                string fullPath = $@"{Path.GetTempPath()}\{fileName}.wav";

                using (MediaFoundationReader reader = new StreamMediaFoundationReader(stream))
                using (ResamplerDmoStream resampledReader = new ResamplerDmoStream(reader,
                                new WaveFormat(reader.WaveFormat.SampleRate,
                                reader.WaveFormat.BitsPerSample,
                                reader.WaveFormat.Channels))) // resample the file to PCM with same sample rate, channels and bits per sample

                using (WaveFileWriter waveWriter = new WaveFileWriter(fullPath, resampledReader.WaveFormat)) // create WAVe file
                {
                    resampledReader.CopyTo(waveWriter); // copy wave stream to wave file
                    return fullPath;

                }

            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

    }
}