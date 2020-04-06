using System;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using System.Collections.Concurrent;

namespace DRRCommon.Logger
{
    /// <summary>
    /// 常用文件IO操作类
    /// </summary>
    public class Logger_ForFile : ILogger
    {
        private Logger_Base loggerBase;

        private string _newLine = Environment.NewLine;
        /// <summary>
        /// 分行符
        /// </summary>
        public string newLine
        {
            set
            {
                _newLine = value;
                if (loggerBase != null)
                {
                    loggerBase.NewLine = _newLine;
                }
            }
            get { return _newLine; }
        }
        private string _newSection = Environment.NewLine;
        /// <summary>
        /// 分段符
        /// </summary>
        public string newSection
        {
            set
            {
                _newSection = value;
                if (loggerBase != null)
                {
                    loggerBase.NewSection = _newSection;
                }
            }
            get { return _newSection; }
        }

        private bool _TimeStampEnable = false;
        public bool TimeStampEnable
        {
            set
            {
                _TimeStampEnable = value;
                if (loggerBase != null)
                {
                    loggerBase.SetTimeStamp(_TimeStampEnable);
                }
            }
            get
            { return _TimeStampEnable; }
        }


        /// <summary>
        /// 记录分割模式
        /// </summary>
        public enum SegmentMode
        {
            /// <summary>
            /// 从不分割
            /// </summary>
            Never,
            /// <summary>
            /// 按月分割
            /// </summary>
            Month,
            /// <summary>
            /// 按周分割
            /// </summary>
            Week,
            /// <summary>
            /// 按天分割
            /// </summary>
            Day,
            /// <summary>
            /// 按小时分割
            /// </summary>
            Hour,
            /// <summary>
            /// 按分钟分割
            /// </summary>
            Minute
        }

        private ConcurrentQueue<string> _QueueLog;

        private string _Path = "Log\\";
        private string Path
        {
            set
            {
                if (value.Substring(value.Length - 1, 1) == "\\")
                {
                    _Path = value;
                }
                else
                {
                    _Path = value + "\\";
                }
            }
            get { return _Path; }
        }
        private delegate bool HandlerGetFileNameCheck(DateTime dateTime);
        HandlerGetFileNameCheck GetFileNameCheck;

        private string GetFileName(DateTime dateTime)
        {
            if (GetFileNameCheck(dateTime))
            {
                _TimeStampStart = dateTime;
            }

            return _TimeStampStart.ToString(_TimeFormat) + ".txt";
        }

        private DateTime _TimeStampStart;
        private string _TimeFormat = "yyyy-MM-dd_HH-mm-ss";

        /// <summary>
        /// Log 到 txt 文件中
        /// </summary>
        /// <param name="path">路径 默认为程序运行路径下的 Log 文件夹</param>
        /// <param name="logMode">选择 LogMode，默认为 Trace 模式</param>
        /// <param name="segmentMode">选择 Log 的记录分割模式。默认为按天分割</param>
        /// <param name="flushInterval">将缓存写入文件的周期，单位为毫秒。默认 10000ms</param>
        public Logger_ForFile(string path = "Log\\", LogMode logMode = LogMode.Trace, SegmentMode segmentMode = SegmentMode.Day, int flushInterval = 10000)
        {
            _TimeStampStart = DateTime.Now;

            loggerBase = Logger_Base.Create(logMode);

            newLine = _newLine;
            newSection = _newSection;

            // 校验 flushInterval 取值范围
            // 不能为 0 以避免自旋消耗 CPU 性能
            flushInterval = flushInterval > 0 ? flushInterval : 1;


            _QueueLog = new ConcurrentQueue<string>();
            Path = path;

            switch (segmentMode)
            {
                case SegmentMode.Never:
                    GetFileNameCheck = (DateTime dateTime) => { return false; };
                    break;
                case SegmentMode.Month:
                    GetFileNameCheck = (DateTime dateTime) => { return dateTime.Month != _TimeStampStart.Month; };
                    break;
                case SegmentMode.Week:
                    GetFileNameCheck = (DateTime dateTime) => { return dateTime.GetWeek() != _TimeStampStart.GetWeek(); };
                    break;
                case SegmentMode.Day:
                    GetFileNameCheck = (DateTime dateTime) => { return dateTime.Day != _TimeStampStart.Day; };
                    break;
                case SegmentMode.Hour:
                    GetFileNameCheck = (DateTime dateTime) => { return dateTime.Hour != _TimeStampStart.Hour; };
                    break;
                case SegmentMode.Minute:
                    GetFileNameCheck = (DateTime dateTime) => { return dateTime.Minute != _TimeStampStart.Minute; };
                    break;
                default:
                    break;
            }

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (_QueueLog.Count != 0)
                    {
                        string logBuff;
                        string header = "============" + DateTime.Now.ToString(_TimeFormat) + "============" + newLine;
                        string log = "";
                        string fileName = _Path + "Log_" + GetFileName(DateTime.Now);
                        while (_QueueLog.Count != 0)
                        {
                            if (_QueueLog.TryDequeue(out logBuff))
                            {
                                log += logBuff;
                            }
                        }
                        if (log != string.Empty)
                        {
                            try
                            {
                                Directory.CreateDirectory(_Path);
                                File.AppendAllText(fileName, header + log, Encoding.UTF8);
                            }
                            catch (Exception e)
                            {
                                throw e;
                            }
                        }
                    }

                    System.Threading.Thread.Sleep(flushInterval);
                }
            });
        }

        /// <summary>
        /// Log 空行
        /// </summary>
        public void WriteLine()
        {
            _QueueLog.Enqueue(loggerBase?.Process());
        }
        /// <summary>
        /// Log 一行字符串
        /// </summary>
        /// <param name="str"></param>
        public void WriteLine(string str)
        {
            _QueueLog.Enqueue(loggerBase?.Process(str));
        }
        /// <summary>
        /// Log Exception 对象
        /// </summary>
        /// <param name="e"></param>
        public void WriteLine(Exception e)
        {
            _QueueLog.Enqueue(loggerBase?.Process(e));
        }
        /// <summary>
        /// Log 任意变量
        /// </summary>
        /// <param name="obj"></param>
        public void WriteLine(object obj)
        {
            _QueueLog.Enqueue(loggerBase?.Process(obj));
        }
    }

    static class DateTimeExtension
    {
        public static int GetWeek(this DateTime dateTime)
        {
            DateTime time = Convert.ToDateTime(dateTime.ToString("yyyy") + "-01-01");
            TimeSpan ts = dateTime - time;
            int index = (int)time.DayOfWeek;
            int day = int.Parse(ts.TotalDays.ToString("F0"));
            if (index == 0)
            {
                day--;
            }
            else
            {
                day = day - (7 - index) - 1;
            }
            int week = ((day + 7) / 7) + 1;
            return week;
        }
    }
}
