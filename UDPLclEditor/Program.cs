using System;
using System.Collections.Generic;

using Core;
using DRRCommon.Logger;

namespace UDPLclEditor
{
    class Program
    {
        static EditCore core;
        static void Main(string[] args)
        {
            if (args.Length % 2 != 0)
            {
                Logger.Error.WriteLine("Para Input Error!");
                return;
            }

            Dictionary<string, string> para = new Dictionary<string, string>();

            for (int i = 0; i < args.Length; i += 2)
            {
                para.Add(args[i], args[i + 1]);
            }

            core = new EditCore(new List<string>()
            {
                "../../../../UDPReplayer/bin/Release/netcoreapp3.1/SJG/SJG-5_0.lcl",
                "../../../../UDPReplayer/bin/Release/netcoreapp3.1/SJG/SJG-5_1.lcl",
            });

            core.Clip(0, 481, new double[] { 100, 600 }, "SJG_Clip", "SJG-5");
        }
    }
}
