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

        private Writer _writer;
        public RecordCore(double[] segmentPara, string path, string name, string notes, List<IPEndPoint> points, double intervalTime = 1.0)
        {
            _intervalTime = intervalTime;
            _startTimeStamp = DateTime.UtcNow.TotalSeconds();

            _writer = new Writer(segmentPara,
                path, name, notes,
                points,
                intervalTime, _startTimeStamp);

            ProcessStart();
        }

        public void WriteComplete()
        {
            _writer.FlushAndClose();
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
                                _writer.Append(pkg);

                                //TODO: 用事件来向外输出
                                // Core 中不应该有直接输出
                                Logger.Info.WriteLine("Pkg_Count: " + pkg.MsgCount
                                                    + " Compress_Rate: " + (pkg.codedLength * 100.0 / (pkg.originLength == 0 ? -pkg.codedLength : pkg.originLength)).ToString("f2") + "%"
                                                    + " Pkg_Time: " + pkg.time);

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
                            _writer.Append(pkg);

                            //TODO: 用事件来向外输出
                            // Core 中不应该有直接输出
                            Logger.Info.WriteLine("Pkg_Count: " + pkg.MsgCount
                                                + " Compress_Rate: " + (pkg.codedLength * 100.0 / (pkg.originLength == 0 ? -pkg.codedLength : pkg.originLength)).ToString("f2") + "%"
                                                + " Pkg_Time: " + pkg.time);

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
