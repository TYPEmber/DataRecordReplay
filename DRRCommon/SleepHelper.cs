using System;
using System.Net.Sockets;

namespace DRRCommon
{
    public static class SleepHelper
    {
        static Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        public static void Delay(int millSeconds)
        {
            _socket.Poll(millSeconds * 1000, SelectMode.SelectRead);
        }

        public static void Delay(double seconds)
        {
            _socket.Poll((int)(seconds * 1000 * 1000), SelectMode.SelectRead);
        }

        public static void Delay()
        {
            _socket.Poll(1, SelectMode.SelectRead);
        }
    }
}
