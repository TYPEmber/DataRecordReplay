using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DRRCommon.Logger
{
    /// <summary>
    /// Log 集合
    /// </summary>
    public class Loggers
    {
        private static ConcurrentDictionary<int, ILogger> _Loggers = new ConcurrentDictionary<int, ILogger>();
        /// <summary>
        /// 添加
        /// 以 + 号后 Log 的 HashCode 为键存储
        /// </summary>
        /// <param name="loggers"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static Loggers operator +(Loggers loggers, ILogger logger)
        {
            _Loggers.TryAdd(logger.GetHashCode(), logger);
            return loggers;
        }
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="loggers"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static Loggers operator -(Loggers loggers, ILogger logger)
        {
            _Loggers.TryRemove(logger.GetHashCode(), out logger);
            return loggers;
        }

        /// <summary>
        /// Log 空行
        /// </summary>
        public void WriteLine()
        {
            var logList = _Loggers.Values.ToList();
            foreach (var log in logList)
            {
                log.WriteLine();
            }
        }
        /// <summary>
        /// Log 一行字符串
        /// </summary>
        /// <param name="str"></param>
        public void WriteLine(string str)
        {
            var logList = _Loggers.Values.ToList();
            foreach (var log in logList)
            {
                log.WriteLine(str);
            }
        }
        /// <summary>
        /// Log Exception 对象
        /// </summary>
        /// <param name="e"></param>
        public void WriteLine(Exception e)
        {
            var logList = _Loggers.Values.ToList();
            foreach (var log in logList)
            {
                log.WriteLine(e);
            }
        }
        /// <summary>
        /// Log 任意变量
        /// </summary>
        /// <param name="obj"></param>
        public void WriteLine(object obj)
        {
            var logList = _Loggers.Values.ToList();
            foreach (var log in logList)
            {
                log.WriteLine(obj);
            }
        }
    }
}
