using System;
using System.Collections.Generic;
using System.Text;

using DRRCommon.Logger;

namespace DRRCommon.Logger
{
    public static class Logger
    {
        public static ILogger Debug = new Logger_ForLocal(logMode: LogMode.Debug);
        public static ILogger Info = new Logger_ForLocal(logMode: LogMode.Trace);
        //public static ILogger Error = new Logger_ForFile(path:"Error\\", flushInterval:100);
        public static ILogger Error = new Logger_ForLocal(logMode: LogMode.Trace);
    }
}
