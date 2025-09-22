using DSharpPlus.Entities;
using DSharpPlus;

namespace Aerochat.Services
{
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
            catch (DSharpPlus.Exceptions.UnauthorizedException ex)
            {
                throw new DiscordUnauthorizedException(ex.Message);
            }
          
        }
    }

}
