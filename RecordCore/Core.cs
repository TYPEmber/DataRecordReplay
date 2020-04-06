using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EDCoder;
using FileManager;
using DRRCommon;
using System.Collections.Generic;
using System.Net;

namespace RecordCore
{
    public class Core
    {
        private double _startTimeStamp;
        private double _intervalTime;

        private Writer _writer;
        public Core(List<double> segmentPara, string path, string notes, List<IPEndPoint> points, double intervalTime = 1.0)
        {
            _intervalTime = intervalTime;
            _startTimeStamp = DateTime.UtcNow.TotalSeconds();

            _writer = new Writer(segmentPara,
                path, notes,
                points,
                intervalTime, _startTimeStamp);

            ProcessStart();
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
