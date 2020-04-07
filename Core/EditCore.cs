using System;
using System.Collections.Generic;
using System.Text;

using DRRCommon;
using FileManager;

namespace Core
{
    public class EditCore
    {
        Reader _reader;
        Writer _writer;

        public EditCore(IEnumerable<string> paths)
        {
            _reader = new Reader(paths, false);
        }

        public File.Info FileInfo { get { return _reader.GetFilesInfo(); } }

        public void Clip(long startIndex, long endIndex, double[] segmentPara, string path, string name, string notes = null)
        {
            _reader.Set(startIndex);

            var info = _reader.GetFilesInfo();
            if (notes == null)
            {
                notes = info.notes;
            }

            _writer = new Writer(segmentPara, path, name, notes,
                new List<System.Net.IPEndPoint>(info.points),
                info.timeInterval, info.time + startIndex * info.timeInterval);

            int i = 0;
            while (i <= endIndex - startIndex)
            {
                var pkg = _reader.Get();

                if (pkg == null)
                {
                    SleepHelper.Delay(1);
                    continue;
                }

                _writer.AppendCoded(pkg);

                _reader.Return(ref pkg);

                i++;
            }

            _writer.FlushAndClose();
        }
    }
}
