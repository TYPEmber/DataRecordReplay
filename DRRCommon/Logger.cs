using System;
using System.Collections.Generic;
using System.Text;

using DRRCommon.Logger;

namespace DRRCommon.Logger
{
    public static class Logger
    {
        public static ILogger Debug = new Logger_ForLocal(logMode: LogMode.Debug);
    }
}
