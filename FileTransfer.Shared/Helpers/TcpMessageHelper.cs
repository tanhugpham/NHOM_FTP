using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Shared.Helpers
{
    public static class TcpMessageHelper
    {
        public static async Task SendStringAsync(
            Stream stream,
            string message)
        {
            byte[] messageBytes =
                Encoding.UTF8.GetBytes(message);

            byte[] lengthBytes =
                BitConverter.GetBytes(messageBytes.Length);

            await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);

            await stream.WriteAsync(messageBytes, 0, messageBytes.Length);

            await stream.FlushAsync();
        }

        public static async Task<string> ReadStringAsync(
            Stream stream)
        {
            byte[] lengthBytes =
                await ReadExactAsync(stream, 4);

            int messageLength =
                BitConverter.ToInt32(lengthBytes, 0);

            if (messageLength <= 0)
            {
                throw new Exception("Invalid message length");
            }

            byte[] messageBytes =
                await ReadExactAsync(stream, messageLength);

            return Encoding.UTF8.GetString(messageBytes);
        }

        private static async Task<byte[]> ReadExactAsync(
            Stream stream,
            int length)
        {
            byte[] buffer =
                new byte[length];

            int totalRead = 0;

            while (totalRead < length)
            {
                int bytesRead =
                    await stream.ReadAsync(
                        buffer,
                        totalRead,
                        length - totalRead);

                if (bytesRead == 0)
                {
                    throw new IOException("Connection closed");
                }

                totalRead += bytesRead;
            }

            return buffer;
        }
    }
}