using Aerochat.Windows;

namespace Aerotest.ChatRequests
{
    [TestFixture, Apartment(ApartmentState.STA)]
    public class GivenARequestToSendAnImage
    {
        [SetUp]
        public async Task WhenTheUserIsNotAuthorized()
        {
            var chat = new Chat(12, false, null, null);
            await chat.OnChannelChange();
        }

        [Test]
        public void ThenAnErrorIsReturned()
        {
            Assert.Pass();
        }
    }
}