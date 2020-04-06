using System;
using System.Collections.Generic;
using System.Net;
using DRRCommon;
using DRRCommon.Network;

namespace UDPRecorder
{
    class Program
    {
        static RecordCore.Core core;
        static void Main(string[] args)
        {
            core = new RecordCore.Core(new List<double>() { 500, 600 }, "test/tt", "El Psy Congroo.", new List<System.Net.IPEndPoint>() 
            {
                new IPEndPoint(IPAddress.Loopback, 5603),
                new IPEndPoint(IPAddress.Loopback, 8912)
            });

            UDPReciverWithTime reciver = new UDPReciverWithTime(5603).Start();
            UDPReciverWithTime reciver1 = new UDPReciverWithTime(8912).Start();

            reciver1.GetSocket().ReceiveBufferSize = 1024 * 1024;
            reciver.DataRcv_Event += Reciver1_DataRcv_Event;
            reciver1.DataRcv_Event += Reciver1_DataRcv_Event;

            reciver.QueueHeap_Event += Reciver1_QueueHeap_Event;
            reciver1.QueueHeap_Event += Reciver1_QueueHeap_Event;

            reciver.QueueHeapCountMax = 1024;
            reciver1.QueueHeapCountMax = 1024;

            while (true)
            {
                //core.Add(DateTime.UtcNow.TotalSeconds(), new byte[] { 1, 2, 3, 4 }, 1234, new byte[] { 1, 2, 3, 4, 5 });

                SleepHelper.Delay(500);
            }
        }

        private static UDPReciverWithTime.QueueClearMode Reciver1_QueueHeap_Event(int heapCount)
        {
            throw new NotImplementedException();
        }

        private static void Reciver1_DataRcv_Event(byte[] rcvBytes, IPEndPoint point, DateTime time)
        {
            core.Add(time.TotalSeconds(), point.Address.GetAddressBytes(), (ushort)point.Port, rcvBytes);
        }
    }
}
