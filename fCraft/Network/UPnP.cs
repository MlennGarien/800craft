﻿using System;
using System.Text;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;

namespace fCraft.Network
{
    public class UPnP
    {
        public UPnP()
        {

        }

        public static void OpenFirewallPort(int port)
        {
            System.Net.NetworkInformation.NetworkInterface[] nics =
            System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();

            foreach (System.Net.NetworkInformation.NetworkInterface nic in nics)
            {
                try
                {
                    string machineIP =
                    nic.GetIPProperties().UnicastAddresses[0].Address.ToString();

                    foreach (System.Net.NetworkInformation.GatewayIPAddressInformation gwInfo in nic.GetIPProperties().GatewayAddresses)
                    {
                        try
                        {
                            OpenFirewallPort(machineIP, gwInfo.Address.ToString(), port);
                        }
                        catch
                        { }
                    }
                }
                catch { }
            }

        }
        public static void OpenFirewallPort(string machineIP, string firewallIP, int openPort)
        {
            string svc = getServicesFromDevice(firewallIP);
            openPortFromService(svc, "urn:schemas-upnp-org:service:WANIPConnection:1", machineIP, firewallIP, 80, openPort);
            openPortFromService(svc, "urn:schemas-upnp-org:service:WANPPPConnection:1", machineIP, firewallIP, 80, openPort);
        }
        private static string getServicesFromDevice(string firewallIP)
        {
            //To send a broadcast and get responses from all, send to 239.255.255.250
            string queryResponse = "";
            try
            {
                string query = "M-SEARCH * HTTP/1.1\r\n" +
                "Host:" + firewallIP + ":1900\r\n" +
                "ST:upnp:rootdevice\r\n" +
                "Man:\"ssdp:discover\"\r\n" +
                "MX:3\r\n" +
                "\r\n" +
                "\r\n";

                //use sockets instead of UdpClient so we can set a timeout easier
                Socket client = new Socket(AddressFamily.InterNetwork,
                SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint endPoint = new
                IPEndPoint(IPAddress.Parse(firewallIP), 1900);

                //1.5 second timeout because firewall should be on same segment (fast)
                client.SetSocketOption(SocketOptionLevel.Socket,
                SocketOptionName.ReceiveTimeout, 1500);

                byte[] q = Encoding.ASCII.GetBytes(query);
                client.SendTo(q, q.Length, SocketFlags.None, endPoint);
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint senderEP = (EndPoint)sender;

                byte[] data = new byte[1024];
                int recv = client.ReceiveFrom(data, ref senderEP);
                queryResponse = Encoding.ASCII.GetString(data);
            }
            catch { }

            if (queryResponse.Length == 0)
                return "";

            string location = "";
            string[] parts = queryResponse.Split(new string[] {
System.Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                if (part.ToLower().StartsWith("location"))
                {
                    location = part.Substring(part.IndexOf(':') + 1);
                    break;
                }
            }
            if (location.Length == 0)
                return "";

            //then using the location url, we get more information:

            System.Net.WebClient webClient = new WebClient();
            try
            {
                string ret = webClient.DownloadString(location);
                Debug.WriteLine(ret); //change to textbox
                return ret;//return services
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex.Message); //change to textbox
            }
            finally
            {
                webClient.Dispose();
            }
            return "";
        }
        private static void openPortFromService(string services, string serviceType, string machineIP, string firewallIP, int gatewayPort, int portToForward)
        {
            if (services.Length == 0)
                return;
            int svcIndex = services.IndexOf(serviceType);
            if (svcIndex == -1)
                return;
            string controlUrl = services.Substring(svcIndex);
            string tag1 = "<controlURL>";
            string tag2 = "</controlURL>";
            controlUrl = controlUrl.Substring(controlUrl.IndexOf(tag1)
            + tag1.Length);
            controlUrl =
            controlUrl.Substring(0, controlUrl.IndexOf(tag2));


            string soapBody = "<s:Envelope " +
            "xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/ \" " +

            "s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/ \">" +
            "<s:Body>" +
            "<u:AddPortMapping xmlns:u=\"" + serviceType + "\">" +
            "<NewRemoteHost></NewRemoteHost>" +
            "<NewExternalPort>" + portToForward.ToString() +
            "</NewExternalPort>" +
            "<NewProtocol>TCP</NewProtocol>" +
            "<NewInternalPort>" + portToForward.ToString() +
            "</NewInternalPort>" +
            "<NewInternalClient>" + machineIP +
            "</NewInternalClient>" +
            "<NewEnabled>1</NewEnabled>" +
            "<NewPortMappingDescription>800Craft</NewPortMappingDescription>" +
            "<NewLeaseDuration>0</NewLeaseDuration>" +
            "</u:AddPortMapping>" +
            "</s:Body>" +
            "</s:Envelope>";

            byte[] body =
            System.Text.UTF8Encoding.ASCII.GetBytes(soapBody);

            string url = "http://" + firewallIP + ":" +
            gatewayPort.ToString() + controlUrl;
            System.Net.WebRequest wr =
            System.Net.WebRequest.Create(url);//+ controlUrl);
            wr.Method = "POST";
            wr.Headers.Add("SOAPAction", "\"" + serviceType +
            "#AddPortMapping\"");
            wr.ContentType = "text/xml;charset=\"utf-8\"";
            wr.ContentLength = body.Length;

            System.IO.Stream stream = wr.GetRequestStream();
            stream.Write(body, 0, body.Length);
            stream.Flush();
            stream.Close();

            WebResponse wres = wr.GetResponse();
            System.IO.StreamReader sr = new
            System.IO.StreamReader(wres.GetResponseStream());
            string ret = sr.ReadToEnd();
            sr.Close();

            Debug.WriteLine("Setting port forwarding:" + //change to textbox
            portToForward.ToString() + "\r\r" + ret);
        }
    }
}