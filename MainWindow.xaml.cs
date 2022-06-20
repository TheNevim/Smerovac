using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Arp;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Base;
using PcapDotNet.Packets.Icmp;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using Timer = System.Timers.Timer;

namespace Smerovac
{
    public partial class MainWindow : Window
    {
        
        Port _port1 = new Port(); //Porty 
        Port _port2 = new Port();

        ArpTable _arpTable = new ArpTable(); //Arp tabulka
        RouteTable _routeTable = new RouteTable(); //Smerovacia tabulka

        IList<LivePacketDevice> _allDevices = LivePacketDevice.AllLocalMachine;
        public List<Port> allPorts = new List<Port>();
        public List<WaitingPacket> WaitingPackets= new List<WaitingPacket>();

        

        void Test(object sender, RoutedEventArgs e)
        {
            
        }
        
        void  RemoveRoute(object sender, RoutedEventArgs e)
        {
            if (TableRoute.SelectedItems.Count == 0)
            {
                return;
            }
            
            RouteTable.RouteRecord r = (RouteTable.RouteRecord) TableRoute.SelectedItems[0];
            if (r == null)
            {
                return;
            }
            
            if (r.Type != "C")
            {
                _routeTable.RemoveSpecific(r.Subnet,r.Mask,r.Type,r.Interface,r.NextHop,r.Metric,r.Ad,allPorts);
                _routeTable.ActivateUnactive(r.Subnet,r.Mask);
                TableRoute.Items.Clear();
                _routeTable.Update(TableRoute);
            }

        }
        

        void AddStatic(object sender, RoutedEventArgs e)
        {
            if (Nexthop.Text == "" && Interface.Text=="")
            {
                return;
            }
            else
            {
                if (Subnet.Text != "" && Mask.Text != "")
                {
                    IPAddress sub = IPAddress.Parse(Subnet.Text);
                    IPAddress mas = IPAddress.Parse(Mask.Text);
                   
                    if (Nexthop.Text != "")
                    {
                        _routeTable.RemoveSpecific(sub,mas,"S",Interface.Text,Nexthop.Text,0,1,allPorts);
                        _routeTable.NewRecord(sub, mas,"S",Interface.Text,Nexthop.Text,0,1);
                        TableRoute.Items.Clear();
                        _routeTable.Update(TableRoute);
                        return;
                    }
                    if (Interface.Text != "")
                    {
                        _routeTable.RemoveSpecific(sub,mas,"S",Interface.Text,"",0,1,allPorts);
                        _routeTable.NewRecord(sub, mas,"S",Interface.Text,"",0,1);
                        TableRoute.Items.Clear();
                        _routeTable.Update(TableRoute);
                        
                    }
                }
            }
        }

        //Priradenie IP portom
        void AssignIpPort1(object sender, RoutedEventArgs e)
        {
            TableRoute.Items.Clear();
            if (_port1.Subnet != null)
            {
                _routeTable.RemoveSpecific(_port1.Subnet,_port1.Mask,"C",_port1.Name,"",0,0,allPorts);
                _routeTable.ActivateUnactive(_port1.Ip,_port1.Mask);
            }

            int len = Interface.Items.Count;
            for (int pocet = 1; pocet <= len;pocet++)
            {
                if(Interface.Items.GetItemAt(pocet-1).ToString()==_port1.Name)
                {
                    break;
                }
                if (pocet == len)
                {
                    Interface.Items.Add(_port1.Name);
                }
            } 
            
            _port1.Ip = IPAddress.Parse(IpPort1.Text);
            _port1.Mask = IPAddress.Parse(MaskPort1.Text);
            IPAddress subnet = IPAddress.Parse("0.0.0.0"); 
            subnet.Address = (_port1.Ip.Address & _port1.Mask.Address);
            _port1.Subnet = subnet;
            
            _routeTable.NewRecord(_port1.Subnet,_port1.Mask,"C",_port1.Name,"",0,0);
            if (_port2.rip == true)
            {
                _port2.sendAfterDeleteUpdate(_port1.Subnet.ToString(),_port1.Mask.ToString(),"");
            }
            _routeTable.Update(TableRoute);
            
        }

