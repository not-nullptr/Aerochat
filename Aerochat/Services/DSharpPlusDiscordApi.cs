using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;

namespace Aerochat.Services
{
    public interface IDiscordApi
    {
        Task<DiscordMessage> SendMessageAsync(ulong channelId, DiscordMessageBuilder builder);
        Task TriggerTypingAsync(ulong channelId);
        Task DeleteMessageAsync(ulong channelId, ulong messageId, DiscordMessage discordMessage);
        Task<DiscordChannel> GetChannelAsync(ulong channelId);
        Task<DiscordGuild> GetGuildAsync(ulong guildId);
    }

    public class DiscordUnauthorizedException : Exception
    {
        public DiscordUnauthorizedException(string? message = null) : base(message) { }
    }

    public sealed class DSharpPlusDiscordApi : IDiscordApi
    {
        private readonly DiscordClient _client;

        public DSharpPlusDiscordApi(DiscordClient client) => _client = client;

        public async Task<DiscordMessage> SendMessageAsync(ulong channelId, DiscordMessageBuilder builder)
        {
            try
            {
                var channel = await _client.GetChannelAsync(channelId).ConfigureAwait(false);
                return await channel.SendMessageAsync(builder).ConfigureAwait(false);
            }
            catch (UnauthorizedException ex)
            {
                throw new DiscordUnauthorizedException(ex.Message);
            }
        }

        public async Task TriggerTypingAsync(ulong channelId)
        {
            try
            {
                var channel = await _client.GetChannelAsync(channelId).ConfigureAwait(false);
                await channel.TriggerTypingAsync().ConfigureAwait(false);
            }
            catch (UnauthorizedException ex)
            {
                throw new DiscordUnauthorizedException(ex.Message);
            }
        }

        public async Task DeleteMessageAsync(ulong channelId, ulong messageId, DiscordMessage discordMessage)
        {
            try
            {
                var channel = await _client.GetChannelAsync(channelId).ConfigureAwait(false);
                await channel.DeleteMessageAsync(discordMessage).ConfigureAwait(false);
            }
            catch (UnauthorizedException ex)
            {
                throw new DiscordUnauthorizedException(ex.Message);
            }
        }

        public Task<DiscordChannel> GetChannelAsync(ulong channelId)
            => _client.GetChannelAsync(channelId);

        public Task<DiscordGuild> GetGuildAsync(ulong guildId)
            => _client.GetGuildAsync(guildId);
    }
}
