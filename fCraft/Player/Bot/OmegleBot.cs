// C# OmegleBot class
// by Andrew Brown
// Free to be modified, redistributed, or otherwise have your way with in any way you see fit
// http://www.drusepth.net/
 
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections;
using fCraft;

public class OmegleBot
{
    private string id;
    Player player;
    public string ID
    {
        get { return id; }
        set { id = value; }
    }

    private string lastMessage = String.Empty;
    public string LastMessage
    {
        get { return lastMessage; }
        set { lastMessage = value; }
    }

    private bool paired = false;
    public bool Paired
    {
        get { return paired; }
        set { paired = value; }
    }

    private Thread t;
    public Thread Thread
    {
        get { return t; }
        set { t = value; }
    }
    public OmegleBot() { }
    public OmegleBot(Player player_)
    {
        player = player_;
        // Logic
        Paired = true;
        ID = Connect();
    }

    public string Connect()
    {
        try
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("http://omegle.com/start");
            request.Method = "POST";
            WebResponse response = request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());
            ID = reader.ReadToEnd().Replace("\"", "");
            response.Close(); 
            reader.Close(); reader.Dispose();
            t = new Thread(EventWatcherThread);
            t.Start();
            return ID;
        }
        catch
        {
            player.Message(Color.Olive + "(Omegle)" + "&WCouldn't get a new chatter ID");
            return String.Empty;
        }
    }

    public void Disconnect()
    {
        Request("http://www.omegle.com/disconnect", "id=" + ID);
        player.OmBot = null;
        player.Message(Color.Olive + "(Omegle)" + "You have disconnected");
        t.Abort();
    }

    public void Say(string what)
    {
        try
        {
            player.Message(Color.Olive + "(Omegle)" + "You: " + what);
            Request("http://www.omegle.com/send", "id=" + ID + "&amp;msg=" + what);
        }
        catch
        {
            player.Message(Color.Olive + "(Omegle)" + "&WSomething went wrong with sending the /Om message");
        }
    }

    private void EventWatcherThread()
    {
        while (true)
        {
            if (Paired)
                GetEvents();
            else
                Disconnect();

            Thread.Sleep(1000);
        }
    }

    private void GetEvents()
    {
        string event_json = Request("http://www.omegle.com/events", "id=" + ID);

        if (event_json != "null")
        {
            Hashtable events = ParseJSON(event_json);
            HandleEvents(events);
        }
    }
    private void HandleEvents(Hashtable events)
    {
        IDictionaryEnumerator en = events.GetEnumerator();
        while (en.MoveNext())
        {
            switch (en.Key.ToString().Replace("[", "").Replace("]", ""))
            {
                case "typing":
                    //player.Message(Color.Olive + "(Omegle)" + "Stranger is typing");
                    break;

                case "waiting":
                    break;

                case "connected":
                    Paired = true;
                    player.Message(Color.Olive + "(Omegle)" + "Found a new stranger!");
                    break;
                case "null":
                    Disconnect();
                    break;

                default:
                        string message = en.Value.ToString().Replace("\"[[gotmessage\", ", "").Replace("]]", "");
                        player.Message(Color.Olive + "(Omegle)Stranger: " + message);
                        break;
                case "strangerDisconnected":
                    Paired = false;
                    player.Message(Color.Olive + "(Omegle)" + "The stranger has disconnected.");
                    break;
            }

        }

    }

    private Hashtable ParseJSON(string json)
    {
        Hashtable result = new System.Collections.Hashtable();

        // [["connected"], ["gotMessage", "lol"]]
        json = json.Remove(0, 1);
        json = json.Remove(json.Length - 1, 1);
        string[] json_messages = json.Split(']');

        foreach (string message in json_messages)
        {
            string m = message;

            if (m == "")
                break;

            if (message.Substring(0, 2) == ", ")
                m = message.Remove(0, 2);

            m.Remove(0, 1); // Remove ["

            string[] split = m.Split(',');
            string key = "", value = "";

            if (split.Length == 1)
            {
                key = split[0];

                // Strip off " surrounding key
                key = key.Remove(0, 2);
                key = key.Remove(key.Length - 1, 1);
            }

            if (split.Length == 2)
            {
                value = split[1].Remove(0, 1);

                // Strip off " surrounding value
                value = value.Remove(0, 1);
                value = value.Remove(value.Length - 1, 1);
            }

            try
            {
                result.Add(key, value);
            }
            catch { }
        }

        return result;
    }

    private string Request(string url, string parameters)
    {
        try
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.Method = "POST";
            request.Host = "omegle.com";

            byte[] byteArray = Encoding.UTF8.GetBytes(parameters);

            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;

            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            WebResponse response = request.GetResponse();

            dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();

            reader.Close();
            reader.Dispose();
            dataStream.Close();
            response.Close();
            return responseFromServer;
        }
        catch
        {
            player.Message(Color.Olive + "(Omegle)" + "&WFailed to send the request to Omegle.com :(");
            Disconnect();
            return String.Empty;
        }
    }
}