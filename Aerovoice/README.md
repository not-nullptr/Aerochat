# Aerovoice
An experimental "extension" of DSharpPlus made for Aerochat which adds voice chat support.

## Why?
The current DSharpPlus implementation is broken. That's the only reason why. This not only implements what DSharpPlus already had, but it also has support for all (!!) known encryption methods Discord supports, which DSharpPlus does not (which is an issue since Discord are dropping support for the older ones DSharpPlus use in November)

## How do I use this?
This is a library, not a program. This is the very simple C# program I've been using to test:
```cs
using DSharpPlus;
using Aerovoice.Clients;
using DSharpPlus.Entities;

var client = new DiscordClient(new DiscordConfiguration
{
    Token = "!!!!!!!!YOUR TOKEN GOES HERE!!!!!!!!",
    TokenType = TokenType.User,
    LogUnknownEvents = false,
    MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.None
});

await client.ConnectAsync();

client.Ready += Client_Ready;

async Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
{
    var voiceSocket = new VoiceSocket(client);
    ulong channelId = !!!!!!!!YOUR CHANNEL ID GOES HERE!!!!!!!!;
    var channel = await client.GetChannelAsync(channelId);
    await voiceSocket.ConnectAsync(channel);
}

Console.ReadLine();
```
