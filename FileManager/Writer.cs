using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

using DRRCommon;
using DRRCommon.Network;

namespace FileManager
{
    public class Writer
    {
        public enum EnumSegMentMode
        {
            None,
            DATA,
            TIME,            
            HYBIRD
        }

        // _segmentPara[0] 为分割数据 单位为 MB
        // _segmentPara[1] 为分割时间 单位为 s
        // 数值为 0 则表明不分割该项
        private List<double> _segmentPara;

        private File _current;

        public Writer(List<double> segmentPara, string path, string notes, List<IPEndPoint> listenPoints, double timeInterval, double startTime)
        {
            //TODO: para check
            _segmentPara = segmentPara;

            _current = new File()
            {
                notes = notes,
                listenPoints = listenPoints,
                pathHeader = path,
                partNum = 0,
                header = new File.Header()
                {
                    version_file = 1,
                    version_code = 1,
                    time = startTime,
                    timeInterval = timeInterval,
                }
            }.Create();
        }

        public void Append(Package pkg)
        {
            bool segFlag = false;

            // 表明数据上要做分割
            if (_segmentPara[0] != 0)
            {
                if (_current.Position + pkg.codedLength > _segmentPara[0] * 1024 * 1024)
                {
                    segFlag = true;
                }
            }
            // else ：如果已在数据上完成分割则本次无需判断是否在时间长度上需要被分割
            // 表明时间长度上要做分割
            if (_segmentPara[1] != 0 && !segFlag)
            {
                if (pkg.time - _current.header.time > _segmentPara[1])
                {
                    segFlag = true;
                }
            }

            if (segFlag)
            {
                var next = _current.CreateNext(pkg.time);

                _current.Close();
                _current = next;
            }

            EDCoder.Encoder.GetBytes(ref pkg);

            _current.Write(pkg);
        }

    }
}
