using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DRRCommon;

namespace FileManager
{
    public class Reader
    {
        List<File> _files;
        IndexManager _index;

        File _current;
        long _currentIndex;

        public long Count { get { return _index.Total; } }
        public double Interval { get { return _current.header.timeInterval; } }

        public Reader(IEnumerable<string> paths)
        {
            _files = new List<File>();

            foreach (var path in paths)
            {
                _files.Add(new File().Load(path));
            }

            _index = new IndexManager(_files);
            _current = _files[0];


            // 缓冲加载线程
            BufferThread();
        }

        private void ReadFile(ref Package pkg)
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
                }
            }

            _current.Read(ref pkg);
            pkg.index = _currentIndex;
            pkg.time = pkg.index * _current.header.timeInterval + _current.header.time;
        }

        public File.Info GetFileInfo()
        {
            var info = _current.GetInfo();
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


            // 缓冲队列清空
            _buffer.Clear();

            if (_index.Convert(index, out File current, out int oindex))
            {
                _currentIndex = index;
                _current = current;
                _current.Locate(oindex);

                return true;
            }
            else
            {
                return false;
            }
        }

        ConcurrentQueue<Package> _buffer = new ConcurrentQueue<Package>();
        private void BufferThread()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (_buffer.Count <= 10)
                    {
                        Package pkg = PackagePool.Rent();
                        //Package pkg = new Package();

                        this.ReadFile(ref pkg);

                        EDCoder.Decoder.GetMessages(ref pkg);

                        _buffer.Enqueue(pkg);

                        //减少内存占用
                        //Console.WriteLine(GC.GetTotalMemory(false) - 10 * (pkg.codedBytes.Length + pkg.originBytes.Length + pkg.msgsAsBytesLength));
                        if (GC.GetTotalMemory(false) > 10 * (pkg.codedBytes.Length + pkg.originBytes.Length + pkg.msgsAsBytesLength))
                        {
                            GC.Collect();
                        }

                        _currentIndex++;
                    }

                    SleepHelper.Delay(10);
                }
            });
        }
    }
}
