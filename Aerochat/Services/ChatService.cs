using DSharpPlus.Entities;
using DSharpPlus;
using Google.Protobuf.WellKnownTypes;

namespace Aerochat.Services
{
    public interface IChatService
    {
        Task<DiscordChannel> GetChannelAsync(ulong channelId);
        Task<DiscordUser> GetCurrentUser();
        bool TryGetCachedUser(ulong id, out DiscordUser user);
        Task<DiscordProfile> GetUserProfileAsync(ulong userId, bool updateCache = false);
        Task<IReadOnlyList<DiscordMessage>> GetMessagesAsync(DiscordChannel newChannel);
        Task<SendResult> SendAsync(ulong channelId, DiscordMessageBuilder builder);
    }

    public class ChatService : IChatService
    {
        private readonly DiscordClient _discordClient;
        private readonly IDiscordApi _discordApi;
        public ChatService(DiscordClient discordClient, IDiscordApi discordDiscordApi)
        {
            _discordClient = discordClient;
            _discordApi = discordDiscordApi;
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

        public async Task<SendResult> SendAsync(ulong channelId, DiscordMessageBuilder builder)
        {
            try
            {
                var msg = await _discordApi.SendMessageAsync(channelId, builder).ConfigureAwait(false);
                return new SendResult(true, msg);
            }
            catch (DiscordUnauthorizedException ex)
            {
                return new SendResult(false, null, "Unauthorized", ex.Message);
            }
            catch (Exception ex)
            {
                return new SendResult(false, null, "Unknown", ex.Message);
            }
        }
    }
    public record SendResult(bool Success, DiscordMessage? Message, string? ErrorCode = null, string? ErrorMessage = null);

}
