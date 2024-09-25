using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerovoice.Decryptors
{
    public abstract class BaseDecryptor(byte[] key)
    {
        protected byte[] _key = key;

        public static string Name { get; } = "";
        public string PName => (string)GetType().GetProperty("Name")!.GetValue(null)!;

        public abstract byte[] Decrypt(byte[] data);

        public void SetKey(byte[] key)
        {
            _key = key;
        }
    }
}
