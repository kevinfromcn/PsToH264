using System.IO;

namespace PsToH264
{
    class Program
    {
        static void Main(string[] args)
        {
            PsToH264Util covertUtil = new PsToH264Util();

            BinaryReader reader = new BinaryReader(new FileStream("/test.ps", FileMode.Open));
            BinaryWriter writer = new BinaryWriter(new FileStream("/test.h264", FileMode.Open));

            byte[] readBuffer = new byte[1024];
            int readLenght = 0;
            while ((readLenght = reader.Read(readBuffer, 0, 1024)) > 0)
            {
                byte[] buffer;
                if (readLenght == readBuffer.Length)
                {
                    buffer = readBuffer;
                }
                else
                {
                    buffer = new byte[readLenght];
                    readBuffer.CopyTo(buffer, readLenght);
                }

                covertUtil.Write(buffer);
            }

            covertUtil.ExecuteParsing();

            writer.Write(covertUtil.Read());

            reader.Close();
            reader.Dispose();
            writer.Close();
            writer.Dispose();

            covertUtil.Dispose();
        }
    }
}
