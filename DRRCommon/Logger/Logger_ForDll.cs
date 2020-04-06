using System;

namespace DRRCommon.Logger
{
    /// <summary>
    /// 为跨 Dll 调用的程序提供 Log
    /// </summary>
    public class Logger_ForDll : ILogger
    {
        public delegate void HandlerLogCompleted(string str);
        public event HandlerLogCompleted LogCompletedEvent;

        private string _newLine = "\r";
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

        private Logger_Base loggerBase;

        /// <summary>
        /// 为跨 Dll 调用的程序提供 Log
        /// </summary>
        /// <param name="handler">传入调用 DLL 方的处理函数</param>
        /// <param name="logMode">选择 LogMode，默认为 Trace 模式</param>
        /// <param name="newLine">换行字符</param>
        /// <param name="newSection">换段字符</param>
        public Logger_ForDll(HandlerLogCompleted handler, LogMode logMode = LogMode.Trace)
        {
            LogCompletedEvent += handler;

            loggerBase = Logger_Base.Create(logMode);

            newLine = _newLine;
            newSection = _newSection;
        }
        /// <summary>
        /// Log 空行
        /// </summary>
        public void WriteLine()
        {
            LogCompletedEvent?.Invoke(loggerBase?.Process());
        }
        /// <summary>
        /// Log 一行字符串
        /// </summary>
        /// <param name="str"></param>
        public void WriteLine(string str)
        {
            LogCompletedEvent?.Invoke(loggerBase?.Process(str));
        }
        /// <summary>
        /// Log Exception 对象
        /// </summary>
        /// <param name="e"></param>
        public void WriteLine(Exception e)
        {
            LogCompletedEvent?.Invoke(loggerBase?.Process(e));
        }
        /// <summary>
        /// Log 任意变量
        /// </summary>
        /// <param name="obj"></param>
        public void WriteLine(object obj)
        {
            LogCompletedEvent?.Invoke(loggerBase?.Process(obj));
        }
    }
}
