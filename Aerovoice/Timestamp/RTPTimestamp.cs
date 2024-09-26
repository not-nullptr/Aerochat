using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerovoice.Timestamp
{
    public class RTPTimestamp
    {
        private readonly int clockFrequency;
        private uint timestamp;

        public RTPTimestamp(int clockFrequency)
        {
            this.clockFrequency = clockFrequency;
            var random = new Random();
            this.timestamp = (uint)random.Next(0, int.MaxValue) * 2u + (uint)random.Next(0, 2); 
        }

        public void Increment(int samples)
        {
            timestamp += (uint)samples;
        }

        public uint GetCurrentTimestamp()
        {
            return timestamp;
        }

        public void SetCurrentTimestamp(uint newTimestamp)
        {
            timestamp = newTimestamp;
        }

        public int GetClockFrequency()
        {
            return clockFrequency;
        }
    }
}
