using DSharpPlus.Entities;

namespace Aerochat.Services
{
    public interface IDiscordApi
    {
        Task<DiscordMessage> SendMessageAsync(ulong channelId, DiscordMessageBuilder builder);
    }

}
