﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Mono.Options;
using SharpScan.Plugins;
using Tamir.SharpSsh.java.io;

namespace SharpScan
{
    public class Program
    {
        public static int onlinePC = 0;
        private static List<Task> scanTasks = new List<Task>();
        public static List<OnlinePC> onlineHostList = new List<OnlinePC>();
        public static List<string> IpPortList = new List<string>();
        public static int alivePort = 0;
        private static StreamWriter fileWriter;
        public static bool showHelp = false;
        public static bool icmpScan = false;
        public static bool arpScan = false;
        public static bool isUDP = false;
        public static bool POC = false;
        public static string Mode = "";
        public static string targetSegment = "";
        public static string outputFile = "";
        public static List<string> IPlist;
        public static string portRange = "";
        public static string maxConcurrency = "600";
        public static string delay = "10";
        public static string userName = "";
        public static string passWord = "";

        public class OnlinePC
        {
            public string IP { get; set; }
            public List<int> Port = new List<int>();
            public string Url { get; set; }
            public List<string> Service { get; set; }
            public string HostName { get; set; }
            public string OS { get; set; }
            public string Infostr { get; set; }
        }
        public static string StringPating = @"
  ______   __                                       ______                                
 /      \ /  |                                     /      \                               
/$$$$$$  |$$ |____    ______    ______    ______  /$$$$$$  |  _______   ______   _______  
$$ \__$$/ $$      \  /      \  /      \  /      \ $$ \__$$/  /       | /      \ /       \ 
$$      \ $$$$$$$  | $$$$$$  |/$$$$$$  |/$$$$$$  |$$      \ /$$$$$$$/  $$$$$$  |$$$$$$$  |
 $$$$$$  |$$ |  $$ | /    $$ |$$ |  $$/ $$ |  $$ | $$$$$$  |$$ |       /    $$ |$$ |  $$ |
/  \__$$ |$$ |  $$ |/$$$$$$$ |$$ |      $$ |__$$ |/  \__$$ |$$ \_____ /$$$$$$$ |$$ |  $$ |
$$    $$/ $$ |  $$ |$$    $$ |$$ |      $$    $$/ $$    $$/ $$       |$$    $$ |$$ |  $$ |
 $$$$$$/  $$/   $$/  $$$$$$$/ $$/       $$$$$$$/   $$$$$$/   $$$$$$$/  $$$$$$$/ $$/   $$/ 
                                        $$ |                                              
                                        $$ |                                              
                                        $$/                                               
";
        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: SharpScan [OPTIONS]\n");
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);

            Console.WriteLine("\nExample:");
            Console.WriteLine("  SharpScan.exe -t 192.168.1.1/24");
            Console.WriteLine("  SharpScan.exe -t 192.168.1.107 -p 100-1024");
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine(StringPating);

            var options = new OptionSet
            {
                { "i|icmp", "Perform icmp scan", i => icmpScan = i != null },
                { "a|arp", "Perform arp scan", a => arpScan = a != null },
                { "U|udp", "Perform udp scan", udp => isUDP = udp != null },
                { "t|Target=", "Target segment to scan", t => targetSegment = t },
                { "p|ports=", "Ports to scan (e.g. \"0-1024\" or \"80,443,8080\")", p => portRange = p },
                { "d|delay=", "Scan delay(ms),Defalt:1000", p => delay = p },
                { "t|Thread=", "Maximum number of concurrent scans(Thread number),Defalt:600", m => maxConcurrency = m },
                { "u|username=", "Username for authentication", u => userName = u },
                { "pw|password=", "Password for authentication", pw => passWord = pw },
                { "m|mode=", "Password for authentication", mode => Mode = mode },
                { "h|help", "Show this usage and help", h => showHelp = h != null },
                { "o|output=", "Output file to save console output", o => outputFile = o }
            };

            try
            {
                options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine($"SharpScan: {e.Message}");
                Console.WriteLine("Try `SharpScan --help` for more information.");
                return;
            }

            if (showHelp)
            {
                ShowHelp(options);
                return;
            }

            if (!icmpScan && !arpScan)
            {
                icmpScan = true;
            }
            Console.WriteLine($"Delay:{delay}   MaxConcurrency:{maxConcurrency}");
            new SetTls12UserRegistryKeys();
            if (!string.IsNullOrEmpty(targetSegment))
            {
                IPlist = SharpScan.GetIP.IPList(targetSegment);
            }
            if (!string.IsNullOrEmpty(portRange))
            {
                if (isUDP)
                {
                    await new UdpPortscan().ScanPortRange(targetSegment, portRange, Convert.ToInt32(delay), Convert.ToInt32(maxConcurrency));

                }
                else { await new TcpPortscan().ScanPortRange(targetSegment, portRange, Convert.ToInt32(delay), Convert.ToInt32(maxConcurrency)); }

                return;
            }

            if (string.IsNullOrEmpty(targetSegment))
            {
                ShowHelp(options);
                return;
            }

            if (!string.IsNullOrEmpty(outputFile))
            {
                fileWriter = new StreamWriter(outputFile, false) { AutoFlush = true };
                var multiTextWriter = new MultiTextWriter(Console.Out, fileWriter);
                Console.SetOut(multiTextWriter);
                Console.SetError(multiTextWriter);
            }

            Console.WriteLine("\r\nC_Segment: " + targetSegment + ".");
            Console.WriteLine("===================================================================");
            Console.WriteLine($"{"IP",-28} {"HostName",-28} {"OsVersion",-40}");

            if (icmpScan)
            {
                await Task.Run(() => new ICMPScan().ICMPScanPC(Program.IPlist, Convert.ToInt32(delay), Convert.ToInt32(maxConcurrency)));
                Console.WriteLine("===================================================================");
                Console.WriteLine("[+] onlinePC: " + Program.onlinePC);
                Console.WriteLine("===================================================================");
            }

            if (arpScan)
            {
                var arpTask = Task.Run(() => new ARPScan().ARPScanPC(Program.IPlist, Convert.ToInt32(delay), Convert.ToInt32(maxConcurrency)));
                await Task.WhenAll(arpTask);
                Console.WriteLine("===================================================================");
                Console.WriteLine("[+] onlinePC: " + Program.onlinePC);
                Console.WriteLine("===================================================================");
            }

            await new TcpPortscan().ScanPortAsync(Convert.ToInt32(delay), Configuration.PortList, Convert.ToInt32(maxConcurrency));

            Console.WriteLine("===================================================================");
            Console.WriteLine($"[+] alive ports len is: {alivePort}");
            Console.WriteLine("===================================================================");
            GC.Collect();

            new DomainCollect();

            await new HandlePOC().HandleDefault();

            if (fileWriter != null)
            {
                fileWriter.Close();
            }
        }
    }
}
