using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileManager
{
    public class IndexManager
    {
        List<File> _files;

        List<List<long>> _indexs = new List<List<long>>();
        List<int> _indexsCount = new List<int>();
        int _totalIndexs;

        public long Total { get { return _totalIndexs; } }

        // for Class Reader
        public IndexManager(List<File> files)
        {
            foreach (var file in files)
            {
                _indexs.Add(file.index);
                _totalIndexs += file.index.Count;
                _indexsCount.Add(file.index.Count);
            }

            for (int i = 1; i < _indexsCount.Count; i++)
            {
                _indexsCount[i] += _indexsCount[i - 1];
            }
        }

        public bool Convert(long index, out File current, out int innerIndex)
        {
            for (int i = 0; i < _indexs.Count; i++)
            {
                if (index - _indexsCount[i] < 0)
                {
                    if (i > 0)
                    {
                        index -= _indexsCount[i - 1];
                    }
                    else
                    {
                        index -= 0;
                    }

                    current = _files[i];
                    innerIndex = (int)index;

                    return true;
                }
            }

            current = null;
            innerIndex = 0;
            return false;
        }
    }
}
