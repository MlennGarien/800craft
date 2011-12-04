using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using fCraft;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Principal;


namespace HeartbeatSender
{
    class HeartbeatSender
    {
        public static float count = 1;
        
        public static bool On = false;
        public static string Players = ConfigKey.MaxPlayers.GetString();
        public static string Name = "Au70 Galaxy";
        public static string ServerName = Uri.EscapeDataString(Name);
        public static string Public = "True";
        public static int Port = 25565;
       
        public static string Salt = Heartbeat.Salt.ToString(); 
        

        static void Main()
        {
            string appGuid = ((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), false).GetValue(0)).Value.ToString();
            string mutexId = string.Format("Global\\{{{0}}}", appGuid);

            using (var mutex = new Mutex(false, mutexId))
            {
                try
                {
                    try
                    {
                        if (!mutex.WaitOne(TimeSpan.FromSeconds(1), false))
                        {
                            Console.WriteLine("-------------------------------------\n");
                            Console.WriteLine("The HeartbeatSaver is already running\n");
                            Console.WriteLine("-------------------------------------\n\nExiting...");
                            Thread.Sleep(2000);
                            Environment.Exit(0);
                        }
                    }
                    catch (AbandonedMutexException)
                    {
                        Console.WriteLine("Something went wrong :(");
                    }
                    
                    // this is what we are sending
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("public={0}&max={1}&users={2}&port={3}&version=7&salt={4}&name={5}", 
                    Public,
                    120,
                    Players,
                    Port, 
                    Salt, 
                    ServerName);

                    string post_data = sb.ToString();
                    Console.WriteLine("Sending " + sb.ToString() + "\n");
                    Console.WriteLine("Count: " + count + "\n");


                    // this is where we will send it
                    string uri = "http://www.minecraft.net/heartbeat.jsp";

                    // create a request
                    HttpWebRequest request = (HttpWebRequest)
                    WebRequest.Create(uri); request.KeepAlive = true;
                    request.ProtocolVersion = HttpVersion.Version10;
                    request.Method = "POST";

                    // turn request string into a byte stream
                    byte[] postBytes = Encoding.ASCII.GetBytes(post_data);

                    request.ContentType = "application/x-www-form-urlencoded";
                    request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                    request.ContentLength = postBytes.Length;
                    request.Timeout = 15000;
                    Stream requestStream = request.GetRequestStream();

                    // send it
                    requestStream.Write(postBytes, 0, postBytes.Length);
                    requestStream.Flush();
                    requestStream.Close();
                    //message


                    Thread.Sleep(5000);
                    count++;
                    Main();
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }
    }
}
        
    

            
