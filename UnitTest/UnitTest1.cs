using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Threading;

namespace UnitTest
{
    [TestClass]
    public class UnitTest1
    {
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


            Thread.Sleep(10000);

            Assert.AreEqual(msg_count, 20000);
        }
    }
}
