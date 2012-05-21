//Copyright (C) <2012>  <Jon Baker, Glenn Mariën and Lao Tszy>

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using fCraft;
using System.Text.RegularExpressions;

namespace fCraft
{
    public static class GlobalChat
    {
        public static TcpClient IRCConnection = null;
        public static NetworkStream ns = null;
        public static StreamReader sr = null;
        public static StreamWriter sw = null;
        public static string server = "irc.esper.net";
        public static int port = 6667;
        public static string nick = RemoveTroublesomeCharacters(ConfigKey.ServerName.GetString());
        public static string name = "#800craft";
        public static Thread thread;

        public static string RemoveTroublesomeCharacters(string inString)
        {
            if (inString == null) return null;
            StringBuilder newString = new StringBuilder();
            char ch;
            for (int i = 0; i < inString.Length; i++){
                ch = inString[i];
                if ((ch <= 0x007A && ch >= 0x0061) || (ch <= 0x005A && ch >= 0x0041) || (ch <= 0x0039 && ch >= 0x0030) || ch == ']')
                {
                    newString.Append(ch);
                }
            }
            return newString.ToString();
        }

        public static void StartStream()
        {
            try
            {
                thread = new Thread(new ThreadStart(delegate
                {
                    //Stops an error
                    if (nick.Length > 55)
                    {
                        Logger.Log(LogType.Error, "GlobalChat: Name cannot exceed 55 characters");
                        return;
                    }
                    nick = "[" + nick + "]";
                    
                    //start irc connection
                    IRCConnection = new TcpClient(server, port);
                    try
                    {
                        //open streams
                        ns = IRCConnection.GetStream();
                        sr = new StreamReader(ns);
                        sw = new StreamWriter(ns);
                    }
                    catch (Exception e)
                    {
                        Logger.Log(LogType.Error, "Communication error: " + e);
                    }
                    //register
                    sendData("USER", nick + " 800CraftBot " + " 800CraftGlobalChat" + " :" + name);
                    sendData("NICK", nick);
                    //send a ping and join if success
                    IRCWork();

                })); thread.Start();
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, "Connection Error: " + e);
            }

            finally
            {
                //close all things
                if (sr != null)
                    sr.Close();
                if (sw != null)
                    sw.Close();
                if (ns != null)
                    ns.Close();
                if (IRCConnection != null)
                    IRCConnection.Close();
            }
        }

        //sends and handles a players message to the global chat
        public static void sendMessage(Player player, string message)
        {
            if (message == null)
            {
                return;
            }
            string data = IRCCommands.Privmsg(name, player.ClassyName + Color.White + ": " + message);
                data = Color.ToIRCColorCodes(data);
            sw.WriteLine(data);
            sw.Flush();
            //relay noraml message back to player
            player.Message("&i(Global)" + player.ClassyName + Color.White + ": " + message);
        }

        public static void sendData(string cmd, string param)
        {
            if (param == null)
            {
                sw.WriteLine(cmd);
                sw.Flush();
               // Logger.Log(LogType.GlobalChat, cmd); //if debug
            }
            else
            {
                sw.WriteLine(cmd + " " + param);
                sw.Flush();
                //Logger.Log(LogType.GlobalChat, cmd + " " + param);
            }
        }

        public static void IRCWork()
        {
            string[] ex;
            string data;
            bool shouldRun = true;
            IRCMessage msg;
            while (shouldRun)
            {
                Thread.Sleep(10); //wait to stop flood excess
                data = sr.ReadLine();
                msg = IRC.MessageParser(data, nick);
                string processedMessage = msg.Message;
                var SendList = Server.Players.Where(p => p.Info.IsFollowing);
                if (msg.Type == IRCMessageType.ChannelAction)
                {
                    if (processedMessage.StartsWith("\u0001ACTION"))
                    {
                        processedMessage = processedMessage.Substring(8);
                    }
                    else
                    {
                        return;
                    }
                }
                if (msg.Type == IRCMessageType.ChannelMessage)
                {
                    processedMessage = IRC.NonPrintableChars.Replace(processedMessage, "");
                    if (processedMessage.Length > 0)
                    {
                        SendList.Message("&i(Global){0}{1}: {2}",
                                        msg.Nick, Color.White, processedMessage);
                    }
                    else if (msg.Message.StartsWith("#"))
                    {
                        SendList.Message("&i(Global){0}{1}: {2}",
                                        msg.Nick, Color.White, processedMessage.Substring(1));
                    }
                }
                if (msg.Type == IRCMessageType.Join)
                {
                    SendList.Message("&i(Global) Server {0} joined the 800Craft Global Chat",
                                    msg.Nick);
                }

                if (msg.Type == IRCMessageType.Quit || msg.Type == IRCMessageType.Part)
                {
                    SendList.Message("&i(Global) Server {0} left the 800Craft Global Chat",
                                    msg.Nick);
                }

                char[] charSeparator = new char[] { ' ' };
                ex = data.Split(charSeparator, 5);
                if (ex[0] == "PING")
                {
                    sendData("PONG", ex[1]);
                    sendData("JOIN", name);
                    Logger.Log(LogType.SystemActivity, "Joined 800Craft Global Chat with Bot Name: " + nick);
                }
            }
        }
    }
}