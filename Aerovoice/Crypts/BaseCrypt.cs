using Aerovoice.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerovoice.Crypts
{
    public abstract class BaseCrypt
    {
        public BaseCrypt()
        {
            Logger.Log($"Encryption initialized! (\"{PName}\")");
        }

        public static string Name { get; } = "";
        public string PName => (string)GetType().GetProperty("Name")!.GetValue(null)!;

        public abstract byte[] Decrypt(byte[] data, byte[] key);
        public abstract byte[] Encrypt(byte[] data, byte[] key);
    }
}
