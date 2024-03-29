﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DRRCommon;

namespace FileManager
{
    public class Reader
    {
        DRRCommon.Logger.ILogger _logger;

        List<File> _files;
        IndexManager _index;

        File _current;
        long _currentIndex;
        bool _alive;

        public double StartTime { private set; get; }
        public long Count { get { return _index.Total; } }
        public double Interval { get { return _current.header.timeInterval; } }

        public Reader(IEnumerable<string> paths, bool flagAutoDecode = true, DRRCommon.Logger.ILogger logger = null)
        {
            _alive = true;
            _logger = logger;
            _files = new List<File>();

            foreach (var path in paths)
            {
                _files.Add(new File().Load(path));
            }

            _index = new IndexManager(_files);
            _current = _files[0];

            StartTime = _current.header.time;

            if (flagAutoDecode)
            {
                // 缓冲加载线程
                BufferThread();
            }
            else
            {
                BufferThreadWithoutDecode();
            }

        }

        public void Destroy()
        {
            _alive = false;
            PackagePool.Clear();

            _current.Close();
            foreach (var pkg in _buffer)
            {
                pkg.Clear();
            }
            _buffer.Clear();
        }

        public File.Info GetFilesInfo()
        {
            var info = _current.GetInfo();

            info.time = StartTime;
            info.totalIndex = this.Count;

            return info;
        }

        public Package Get()
        {
            _buffer.TryDequeue(out Package pkg);

            return pkg;
        }

        public Package Peek()
        {
            _buffer.TryPeek(out Package pkg);

            return pkg;
        }

        public void Return(ref Package pkg)
        {
            PackagePool.Return(ref pkg);
        }

        private object _lockerCurrentIndex = new object();

        public bool Set(long index)
        {
            _buffer.TryPeek(out Package pkg);

            if (pkg != null)
            {
                // 避免未变化指针但却重置缓冲队列
                if (index == pkg.index)
                {
                    return true;
                }
            }

            if (_index.Convert(index, out File current, out int oindex))
            {
                _currentIndex = index;
                _current = current;
                _current.Locate(oindex);

                _buffer.Clear();

                return true;
            }
            else
            {
                return false;
            }
        }


        private bool ReadFile(ref Package pkg)
        {
            // 当前 file 读取到末尾
            if (_current.Position >= _current.Length)
            {
                // 表明还有后续文件
                if (_currentIndex < _index.Total)
                {
                    if (_index.Convert(_currentIndex, out File current, out int index))
                    {
                        _current = current;
                        _current.Locate(index);
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                else
                {
                    // 播放至尽头
                    // pkg = null 表明文件已读完
                    //PackagePool.Return(ref pkg);
                    pkg.Clear();
                    return false;
                }
            }

            _current.Read(ref pkg);
            return true;
        }

        ConcurrentQueue<Package> _buffer = new ConcurrentQueue<Package>();

        private void BufferThread()
        {
            int bufferSize = 3;
            Task.Run(() =>
            {
                while (_alive)
                {
                    try
                    {
                        if (_buffer.Count <= bufferSize)
                        {
                           //Package pkg = PackagePool.Rent();
                            Package pkg = new Package();

                            lock (_lockerCurrentIndex)
                            {
                                if (!this.ReadFile(ref pkg))
                                {
                                    SleepHelper.Delay();
                                    continue;
                                }

                                pkg.index = _currentIndex;
                                pkg.time = pkg.index * _current.header.timeInterval + StartTime;

                                EDCoder.Decoder.GetMessages(ref pkg);

                                _buffer.Enqueue(pkg);

                                //减少内存占用
                                //Console.WriteLine(GC.GetTotalMemory(false) - 10 * (pkg.codedBytes.Length + pkg.originBytes.Length + pkg.msgsAsBytesLength));
                                if (GC.GetTotalMemory(false) > bufferSize * (pkg.codedBytes.Length + pkg.originBytes.Length + pkg.msgsAsBytesLength))
                                {
                                    GC.Collect();
                                }

                                _currentIndex++;
                            }
                        }
                        else
                        {
                            SleepHelper.Delay();
                        }
                    }
                    catch (Exception e)
                    {
                        _logger?.WriteLine(e);
                    }

                }
            });
        }

        private void BufferThreadWithoutDecode()
        {
            Task.Run(() =>
            {
                while (_alive)
                {
                    if (_buffer.Count <= 3)
                    {
                        //Package pkg = PackagePool.Rent();
                        Package pkg = new Package();

                        this.ReadFile(ref pkg);

                        if (pkg == null)
                        {
                            SleepHelper.Delay();
                            continue;
                        }

                        pkg.index = _currentIndex;
                        pkg.time = pkg.index * _current.header.timeInterval + StartTime;

                        //EDCoder.Decoder.GetMessages(ref pkg);

                        _buffer.Enqueue(pkg);

                        //减少内存占用
                        //Console.WriteLine(GC.GetTotalMemory(false) - 10 * (pkg.codedBytes.Length + pkg.originBytes.Length + pkg.msgsAsBytesLength));
                        if (GC.GetTotalMemory(false) > 3 * (pkg.codedBytes.Length + pkg.originBytes.Length + pkg.msgsAsBytesLength))
                        {
                            GC.Collect();
                        }

                        _currentIndex++;
                    }

                    SleepHelper.Delay();
                }
            });
        }
    }
}
