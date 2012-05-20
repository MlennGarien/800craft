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
        public static string nick = IRC.NonPrintableChars.Replace(ConfigKey.ServerName.GetString().Replace(" ", ""), "").ToLower();
        public static string name = "#au70";
        public static Thread thread;

        public static void StartStream()
        {
            thread = new Thread(new ThreadStart(delegate
            {
                try
                {
                    if (nick.Length > 55)
                    {
                        Logger.Log(LogType.Error, "GlobalChat: Name cannot exceed 55 characters");
                        return;
                    }
                    IRCConnection = new TcpClient(server, port);
                    try
                    {
                        ns = IRCConnection.GetStream();
                        sr = new StreamReader(ns);
                        sw = new StreamWriter(ns);
                    }
                    catch (Exception e)
                    {
                        Logger.Log(LogType.Error, "Communication error: "+e);
                    }
                    sendData("USER", nick + " 800CraftBot " + " 800CraftGlobalChat" + " :" + name);
                    sendData("NICK", nick);
                    IRCWork();
                }
                catch(Exception e)
                {
                    Logger.Log(LogType.Error, "Connection Error: "+ e);
                }

                finally
                {
                    if (sr != null)
                        sr.Close();
                    if (sw != null)
                        sw.Close();
                    if (ns != null)
                        ns.Close();
                    if (IRCConnection != null)
                        IRCConnection.Close();
                }

            })); thread.Start();
        }

        public static void sendMessage(Player player, string message)
        {
            if (message == null)
            {
                return;
            }
            sw.WriteLine(IRCCommands.Privmsg(name, player.ClassyName + Color.White + ": " + message));
            sw.Flush();
            player.Message(player.ClassyName + Color.White + ": " + message);
        }

        public static void sendData(string cmd, string param)
        {
            if (param == null)
            {
                sw.WriteLine(cmd);
                sw.Flush();
                //Logger.Log(LogType.SystemActivity, cmd);
            }
            else
            {
                sw.WriteLine(cmd + " " + param);
                sw.Flush();
                //Logger.Log(LogType.Error, cmd + " " + param);
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
                Thread.Sleep(10);
                data = sr.ReadLine();
                msg = IRC.MessageParser(data, nick);
                string processedMessage = msg.Message;
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

                        Server.Message("&i(Global) {0}{1}: {2}",
                                        msg.Nick, Color.White, processedMessage);

                    }
                    else if (msg.Message.StartsWith("#"))
                    {
                        Server.Message("&i(Global) {0}{1}: {2}",
                                        msg.Nick, Color.White, processedMessage.Substring(1));
                    }
                }

                char[] charSeparator = new char[] { ' ' };
                ex = data.Split(charSeparator, 5);
                if (ex[0] == "PING")
                {
                    sendData("PONG", ex[1]);
                    sendData("JOIN", name);
                }
            }
        }
    }
}