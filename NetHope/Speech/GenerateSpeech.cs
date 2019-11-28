using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using NetHope.Resources;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
 
/* This class seems to be aimed at generating speech for the bot so that a user could hold a
 * verbal conversation with it. It is not being implemented and can't be sure if it actually
 * works. 17/4/19
 */

namespace NetHope.Speech
{
    public class GenerateSpeech
    {
        private static string AudioAccountKey = ConfigurationManager.AppSettings.Get("AudioAccountKey");
        private static string AudioAccountName = ConfigurationManager.AppSettings.Get("AudioAccountName");
        private static string Container = ConfigurationManager.AppSettings.Get("AudioAccountContainer");
        private static string AudioFileUrl = ConfigurationManager.AppSettings.Get("AudioFileUrl");
        private static string TextToSpeechUrl= ConfigurationManager.AppSettings.Get("TextToSpeechUrl");
        private static string SpeechToSpeechUrl = ConfigurationManager.AppSettings.Get("SpeechToSpeechUrl");
        private static string SpeechTranslationKey = ConfigurationManager.AppSettings.Get("SpeechTranslationKey");
        public static async Task<Activity> CreateSpeech(Activity activity)
        {
            Stream stream = await CreateSpeechStream(activity.Text);
            byte[] bytes = SpeechToTextAuth.StreamToBytes(stream);
            MemoryStream ms = new MemoryStream(bytes); // convert the stream to bytes and back to memory stream so its an object with length


            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName="+ AudioAccountName+";AccountKey="+AudioAccountKey+";EndpointSuffix=core.windows.net");
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(Container);
            string blobName = Guid.NewGuid().ToString() + ".wav";

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
            await blockBlob.UploadFromStreamAsync(ms);

            activity.Attachments = CreateAudioAttachment(blobName);

            return activity;
        }

        public async static Task<Stream> CreateSpeechStream(string sentence)
        {
            string accessToken;


            TextToSpeechAuth auth = new TextToSpeechAuth(SpeechToSpeechUrl, SpeechTranslationKey);

            try
            {
                accessToken = auth.GetAccessToken();
            }
            catch (Exception)
            {
                return null;
            }

            string requestUri = TextToSpeechUrl;
            var cortana = new Synthesize();

            Stream speechStream = await cortana.Speak(CancellationToken.None, new Synthesize.InputOptions()
            {
                RequestUri = new Uri(requestUri),
                // Text to be spoken.
                Text = sentence,
                VoiceType = Gender.Female,
                Locale = StringResources.en_US,
                VoiceName = StringResources.generateSpeechName,

                // Service can return audio in different output format.
                OutputFormat = AudioOutputFormat.Riff24Khz16BitMonoPcm,
                AuthorizationToken = "Bearer " + accessToken,
            });

            return speechStream;
        }

        private static IList<Attachment> CreateAudioAttachment(string filePath)
        {
            return new List<Attachment>
            {
                new AudioCard
                {
                    Media = new List<MediaUrl>
                    {
                        new MediaUrl()
                        {
                            Url = AudioFileUrl+filePath
                        }
                    },
                }.ToAttachment()
            };
        }

        public static void DeleteOldBlobs()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=" + AudioAccountName + ";AccountKey="+ AudioAccountKey +";EndpointSuffix=core.windows.net");
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(Container);
            List<CloudBlockBlob> blobs = container.ListBlobs("", true).OfType<CloudBlockBlob>().Where(b => (DateTime.UtcNow.AddSeconds(-60) > b.Properties.LastModified.Value.DateTime)).ToList();

            foreach (CloudBlockBlob blob in blobs)
            {
                blob.DeleteIfExists();
            }
        }
    }
}