using System;
using System.Collections.Generic;
using System.Net;
using DRRCommon;
using DRRCommon.Network;
using DRRCommon.Logger;

namespace UDPRecorder
{
    class Program
    {
        static RecordCore.Core _core;
        static List<UDPReciverWithTime> _recivers = new List<UDPReciverWithTime>();

        static void InitialReciver(string ip, int port, ref List<IPEndPoint> points)
        {
            var point = new IPandPort(ip, port);
            var reciver = new UDPReciverWithTime(point);
            _recivers.Add(reciver);

            reciver.GetSocket().ReceiveBufferSize = 1024 * 1024;
            reciver.QueueHeapCountMax = 1024;
            reciver.QueueHeap_Event += Reciver_QueueHeap_Event;

            reciver.DataRcv_Event += Reciver_DataRcv_Event;

            points.Add(new IPEndPoint(IPAddress.Parse(ip), port));

            Logger.Info.WriteLine("Initial Reciver Success! Listen On " + ip + ":" + port);
        }

        static void Main(string[] args)
        {
            if (args.Length % 2 != 0)
            {
                Logger.Error.WriteLine("Para Input Error!");
                return;
            }

            Dictionary<string, string> para = new Dictionary<string, string>();

            for (int i = 0; i < args.Length; i += 2)
            {
                para.Add(args[i], args[i + 1]);
            }

            List<IPEndPoint> points = new List<IPEndPoint>();
            if (para.ContainsKey("-l"))
            {
                var pp = para["-l"].Split(" ");

                foreach (var item in pp)
                {
                    var p = item.Split(":");
                    string ip = "";
                    int port = 0;
                    // 仅指定 port
                    if (p.Length == 1)
                    {
                        ip = "0.0.0.0";
                    }
                    // 指定 ip 与 port
                    else if (p.Length == 2)
                    {
                        ip = p[0];
                        p[0] = p[1];
                    }
                    else
                    {
                        Logger.Error.WriteLine("Wrong Reciver Para!");
                    }

                    try
                    {
                        port = int.Parse(p[0]);
                    }
                    catch (Exception)
                    {
                        Logger.Error.WriteLine("Wrong Reciver Port!");
                        return;
                    }

                    InitialReciver(ip, port, ref points);
                }
            }
            else
            {
                Logger.Error.WriteLine("No Reciver Para!");
                return;
            }

            List<double> segPara = new List<double>() { 2048, 3600 };
            if (para.ContainsKey("-s"))
            {
                var ss = para["-i"].Split(" ");
                try
                {
                    segPara[0] = double.Parse(ss[0]);
                    segPara[1] = double.Parse(ss[1]);
                }
                catch (Exception)
                {
                    Logger.Error.WriteLine("Wrong SegMentPara!");
                    return;
                }

            }

            double indexInterval = 1;
            if (para.ContainsKey("-i"))
            {
                try
                {
                    indexInterval = double.Parse(para["-i"]);
                }
                catch (Exception)
                {
                    Logger.Error.WriteLine("Wrong IndexInterval!");
                    return;
                }
            }

            string notes = "";
            if (para.ContainsKey("-n"))
            {
                notes = para["-n"];
            }


            string path = "/";
            string name = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
            if (para.ContainsKey("-p"))
            {
                path = para["-p"];
            }
            if (para.ContainsKey("-f"))
            {
                if (para["-f"].Contains("/"))
                {
                    Logger.Error.WriteLine("File name can not contains '/'!");
                    return;
                }
                else
                {
                    name = para["-f"];
                }
            }

            _core = new RecordCore.Core(segPara, path, name, notes, points);

            // 保证在 core 实例化之后
            // 否则回调函数里的 core 会为 null
            foreach (var rec in _recivers)
            {
                rec.Start();
            }

            while (true)
            {
                var key = Console.Read();
                if (key == 'c')
                {
                    foreach (var rec in _recivers)
                    {
                        rec.Stop();
                    }

                    _core.WriteComplete();
                    break;
                }
            }
        }

        private static UDPReciverWithTime.QueueClearMode Reciver_QueueHeap_Event(int heapCount)
        {
            Logger.Info.WriteLine("HeapEvent!  Count:" + heapCount);
            return UDPReciverWithTime.QueueClearMode.Cancel;
        }

        private static void Reciver_DataRcv_Event(byte[] rcvBytes, IPEndPoint point, DateTime time)
        {
            _core.Add(time.TotalSeconds(), point.Address.GetAddressBytes(), (ushort)point.Port, rcvBytes);
        }
    }
}
