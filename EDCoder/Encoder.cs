using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

using DRRCommon;

namespace EDCoder
{
    public static class Encoder
    {
        //static byte[] b1 = new byte[1024 * 1024 * 10];
        //static byte[] b2 = new byte[1024 * 1024 * 3];

        public static void GetBytes(ref Package pkg)
        {
            // 后期如果加入 pkg 的 header 与 tailer 就在此处修改
            pkg.originLength = pkg.msgsAsBytesLength;

            pkg.originBytes = SharedMemory.Rent(pkg.originLength);
            //pkg.originBytes = b1;

            int offset = 0;
            foreach (var msg in pkg.GetMessages())
            {
                CopyMsgToArray(msg, pkg.originBytes, ref offset);
            }

            // 部分短数据压缩后反而会更长
            pkg.codedBytes = SharedMemory.Rent(pkg.originLength + 100);
            //pkg.codedBytes = b2;

            Compress(pkg.originBytes, pkg.originLength, ref pkg.codedBytes, ref pkg.codedLength);
        }

        private static unsafe void CopyMsgToArray(Message msg, byte[] array, ref int offset)
        {
            fixed (byte* p = &array[0])
            {
                var intptr = new IntPtr(p);

                Marshal.StructureToPtr(msg.header, intptr + offset, true);

                offset += msg.GetHeaderLength();

                var pin = msg.bytes.Pin();
                Buffer.MemoryCopy(pin.Pointer, (intptr + offset).ToPointer(), msg.header.bLength, msg.header.bLength);
                pin.Dispose();
                //Marshal.Copy(msg.bytes.ToArray(), 0, intptr + offset, msg.bytes.Length);

                offset += msg.header.bLength;
            }
        }

        private static void Compress(byte[] odata, int olength, ref byte[] cdata, ref int clength)
        {
            using (MemoryStream msWrite = new MemoryStream(cdata))
            {
                // 因为在写入的时候要压缩后写入，所以需要创建压缩流来写入(因此在压缩写入前需要先创建写入流)
                // 压缩的时候就是要将压缩好的数据写入到指定流中，通过fsWrite写入到新的路径下
                using (GZipStream zip = new GZipStream(msWrite, CompressionLevel.Optimal, true))
                {
                    zip.Write(odata, 0, olength);
                }

                // 每个 pkg 压缩后长度不得超过 int.MaxValue
                if (msWrite.Position > int.MaxValue)
                {
                    throw new Exception("Too Much Data!: msWrite.Position > int.MaxValue");
                }
                else
                {
                    clength = (int)msWrite.Position;
                }
            }
        }
    }
}
