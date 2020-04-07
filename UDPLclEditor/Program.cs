using System;
using System.Collections.Generic;
using Core;

namespace UDPLclEditor
{
    class Program
    {
        static EditCore core;
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            core = new EditCore(new List<string>()
            {
                "../../../../UDPReplayer/bin/Release/netcoreapp3.1/SJG/SJG-5_0.lcl",
                "../../../../UDPReplayer/bin/Release/netcoreapp3.1/SJG/SJG-5_1.lcl",
            });

            core.Clip(0, 481, new double[] { 100, 600 }, "SJG_Clip", "SJG-5");
        }
    }
}
