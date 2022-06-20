using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows.Controls;
using System.Windows.Threading;


namespace Smerovac
{
    public class RouteTable
    {
        public class RouteRecord
        {
            public IPAddress Subnet { get; set; }
            public IPAddress Mask { get; set; }
            public int MaskLenght { get; set; }
            public string Type { get; set; }
            public string Interface { get; set; }
            public string NextHop { get; set; }
            public int Metric { get; set; }
            public int Ad { get; set; }
            public bool Active { get; set; }
            public int Timer { get; set; }
            public int TimerStage { get; set; }
        }

        public List<RouteRecord> RouteRecords = new List<RouteRecord>();
        public List<RouteRecord> RipDatabase = new List<RouteRecord>();

        public int updateRIP = 0;
        public int invalidRIP = 0;
        public int flushRIP = 0;
        public int holdownRIP = 0;
        

        public RouteRecord IsInRouteTable(IPAddress caught)
        {
            foreach (RouteRecord record in RouteRecords.ToList())
            {
                if (record.Subnet.Address == (caught.Address & record.Mask.Address))
                {
                    return record;
                }
            }

            return null;
        }

        public int CheckRip(string ip, string mask, int metric, string portname, List<Port> allPorts)
        {
            foreach (RouteRecord record in RouteRecords.ToList())
            {
                if (record.Subnet.ToString() == ip && record.Mask.ToString() == mask)
                {
                    if (record.Type == "R")
                    {
                        if (record.TimerStage == 1 && record.Interface == portname)
                        {
                            record.Timer = 0;
                            return 0;
                        }
     
                        if (record.TimerStage == 1 && record.Interface != portname)
                        {
                            if (record.Metric > metric)
                            {
                                RouteRecords.Remove(record);
                                return 1;
                            }
                            return 0;
                        }

                        if (record.TimerStage == 2 && record.Interface == portname)
                        {
                            record.Timer = 0;
                            record.TimerStage = 1;
                            record.Metric = metric;
                            return 0;
                        }
                        
                        if (record.TimerStage == 2 && record.Interface != portname)
                        {
                            return 0;
                        }
                        
                        if (record.TimerStage == 3 && record.Interface == portname)
                        {
                            record.TimerStage = 1;
                            record.Metric = metric;
                            record.Active = true;
                            record.Timer = 0;
                            return 0;
                        }
                        
                        if (record.TimerStage == 3 && record.Interface != portname)
                        {
                            return 0;
                        }
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
            return 1;
    }

public int UpdateRip()
{
 int c = 0;
 
 foreach (RouteRecord record in RouteRecords.ToList())
 {
     if (record.Type == "R")
     {
         record.Timer++;
         
         if(record.Timer==invalidRIP  && record.TimerStage==1)
         {
             record.TimerStage = 2;
             record.Metric = 16;
             c++;
             continue;
         }
         
         if(record.Timer==flushRIP+invalidRIP && record.TimerStage==2)
         {
             c++;
             record.TimerStage = 3;
             record.Active = false;
         }
         
         if(record.Timer==holdownRIP+invalidRIP && record.TimerStage==3)
         {
             c++;
             RouteRecords.Remove(record);
         }
     }
 }
 
 return c;
 
}

public RouteRecord FindBestRoute(IPAddress destination)
{
 RouteRecord bestroutestring = new RouteRecord();
 bestroutestring.NextHop = "";
 bestroutestring.Interface = "";
 
 List<RouteRecord> bestroute = new List<RouteRecord>();
 
 
 foreach (RouteRecord record in RouteRecords.ToList())
 {
     if (record.Subnet.Address == (destination.Address & record.Mask.Address))
     {
         bestroute.Add(record);
     }
 }
 for (int i = 0; i < bestroute.Count; i++)
 {
     if (bestroute[i].Active)
     {
         if (bestroute[0].Interface == "")
         {
             if (bestroute[0].NextHop != "")
             {
                 RouteRecord route = FindBestRoute(IPAddress.Parse(bestroute[0].NextHop));
                 if (route.NextHop == "")
                 {
                     bestroutestring.NextHop = bestroute[0].NextHop;
                     bestroutestring.Interface = route.Interface;
                 }
                 else
                 {
                     bestroutestring.Interface = route.Interface;
                     bestroutestring.NextHop = route.NextHop;
                 }
                 return bestroutestring;
             }
             else
             {
                 return bestroutestring;
             }
         }
         else
         {
             return bestroute[0];
         }
     }
 }

 
 return bestroutestring;
}


public void RemoveSpecific(IPAddress subnet, IPAddress mask, string type, string inter, String nexthop,
 int metric, int ad, List<Port> allPorts)
{

 foreach (RouteRecord r in RouteRecords.ToList())
 {
     if (r.Subnet.ToString()==subnet.ToString() && r.Mask.ToString() == mask.ToString() && r.Type == type && r.Interface == inter
         && r.Metric == metric && r.Ad == ad && r.NextHop==nexthop)
     {
         
         foreach (Port foundPort in allPorts.ToList())
         {
             if (foundPort.Name != r.Interface && foundPort.rip==true)
             {
                 foundPort.sendDeleteUpdate(r.Subnet.ToString(),r.Mask.ToString(),r.NextHop);
                 break;
             }
         }

         RouteRecords.Remove(r);
         return;
     }
     
 }
}

public void Update(ListView tableRoute)
{
 foreach (RouteRecord r in RouteRecords)
 {
     if (r.TimerStage == 3)
     {
         continue;
     }
     tableRoute.Items.Add(r);
 }
}

int Sortuj(RouteRecord x, RouteRecord y)
{
    int result = x.MaskLenght > y.MaskLenght ? -1 : 
         x.MaskLenght < y.MaskLenght ? 1 : 
         x.MaskLenght == y.MaskLenght ? 0 : 0;
    return result;
}

bool canIadd(RouteRecord route)
{

 foreach (RouteRecord r in RouteRecords.ToList())
 {
     if (r.Subnet.ToString() == route.Subnet.ToString() && r.MaskLenght==route.MaskLenght)
     {
         
         if (route.Ad < r.Ad)
         {
             r.Active = false;
             
             return true;
         }
         
         if (route.Ad == r.Ad)
         {
             if (route.Metric < r.Metric)
             {
                 r.Active = false;
                 return true;
             }
             else
             {
                 return false;
             }
         }
         
         if (route.Ad > r.Ad)
         {
             return false;
         }
     }
 }

 return true;
}


public void ActivateUnactive(IPAddress ip, IPAddress mask)
{
 int len = RouteRecords.Count;
 for (int i = 0; i < len; i++)
 {
     if (RouteRecords[i].Subnet.Address == (ip.Address & mask.Address) && RouteRecords[i].Active==false)
     {
         RouteRecords[i].Active = true;
     }
 }
}

public void NewRecord(IPAddress network, IPAddress mask, string type, string inter, String nextHop, int metric,
 int ad, int timer = 0, int stage = 1)
{
 RouteRecord o = new RouteRecord();
 
 
 o.Subnet = network;
 o.Mask = mask;
 
 int ones = 0;
 Array.ForEach((mask.ToString()).Split('.'),(s)=>Array.ForEach(Convert.ToString(int.Parse(s),2).Where(c=>c=='1').ToArray(),(k)=>ones++));
 
 o.MaskLenght = ones;
 o.Type = type;
 o.Interface = inter;
 o.NextHop = nextHop;
 o.Metric = metric;
 o.Ad = ad;
 o.Timer = timer;
 o.TimerStage = stage;
 
 if (canIadd(o))
 {
     o.Active = true;
     RouteRecords.Add(o);
     RouteRecords.Sort(Sortuj); 
 }

}
}
}