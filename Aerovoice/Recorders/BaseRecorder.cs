using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerovoice.Recorders
{
    public abstract class BaseRecorder : IDisposable
    {
        public event EventHandler<byte[]>? DataAvailable;

        public abstract void Start();
        public abstract void Stop();

        protected void OnDataAvailable(byte[] data)
        {
            DataAvailable?.Invoke(this, data);
        }

        public abstract void Dispose();
    }
}
