using Aerochat.Hoarder;
using Aerochat.Services;
using DSharpPlus.Entities;
using Moq;

namespace Aerotest.MessageRequests
{
    public class AndTheUserIsUnauthorized
    {
        private SendResult _result;

        [SetUp]
        public async Task Setup()
        {
            var api = new Mock<IDiscordApi>();
            api.Setup(a => a.SendMessageAsync(It.IsAny<ulong>(), It.IsAny<DiscordMessageBuilder>()))
                .ThrowsAsync(new DiscordUnauthorizedException("401"));

            var chatService = new ChatService(Discord.Client, api.Object);

            _result = await chatService.SendAsync(123UL, new DiscordMessageBuilder().WithContent("hi"));
        }

        [Test]
        public void ThenAnErrorIsHandledGracefully()
        {
            Assert.False(_result.Success);
            Assert.IsNull(_result.Message);
            Assert.That(_result.ErrorCode, Is.EqualTo("Unauthorized"));
        }
    }
}