using DSharpPlus.Entities;
using DSharpPlus;
using Aerochat.Hoarder;
using System.Threading.Channels;

namespace Aerochat.Services
{
    internal interface IChatService
    {
        Task<DiscordChannel> GetChannelAsync(ulong channelId);
    }

    internal class ChatService : IChatService
    {
        private DiscordClient _discordClient;

        public ChatService(DiscordClient discordClient)
        {
            _discordClient = discordClient;
        }

        public async Task<DiscordChannel> GetChannelAsync(ulong channelId)
        {
            _discordClient.TryGetCachedChannel(channelId, out DiscordChannel newChannel);
            if (newChannel is null)
            {
                newChannel = await _discordClient.GetChannelAsync(channelId);
            }

            return newChannel;
        }
    }

}
