﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Mono.Options;
using SharpRDPCheck;
using SharpScan.Plugins;
using Tamir.SharpSsh.Sharp.io;
using static System.Net.WebRequestMethods;

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
        public static bool icmpScan = true;
        public static bool arpScan = false;
        public static bool isUDP = false;
        public static bool nopoc = false;
        public static string mode { get; set; }
        public static string hTarget { get; set; }
        public static string search { get; set; }

        public static List<string> IPlist;
        public static string portRange { get; set; }
        public static string socks5 { get; set; }
        public static string command { get; set; }
        public static string maxConcurrency = "600";
        public static string delay = "10";
        public static string userName { get; set; }
        public static string userNameFile { get; set; }
        public static string passWord { get; set; }
        public static string passWordFile { get; set; }
        public static List<string> userList { get; set; }
        public static List<string> passwordList { get; set; }
        public static string outputFile = "";


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
            Console.WriteLine("  SharpScan.exe -help");
            Console.WriteLine("  SharpScan.exe -h 192.168.1.1/24");
            Console.WriteLine("  SharpScan.exe -h 192.168.1.107 -p 100-1024");
        }

        public static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(StringPating);

            var options = new OptionSet
            {
                { "i|icmp", "Perform icmp scan", i => icmpScan = i != null },
                { "a|arp", "Perform arp scan", a => arpScan = a != null },
                { "U|udp", "Perform udp scan", udp => isUDP = udp != null },
                { "h|hTarget=", "Target segment to scan", h => hTarget = h },
                { "p|ports=", "Ports to scan (e.g. \"0-1024\" or \"80,443,8080\")", p => portRange = p },
                { "u|username=", "Username for authentication", u => userName = u },
                { "pw|password=", "Password for authentication", pwd => passWord = pwd },
                { "uf|ufile=", "Username file for authentication", uf => userNameFile = uf },
                { "pwf|pwdfile=", "Password file for authentication", pwdf => passWordFile = pwdf },
                { "m|mode=", "Scanning poc mode(e.g. ssh/smb/rdp/ms17010)", m => Program.mode = m },
                { "c|command=", "Command Execution", c => command = c },
                { "d|delay=", "Scan delay(ms),Defalt:1000", p => delay = p },
                { "t|thread=", "Maximum num of concurrent scans,Defalt:600", t => maxConcurrency = t },
                { "s|search=", "Search all files", s => search = s },
                { "socks5=", "Open socks5 port", socks5 => Program.socks5 = socks5 },
                { "nopoc", "Not using proof of concept(POC)", nopoc => Program.nopoc =nopoc!= null },
                { "o|output=", "Output file to save console output", o => Program.outputFile = o },
                { "help|show", "Show this usage and help", h => showHelp = h != null },
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
            if (args.Length < 2 || showHelp)
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


            await Init(args);

            if (fileWriter != null)
            {
                fileWriter.Close();
            }
            Console.ResetColor();
        }




        static async Task Init(string[] args)
        {
            if (!string.IsNullOrEmpty(search))
            {
                string[] drives = Environment.GetLogicalDrives();
                FileSearch.Start(drives);
                return;
            }

            Console.WriteLine($"Delay:{delay}   MaxConcurrency:{maxConcurrency}");
            new SetTls12UserRegistryKeys();


            if (!string.IsNullOrEmpty(hTarget))
            {
                IPlist = SharpScan.GetIP.IPList(hTarget);
            }


            if (!string.IsNullOrEmpty(socks5))
            {
                new Socks5().Run(Convert.ToInt32(socks5), Program.userName, Program.passWord);
                return;
            }

            if (!string.IsNullOrEmpty(userNameFile) && System.IO.File.Exists(userNameFile))
            {
                string[] lines = System.IO.File.ReadAllLines(userNameFile);
                userList = new List<string>(lines);
            }

            if (!string.IsNullOrEmpty(passWordFile) && System.IO.File.Exists(passWordFile))
            {
                string[] lines = System.IO.File.ReadAllLines(passWordFile);
                passwordList = new List<string>(lines);
            }


            if (!string.IsNullOrEmpty(portRange))
            {
                if (isUDP)
                {
                    await new UdpPortscan().ScanPortRange(hTarget, portRange, Convert.ToInt32(delay), Convert.ToInt32(maxConcurrency));
                }
                else
                {
                    await new TcpPortscan().ScanPortRange(hTarget, portRange, Convert.ToInt32(delay), Convert.ToInt32(maxConcurrency));
                }
                return;
            }


            if (!string.IsNullOrEmpty(mode))
            {
                await new HandlePOC().ModPacket(mode);
                return;
            }


            if (arpScan)
            {
                icmpScan = false;
                var arpTask = Task.Run(() => new ARPScan().ARPScanPC(Program.IPlist, Convert.ToInt32(delay), Convert.ToInt32(maxConcurrency)));
                await Task.WhenAll(arpTask);

            }

            if (icmpScan)
            {
                await Task.Run(() => new ICMPScan().ICMPScanPC(Program.IPlist, Convert.ToInt32(delay), Convert.ToInt32(maxConcurrency)));
            }



            await new TcpPortscan().ScanPortDefault(Convert.ToInt32(delay), Configuration.PortList, Convert.ToInt32(maxConcurrency));


            if (!nopoc)
            {
                new DomainCollect();
                await new HandlePOC().HandleDefault();
            }

        }
    }
}
