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

namespace Core
{
    public class ReplayCore
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

        public ReplayCore(IEnumerable<string> paths, Dictionary<IPEndPoint, IPEndPoint> map, DeleSendHandler sendHandler)
        {
            _reader = new Reader(paths);

            _sendHandler = sendHandler;

            foreach (var m in map)
            {
                var key = ConverToIP64(m.Key.Address.GetAddressBytes(), (ushort)m.Key.Port);

                _map.Add(key, m.Value);
            }

            RePlayThread();
        }

        public File.Info FileInfo { get { return _reader.GetFilesInfo(); } }

        Stopwatch _watch = new Stopwatch();

        public bool IsPlaying { get { return _watch.IsRunning; } }
        // 播放/暂停
        public void P()
        {
            if (_watch.IsRunning)
            {
                _watch.Stop();
            }
            else
            {
                // 保证缓冲队列有数据
                while (_reader.Peek() == null)
                {
                    SleepHelper.Delay(1);
                }
                _watch.Start();
            }
        }

        public bool JumpTo(long index)
        {
            _watch.Reset();
            return _reader.Set(index);
        }

        public delegate void DeleSendHandler(ReadOnlySpan<byte> bytes, IPEndPoint point);
        private DeleSendHandler _sendHandler;

        public double SpeedRate { set; get; } = 1;

        private void RePlayThread()
        {
            Task.Run(() =>
            {
                //TODO: 对 delay 进行测算
                double sleepDelay = 0.0005;
                double sendDelay = 0.000005;

                while (true)
                {
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
                        while (_watch.ElapsedMilliseconds / 1000.0 + sleepDelay + sendDelay < sendTiming)
                        {
                            SleepHelper.Delay(sleepDelay);
                        }

                        _map.TryGetValue(ConverToIP64(msg.header.ip, msg.header.port), out var point);
                        if (point == null)
                        {
                            continue;
                        }

                        _sendHandler?.Invoke(msg.bytes.Span, point);
                    }

                    // 缓解内存压力
                    _reader.Return(ref pkg);

                    while (_watch.ElapsedMilliseconds / 1000.0 + sleepDelay < _reader.Interval / SpeedRate)
                    {
                        //Logger.Debug.WriteLine(watch.ElapsedMilliseconds);
                        SleepHelper.Delay(sleepDelay);
                    }

                    //TODO: 用事件来向外输出
                    // Core 中不应该有直接输出
                    Logger.Debug.WriteLine(pkg.index + " " +_watch.ElapsedMilliseconds);

                    _watch.Restart();
                }

            });
        }
    }
}
