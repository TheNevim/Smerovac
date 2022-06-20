using System.Net;
using PcapDotNet.Packets;
using PcapDotNet.Packets.IpV4;

namespace Smerovac
{
    public class WaitingPacket
    {

        public Packet Paket;
        public Port Port;
        public IPAddress Ip;

        public WaitingPacket(Port port, Packet paket, IPAddress ip)
        {
            this.Paket = paket;
            this.Port = port;
            this.Ip = ip;
        }
       
    }
}