using System;
using System.Net;
using System.Xml;
using System.Data;

namespace fCraft
{
    public static class GeoIP
    {

        public static string GetGeoLocationByIP(string strIPAddress)
        {

            //Create a WebRequest
            WebRequest rssReq = WebRequest.Create("http://freegeoip.appspot.com/xml/" + strIPAddress);

            //Create a Proxy
            WebProxy px = new WebProxy("http://freegeoip.appspot.com/xml/" + strIPAddress, true);

            //Assign the proxy to the WebRequest
            rssReq.Proxy = px;

            //Set the timeout in Seconds for the WebRequest
            rssReq.Timeout = 2000;

            try
            {

                //Get the WebResponse

                WebResponse rep = rssReq.GetResponse();

                //Read the Response in a XMLTextReader

                XmlTextReader xtr = new XmlTextReader(rep.GetResponseStream());

                //Create a new DataSet

                DataSet ds = new DataSet();

                ds.ReadXml(xtr);
                DataTable dt = ds.Tables[0];

                if (dt != null && dt.Rows.Count > 0)
                {
                    if (dt.Rows[0]["Status"].ToString().Equals("true", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (strIPAddress != "127.0.0.1" && !strIPAddress.StartsWith("192.168."))
                            return dt.Rows[0]["CountryName"].ToString();
                        else
                            return "LocalHost";
                    }
                }
                return "";
            }
            catch
            {
                return "";
            }
        }
    }
}

