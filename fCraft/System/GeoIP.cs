using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Net;

public class LocationInfo
{
    public string CountryName { get; set; }
    public string CountryCode { get; set; }
    public string Name { get; set; }
}

public class GeoLocation
{
    private static Dictionary<string, LocationInfo> cachedIps =
        new Dictionary<string, LocationInfo>();

    public static LocationInfo GetLocationInfo(string ipParam)
    {
        LocationInfo result = null;
        IPAddress i = Dns.GetHostEntry(ipParam).AddressList[0];
        string ip = i.ToString();

        if (!cachedIps.ContainsKey(ip))
        {
            string r;
            using (WebClient webClient = new WebClient())
            {
                r = webClient.DownloadString(
                    String.Format("http://api.hostip.info/?ip={0}&position=true", ip));
            }

            XDocument xmlResponse = XDocument.Parse(r);

            try
            {
                foreach (XElement element in xmlResponse.Root.Nodes())
                {
                    if (element.Name.LocalName == "featureMember")
                    {
                        //element Hostip is the first element in featureMember  
                        XElement hostIpNode = (XElement)element.Nodes().First();

                        result = new LocationInfo();

                        //loop thru the elements in Hostip  
                        foreach (XElement node in hostIpNode.Elements())
                        {
                            if (node.Name.LocalName == "name")
                                result.Name = node.Value;

                            if (node.Name.LocalName == "countryName")
                                result.CountryName = node.Value;

                            if (node.Name.LocalName == "countryAbbrev")
                                result.CountryCode = node.Value;
                        }
                    }
                }
            }
            catch (NullReferenceException)
            {
                //Looks like we didn't get what we expected.  
            }

            if (result != null)
            {
                cachedIps.Add(ip, result);
            }
        }
        else
        {
            result = cachedIps[ip];
        }
        return result;
    }
}