using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using DRRCommon;
using FileManager;
using EDCoder;
using System.Net;
using System.Diagnostics;
using DRRCommon.Logger;

namespace ReplayCore
{
    public class Core
    {
        Reader _reader;

        Dictionary<long, IPEndPoint> _map = new Dictionary<long, IPEndPoint>();

        private long ConverToIP64(byte[] ipb, ushort port)
        {
            long result = 0;

            for (int i = 0; i < ipb.Length; i++)
            {
                result += ipb[i] * (int)Math.Pow(255, i);
            }
            result <<= 32;
            result |= (long)port;

            return result;
        }

        public Core(List<string> paths, Dictionary<IPEndPoint, IPEndPoint> map)
        {
            _reader = new Reader(paths);

            foreach (var m in map)
            {
                var key = ConverToIP64(m.Key.Address.GetAddressBytes(), (ushort)m.Key.Port);

                _map.Add(key, m.Value);
            }

            RePlayThread();
        }

        public bool JumpTo(long index)
        {
            return _reader.Set(index);
        }

        public delegate void DeleSendHandler(ReadOnlySpan<byte> bytes, IPEndPoint point);
        public DeleSendHandler SendHandler;

        public double SpeedRate { set; get; } = 1;

        private void RePlayThread()
        {
            Task.Run(() =>
            {
                Stopwatch watch = new Stopwatch();
                double sleepDelay = 0.0005;
                double sendDelay = 0.000005;

                while (true)
                {
                    watch.Restart();

                    var pkg = _reader.Get();

                    if (pkg == null)
                    {
                        SleepHelper.Delay(1);
                        continue;
                    }

                    // 播放完毕
                    if (pkg.index >= _reader.Count)
                    {
                        SleepHelper.Delay(1);
                        continue;
                    }

                    foreach (var msg in pkg.GetMessages())
                    {
                        var sendTiming = (msg.header.time - pkg.time) / SpeedRate;
                        while (watch.ElapsedMilliseconds / 1000.0 + sleepDelay + sendDelay < sendTiming)
                        {
                            SleepHelper.Delay(sleepDelay);
                        }

                        _map.TryGetValue(ConverToIP64(msg.header.ip, msg.header.port), out var point);
                        if (point == null)
                        {
                            continue;
                        }

                        SendHandler?.Invoke(msg.bytes.Span, point);
                    }

                    // 缓解内存压力
                    _reader.Return(ref pkg);

                    while (watch.ElapsedMilliseconds / 1000.0 + sleepDelay < _reader.Interval / SpeedRate)
                    {
                        //Logger.Debug.WriteLine(watch.ElapsedMilliseconds);
                        SleepHelper.Delay(sleepDelay);
                    }

                    Logger.Debug.WriteLine(watch.ElapsedMilliseconds);
                }

            });
        }
    }
}
