using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;


namespace Smerovac
{
    public class ArpTable
    {
        public class ArpRecord
        {
            public  string Ip { get; set; }
            public  string Mac { get; set; }
            public  int Time { get; set; }
            
        }

        public List<ArpRecord> ArpRecords = new List<ArpRecord>();
        
        public ArpRecord IsInArpTable(string ip)
        {
            foreach (ArpRecord record in ArpRecords.ToList())
            {
                if ((record.Ip)==ip)
                {
                    return record;
                }
            }
            return null;
        }
        
        public void NewRecord(string ip,string mac, int time)
        {
            ArpRecord o = new ArpRecord();
            ArpRecord p = IsInArpTable(ip);
            
            if (p == null)
            {
                o.Ip = ip;
                o.Mac = mac;
                o.Time = time;
                this.ArpRecords.Add(o);
            }
            else
            {
                if (p.Mac==mac)
                {
                    p.Time = time;
                }
            }
        }

        public void ArpClear()
        {
            foreach (ArpRecord record in ArpRecords.ToList())
            {
                ArpRecords.Remove(record);
            }
        }
        
        public void Update(TextBlock arpTableshow)
        {
            string table = "IP \t\t MAC \t\t Timer \n";
            foreach (ArpRecord record in ArpRecords.ToList())
            {
                if ((record.Time)==0)
                {
                    ArpRecords.Remove(record);
                }
                else
                {
                    record.Time = ((record.Time) - 1);
                    table += record.Ip + "\t" + record.Mac + "\t" + record.Time + "\n";
                }
            }
            arpTableshow.Text = table;
     
        }
    }
}