        void AssignIpPort2(object sender, RoutedEventArgs e)
        {
            TableRoute.Items.Clear();
            if (_port2.Subnet != null)
            {
                _routeTable.RemoveSpecific(_port2.Subnet,_port2.Mask,"C",_port2.Name,"",0,0,allPorts);
                _routeTable.ActivateUnactive(_port2.Ip,_port2.Mask);
            }

            int len = Interface.Items.Count;
            for (int pocet = 1; pocet <= len;pocet++)
            {
                if(Interface.Items.GetItemAt(pocet-1).ToString()==_port2.Name)
                {
                    break;
                }
                if (pocet == len)
                {
                    Interface.Items.Add(_port2.Name);
                }
            } 

            _port2.Ip = IPAddress.Parse(IpPort2.Text);
            _port2.Mask = IPAddress.Parse(MaskPort2.Text);
            IPAddress subnet = IPAddress.Parse("0.0.0.0"); 
            subnet.Address = (_port2.Ip.Address & _port2.Mask.Address);
            _port2.Subnet = subnet;
            
            _routeTable.NewRecord(_port2.Subnet,_port2.Mask,"C",_port2.Name,"",0,0);
            if (_port1.rip == true)
            {
                _port1.sendAfterDeleteUpdate(_port2.Subnet.ToString(),_port2.Mask.ToString(),"");
            }
            _routeTable.Update(TableRoute);
        }


        void Start(object sender, RoutedEventArgs e)
        {
            _allDevices = LivePacketDevice.AllLocalMachine;
            LivePacketDevice device = _allDevices[Interface1.SelectedIndex];

            Interface.Items.Add("");
            _port1.Start(Interface1.SelectedIndex,Ripport1.IsChecked);
            _port1.Name = device.Name;
            var backgroundThread1 = new Thread((() => PortWork(_port1)));
            backgroundThread1.Start();
            
            
            _port2.Start(Interface2.SelectedIndex,Ripport1.IsChecked);
            device = _allDevices[Interface2.SelectedIndex];
            _port2.Name = device.Name;
            var backgroundThread2 = new Thread((() => PortWork(_port2)));
            backgroundThread2.Start();
            
            allPorts.Add(_port1);
            allPorts.Add(_port2);
        }
        
        void SendArp(object sender, RoutedEventArgs e)
        {
            _port1.Communicator.SendPacket(ArpRequestPacket(ArpIp.Text,_port1));
            _port2.Communicator.SendPacket(ArpRequestPacket(ArpIp.Text,_port2));
        }
        
        void ClearArp(object sender, RoutedEventArgs e)
        {
            _arpTable.ArpClear();
        }

        void Rip2box(object sender, RoutedEventArgs e)
        {

            switch (Ripport2.IsChecked)
            {
                case true:
                    _port2.rip = true;
                    _port2.sendRipRequest();
                    _port2.Timer = 0;
                    break;
                case false:
                    _port2.rip = false;
                    break;
                case null:
                    _port2.rip = false;
                    break;
            }
        }

        void Rip1box(object sender, RoutedEventArgs e)
        {
            switch (Ripport1.IsChecked)
            {
                case true:
                    _port1.rip = true;
                    _port1.sendRipRequest();
                    _port2.Timer = 0;
                    break;
                case false:
                    _port1.rip = false;
                    break;
                case null:
                    _port1.rip = false;
                    break;
            }
        }
 
        void UpdateTimers(object sender, RoutedEventArgs e)
        {
            _routeTable.updateRIP = Int32.Parse(UpdateRIP.Text);
            _routeTable.invalidRIP = Int32.Parse(InvalidRIP.Text);
            _routeTable.flushRIP = Int32.Parse(FlushRIP.Text);
            _routeTable.holdownRIP = Int32.Parse(HowldownRIP.Text);
        }
        
