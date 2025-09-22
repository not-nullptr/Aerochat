using DSharpPlus.Entities;
using DSharpPlus;
using Aerochat.Hoarder;
using System.Threading.Channels;

namespace Aerochat.Services
{
    internal interface IChatService
    {
        Task<DiscordChannel> GetChannelAsync(ulong channelId);
        Task<DiscordUser> GetCurrentUser();
        bool TryGetCachedUser(ulong id, out DiscordUser user);
        Task<DiscordProfile> GetUserProfileAsync(ulong userId, bool updateCache = false);
        Task<IReadOnlyList<DiscordMessage>> GetMessagesAsync(DiscordChannel newChannel);
    }

    internal class ChatService : IChatService
    {
        private readonly DiscordClient _discordClient;

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

        public async Task<DiscordUser> GetCurrentUser()
        {
            return _discordClient.CurrentUser;
        }
        public bool TryGetCachedUser(ulong id, out DiscordUser user)
            => _discordClient.TryGetCachedUser(id, out user);

        public async Task<DiscordProfile> GetUserProfileAsync(ulong userId, bool updateCache = false)
        {
            return await _discordClient.GetUserProfileAsync(userId, updateCache);
        }
        public Task<IReadOnlyList<DiscordMessage>> GetMessagesAsync(DiscordChannel newChannel) =>
            newChannel.GetMessagesAsync(50);

        
    }

}
