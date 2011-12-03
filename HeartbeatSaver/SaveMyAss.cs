using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;

namespace HeartbeatSender
{
    class HeartbeatSender
    {
        static void Main()
        {
            bool on = true;
            do
            {
                // this is what we are sending
                string post_data = "port=20010&max=32&name=Rebelliousdude&public=True&version=7&salt=kr6YzJqjeBPwD9FCGbNCcdbSH9HqcwTS&users=0";

                // this is where we will send it
                string uri = "http://www.minecraft.net/heartbeat.jsp";

                // create a request
                HttpWebRequest request = (HttpWebRequest)
                WebRequest.Create(uri); request.KeepAlive = true;
                request.ProtocolVersion = HttpVersion.Version10;
                request.Method = "POST";

                // turn our request string into a byte stream
                byte[] postBytes = Encoding.ASCII.GetBytes(post_data);


                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = postBytes.Length;
                Stream requestStream = request.GetRequestStream();

                // send it
                requestStream.Write(postBytes, 0, postBytes.Length);
                requestStream.Close();

                Console.WriteLine("Sent");
            }
            while (on);
        }
    }
}

