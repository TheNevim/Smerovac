using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;


namespace Smerovac
{
    public class Port
    {
        public IPAddress Ip { get; set; }
        public IPAddress Mask { get; set; }
        public IPAddress Subnet { get; set; }

        public string Name { get; set; }
        public PacketCommunicator Communicator { get; set; }
        
        public bool ? rip { get; set; }
        
        public int Timer { get; set; }
        public string RipUpdate =  "010200000000000000000000000000000000000000000010";
        public string Rip = "02020000";
        public string Ripfix = "00020000";



        public void Start(int deviceindex,bool ? enable)
        {
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;
            LivePacketDevice device = allDevices[deviceindex];
            Name = device.Name;
            rip = enable;
            
            Communicator = device.Open(65536,
                PacketDeviceOpenAttributes.Promiscuous | PacketDeviceOpenAttributes.NoCaptureLocal, 1);
        }
        
        public string toHexa(string st)
        {
            return String.Concat((st).Split('.').Select(x => int.Parse(x).ToString("X2")));
        }
        public void sendRipRequest()
        {
            string hex = RipUpdate;
            byte[] bytes =  Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
            Communicator.SendPacket(Rippaket(bytes));
        }

        public void sendRipUpdate(RouteTable _routeTable)
        {
            if (rip == true)
            {
                Timer++;
                if (_routeTable.updateRIP == Timer)
                {
                    Timer = 0;
                    string update = "";
                    update += Rip;
            
                    foreach (RouteTable.RouteRecord route in _routeTable.RouteRecords.ToList())
                    {
                        if (route.Active && route.Interface != Name && route.Type!="S" )
                        {
                            update += Ripfix + toHexa(route.Subnet.ToString()) + toHexa(route.Mask.ToString());
                    
                            if (route.NextHop == "")
                            {
                                update += String.Concat(("0.0.0.0").Split('.').Select(x => int.Parse(x).ToString("X2")));
                            }
                            else
                            {
                                update += String.Concat((route.NextHop).Split('.').Select(x => int.Parse(x).ToString("X2")));
                            }
                   
                            if (route.Metric == 16)
                            {
                                update+= (route.Metric).ToString("x8");
                            }
                            else
                            {
                                update+= (route.Metric+1).ToString("x8");
                            }

                        }
                    }
                    byte[] bytes =  Enumerable.Range(0, update.Length)
                        .Where(x => x % 2 == 0)
                        .Select(x => Convert.ToByte(update.Substring(x, 2), 16))
                        .ToArray();
                    Communicator.SendPacket(Rippaket(bytes));
                }
            }
            else
            {
                return;
            }
            
        }
        
        public void sendDeleteUpdate(string subnet, string mask, string nexthop)
        {
            string update = "";
            update += Rip;

            update += Ripfix + toHexa(subnet) + toHexa(mask);
            
            if (nexthop == "")
            {
                update += String.Concat(("0.0.0.0").Split('.').Select(x => int.Parse(x).ToString("X2")));
            }
            else
            {
                update += String.Concat((nexthop).Split('.').Select(x => int.Parse(x).ToString("X2")));
            }
            update += (16).ToString("x8");
            
            byte[] bytes =  Enumerable.Range(0, update.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(update.Substring(x, 2), 16))
                .ToArray();
            
            Communicator.SendPacket(Rippaket(bytes));
        }

        public void sendAfterDeleteUpdate(string subnet, string mask, string nexthop)
        {
            string update = "";
            update += Rip;

            update += Ripfix + toHexa(subnet.ToString()) + toHexa(mask.ToString());
            
            if (nexthop == "")
            {
                update += String.Concat(("0.0.0.0").Split('.').Select(x => int.Parse(x).ToString("X2")));
            }
            else
            {
                update += String.Concat((nexthop).Split('.').Select(x => int.Parse(x).ToString("X2")));
            }
            update += (1).ToString("x8");
            
            byte[] bytes =  Enumerable.Range(0, update.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(update.Substring(x, 2), 16))
                .ToArray();
            Communicator.SendPacket(Rippaket(bytes));
        }
        
        public  Packet Rippaket(byte[] bytes)
        {
            EthernetLayer ethernetLayer =
                new EthernetLayer
                {
                    Source = new MacAddress("02:00:4C:4F:4F:50"),
                    Destination = new MacAddress("01:00:5e:00:00:09"),
                    EtherType = EthernetType.None, 
                };

            IpV4Layer ipV4Layer =
                new IpV4Layer
                {
                    Source = new IpV4Address(Ip.ToString()),
                    CurrentDestination = new IpV4Address("224.0.0.9"),
                    Fragmentation = IpV4Fragmentation.None,
                    HeaderChecksum = null, 
                    Identification = 0,
                    Options = IpV4Options.None,
                    Protocol = null, 
                    Ttl = 10,
                    TypeOfService = 0,
                };

            UdpLayer udpLayer =
                new UdpLayer
                {
                    SourcePort = 520,
                    DestinationPort = 520,
                    Checksum = null, 
                    CalculateChecksumValue = true,
                };
            
            PayloadLayer payloadLayer =
                new PayloadLayer
                {
                    Data = new Datagram(bytes)
                };

            PacketBuilder builder = new PacketBuilder(ethernetLayer, ipV4Layer, udpLayer, payloadLayer);

            return builder.Build(DateTime.Now);
        }
    }
}