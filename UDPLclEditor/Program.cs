using System;
using System.Collections.Generic;
using System.IO;
using Core;
using DRRCommon.Logger;

namespace UDPLclEditor
{
    class Program
    {
        static EditCore core;

        static void LoadPara(string[] args)
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

            string[] paths = null;
            // 加载单一文件
            if (para.ContainsKey("-f"))
            {
                if (File.Exists(para["-f"]) && para["-f"].EndsWith(".lcl"))
                {
                    paths = new string[] { para["-f"] };
                }
                else
                {
                    Logger.Error.WriteLine("No Such File!");
                    return;
                }
            }
            // 加载文件夹
            else if (para.ContainsKey("-p"))
            {
                if (Directory.Exists(para["-p"]))
                {
                    paths = Directory.GetFiles(para["-p"], "*.lcl");
                }
                else
                {
                    Logger.Error.WriteLine("No Such Path!");
                    return;
                }
            }
            else
            {
                Logger.Error.WriteLine("No File Path!");
                return;
            }

            double[] segPara = new double[] { 2048, 3600 };
            if (para.ContainsKey("-s"))
            {
                var ss = para["-s"].Split(" ");
                try
                {
                    segPara[0] = double.Parse(ss[0]);
                    segPara[1] = double.Parse(ss[1]);
                }
                catch (Exception)
                {
                    Logger.Error.WriteLine("Wrong SegMentPara!");
                    return;
                }

            }
            else
            {
                Logger.Error.WriteLine("No SegMentPara!");
                return;
            }

            core = new EditCore(paths);

            // 获取文件 info
            var fInfo = core.FileInfo;

            Logger.Info.WriteLine("TotalIndex: " + fInfo.totalIndex
                    + " StartTime: " + fInfo.time
                    + " TotalTime: " + fInfo.totalIndex * fInfo.timeInterval + "s");

            string path = "Clip_"+ DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss") + "/";
            string name = "data";
            if (para.ContainsKey("-op"))
            {
                path = para["-op"];
            }

            if (para.ContainsKey("-of"))
            {
                if (para["-of"].Contains("/"))
                {
                    Logger.Error.WriteLine("File name can not contains '/'!");
                    return;
                }
                else
                {
                    name = para["-of"];
                }
            }

            int si, ei;
            if (para.ContainsKey("-r"))
            {
                var s = para["-r"].Split(" ");

                try
                {
                    si = int.Parse(s[0]);
                    ei = int.Parse(s[1]);
                }
                catch (Exception)
                {
                    Logger.Error.WriteLine("Wrong Input For Clip Range!");
                    return;
                }

                if (si < 0 || ei > fInfo.totalIndex - 1 || si > ei)
                {
                    Logger.Error.WriteLine("Wrong Input For Clip Range!");
                    return;
                }
            }
            else
            {
                Logger.Error.WriteLine("No Clip Range!");
                return;
            }

            core.Clip(si, ei, segPara, path, name);

            Logger.Info.WriteLine("Clip Completed!");
        }

        static void Main(string[] args)
        {
            LoadPara(args);

            while (true)
            {
                var key = Console.ReadKey();

                if (key.Key == ConsoleKey.C)
                {
                    Logger.Info.WriteLine();
                    Logger.Info.WriteLine("Close?");

                    var k = Console.ReadKey();

                    if (k.Key == ConsoleKey.Y)
                    {
                        return;
                    }
                    else
                    {
                        // 换行
                        Logger.Info.WriteLine();
                    }
                }
            }
        }
    }
}
