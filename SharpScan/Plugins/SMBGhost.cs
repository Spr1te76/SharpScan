﻿using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
namespace SharpScan
{
    public class SMBGhost
    {
        public void SMBGhostScan(string ipAddress)
        {
            int port = 445;
            int timeout = 10000; // 10 seconds
            int retryCount = 3;

            byte[] packet = BuildPacket();

            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                    {
                        socket.Connect(new IPEndPoint(IPAddress.Parse(ipAddress), port));
                        socket.SendTimeout = timeout;
                        socket.ReceiveTimeout = timeout;
                        if (socket.Connected)
                        {
                            //Console.WriteLine("calc");
                            socket.Send(packet);
                            byte[] buffer = new byte[1024];
                            int received = socket.Receive(buffer);

                            if (received == 0)
                            {
                                Console.WriteLine("No response received from the server.");
                                return;
                            }

                            if (IsVulnerable(buffer, received))
                            {
                                Console.WriteLine($"[+] {ipAddress} CVE-2020-0796 SmbGhost Vulnerable");
                            }
                            else
                            {
                                Console.WriteLine($"[-] {ipAddress} Not Vulnerable");
                            }


                        }


                        break;
                    }
                }
                catch (SocketException ex)
                {
                    //Console.WriteLine($"Socket error: {ex.Message}");
                    //if (i == retryCount - 1)
                    //{
                    //    Console.WriteLine("Maximum retry attempts reached. Exiting.");
                    //}
                    //else
                    //{
                    //    Console.WriteLine("Retrying...");
                    //}
                }
                catch (Exception ex)
                {
                    //Console.WriteLine($"Unexpected error: {ex.Message}");
                    break;
                }
            }
        }

        static byte[] BuildPacket()
        {
            return new byte[]
            {
            0x00, 0x00, 0x00, 0xc0,
            0xfe, 0x53, 0x4d, 0x42, 0x40, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x24, 0x00, 0x08, 0x00, 0x01, 0x00, 0x00, 0x00, 0x7f, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x78, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x02, 0x02, 0x10, 0x02, 0x22, 0x02, 0x24, 0x02, 0x00, 0x03, 0x02, 0x03, 0x10, 0x03, 0x11, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x26, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x20, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x0a, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00
            };
        }

        static bool IsVulnerable(byte[] response, int length)
        {
            if (length < 76)
            {
                return false;
            }

            return response.Length >= 76 &&
                   response[72] == 0x11 && response[73] == 0x03 &&
                   response[74] == 0x02 && response[75] == 0x00;
        }
    }
}

