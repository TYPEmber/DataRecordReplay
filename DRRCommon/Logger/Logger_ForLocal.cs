#define TRACE
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DRRCommon.Logger
{
    /// <summary>
    /// Log 到控制台输出
    /// </summary>
    public class Logger_ForLocal : ILogger
    {
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
        private string _newSection = "";
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
        /// 输出模式
        /// </summary>
        public enum OutputMode
        {
            /// <summary>
            /// 指定输出到控制台
            /// </summary>
            Console,
            /// <summary>
            /// 指定输出到标准输出流
            /// </summary>
            Standard
        }

        private Logger_Base loggerBase;
        /// <summary>
        /// Log 到控制台输出
        /// </summary>
        /// <param name="outMode">选择 OutputMode，默认为输出到控制台模式</param>
        /// <param name="logMode">选择 LogMode，默认为 Trace 模式</param>
        public Logger_ForLocal(OutputMode outMode = OutputMode.Console, LogMode logMode = LogMode.Trace)
        {
            loggerBase = Logger_Base.Create(logMode);

            newLine = _newLine;
            newSection = _newSection;

            switch (outMode)
            {
                case OutputMode.Console:
                    HandlerWrite = Console.Write;
                    break;
                case OutputMode.Standard:
                    HandlerWrite = (string str)=> { Trace.Write(str); };
                    break;
                default:
                    break;
            }

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    while (_queue.Count > 0)
                    {
                        if(_queue.TryDequeue(out string str))
                        {
                            HandlerWrite(str);
                        }
                    }

                    Thread.Sleep(1);
                }
            });
        }

        ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();

        delegate void DelegateWrite(string str);
        DelegateWrite HandlerWrite;

        /// <summary>
        /// Log 空行
        /// </summary>
        public void WriteLine()
        {
            _queue.Enqueue(loggerBase?.Process());
        }
        /// <summary>
        /// Log 一行字符串
        /// </summary>
        /// <param name="str"></param>
        public void WriteLine(string str)
        {
            _queue.Enqueue(loggerBase?.Process(str));
        }
        /// <summary>
        /// Log Exception 对象
        /// </summary>
        /// <param name="e"></param>
        public void WriteLine(Exception e)
        {
            _queue.Enqueue(loggerBase?.Process(e));
        }
        /// <summary>
        /// Log 任意变量
        /// </summary>
        /// <param name="obj"></param>
        public void WriteLine(object obj)
        {
            _queue.Enqueue(loggerBase?.Process(obj));
        }
    }
}
