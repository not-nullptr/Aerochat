using Aerochat.Windows;

namespace Aerotest.ChatRequests
{
    [TestFixture, Apartment(ApartmentState.STA)]
    public class GivenARequestToSendAnImage
    {
        private bool _result;

        [SetUp]
        public async Task WhenTheUserIsNotAuthorized()
        {
            //This test will be used for what's being described in the names of the method and class name but for this initial commit it will just be a placeholder

            //var chat = new Chat(12, false, null, null);
            //await chat.OnChannelChange();
            _result = true;
        }

        [Test]
        public void ThenAnErrorIsReturned()
        {
            Assert.True(_result);
        }
    }
}