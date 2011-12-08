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
using System.Runtime.Serialization.Json;
using ServiceStack.Text;


namespace HeartbeatSender
{
    class HeartbeatSender
    {

        public static bool ShowIntro = true;



        static void Main()
        {
            if (ShowIntro == true)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("************************************\n");
                Console.WriteLine(" Welcome to 800Craft HeartbeatSaver\n");
                Console.WriteLine(" This program is designed to send a\n    Heartbeat to minecraft.net      \n");
                Console.WriteLine("************************************\n");
                Console.ResetColor();
                //Console.WriteLine("\nSending Information... Name: {0}, Port: {1}\n\n", Name, Port);

                Console.ForegroundColor = ConsoleColor.Yellow;
                ShowIntro = false;
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
                        using (StreamReader fs = new StreamReader(Paths.HbDataFileName))
                        {
                            String line;
                            while ((line = fs.ReadToEnd()) != null)
                            {
                                string data = (string)JsonSerializer.DeserializeFromString(line, typeof(string));
                                StringBuilder sb = new StringBuilder();
                                sb.AppendFormat(data);
                                int count = 1;
                                string post_data = sb.ToString();
                                Console.WriteLine("Sending Heartbeat... Count: " + count + "\n");
                                Console.WriteLine("Sending " + post_data + "\n");
                                count++;
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

                                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                                try
                                {
                                    Console.WriteLine(new StreamReader(response.GetResponseStream()).ReadToEnd());
                                    Console.WriteLine(response.StatusCode + "\n");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("" + ex);
                                }

                                Thread.Sleep(5000);
                                count++;
                                Console.ResetColor();
                                Main();
                            }
                        }
                    }






                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
        }
    }
}
        
    

