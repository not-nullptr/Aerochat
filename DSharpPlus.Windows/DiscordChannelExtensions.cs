using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Net.Abstractions;
using DSharpPlus.Net.Serialization;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace DSharpPlus.Windows
{
    public static class DiscordChannelExtensions
    {
        public static async Task SendFilesWithProgressAsync(this DiscordChannel channel, HttpClient httpClient, string message, IEnumerable<IMention> mentions, DiscordMessage replyTo, Dictionary<string, IInputStream> files, IProgress<double?> progress)
        {
            var progress2 = new Progress<HttpProgress>(e =>
            {
                if (e.TotalBytesToSend != null)
                    progress.Report((e.BytesSent / (double)e.TotalBytesToSend) * 100);
            });

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri($"https://discordapp.com/api/v8/channels/{channel.Id}/messages"));
            httpRequestMessage.Headers.Add("Authorization", DSharpPlus.Utilities.GetFormattedToken(channel.Discord));

            var cont = new HttpMultipartFormDataContent();
            var pld = new RestChannelMessageCreatePayload
            {
                HasContent = !string.IsNullOrWhiteSpace(message),
                Content = message
            };

            if (mentions != null)
                pld.Mentions = new DiscordMentions(mentions);

            if (replyTo != null)
                pld.MessageReference = new InternalDiscordMessageReference() { MessageId = replyTo.Id };

            cont.Add(new HttpStringContent(DiscordJson.SerializeObject(pld)), "payload_json");

            for (var i = 0; i < files.Count; i++)
            {
                var file = files.ElementAt(i);
                cont.Add(new HttpStreamContent(file.Value), $"file{i}", file.Key);
            }

            httpRequestMessage.Content = cont;

            await httpClient.SendRequestAsync(httpRequestMessage).AsTask(progress2);
        }
    }
}
