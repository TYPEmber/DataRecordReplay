using DRRCommon;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [DllImport("drr_udp_helper.dll")]
        static extern int listen(byte[] addr, ushort port, ulong read_buf_size);

        [DllImport("drr_udp_helper.dll", CallingConvention = CallingConvention.Cdecl)]
        unsafe static extern int receive( byte[] bytes, ref ushort size, byte[] addr, ref ushort port, ref double time);

        [TestMethod]
        public void test_drr_udp_helper()
        {
            var ret = listen(new byte[] { 0, 0, 0, 0 }, 19208, 4096);

            Assert.AreEqual(0, ret);
        }

        [TestMethod]
        unsafe public void test_drr_udp_helper_rcv()
        {
            var ret = listen(new byte[] { 0, 0, 0, 0 }, 19208, 4096);

            //Thread.Sleep(10000);

            byte[] bytes = new byte[4096];
            byte[] addr = new byte[4];
            ushort size = 0, port = 0;
            double time = 0;


            ret = receive( bytes, ref size,  addr, ref port, ref time);
            ret = receive( bytes, ref size,  addr, ref port, ref time);

            Assert.AreEqual(0, ret);
        }

        [TestMethod]
        public void test_drr_udp_helper_queue_rcv()
        {
            int msg_count = 0;

            var ret = listen(new byte[] { 0, 0, 0, 0 }, 19208, 1024 * 1024);

            Core.RecordCore core = new Core.RecordCore(new double[] { 0, 0 }, "D:/Data/test", "test", "unit test",
    new System.Collections.Generic.List<System.Net.IPEndPoint>() { new System.Net.IPEndPoint(IPAddress.Any, 19208) });

                     
            ushort size = 0, port = 0;
            double time = 0;

            Stopwatch watch = new Stopwatch();
            watch.Start();

            while (watch.ElapsedMilliseconds < 20000)
            {
                byte[] addr = new byte[4];
                byte[] bytes = new byte[4096];
                ret = receive(bytes, ref size, addr, ref port, ref time);
                if (ret == -1)
                {
                    Thread.Sleep(1);
                }
                else
                {
                    msg_count++;
                    core.Add(time, addr, port, bytes);
                }
            }

            Assert.AreEqual(600_000, msg_count);
        }

        [TestMethod]
        public void test_network_udp_rcv()
        {
            int msg_count = 0;

            DRRCommon.Network.UDPReciverWithTime reciver = new DRRCommon.Network.UDPReciverWithTime(19208);
            reciver.GetSocket().ReceiveBufferSize = 1024 * 1024 * 2;
            reciver.DataRcv_Event += (byte[] rcvBytes, System.Net.IPEndPoint point, System.DateTime time) =>
            {
                msg_count++;
            };


            reciver.Start();


            Thread.Sleep(20000);

            Assert.AreEqual(msg_count, 600_000);
        }

        [TestMethod]
        public void test_network_udp_queue_rcv()
        {
            int msg_count = 0;

            DRRCommon.Network.UDPReciverWithTime reciver = new DRRCommon.Network.UDPReciverWithTime(19208);
            reciver.GetSocket().ReceiveBufferSize = 1024 * 1024 * 30;
            //reciver.QueueHeapCountMax = 1;

            Core.RecordCore core = new Core.RecordCore(new double[] { 0, 0 }, "D:/Data/test", "test", "unit test",
    new System.Collections.Generic.List<System.Net.IPEndPoint>() { new System.Net.IPEndPoint(IPAddress.Any, 19208) });

            reciver.DataRcv_Event += (byte[] rcvBytes, System.Net.IPEndPoint point, System.DateTime time) =>
            {
                msg_count++;
                core.Add(time.TotalSeconds(), point.Address.GetAddressBytes(), (ushort)point.Port, rcvBytes);
            };

            reciver.QueueHeap_Event += (int heapCount) =>
            {
                throw new System.Exception("Heap: " + heapCount);
            };

            reciver.Start();

            Thread.Sleep(20000);

            Assert.AreEqual(600_000, msg_count);
        }

        [TestMethod]
        public void test_network_udp_send()
        {
            var sender = new UdpClient();
            sender.Client.Blocking = false;
            sender.Client.SendBufferSize = 50 * 1024 * 1024;
            var remote = new IPEndPoint(IPAddress.Parse("192.168.0.4"), 19208);
            //sender.Connect(remote);

            Stopwatch watch = new Stopwatch();
            watch.Start();

            byte[] bytes = new byte[4096];
            var count = 0;
            var count_invalid = 0;
            while (count < 600_000)
            {
                //sender.Client.Send(bytes);
                //sender.SendAsync(bytes, bytes.Length, remote);

                while (true)
                {
                    try
                    {
                        sender.Send(bytes, bytes.Length, remote);
                        break;
                    }
                    catch (SocketException e)
                    {
                        if (e.SocketErrorCode == SocketError.WouldBlock)
                        {
                            SleepHelper.Delay();
                        }
                        else
                        {
                            count_invalid++;
                            throw e;
                        }
                    }
                }


                //sender.Send(bytes, bytes.Length);
                count++;

                //if (count % 100 == 0)
                //{
                //    Thread.Sleep(1);
                //}
            }

            watch.Stop();

            System.Console.WriteLine(watch.ElapsedMilliseconds + "===" + watch.ElapsedMilliseconds / 600_000);

            Assert.AreEqual(0, count_invalid);
        }
    }
}
