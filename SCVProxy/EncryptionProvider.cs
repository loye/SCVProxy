using System;

namespace SCVProxy
{
    public class EncryptionProvider
    {
        private byte[] seed;

        public EncryptionProvider(string key = null)
        {
            seed = new byte[16];
            char[] ka = String.IsNullOrEmpty(key) ? "!1@2#3$4%5^6&7*8".ToCharArray() : key.ToCharArray();
            int kl = ka.Length - 1;
            for (int i = 0; i < 16; i++)
            {
                seed[i] = (byte)((ka[i & kl] + kl) & 85);
            }
        }

        public void Encrypt(byte[] src, int startIndex = 0, int length = -1, int offset = 0)
        {
            int len = (length == -1 || length > src.Length) ? src.Length : length + startIndex;
            for (int i = startIndex + offset, os = offset; i < len; i++, os++)
            {
                int steps = (os & 7) + ((os & 8) == 0 ? -8 : 1);
                src[i] = (byte)~((src[i] + steps * seed[os & 15]) & 255);
            }
        }

        public void Decrypt(byte[] src, int startIndex = 0, int length = -1, int offset = 0)
        {
            int len = (length == -1 || length > src.Length) ? src.Length : length + startIndex;
            for (int i = startIndex + offset, os = offset; i < len; i++, os++)
            {
                int steps = (os & 7) + ((os & 8) == 0 ? -8 : 1);
                src[i] = (byte)((~src[i] - steps * seed[os & 15]) & 255);
            }
        }
    }
}