        public MainWindow()
        {
            InitializeComponent();

            for (int a = 0; a != _allDevices.Count; ++a)
            {
                Interface1.Items.Add(_allDevices[a].Description+ _allDevices[a].Name);
                Interface2.Items.Add( _allDevices[a].Description + _allDevices[a].Name);
            }
            
            Interface1.SelectedItem = Interface1.Items[4];
            Interface2.SelectedItem = Interface2.Items[10];
            IpPort1.Text = "172.16.0.1";
            MaskPort1.Text = "255.255.255.0";
            
            IpPort2.Text = "192.168.0.2";
            MaskPort2.Text = "255.255.255.0";
            
            ArpTableTimer.Text = "60";
            ArpIp.Text = "169.254.167.20";

            Subnet.Text = "1.1.1.0";
            Mask.Text = "255.255.255.0";

            UpdateRIP.Text = "10";
            InvalidRIP.Text = "20";
            FlushRIP.Text = "20";
            HowldownRIP.Text = "60";

            Timer atimer = new Timer(1000);
         
            
            atimer.Elapsed +=  ( sender, e ) =>
            {
                Dispatcher.Invoke(() =>
                {
                    _arpTable.Update(ArpTable);
                    int upravy = _routeTable.UpdateRip();
                    
                    TableRoute.Items.Clear();
                    _routeTable.Update(TableRoute);
                    
                    _port1.sendRipUpdate(_routeTable);
                    _port2.sendRipUpdate(_routeTable);
                });

            };
            atimer.AutoReset = true;
            atimer.Enabled = true;
        }
        
        private void PortWork(Port port)
        {
            using (port.Communicator)
            {
                port.Communicator.SetFilter("not ether src 02:00:4C:4F:4F:50");
                port.Communicator.ReceivePackets(0, PacketHandler);
            }

            void PacketHandler(Packet packet)
            {
                if (packet.Length > 1500 || packet.Ethernet.Source.ToString() == "02:00:4C:4F:4F:50")
                {
                    return;
                }
                
                //ARP
                if (packet.Ethernet.EtherType.ToString() == "Arp")
                {
                    IPAddress caught = IPAddress.Parse(packet.Ethernet.Arp.TargetProtocolIpV4Address.ToString());
                    //Jedná sa o rovnaký subnet ako na interface
                    if (port.Subnet.Address == (caught.Address & _port1.Mask.Address))
                    {
                        //Requesty na moju MAC
                        if (packet.Ethernet.Arp.TargetProtocolIpV4Address.ToString() == port.Ip.ToString() &&
                            packet.Ethernet.Arp.Operation.ToString() != "Reply")
                        {
                            port.Communicator.SendPacket(ArpReplyPacket(packet));
                            return;
                        }
                        
                    }
                    //Nejedná sa o rovnaky subnet ako na interface
                    else
                    {
                        if (packet.Ethernet.Arp.Operation.ToString() != "Reply")
                        {
                            RouteTable.RouteRecord record = _routeTable.IsInRouteTable(caught);
                            if (record != null)
                            {
                                port.Communicator.SendPacket(ArpReplyPacket(packet));  
                            }
                        }
                    }
                    
                    
                    if (packet.Ethernet.Arp.TargetProtocolIpV4Address.ToString() == port.Ip.ToString() &&
                        packet.Ethernet.Arp.Operation.ToString() == "Reply")
                    {
                        Dispatcher.Invoke(() =>
                        {
                            _arpTable.NewRecord( new IPAddress(packet.Ethernet.Arp.SenderProtocolAddress.ToArray()).ToString(),
                                BitConverter.ToString(packet.Ethernet.Arp.SenderHardwareAddress.ToArray()).Replace("-", ":"),
                                Int32.Parse(ArpTableTimer.Text));
                        });
                       
                    }
                }
    
                //SMEROVANIE
                if (packet.Ethernet.IpV4.IsValid)
                {
                    
                    if (packet.Ethernet.IpV4.Destination.ToString() == "224.0.0.9" && port.rip==true)
                    {
                        spracujRip(packet,port.Name,port);
                        return;
                    }
                    
                    
                    if (packet.Ethernet.IpV4.Destination.ToString() == port.Ip.ToString())
                    {
                       
                        if (packet.Ethernet.IpV4.Udp.IsValid)
                        {
                            if (packet.Ethernet.IpV4.Udp.SourcePort.ToString() == "520" &&
                                packet.Ethernet.IpV4.Udp.DestinationPort.ToString() == "520" && port.rip==true)
                            {
                                spracujRip(packet,port.Name,port);
                                return;
                            }
                        }
                       
                    }

                    IPAddress caught = IPAddress.Parse(packet.Ethernet.IpV4.Destination.ToString());
          
                    RouteTable.RouteRecord route = _routeTable.FindBestRoute(caught);
                    
                        
                    if (route != null)
                    {
                        if (route.Interface == port.Name || route.Interface=="")
                        {
                            return;
                        }
                        
                        foreach (Port foundPort in allPorts.ToList())
                        {
                            if (foundPort.Name == route.Interface)
                            {

                                ArpTable.ArpRecord re;
                                string ip = "";
                                if (route.NextHop == null || route.NextHop == "null" || route.NextHop=="")
                                {
                                    re = _arpTable.IsInArpTable(caught.ToString());
                                    ip = caught.ToString();
                                }
                                else
                                {
                                    re = _arpTable.IsInArpTable(route.NextHop);
                                    ip = route.NextHop;
                                }
                                
                                
                                if (re != null)
                                {
                                    foundPort.Communicator.SendPacket(BuildIpv4Paket(packet,re));
                                }
                                else
                                {
                                    foundPort.Communicator.SendPacket(ArpRequestPacket(ip, foundPort));
                                        Thread.Sleep(1000);
                                        re = _arpTable.IsInArpTable(ip);

                                        if (re != null)
                                        {
                                            foundPort.Communicator.SendPacket(BuildIpv4Paket(packet,re));
                                            break;
                                        }
                                }
                            }
                        }
                    }
                }
            }
        }

