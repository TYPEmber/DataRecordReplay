using System;
using System.IO;
using System.IO.Compression;

namespace EDCoder
{
    public static class Encoder
    {
        public static void GetBytes(Package pkg, ref byte[] bytes, ref int length)
        {

        }

        private static int Compress(byte[] data, ref byte[] cdata, ref int clength)
        {
            using (MemoryStream msWrite = new MemoryStream(cdata))
            {
                //因为在写入的时候要压缩后写入，所以需要创建压缩流来写入(因此在压缩写入前需要先创建写入流)
                //压缩的时候就是要将压缩好的数据写入到指定流中，通过fsWrite写入到新的路径下
                using (GZipStream zip = new GZipStream(msWrite, CompressionLevel.Optimal, true))
                {
                    zip.Write(data, 0, data.Length);
                }

                // 每个 pkg 压缩后长度不得超过 int.MaxValue
                if (msWrite.Position > int.MaxValue)
                {
                    throw new Exception("Too Much Data!: msWrite.Position > int.MaxValue");
                }
                else
                {
                    return (int)msWrite.Position;
                }
            }
        }
    }
}
