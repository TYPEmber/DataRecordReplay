using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DRRCommon
{
    public static class SharedMemory
    {
        private static ArrayPool<byte> _pool = ArrayPool<byte>.Shared;

        public static byte[] Rent(int minLength)
        {
            return _pool.Rent(minLength);
        }

        public static void Return(byte[] buffer)
        {
            if (buffer == null)
            {
                return;
            }
            _pool.Return(buffer);
        }
    }

    //public static class SharedMemory
    //{
    //    private static ArrayPool<byte> _pool = ArrayPool<byte>.Shared;

    //    public static byte[] Rent(int minLength)
    //    {
    //        return _pool.Rent(minLength);
    //    }

    //    public static void Return(byte[] buffer)
    //    {
    //        if (buffer == null)
    //        {
    //            return;
    //        }

    //        _pool.Return(buffer);
    //    }



    //    private static LinkedList<Message>
    //    public static Message RentMsg()
    //    {

    //    }
    //}

    public static class PackagePool
    {
        static ConcurrentQueue<Package> _pool = new ConcurrentQueue<Package>();

        public static Package Rent()
        {
            Package pkg = null;

            if (_pool.Count == 0)
            {
                pkg = new Package();
            }
            else
            {
                _pool.TryDequeue(out pkg);
            }

            return pkg;
        }
        public static void Return(ref Package pkg)
        {
            pkg.Clear();
            _pool.Enqueue(pkg);
        }

        public static void Clear()
        {
            _pool.Clear();
        }
    }

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
    public class Package
    {
        public double time;
        public long index;
        /// <summary>
        /// msgs 转化为 bytes 数组之后的长度
        /// 由 Add 方法生成
        /// </summary>
        public int msgsAsBytesLength { private set; get; }
        // 无需随机访问
        // 因此使用链表可以避免扩容时带来的拷贝
        private LinkedList<Message> msgs;
        public int codedLength;
        public byte[] codedBytes;
        public int originLength;
        public byte[] originBytes;

        public int MsgCount { get { return msgs.Count; } }

        public Package()
        {
            msgs = new LinkedList<Message>();
        }

        ~Package()
        {
            SharedMemory.Return(originBytes);
            SharedMemory.Return(codedBytes);

            // 减少内存压力
            //GC.Collect();
        }

        public IEnumerable<Message> GetMessages()
        {
            return msgs as IEnumerable<Message>;
        }

        public void Add(Message msg)
        {
            msgs.AddLast(msg);
            msgsAsBytesLength += msg.GetHeaderLength() + msg.header.bLength;
        }

        public void Clear()
        {
            msgs.Clear();
            msgsAsBytesLength = 0;

            SharedMemory.Return(originBytes);
            SharedMemory.Return(codedBytes);

            codedBytes = null;
            codedLength = 0;
            originBytes = null;
            originLength = 0;
        }
    }

    // 使用 struct 声明
    // 节省内存开销
    public struct Message
    {
        [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
        public struct Header
        {
            public double time;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] ip;
            public ushort port;
            public int bLength;
        }

        public Header header;

        private ReadOnlyMemory<byte> _bytes;
        public ReadOnlyMemory<byte> bytes { set { _bytes = value; header.bLength = _bytes.Length; } get { return _bytes; } }

        public int GetHeaderLength()
        {
            return 18;
        }
    }

    public static class DateTimeHelper
    {
        private static DateTime OriginStamp = new DateTime(1970, 1, 1);

        public static double TotalSeconds(this DateTime dateTime)
        {
            return (dateTime - OriginStamp).TotalSeconds;
        }
    }
}
