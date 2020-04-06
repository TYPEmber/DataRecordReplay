using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace DRRCommon.Logger
{
    /// <summary>
    /// Logger 接口
    /// </summary>
    public interface ILogger
    {
        bool TimeStampEnable { set; get; }
        /// <summary>
        /// 分行符
        /// </summary>
        string newLine { set; get; }
        /// <summary>
        /// 分段符
        /// </summary>
        string newSection { set; get; }
        /// <summary>
        /// Log 空行
        /// </summary>
        void WriteLine();
        /// <summary>
        /// Log 一行字符串
        /// </summary>
        /// <param name="str"></param>
        void WriteLine(string str);
        /// <summary>
        /// Log Exception 对象
        /// </summary>
        /// <param name="e"></param>
        void WriteLine(Exception e);
        /// <summary>
        /// Log 任意变量
        /// </summary>
        /// <param name="obj"></param>
        void WriteLine(object obj);
    }

    //public static class Logger
    //{
    //    private static ILogger _Logger;

    //    public static void Init(ILogger logger)
    //    {
    //        _Logger = logger;
    //    }

    //    public static void WriteLine()
    //    {
    //        _Logger.WriteLine();
    //    }

    //    public static void WriteLine(string str)
    //    {
    //        _Logger.WriteLine(str);
    //    }

    //    public static void WriteLine(Exception e)
    //    {
    //        _Logger.WriteLine(e);
    //    }

    //    public static void WriteLine(object obj)
    //    {
    //        _Logger.WriteLine(obj);
    //    }
    //}

    /// <summary>
    /// Log 模式枚举类
    /// </summary>
    public enum LogMode
    {
        /// <summary>
        /// 仅在连接调试器模式下 Log
        /// </summary>
        Debug,
        /// <summary>
        /// 在所有模式下 Log
        /// </summary>
        Trace
    }

    class Logger_Base
    {
        /// <summary>
        /// private 避免被外界直接通过 new 方法实例化
        /// </summary>
        private Logger_Base()
        {
            SetTimeStamp(false);
        }
        /// <summary>
        /// 构造 Logger_Base 实例
        /// 不适用 new 方法以便返回 null
        /// </summary>
        /// <param name="logMode"></param>
        /// <returns></returns>
        public static Logger_Base Create(LogMode logMode = LogMode.Trace)
        {
            switch (logMode)
            {
                case LogMode.Debug:
                    if (Debugger.IsAttached)
                    {
                        return new Logger_Base();
                    }
                    return null;
                case LogMode.Trace:
                    return new Logger_Base();
                default:
                    return new Logger_Base();
            }
        }

        public void SetTimeStamp(bool enable)
        {
            if (enable)
            {
                ProcessDele = () => { return DateTime.UtcNow.ToString() + " "; };
            }
            else
            {
                ProcessDele = () => { return ""; };
            }
        }

        public delegate string ProcessDelegate();
        ProcessDelegate ProcessDele;

        public string NewLine { set; get; } = Environment.NewLine;

        public string NewSection { set; get; } = Environment.NewLine;

        public bool TimeStampEnable { set; get; } = false;

        public string Process()
        {
            return Process("");
        }

        public string Process(string str)
        {
            return ProcessDele.Invoke() + str + NewLine + NewSection;
        }

        public string Process(Exception ex)
        {
            string buffStr = DateTime.UtcNow + NewLine
            + "Message:" + ex.Message + NewLine
            + "Data:" + ex.Data + NewLine
            + "InnerException:" + ex.InnerException + NewLine
            + "Source:" + ex.Source + NewLine
            + "StackTrace:" + ex.StackTrace + NewLine
            + "TargetSite:" + ex.TargetSite + NewLine;

            return buffStr + NewSection;
        }

        public string Process(object obj)
        {
            var buffStr = "";

            if (obj == null)
            {
                return "NULL" + NewLine + NewSection;
            }

            var type = obj.GetType();

            if (obj.ToString() != type.FullName)
            {
                return obj.ToString() + NewLine + NewSection;
            }

            buffStr += "FullName: " + type.FullName + NewLine;

            if (!type.IsValueType)
            {
                foreach (var name in type.GetProperties().Select(x => x.Name))
                {
                    buffStr += name + " = " + type.GetProperty(name).GetValue(obj, null) + NewLine;
                }
            }
            else
            {
                foreach (var name in type.GetFields().Select(x => x.Name))
                {
                    buffStr += name + " = " + type.GetField(name).GetValue(obj) + NewLine;
                }
            }

            return ProcessDele.Invoke() + buffStr + NewSection;
        }
    }
}
