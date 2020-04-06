using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DRRCommon.Logger
{
    /// <summary>
    /// 为跨进程调用的程序提供 Log
    /// </summary>
    public class Logger_ForProcess
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


        private Logger_Base loggerBase;
        /// <summary>
        /// Log 到控制台输出
        /// </summary>
        /// <param name="outMode">选择 OutputMode，默认为输出到控制台模式</param>
        /// <param name="logMode">选择 LogMode，默认为 Trace 模式</param>
        public Logger_ForProcess(LogMode logMode = LogMode.Trace)
        {
            loggerBase = Logger_Base.Create(logMode);

        }
        /// <summary>
        /// Log 一行字符串
        /// </summary>
        /// <param name="str"></param>
        public void WriteLine(string str)
        {

        }
        /// <summary>
        /// Log Exception 对象
        /// </summary>
        /// <param name="e"></param>
        public void WriteLine(Exception e)
        {

        }
        /// <summary>
        /// Log 任意变量
        /// </summary>
        /// <param name="obj"></param>
        public void WriteLine(object obj)
        {

        }
    }
}
