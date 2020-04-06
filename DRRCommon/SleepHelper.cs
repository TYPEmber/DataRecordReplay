using System;
using System.Net.Sockets;

namespace DRRCommon
{
    public static class SleepHelper
    {
        static Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        public static void Delay(int millSenconds)
        {
            _socket.Poll(millSenconds * 1000, SelectMode.SelectRead);
        }
    }
}
