using NAudio.Wave;

namespace Aerovoice.Recorders {
    public class InputDevices
    {
        public static List<string> FetchInputDevices()
        {
            int waveInDevices = WaveInEvent.DeviceCount;
            List<string> devices = new List<string>();

            for (int waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                WaveInCapabilities deviceInfo = WaveInEvent.GetCapabilities(waveInDevice);
                devices.Add(deviceInfo.ProductName);
                Console.WriteLine($"Found Device {waveInDevice} of {waveInDevices}: {WaveInEvent.GetCapabilities(waveInDevice).ProductName}");
            }
            
            return devices;
        }
    }
}