using System;
using System.Collections.Generic;
using System.Net;

using DRRCommon;
using DRRCommon.Network;
using DRRCommon.Logger;
using System.IO;

namespace UDPReplayer
{
    class Program
    {
        static ReplayCore.Core core;

        static void ParseInputIpPort(string s, out string ip, out ushort port)
        {
            ip = "";
            port = 0;

            var p = s.Split(":");

            // 仅指定 port
            if (p.Length == 1)
            {
                ip = "0.0.0.0";
            }
            // 指定 ip 与 port
            else if (p.Length == 2)
            {
                ip = p[0];
                p[0] = p[1];
            }
            else
            {
                Logger.Error.WriteLine("Wrong Reciver Para!");
            }

            try
            {
                port = ushort.Parse(p[0]);
            }
            catch (Exception)
            {
                Logger.Error.WriteLine("Wrong Reciver Port!");
            }
        }

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

            if (para.Count == 0)
            {
                para.Add("-t", "para.txt");
            }

            if (para.ContainsKey("-t"))
            {
                if (File.Exists(para["-t"]))
                {
                    var str = File.ReadAllText(para["-t"]);

                    List<string> buff = new List<string>();
                    args = File.ReadAllText(para["-t"]).Split(" ");
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i].Contains("\""))
                        {
                            for (int j = i + 1; j < args.Length; j++)
                            {
                                args[i] += " " + args[j];

                                if (args[j].Contains("\""))
                                {
                                    buff.Add(args[i].Trim('\"'));
                                    i = j;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            buff.Add(args[i]);
                        }
                    }

                    args = buff.ToArray();

                    for (int i = 0; i < args.Length; i += 2)
                    {
                        para.Add(args[i], args[i + 1]);
                    }
                }
                else
                {
                    Logger.Error.WriteLine("No Such ParaFile!");
                    return;
                }
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


            // 加载映射关系
            var map = new Dictionary<IPEndPoint, IPEndPoint>();
            if (para.ContainsKey("-m"))
            {
                var ppp = para["-m"].Split(" ");

                foreach (var it in ppp)
                {
                    var pp = it.Split("=>");

                    ParseInputIpPort(pp[0], out string ipO, out ushort portO);
                    ParseInputIpPort(pp[1], out string ipM, out ushort portM);

                    map.Add(new IPEndPoint(IPAddress.Parse(ipO), portO), new IPEndPoint(IPAddress.Parse(ipM), portM));
                }
            }
            else
            {
                Logger.Error.WriteLine("No Map Para!");
            }

            core = new ReplayCore.Core(paths, map);

            // 显示 info
            var info = core.GetFileInfo();

            Logger.Info.WriteLine("Notes: " + info.notes);

            Logger.Info.WriteLine("Maps: ");
            foreach (var point in info.points)
            {
                if (map.ContainsKey(point))
                {
                    Logger.Info.WriteLine(point + " => " + map[point]);
                }
                else
                {
                    Logger.Info.WriteLine(point + " => ");
                }
            }

            UDPSender sender = new UDPSender();

            core.SendHandler = (ReadOnlySpan<byte> bytes, IPEndPoint point) =>
            {
                sender.Send(bytes.ToArray(), point);
            };

            while (true)
            {
                var key = Console.ReadKey();

                // 播放
                // 暂停
                if (key.Key == ConsoleKey.P)
                {
                    Logger.Info.WriteLine();
                    core.P();
                }
                // 跳至
                else if (key.Key == ConsoleKey.J)
                {
                    Logger.Info.WriteLine();
                    Logger.Info.WriteLine("JumpTo: ");

                    bool flag = false;
                    if (core.IsPlaying)
                    {
                        flag = true;
                        core.P();
                    }

                    var line = Console.ReadLine();
                    if (line.EndsWith("%"))
                    {
                        line = line.TrimEnd('%');
                        double per = double.Parse(line) / 100.0;
                        if (per > 1)
                        {
                            per = 1;
                        }
                        else if (per < 0)
                        {
                            per = 0;
                        }
                        var index = info.totalIndex * per;
                        core.JumpTo((long)index);
                    }

                    if (flag)
                    {
                        core.P();
                    }
                }
                // 播放速率
                else if (key.Key == ConsoleKey.R)
                {
                    Logger.Info.WriteLine();
                    Logger.Info.WriteLine("SpeedRate: ");

                    bool flag = false;
                    if (core.IsPlaying)
                    {
                        flag = true;
                        core.P();
                    }

                    var line = Console.ReadLine();


                    double speed = double.Parse(line);

                    if (speed < 0)
                    {
                        speed = 1;
                    }

                    core.SpeedRate = speed;


                    if (flag)
                    {
                        core.P();
                    }
                }
                // 关闭
                else if (key.Key == ConsoleKey.C)
                {
                    Logger.Info.WriteLine();
                    Logger.Info.WriteLine("Close?");

                    bool flag = false;
                    if (core.IsPlaying)
                    {
                        flag = true;
                        core.P();
                    }

                    var k = Console.ReadKey();

                    if (k.Key == ConsoleKey.Y)
                    {
                        return;
                    }

                    if (flag)
                    {
                        core.P();
                    }
                }
            }
        }
    }
}
