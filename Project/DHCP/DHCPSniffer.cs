﻿using System;
using SharpPcap;

namespace DHCP
{
    public class DHCPSniffer
    {
        public static void Main()
        {
            // Print SharpPcap version
            var ver = Pcap.SharpPcapVersion;
            Console.WriteLine("SharpPcap {0}, Example5.PcapFilter.cs\n", ver);

            // Retrieve the device list
            var devices = CaptureDeviceList.Instance;

            // If no devices were found print an error
            if (devices.Count < 1)
            {
                Console.WriteLine("No devices were found on this machine");
                return;
            }

            Console.WriteLine("The following devices are available on this machine:");
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine();

            int i = 0;

            // Scan the list printing every entry
            foreach (var dev in devices)
            {
                Console.WriteLine("{0}) {1}", i, dev.Description);
                i++;
            }

            Console.WriteLine();
            Console.Write("-- Please choose a device to capture: ");
            i = int.Parse(Console.ReadLine());

            using var device = devices[i];

            //Register our handler function to the 'packet arrival' event
            device.OnPacketArrival +=
                new PacketArrivalEventHandler(device_OnPacketArrival);

            //Open the device for capturing
            int readTimeoutMilliseconds = 1000;
            device.Open(DeviceModes.Promiscuous, readTimeoutMilliseconds);

            // tcpdump filter to capture only TCP/IP packets
            string filter = "port 67 or port 68";
            device.Filter = filter;

            Console.WriteLine();
            Console.WriteLine
                ("-- The following filter will be applied: \"{0}\"",
                filter);
            Console.WriteLine
                ("-- Listening on {0}, hit 'Ctrl-C' to exit...",
                device.Description);

            // Start capture packets
            device.Capture();

        }

        /// <summary>
        /// Prints the time and length of each received packet
        /// </summary>
        
        // 패킷이 캡처되었을 때 실행할 함수
        private static void device_OnPacketArrival(object sender, PacketCapture e)
        {
            var rawPacket = e.GetPacket();
            // 전체 패킷 데이터 파싱
            var packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            // 패킷에서 특정 프로토콜의 패킷을 파싱
            var dhcp = packet.Extract<PacketDotNet.DhcpV4Packet>();
            var ip = packet.Extract<PacketDotNet.IPPacket>();
            var udp = packet.Extract<PacketDotNet.UdpPacket>();

            // 필요한 헤더만 정리하여 출력
            Console.WriteLine("");
            Console.WriteLine("============IP Packet============");
            Console.WriteLine("Src: {0} / Dst: {1} / TTL: {2}", ip.SourceAddress, ip.DestinationAddress, ip.TimeToLive);
            Console.WriteLine("============UDP Packet============");
            Console.WriteLine("Src Port: {0} / Dst Port: {1}", udp.SourcePort, udp.DestinationPort);
            Console.WriteLine("============DHCP Packet============");
            Console.WriteLine("Your IP address: " + dhcp.YourAddress);
            Console.WriteLine("Your MAC address: " + dhcp.ClientHardwareAddress);
            Console.WriteLine("DHCP MessageType: " + dhcp.MessageType);
            if (dhcp.MessageType.ToString() == "Request")
            {
                Console.WriteLine("Client ID: " + dhcp.GetOptions()[1]);
                Console.WriteLine("HostName: " + dhcp.GetOptions()[3]);
            }
            if (dhcp.MessageType.ToString() == "Ack")
            {
                Console.WriteLine("Server ID: " + dhcp.GetOptions()[1]);
            }
            Console.WriteLine("===================================");
        }
    }
}