using System;
using System.Net;

using DRRCommon;
using DRRCommon.Network;

namespace UDPReplayer
{
    class Program
    {
        static ReplayCore.Core core;
        static void Main(string[] args)
        {
            var map = new System.Collections.Generic.Dictionary<System.Net.IPEndPoint, System.Net.IPEndPoint>();
            map.Add(new IPEndPoint(IPAddress.Any, 5603), new IPEndPoint(IPAddress.Loopback, 5603));
            map.Add(new IPEndPoint(IPAddress.Any, 8912), new IPEndPoint(IPAddress.Loopback, 8912));
            core = new ReplayCore.Core(new System.Collections.Generic.List<string>() { "test/tt_1.lcl", "test/tt_2.lcl", "test/tt_3.lcl" }, map);

            UDPSender sender = new UDPSender();

            core.SendHandler = (ReadOnlySpan<byte> bytes, IPEndPoint point) =>
            {
                sender.Send(bytes.ToArray(), point);
            };

            while (true)
            {
                SleepHelper.Delay(100);
            }
        }
    }
}
