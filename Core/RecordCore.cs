using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;

using EDCoder;
using FileManager;
using DRRCommon;
using DRRCommon.Logger;

namespace Core
{
    public class RecordCore
    {
        private double _startTimeStamp;
        private double _intervalTime;
        private bool _flagStop = false;

        private Writer _writer;
        public RecordCore(double[] segmentPara, string path, string name, string notes, List<IPEndPoint> points, double intervalTime = 1.0, DeleInfoHandler infoHandler = null)
        {
            _infoHandler = infoHandler;

            _intervalTime = intervalTime;
            _startTimeStamp = DateTime.UtcNow.TotalSeconds();

            _writer = new Writer(segmentPara,
                path, name, notes,
                points,
                intervalTime, _startTimeStamp);

            InfoThread();
            ProcessStart();
        }

        public void WriteComplete()
        {
            _flagStop = true;
            _writer.FlushAndClose();
        }

        public struct ReplayInfo
        {
            /// <summary>
            /// 当前 UTC 时间戳
            /// </summary>
            public DateTime time;
            /// <summary>
            /// 当前 pkg 中 msg 数量
            /// </summary>
            public int count;
            /// <summary>
            /// 当前 pkg 压缩后大小
            /// </summary>
            public int codedLength;
            /// <summary>
            /// 当前 pkg 未压缩大小
            /// </summary>
            public int originLength;
            /// <summary>
            /// 当前 pkg 生成 UTC 时间戳
            /// </summary>
            public double pkgTime;
        }
        private ConcurrentQueue<ReplayInfo> _infos = new ConcurrentQueue<ReplayInfo>();
        public delegate void DeleInfoHandler(ReplayInfo info);
        private DeleInfoHandler _infoHandler;
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

                    if (_flagStop)
                    {
                        return null;
                    }

                    SleepHelper.Delay();
                }
            });
        }

        private ConcurrentQueue<Message> _queue = new ConcurrentQueue<Message>();
        private void ProcessStart()
        {
            Task.Run(() =>
            {
                double indexTime = _startTimeStamp;

                Package pkg = new Package()
                {
                    time = indexTime,
                };

                while (true)
                {
                    if (_queue.Count == 0)
                    {
                        var t = DateTime.UtcNow.TotalSeconds();

                        // 向下取整
                        if (t - indexTime > _intervalTime)
                        {
                            do
                            {
                                if (_flagStop)
                                {
                                    return null;
                                }
                                _writer.Append(pkg);

                                // Core 中不应该有直接输出
                                //Logger.Info.WriteLine("Pkg_Count: " + pkg.MsgCount
                                //                    + " Compress_Rate: " + (pkg.codedLength * 100.0 / (pkg.originLength == 0 ? -pkg.codedLength : pkg.originLength)).ToString("f2") + "%"
                                //                    + " Pkg_Time: " + pkg.time);
                                _infos.Enqueue(new ReplayInfo()
                                {
                                    time = DateTime.UtcNow,
                                    count = pkg.MsgCount,
                                    codedLength = pkg.codedLength,
                                    originLength = pkg.originLength,
                                    pkgTime = pkg.time
                                });

                                indexTime += _intervalTime;

                                //pkg = new Package()
                                //{
                                //    time = indexTime,
                                //};
                                pkg.time = indexTime;
                                pkg.Clear();
                            }
                            while (indexTime + _intervalTime < t);
                        }

                        SleepHelper.Delay(1);

                        continue;
                    }

                    _queue.TryDequeue(out Message item);

                    // 向下取整
                    if (item.header.time - indexTime > _intervalTime)
                    {
                        do
                        {
                            if (_flagStop)
                            {
                                return null;
                            }
                            _writer.Append(pkg);

                            // Core 中不应该有直接输出
                            //Logger.Info.WriteLine("Pkg_Count: " + pkg.MsgCount
                            //                    + " Compress_Rate: " + (pkg.codedLength * 100.0 / (pkg.originLength == 0 ? -pkg.codedLength : pkg.originLength)).ToString("f2") + "%"
                            //                    + " Pkg_Time: " + pkg.time);
                            _infos.Enqueue(new ReplayInfo()
                            {
                                time = DateTime.UtcNow,
                                count = pkg.MsgCount,
                                codedLength = pkg.codedLength,
                                originLength = pkg.originLength,
                                pkgTime = pkg.time
                            });

                            indexTime += _intervalTime;

                            //pkg = new Package()
                            //{
                            //    time = indexTime,
                            //};
                            pkg.time = indexTime;
                            pkg.Clear();
                        }
                        while (indexTime + _intervalTime < item.header.time);
                    }

                    pkg.Add(item);
                }
            });
        }

        public void Add(double time, byte[] ip, ushort port, byte[] bytes)
        {
            Message msg = new Message()
            {
                header = new Message.Header()
                {
                    time = time,
                    ip = ip,
                    port = port,
                },
                bytes = bytes
            };

            _queue.Enqueue(msg);
        }
    }
}
