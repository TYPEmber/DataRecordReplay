using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

using DRRCommon;

namespace EDCoder
{
    public static class Decoder
    {
        public static void GetMessages(ref Package pkg)
        {
            DeCompress(pkg.codedBytes, ref pkg.originBytes, pkg.originLength);

            int offset = 0;
            while (offset < pkg.originLength)
            {
                CopyArrayToMsg(pkg.originBytes, out Message msg, ref offset);
                pkg.Add(msg);
            }
        }

        private static unsafe void CopyArrayToMsg(byte[] array, out Message msg, ref int offset)
        {
            fixed (byte* p = &array[0])
            {
                var intptr = new IntPtr(p);

                msg = new Message();
                msg.header = (Message.Header)Marshal.PtrToStructure(intptr + offset, typeof(Message.Header));

                offset += msg.GetHeaderLength();

                msg.bytes = new ReadOnlyMemory<byte>(array, offset, msg.header.bLength);

                //var bl = msg.header.bLength;
                //msg.bytes = SharedMemory.Rent(msg.header.bLength);
                //msg.header.bLength = bl;
                //Marshal.Copy(intptr + offset, msg.bytes, 0, msg.header.bLength);

                offset += msg.header.bLength;
            }
        }

        private static void DeCompress(byte[] cdata, ref byte[] odata, int olength)
        {
            using (MemoryStream msRead = new MemoryStream(cdata))
            {
                int r = 0;
                //因为在写入的时候要压缩后写入，所以需要创建压缩流来写入(因此在压缩写入前需要先创建写入流)
                //压缩的时候就是要将压缩好的数据写入到指定流中，通过fsWrite写入到新的路径下
                using (GZipStream zip = new GZipStream(msRead, CompressionMode.Decompress, true))
                {
                    r = zip.Read(odata, 0, odata.Length);
                }

                if (r != olength)
                {
                    throw new Exception("DeCompress Failed!");
                }
            }
        }
    }
}