        void spracujRip(Packet packet, string name ,Port port)
        {
            
            string rip = "";
            
            for (int j = 0; j < packet.Ethernet.IpV4.Udp.Payload.Length; j++)
            {
                rip += packet.Ethernet.IpV4.Udp.Payload[j].ToString("x1");
            }
            
            string neco2 = BitConverter.ToString(packet.Ethernet.IpV4.Udp.Payload.ToArray()).Replace("-", "").Substring(8);
            
            int route_number = (neco2.Length)/40;
            
            if (rip[0] == '1')
            {
                port.sendRipUpdate(_routeTable);
            }
            
            if (rip[0] == '2')
            {
                string ripRoute = BitConverter.ToString(packet.Ethernet.IpV4.Udp.Payload.ToArray()).Replace("-", ".").Substring(12);
                
                for (int routa = 0; routa < route_number; routa++)
                {
                    string sta = (ripRoute.Substring(routa*48+12*(routa+1),47));
                    
                    string ip = "";
                    string mask = "";
                    string next = "";
                    int metric = 0;
                    
                    var ip0 = sta.Substring(0, 11).Split('.');
                    var mask0 = sta.Substring(12, 11).Split('.');
                    var next0 = sta.Substring(24, 11).Split('.');
                    var metric0 = sta.Substring(36,11).Split('.');

                    for (int i = 0; i < 4; i++)
                    {
                        if (i == 3)
                        {
                            ip += Convert.ToInt16(ip0[i], 16).ToString();
                            mask += Convert.ToInt16(mask0[i], 16).ToString();
                            next += Convert.ToInt16(next0[i], 16).ToString();
                            metric = Convert.ToInt16(metric0[i], 16);
                        }
                        else
                        {
                            ip += Convert.ToInt16(ip0[i], 16).ToString() + ".";
                            mask += Convert.ToInt16(mask0[i], 16).ToString()+ ".";
                            next += Convert.ToInt16(next0[i], 16).ToString()+ ".";
                        }
                    }
                
                    if (next == "0.0.0.0")
                    {
                        next = packet.Ethernet.IpV4.Source.ToString();
                    }

                    if (metric == 16)
                    {
                        
                        foreach (RouteTable.RouteRecord record in _routeTable.RouteRecords.ToList())
                        {
                            if (record.Subnet.ToString() == ip && record.Mask.ToString() == mask)
                            {
                                if (record.Type == "R")
                                {
                                
                                    _routeTable.RouteRecords.Remove(record);
                                   
                                    foreach (Port p in allPorts)
                                    {
                                        if (p.Name != name)
                                        {
                                            p.sendDeleteUpdate(record.Subnet.ToString(),record.Mask.ToString(),record.NextHop);
                                        }
                                    }
                                    break;
                                
                                }
                            }
                        }
                        continue;
                    }

                    if (_routeTable.CheckRip(ip, mask,  metric, name,allPorts)==1)
                    {
                        _routeTable.NewRecord(IPAddress.Parse(ip), IPAddress.Parse(mask),"R",name,next,metric,120,0,1 );
                    }
                    
                }
                Dispatcher.Invoke(() =>
                {
                    TableRoute.Items.Clear();
                    _routeTable.Update(TableRoute);
                });
                
            }
        }
        
