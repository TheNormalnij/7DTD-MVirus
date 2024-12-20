using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MVirus.src.Shared
{
    [ComVisible(true)]
    public abstract class HashAlgorithmAsync : HashAlgorithm
    {

        public async Task<byte[]> ComputeHashAsync(Stream inputStream)
        {
            //if (m_bDisposed)
            //    throw new ObjectDisposedException(null);

            byte[] buffer = new byte[2 * 1024 * 1024];
            int num;
            do
            {
                num = await inputStream.ReadAsync(buffer, 0, 2 * 1024 * 1024);
                if (num > 0)
                {
                    HashCore(buffer, 0, num);
                }
            }
            while (num > 0);
            HashValue = HashFinal();
            byte[] result = (byte[])HashValue.Clone();
            Initialize();
            return result;
        }

    }
}
