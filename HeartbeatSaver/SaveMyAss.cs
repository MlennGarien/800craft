//HeartBeatSaver v1.0, 15/12/2011 - Jon Baker

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.Serialization.Json;
using ServiceStack.Text;


namespace HeartbeatSaver
{
    public class HeartbeatSender
    {

        public static bool ShowIntro = true;
        public static String line;

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
                        if (File.Exists("HeartbeatSaver.txt"))
                        {
                                //Pass the file path and file name to the StreamReader constructor
                            StreamReader file = new StreamReader("HeartbeatSaver.txt");

                                //Read the first line of text
                                HeartbeatSender.line = file.ReadLine();

                                //close the file
                                file.Close();

                                int count = 1;
                                do
                                {
                                    string post_data = line;
                                    Console.WriteLine("Sending Heartbeat... Count: " + count + "\n");
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
                                    try
                                    {
                                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                                        Console.WriteLine(new StreamReader(response.GetResponseStream()).ReadToEnd());
                                        Console.WriteLine(response.StatusCode + "\n");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("" + ex);
                                    }

                                    Thread.Sleep(5000);
                                    count++;
                                }
                                while (count >= 0);
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
        
    