            Packet BuildIpv4Paket(Packet packet, ArpTable.ArpRecord record)
            {
                EthernetLayer ethernetLayer =
                    new EthernetLayer
                    {
                        Source = new MacAddress("02:00:4C:4F:4F:50"),
                        Destination = new MacAddress(record.Mac),
                        EtherType = EthernetType.None,
                    };

                IpV4Layer ipV4Layer =
                    new IpV4Layer
                    {
                        Source = new IpV4Address(packet.Ethernet.IpV4.Source.ToString()),
                        CurrentDestination = new IpV4Address(packet.Ethernet.IpV4.Destination.ToString()),
                        Fragmentation = IpV4Fragmentation.None,
                        HeaderChecksum = null, 
                        Identification = packet.Ethernet.IpV4.Identification,
                        Options = IpV4Options.None,
                        Protocol = packet.Ethernet.IpV4.Protocol,
                        Ttl = packet.Ethernet.IpV4.Ttl,
                        TypeOfService = packet.Ethernet.IpV4.TypeOfService,
                    };
                
                PacketBuilder builder = new PacketBuilder(ethernetLayer, ipV4Layer,packet.Ethernet.IpV4.Payload.ExtractLayer());

                return builder.Build(DateTime.Now);
            }
            
            Packet ArpRequestPacket(string requestedIp, Port port)
            {
                EthernetLayer ethernetLayer =
                    new EthernetLayer
                    {
                        Source = new MacAddress("02:00:4C:4F:4F:50"),
                        Destination = new MacAddress("FF:FF:FF:FF:FF:FF"),
                        EtherType = EthernetType.Arp,
                    };

                ArpLayer arpLayer =
                    new ArpLayer
                    {
                        ProtocolType = EthernetType.IpV4,
                        Operation = ArpOperation.Request,
                        SenderHardwareAddress = new byte[] {02, 00, 76, 79, 79, 80}.AsReadOnly(), 
                        SenderProtocolAddress = (IPAddress.Parse(port.Ip.ToString())).GetAddressBytes().AsReadOnly(), 
                        TargetHardwareAddress = new byte[] {0, 0, 0, 0, 0, 0}.AsReadOnly(), 
                        TargetProtocolAddress = (IPAddress.Parse(requestedIp)).GetAddressBytes().AsReadOnly(),
                    };

                PacketBuilder builder = new PacketBuilder(ethernetLayer, arpLayer);

                return builder.Build(DateTime.Now);
            }

            Packet ArpReplyPacket(Packet packet)
            {
                EthernetLayer ethernetLayer =
                    new EthernetLayer
                    {
                        Source = new MacAddress("02:00:4C:4F:4F:50"),
                        Destination = packet.Ethernet.Source,
                        EtherType = EthernetType.Arp,
                    };

                ArpLayer arpLayer =
                    new ArpLayer
                    {
                        ProtocolType = EthernetType.IpV4,
                        Operation = ArpOperation.Reply,
                        SenderHardwareAddress = new byte[] {02, 00, 76, 79, 79, 80}.AsReadOnly(),
                        SenderProtocolAddress = packet.Ethernet.Arp.TargetProtocolAddress,
                        TargetHardwareAddress = packet.Ethernet.Arp.SenderHardwareAddress,
                        TargetProtocolAddress = packet.Ethernet.Arp.SenderProtocolAddress,
                    };
                PacketBuilder builder = new PacketBuilder(ethernetLayer, arpLayer);

                return builder.Build(DateTime.Now);
            }
  
    }
}