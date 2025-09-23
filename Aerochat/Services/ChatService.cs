using DSharpPlus.Entities;
using DSharpPlus;
using System.Threading.Channels;
using Aerochat.Services;
using DSharpPlus.EventArgs;
using DSharpPlus.AsyncEvents;

namespace Aerochat.Services
{
    public interface IChatService
    {
        Task<DiscordChannel> GetChannelAsync(ulong channelId);
        Task<DiscordUser> GetCurrentUser();
        bool TryGetCachedUser(ulong id, out DiscordUser user);
        bool TryGetCachedChannel(ulong id, out DiscordChannel channel);
        bool TryGetCachedGuild(ulong id, out DiscordGuild guild);
        Task<DiscordGuild> GetGuild(ulong id, bool isDM, DiscordChannel channel);
        Task SyncGuildsAsync(params DiscordGuild[] guilds);

        Task<DiscordProfile> GetUserProfileAsync(ulong userId, bool updateCache = false);
        Task<IReadOnlyList<DiscordMessage>> GetMessagesAsync(DiscordChannel channel);
        Task<SendResult> SendAsync(ulong channelId, DiscordMessageBuilder builder);
        Task TriggerTypingAsync(ulong channelId);
        Task DeleteMessageAsync(ulong channelId, ulong messageId, DiscordMessage discordMessage);

        UserStatus GetCurrentUserStatus();

        event AsyncEventHandler<DiscordClient, TypingStartEventArgs> TypingStarted;
        event AsyncEventHandler<DiscordClient, MessageCreateEventArgs> MessageCreated;
        event AsyncEventHandler<DiscordClient, MessageDeleteEventArgs> MessageDeleted;
        event AsyncEventHandler<DiscordClient, MessageUpdateEventArgs> MessageUpdated;
        event AsyncEventHandler<DiscordClient, ChannelCreateEventArgs> ChannelCreated;
        event AsyncEventHandler<DiscordClient, ChannelDeleteEventArgs> ChannelDeleted;
        event AsyncEventHandler<DiscordClient, ChannelUpdateEventArgs> ChannelUpdated;
        event AsyncEventHandler<DiscordClient, PresenceUpdateEventArgs> PresenceUpdated;
        event AsyncEventHandler<DiscordClient, VoiceStateUpdateEventArgs> VoiceStateUpdated;
    }

    public class ChatService : IChatService
    {
        private readonly DiscordClient _discordClient;
        private readonly IDiscordApi _discordApi;

        public ChatService(DiscordClient discordClient, IDiscordApi discordDiscordApi)
        {
            _discordClient = discordClient;
            _discordApi = discordDiscordApi ?? new DSharpPlusDiscordApi(discordClient);
        }

        // ===== Events (pass-through) =====
        public event AsyncEventHandler<DiscordClient, TypingStartEventArgs> TypingStarted
        {
            add { _discordClient.TypingStarted += value; }
            remove { _discordClient.TypingStarted -= value; }
        }

        public event AsyncEventHandler<DiscordClient, MessageCreateEventArgs> MessageCreated
        {
            add { _discordClient.MessageCreated += value; }
            remove { _discordClient.MessageCreated -= value; }
        }

        public event AsyncEventHandler<DiscordClient, MessageDeleteEventArgs> MessageDeleted
        {
            add { _discordClient.MessageDeleted += value; }
            remove { _discordClient.MessageDeleted -= value; }
        }

        public event AsyncEventHandler<DiscordClient, MessageUpdateEventArgs> MessageUpdated
        {
            add { _discordClient.MessageUpdated += value; }
            remove { _discordClient.MessageUpdated -= value; }
        }

        public event AsyncEventHandler<DiscordClient, ChannelCreateEventArgs> ChannelCreated
        {
            add { _discordClient.ChannelCreated += value; }
            remove { _discordClient.ChannelCreated -= value; }
        }

        public event AsyncEventHandler<DiscordClient, ChannelDeleteEventArgs> ChannelDeleted
        {
            add { _discordClient.ChannelDeleted += value; }
            remove { _discordClient.ChannelDeleted -= value; }
        }

        public event AsyncEventHandler<DiscordClient, ChannelUpdateEventArgs> ChannelUpdated
        {
            add { _discordClient.ChannelUpdated += value; }
            remove { _discordClient.ChannelUpdated -= value; }
        }

        public event AsyncEventHandler<DiscordClient, PresenceUpdateEventArgs> PresenceUpdated
        {
            add { _discordClient.PresenceUpdated += value; }
            remove { _discordClient.PresenceUpdated -= value; }
        }

        public event AsyncEventHandler<DiscordClient, VoiceStateUpdateEventArgs> VoiceStateUpdated
        {
            add { _discordClient.VoiceStateUpdated += value; }
            remove { _discordClient.VoiceStateUpdated -= value; }
        }

        // ===== Core fetch/caching =====
        public async Task<DiscordChannel> GetChannelAsync(ulong channelId)
        {
            _discordClient.TryGetCachedChannel(channelId, out DiscordChannel channel);
            return channel ?? await _discordClient.GetChannelAsync(channelId).ConfigureAwait(false);
        }

        public Task<DiscordUser> GetCurrentUser() => Task.FromResult(_discordClient.CurrentUser);

        public bool TryGetCachedUser(ulong id, out DiscordUser user)
            => _discordClient.TryGetCachedUser(id, out user);

        public bool TryGetCachedChannel(ulong id, out DiscordChannel channel)
            => _discordClient.TryGetCachedChannel(id, out channel);

        public bool TryGetCachedGuild(ulong id, out DiscordGuild guild)
            => _discordClient.TryGetCachedGuild(id, out guild);

        public async Task<DiscordGuild> GetGuild(ulong id, bool isDM, DiscordChannel channel)
        {
            _discordClient.TryGetCachedGuild(id, out var guild);
            if (guild is null && !isDM)
            {
                guild = await _discordClient.GetGuildAsync(channel.GuildId ?? 0).ConfigureAwait(false);
            }
            return guild!;
        }

        public Task SyncGuildsAsync(params DiscordGuild[] guilds)
            => _discordClient.SyncGuildsAsync(guilds);

        public Task<DiscordProfile> GetUserProfileAsync(ulong userId, bool updateCache = false)
            => _discordClient.GetUserProfileAsync(userId, updateCache);

        public Task<IReadOnlyList<DiscordMessage>> GetMessagesAsync(DiscordChannel channel)
            => channel.GetMessagesAsync(50);

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

        public Task TriggerTypingAsync(ulong channelId)
            => _discordApi.TriggerTypingAsync(channelId);

        public Task DeleteMessageAsync(ulong channelId, ulong messageId, DiscordMessage discordMessage)
            => _discordApi.DeleteMessageAsync(channelId, messageId,discordMessage);

        public UserStatus GetCurrentUserStatus()
            => _discordClient.CurrentUser?.Presence?.Status ?? UserStatus.Offline;
    }
}
public record SendResult(bool Success, DiscordMessage? Message, string? ErrorCode = null, string? ErrorMessage = null);