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

        private double _sleepDelay = 0.001 / 2.0;
        public ReplayCore(IEnumerable<string> paths)
        {
            _reader = new Reader(paths);

            // 用于测算当前设备 _sleepDelay
            Task.Run(() =>
            {
                Stopwatch watch = new Stopwatch();

                watch.Start();
                for (int i = 0; i < 10000; i++)
                {
                    SleepHelper.Delay();
                }
                watch.Stop();

                _sleepDelay = watch.Elapsed.TotalSeconds / 10000 / 2.0;
            });

            InfoThread();
            RePlayThread();
        }

        public ReplayCore Initial(Dictionary<IPEndPoint, IPEndPoint> map, DeleSendHandler sendHandler, DeleInfoHandler infoHandler)
        {
            _sendHandler = sendHandler;
            _infoHandler = infoHandler;

            foreach (var m in map)
            {
                var key = ConverToIP64(m.Key.Address.GetAddressBytes(), (ushort)m.Key.Port);

                _map.Add(key, m.Value);
            }

            return this;
        }

        public File.Info FileInfo { get { return _reader.GetFilesInfo(); } }

        #region Replay Ctrl

        Stopwatch _watch = new Stopwatch();

        public double SpeedRate { set; get; } = 1;
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
                _watch.Start();
            }
        }

        private bool _signalEnd = false;
        private bool _signalJump = false;
        public bool JumpTo(long index)
        {
            _signalJump = _reader.Set(index);

            return _signalJump;
        }
        #endregion

        public struct ReplayInfo
        {
            /// <summary>
            /// 当前 UTC 时间戳
            /// </summary>
            public DateTime time;
            /// <summary>
            /// 已播放完成的 pkg 的 index 编号
            /// </summary>
            public long index;
            /// <summary>
            /// 已播放完成的 pkg 播放耗时
            /// </summary>
            public double pkgCostTime;
        }
        private ConcurrentQueue<ReplayInfo> _infos = new ConcurrentQueue<ReplayInfo>();
        public delegate void DeleInfoHandler(ReplayInfo info);
        private DeleInfoHandler _infoHandler;
        public delegate void DeleSendHandler(ReadOnlySpan<byte> bytes, IPEndPoint point);
        private DeleSendHandler _sendHandler;

        private void InfoThread()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (_infos.TryDequeue(out ReplayInfo info))
                    {
                        _infoHandler?.Invoke(info);
                        continue;
                    }

                    SleepHelper.Delay();
                }
            });
        }

        private void RePlayThread()
        {
            Task.Run(() =>
            {
                double sendDelay = 0.000005;
                _signalJump = false;
                _signalEnd = false;

                if (_watch.IsRunning)
                {
                    _watch.Restart();
                }
                else
                {
                    _watch.Reset();
                }

                while (true)
                {
                    var pkg = _reader.Get();

                    if (pkg == null)
                    {
                        if (_signalEnd || _signalJump)
                        {
                            if (_watch.IsRunning)
                            {
                                _watch.Restart();
                            }
                            else
                            {
                                _watch.Reset();
                            }

                            _signalJump = false;
                        }
                        SleepHelper.Delay();
                        continue;
                    }

                    //Logger.Debug.WriteLine(pkg.index);

                    foreach (var msg in pkg.GetMessages())
                    {
                        var sendTiming = (msg.header.time - pkg.time) / SpeedRate;

                        while (_watch.Elapsed.TotalSeconds + _sleepDelay + sendDelay < sendTiming)
                        {
                            if (_signalJump)
                            {
                                RePlayThread();
                                return;
                            }
                            SleepHelper.Delay();
                        }

                        _map.TryGetValue(ConverToIP64(msg.header.ip, msg.header.port), out var point);
                        if (point == null)
                        {
                            continue;
                        }

                        _sendHandler?.Invoke(msg.bytes.Span, point);
                    }

                    // 发送下一个 pkg 之前的延时
                    while (_watch.Elapsed.TotalSeconds + _sleepDelay < _reader.Interval / SpeedRate)
                    {
                        if (_signalJump)
                        {
                            RePlayThread();
                            return;
                        }
                        SleepHelper.Delay();
                    }


                    // Core 中不应该有直接输出
                    //Logger.Debug.WriteLine(pkg.index + " " + _watch.Elapsed.TotalSeconds);
                    _infos.Enqueue(new ReplayInfo()
                    {
                        time = DateTime.UtcNow,
                        index = pkg.index,
                        pkgCostTime = _watch.Elapsed.TotalSeconds
                    });


                    // 播放完毕
                    if (pkg.index >= _reader.Count - 1)
                    {
                        _signalEnd = true;
                    }
                    else
                    {
                        _signalEnd = false;
                    }

                    // 需要在所有使用 pkg 的代码完成后再归还
                    // 缓解内存压力
                    _reader.Return(ref pkg);

                    _watch.Restart();
                }
            });
        }
    }
}
