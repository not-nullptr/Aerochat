using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Aerovoice.Clients;
public class UDPClient
{
    private UdpClient _client;
    private IPEndPoint _remoteEndPoint;
    private CancellationTokenSource _cancellationTokenSource;

    // Event triggered when a message is received
    public event EventHandler<byte[]>? MessageReceived;

    public UDPClient(string ipAddress, int port)
    {
        _client = new UdpClient();
        _client.Connect(ipAddress, port);
        _remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);

        _cancellationTokenSource = new CancellationTokenSource();
        // Start listening automatically in a background task
        StartListeningInBackground();
    }

    // Send a message to the remote endpoint
    public void SendMessage(byte[] data)
    {
        _client.Send(data, data.Length);
    }

    // Start listening for incoming messages in a background task
    private void StartListeningInBackground()
    {
        Task.Run(async () =>
        {
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var receivedResult = await _client.ReceiveAsync();
                    OnMessageReceived(receivedResult.Buffer);
                }
            }
            catch (ObjectDisposedException)
            {
                // The client has been disposed, stop listening
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving message: {ex.Message}");
            }
        });
    }

    // Method to trigger the MessageReceived event
    protected virtual void OnMessageReceived(byte[] message)
    {
        MessageReceived?.Invoke(this, message);
    }

    // Stop the listener and close the UDP client
    public void Close()
    {
        _cancellationTokenSource.Cancel();
        _client.Close();
    }
}
