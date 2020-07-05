using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

using DRRCommon;
using DRRCommon.Network;

namespace FileManager
{
    public class File
    {
        public class Info
        {
            public int version_file;
            public int version_code;
            /// <summary>
            /// 起始时间
            /// 当前 UTC 时间从 1970-01-01 的总秒数
            /// 单位：s
            /// </summary>
            public double time;
            /// <summary>
            /// 每个 pkg 时间跨度
            /// 单位：s
            /// </summary>
            public double timeInterval;
            /// <summary>
            /// 该 File 记录的是从这些 IPEndPoint 中收到到的数据
            /// </summary>
            public IPEndPoint[] points { set; get; }
            /// <summary>
            /// 备注
            /// </summary>
            public string notes { set; get; }
            /// <summary>
            /// 总 index 数量
            /// </summary>
            public long totalIndex { set; get; }
        }
        public Info GetInfo()
        {
            return new Info()
            {
                version_file = this.header.version_file,
                version_code = this.header.version_code,
                time = this.header.time,
                timeInterval = this.header.timeInterval,
                points = this.listenPoints.ToArray(),
                notes = this.notes,
                totalIndex = this.index.Count
            };
        }

        [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
        public struct Header
        {
            public int version_file;
            public int version_code;
            public double time;
            public double timeInterval;
            public int listenPointsAsBytesCount;
            public long notesAsBytesCount;
            public long fileInfoLength;

            public int GetSize()
            {
                return 44;
            }
        }

        public Header header = new Header();
        public List<IPEndPoint> listenPoints;
        public string notes;

        public int partNum;
        public string pathWithName;

        public int p;
        public List<long> index;

        object locker = new object();
        FileStream fs;

        public long Position { get { return fs.Position; } }
        public long Length { get { return fs.Length; } }

        //public void Seek(long offset, SeekOrigin origin)
        //{
        //    fs.Seek(offset, origin);
        //}

        public bool Locate(int index)
        {
            lock (locker)
            {
                var offset = this.index[index];
                if (fs.Seek(offset, SeekOrigin.Begin) == offset)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public File Create()
        {
            // 初始化
            p = 0;
            index = new List<long>();

            // FileMode.Create : 如果存在同名文件则覆盖
            fs = new FileStream(pathWithName + "_" + partNum + ".lcl", FileMode.Create, FileAccess.Write);

            // 跳过 header 部分
            // 因为部分数据需要之后才能确定
            fs.Seek(header.GetSize(), SeekOrigin.Begin);

            // 写入 listenPoints
            foreach (var point in listenPoints)
            {
                fs.Write(point.Address.GetAddressBytes(), 0, 4);
                fs.Write(BitConverter.GetBytes((ushort)point.Port), 0, 2);
            }

            // 为 header 中部分数据赋值
            header.listenPointsAsBytesCount = listenPoints.Count * (4 + 2);

            // 写入 notes
            var bytes = Encoding.UTF8.GetBytes(notes, 0, notes.Length);
            fs.Write(bytes, 0, bytes.Length);

            // 为 header 中部分数据赋值
            header.notesAsBytesCount = bytes.Length;
            header.fileInfoLength = fs.Position;

            // 回到文件开头
            fs.Seek(0, SeekOrigin.Begin);
            fs.Write(StructBytes.ConvertStructToBytes(header), 0, header.GetSize());

            // 返回文件数据开头
            fs.Seek(header.fileInfoLength, SeekOrigin.Begin);

            return this;
        }
        public File CreateNext(double time)
        {
            File next = this.MemberwiseClone() as File;


            next.partNum++;
            next.header.time = time;
            // Create 中会写入文件
            // 因此需要修改的量需要先修改
            next.Create();

            return next;
        }

        public File Load(string path)
        {
            // 初始化
            p = 0;
            index = new List<long>();

            fs = new FileStream(path, FileMode.Open, FileAccess.Read);

            byte[] bytes = new byte[header.GetSize()];
            fs.Read(bytes, 0, bytes.Length);
            header = (Header)StructBytes.BytesToStructure(bytes, typeof(Header));

            bytes = new byte[header.listenPointsAsBytesCount];
            fs.Read(bytes, 0, bytes.Length);
            listenPoints = new List<IPEndPoint>();
            for (int i = 0; i < header.listenPointsAsBytesCount; i += (4 + 2))
            {
                ReadOnlySpan<byte> ipb = new ReadOnlySpan<byte>(bytes, i, 4);
                listenPoints.Add(new IPEndPoint(new IPAddress(ipb), (int)BitConverter.ToUInt16(bytes, i + 4)));
                //listenPoints.Add(new IPandPort(bytes[i + 0] + "." + bytes[i + 1] + "." + bytes[i + 2] + "." + bytes[i + 3], BitConverter.ToUInt16(bytes, i + 4)));
            }

            bytes = new byte[header.notesAsBytesCount];
            fs.Read(bytes, 0, bytes.Length);
            notes = Encoding.UTF8.GetString(bytes);

            // 建立文件内索引
            bytes = new byte[4];
            while (fs.Position < fs.Length)
            {
                index.Add(fs.Position);

                fs.Read(bytes, 0, 4);
                var ol = BitConverter.ToInt32(bytes);
                fs.Read(bytes, 0, 4);
                var cl = BitConverter.ToInt32(bytes);

                fs.Seek(cl, SeekOrigin.Current);
            }

            // 返回文件数据开头
            fs.Seek(header.fileInfoLength, SeekOrigin.Begin);

            return this;
        }

        //public void Write(byte[] bytes, int offset, int count)
        //{
        //    fs.Write(bytes, offset, count);
        //}

        public void Write(Package pkg)
        {
            lock (locker)
            {
                index.Add(fs.Position);
                fs.Write(BitConverter.GetBytes(pkg.originLength));
                fs.Write(BitConverter.GetBytes(pkg.codedLength));
                fs.Write(pkg.codedBytes, 0, pkg.codedLength);
            }
        }

        public void Read(ref Package pkg)
        {
            lock (locker)
            {
                byte[] bytes = new byte[4];

                fs.Read(bytes, 0, 4);
                var ol = BitConverter.ToInt32(bytes);
                fs.Read(bytes, 0, bytes.Length);
                var cl = BitConverter.ToInt32(bytes);

                pkg.originBytes = SharedMemory.Rent(ol);
                pkg.originLength = ol;
                pkg.codedBytes = SharedMemory.Rent(cl);
                pkg.codedLength = cl;

                fs.Read(pkg.codedBytes, 0, pkg.codedLength);
            }
        }

        public void Flush()
        {
            lock (locker)
            {
                fs.Flush();
            }
        }
        public void Close()
        {
            lock (locker)
            {
                fs.Close();
            }
        }
    }
}
