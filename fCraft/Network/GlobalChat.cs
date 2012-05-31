/* Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
 * 
 * Based, in part, on SmartIrc4net code. Original license is reproduced below.
 * 
 *
 *
 * SmartIrc4net - the IRC library for .NET/C# <http://smartirc4net.sf.net>
 *
 * Copyright (c) 2003-2005 Mirco Bauer <meebey@meebey.net> <http://www.meebey.net>
 *
 * Full LGPL License: <http://www.gnu.org/licenses/lgpl.txt>
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using fCraft.Events;
using System.Collections.Concurrent;
using JetBrains.Annotations;
using System.Text;

namespace fCraft
{
    public static class GlobalChat
    {
        public sealed class GlobalThread : IDisposable
        {
            TcpClient client;
            StreamReader reader;
            StreamWriter writer;
            Thread thread;
            bool isConnected;
            public bool IsReady;
            bool reconnect;
            public bool ResponsibleForInputParsing;
            public string ActualBotNick;
            string desiredBotNick;
            DateTime lastMessageSent;
            ConcurrentQueue<string> localQueue = new ConcurrentQueue<string>();


            public bool Start([NotNull] string botNick, bool parseInput)
            {
                if (botNick == null) throw new ArgumentNullException("botNick");
                if (botNick.Length > 55){
                    Logger.Log(LogType.Error, "Unable to start Global Chat (Server name exceeds 55 in length)"); 
                    return false;
                }
                desiredBotNick = botNick;
                ResponsibleForInputParsing = parseInput;
                try
                {
                    // start the machinery!
                    thread = new Thread(IoThread)
                    {
                        Name = "800Craft.GlobalChat",
                        IsBackground = true
                    };
                    thread.Start();
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Log(LogType.Error,
                                "GlobalChat: Could not start the bot: {0}", ex);
                    return false;
                }
            }


            void Connect()
            {
                // initialize the client
                IPAddress ipToBindTo = IPAddress.Parse(ConfigKey.IP.GetString());
                IPEndPoint localEndPoint = new IPEndPoint(ipToBindTo, 0);
                client = new TcpClient(localEndPoint)
                {
                    NoDelay = true,
                    ReceiveTimeout = Timeout,
                    SendTimeout = Timeout
                };
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);

                // connect
                client.Connect(hostName, port);

                // prepare to read/write
                reader = new StreamReader(client.GetStream());
                writer = new StreamWriter(client.GetStream());
                isConnected = true;
            }


            public void Send([NotNull] string msg)
            {
                if (msg == null) throw new ArgumentNullException("msg");
                localQueue.Enqueue(msg);
            }

            public static void SendChannelMessage([NotNull] string line)
            {
                if (line == null) throw new ArgumentNullException("line");
                if (channelNames == null) return; // in case IRC bot is disabled.
                    line = Color.ToIRCColorCodes(line);
                for (int i = 0; i < channelNames.Length; i++)
                {
                    SendRawMessage(IRCCommands.Privmsg(channelNames[i], line));
                }
            }

            public static void SendRawMessage([NotNull] string line)
            {
                if (line == null) throw new ArgumentNullException("line");
                OutputQueue.Enqueue(line);
            }


            // runs in its own thread, started from Connect()
            void IoThread()
            {
                string outputLine = "";
                lastMessageSent = DateTime.UtcNow;

                do
                {
                    try
                    {
                        ActualBotNick = desiredBotNick;
                        reconnect = false;
                        Logger.Log(LogType.SystemActivity,
                                    "Connecting to 800Craft Global Chat as {2}",
                                    hostName, port, ActualBotNick);
                        Connect();

                        // register
                        Send(IRCCommands.User(ActualBotNick, 8, ConfigKey.ServerName.GetString()));
                        Send(IRCCommands.Nick(ActualBotNick));

                        while (isConnected && !reconnect)
                        {
                            Thread.Sleep(10);

                            if (localQueue.Count > 0 &&
                                DateTime.UtcNow.Subtract(lastMessageSent).TotalMilliseconds >= SendDelay &&
                                localQueue.TryDequeue(out outputLine))
                            {

                                writer.Write(outputLine + "\r\n");
                                lastMessageSent = DateTime.UtcNow;
                                writer.Flush();
                            }

                            if (OutputQueue.Count > 0 &&
                                DateTime.UtcNow.Subtract(lastMessageSent).TotalMilliseconds >= SendDelay &&
                                OutputQueue.TryDequeue(out outputLine))
                            {
                                writer.Write(outputLine + "\r\n");
                                lastMessageSent = DateTime.UtcNow;
                                writer.Flush();
                            }

                            if (client.Client.Available > 0)
                            {
                                string line = reader.ReadLine();
                                if (line == null) break;
                                HandleMessage(line);
                            }
                        }

                    }
                    catch (SocketException)
                    {
                        Logger.Log(LogType.Warning, "GlobalChat: Disconnected. Will retry in {0} seconds.",
                                    ReconnectDelay / 1000);
                        reconnect = true;

                    }
                    catch (IOException)
                    {
                        Logger.Log(LogType.Warning, "GlobalChat: Disconnected. Will retry in {0} seconds.",
                                    ReconnectDelay / 1000);
                        reconnect = true;
#if !DEBUG
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogType.Error, "GlobalChat: {0}", ex);
                        reconnect = true;
#endif
                    }

                    if (reconnect) Thread.Sleep(ReconnectDelay);
                } while (reconnect);
            }

            public void SendMessage(string message)
            {
                HandleMessage("PRIVMSG " + message);
            }

            void HandleMessage([NotNull] string message)
            {
                if (message == null) throw new ArgumentNullException("message");

                IRCMessage msg = IRC.MessageParser(message, ActualBotNick);
#if DEBUG_IRC
                Logger.Log( LogType.IRC,
                            "[{0}]: {1}",
                            msg.Type, msg.RawMessage );
#endif

                switch (msg.Type)
                {
                    case IRCMessageType.Login:
                        foreach (string channel in channelNames)
                        {
                            Send(IRCCommands.Join(channel));
                        }
                        IsReady = true;
                        AssignBotForInputParsing(); // bot should be ready to receive input after joining
                        return;


                    case IRCMessageType.Ping:
                        // ping-pong
                        Send(IRCCommands.Pong(msg.RawMessageArray[1].Substring(1)));
                        return;


                    case IRCMessageType.ChannelAction:
                    case IRCMessageType.ChannelMessage:
                        // channel chat
                        var SendList = Server.Players.Where(p => p.GlobalChat && !p.IsDeaf);
                        if (!ResponsibleForInputParsing) return;
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
                            
                            processedMessage = IRC.NonPrintableChars.Replace(processedMessage, "");
                            if (processedMessage.Length > 0)
                            {
                                if (msg.Type == IRCMessageType.ChannelAction)
                                {
                                    SendList.Message("&i(Global) * {0} {1}",
                                                    msg.Nick, processedMessage);
                                }
                                else
                                {
                                    SendList.Message("&i(Global) {0}{1}: {2}",
                                                    msg.Nick, Color.White, processedMessage);
                                }
                            }

                            else if (msg.Message.StartsWith("#"))
                            {
                                SendList.Message("&i(Global) {0}{1}: {2}",
                                                msg.Nick, Color.White, processedMessage.Substring(1));
                            }
                        return;


                    case IRCMessageType.Join:
                        if (!ResponsibleForInputParsing) return;
                            Server.Message("&i(Global) Server {0} joined the 800Craft Global Chat",
                                            msg.Nick, msg.Channel);
                        return;


                    case IRCMessageType.Kick:
                        string kicked = msg.RawMessageArray[3];
                        if (kicked == ActualBotNick)
                        {
                            Logger.Log(LogType.SystemActivity,
                                        "Bot was kicked from {0} by {1} ({2}), rejoining.",
                                        msg.Channel, msg.Nick, msg.Message);
                            Thread.Sleep(ReconnectDelay);
                            Send(IRCCommands.Join(msg.Channel));
                        }
                        else
                        {
                            if (!ResponsibleForInputParsing) return;
                            Server.Message("&i(Global) {0} kicked {1} ({2})",
                                            msg.Nick, kicked, msg.Message);
                        }
                        return;


                    case IRCMessageType.Part:
                    case IRCMessageType.Quit:
                        if (!ResponsibleForInputParsing) return;
                            Server.Message("&i(Global) Server {0} left the 800Craft Global Chat",
                                            msg.Nick);
                        return;


                    case IRCMessageType.NickChange:
                        if (!ResponsibleForInputParsing) return;
                        Server.Message("&i(Global) {0} is now known as {1}",
                                        msg.Nick, msg.Message);
                        return;


                    case IRCMessageType.ErrorMessage:
                    case IRCMessageType.Error:
                        bool die = false;
                        switch (msg.ReplyCode)
                        {
                            case IRCReplyCode.ErrorNicknameInUse:
                            case IRCReplyCode.ErrorNicknameCollision:
                                ActualBotNick = ActualBotNick.Remove(ActualBotNick.Length - 4) + "_";
                                Logger.Log(LogType.SystemActivity,
                                            "Error: Nickname \"{0}\" is already in use. Trying \"{0}\"",
                                            ActualBotNick);
                                Send(IRCCommands.Nick(ActualBotNick));
                                break;

                            case IRCReplyCode.ErrorBannedFromChannel:
                            case IRCReplyCode.ErrorNoSuchChannel:
                                Logger.Log(LogType.SystemActivity,
                                            "Error: {0} ({1})",
                                            msg.ReplyCode, msg.Channel);
                                die = true;
                                break;
                                //wont happen
                            case IRCReplyCode.ErrorBadChannelKey:
                                Logger.Log(LogType.SystemActivity,
                                            "Error: Channel password required for {0}. 800Craft does not currently support passworded channels.",
                                            msg.Channel);
                                die = true;
                                break;

                            default:
                                Logger.Log(LogType.SystemActivity,
                                            "Error ({0}): {1}",
                                            msg.ReplyCode, msg.RawMessage);
                                break;
                        }

                        if (die)
                        {
                            Logger.Log(LogType.SystemActivity, "Error: Disconnecting from Global Chat.");
                            reconnect = false;
                            DisconnectThread();
                        }

                        return;


                    case IRCMessageType.QueryAction:
                        // TODO: PMs
                        Logger.Log(LogType.SystemActivity,
                                    "Query: {0}", msg.RawMessage);
                        break;


                    case IRCMessageType.Kill:
                        Logger.Log(LogType.SystemActivity,
                                    "Bot was killed from {0} by {1} ({2}), reconnecting.",
                                    hostName, msg.Nick, msg.Message);
                        reconnect = true;
                        isConnected = false;
                        return;
                }
            }


            public void DisconnectThread()
            {
                IsReady = false;
                AssignBotForInputParsing();
                isConnected = false;
                if (thread != null && thread.IsAlive)
                {
                    thread.Join(1000);
                    if (thread.IsAlive)
                    {
                        thread.Abort();
                    }
                }
                try
                {
                    if (reader != null) reader.Close();
                }
                catch (ObjectDisposedException) { }
                try
                {
                    if (writer != null) writer.Close();
                }
                catch (ObjectDisposedException) { }
                try
                {
                    if (client != null) client.Close();
                }
                catch (ObjectDisposedException) { }
            }


            #region IDisposable members

            public void Dispose()
            {
                try
                {
                    if (reader != null) reader.Dispose();
                }
                catch (ObjectDisposedException) { }

                try
                {
                    if (reader != null) writer.Dispose();
                }
                catch (ObjectDisposedException) { }

                try
                {
                    if (client != null && client.Connected)
                    {
                        client.Close();
                    }
                }
                catch (ObjectDisposedException) { }
            }

            #endregion
        }


        static GlobalThread[] threads;

        const int Timeout = 10000; // socket timeout (ms)
        internal static int SendDelay = 750; //default
        const int ReconnectDelay = 15000;

        static string hostName;
        static int port;
        static string[] channelNames;
        static string botNick;

        static readonly ConcurrentQueue<string> OutputQueue = new ConcurrentQueue<string>();


        static void AssignBotForInputParsing()
        {
            bool needReassignment = false;
            for (int i = 0; i < threads.Length; i++)
            {
                if (threads[i].ResponsibleForInputParsing && !threads[i].IsReady)
                {
                    threads[i].ResponsibleForInputParsing = false;
                    needReassignment = true;
                }
            }
            if (needReassignment)
            {
                for (int i = 0; i < threads.Length; i++)
                {
                    if (threads[i].IsReady)
                    {
                        threads[i].ResponsibleForInputParsing = true;
                        Logger.Log(LogType.SystemActivity,
                                    "Bot \"{0}\" is now responsible for parsing input.",
                                    threads[i].ActualBotNick);
                        return;
                    }
                }
                Logger.Log(LogType.SystemActivity, "All Global Chat bots have disconnected.");
            }
        }

        // includes IRC color codes and non-printable ASCII
        public static readonly Regex NonPrintableChars = new Regex("\x03\\d{1,2}(,\\d{1,2})?|[\x00-\x1F\x7E-\xFF]", RegexOptions.Compiled);

        public static void Init()
        {
            hostName = "irc.esper.net";
            port = 6667;
            channelNames = new[] { "#800craft" };
            for (int i = 0; i < channelNames.Length; i++)
            {
                channelNames[i] = channelNames[i].Trim();
                if (!channelNames[i].StartsWith("#"))
                {
                    channelNames[i] = '#' + channelNames[i].Trim();
                }
            }
            botNick = "[" + RemoveTroublesomeCharacters(ConfigKey.ServerName.GetString()) + "]";
        }

        public static string RemoveTroublesomeCharacters(string inString)
        {
            if (inString == null) return null;
            StringBuilder newString = new StringBuilder();
            char ch;
            for (int i = 0; i < inString.Length; i++)
            {
                ch = inString[i];
                if ((ch <= 0x007A && ch >= 0x0061) || (ch <= 0x005A && ch >= 0x0041) || (ch <= 0x0039 && ch >= 0x0030) || ch == ']')
                {
                    newString.Append(ch);
                }
            }
            return newString.ToString();
        }


        public static bool Start()
        {
            int threadCount = 1;

            if (threadCount == 1)
            {
                GlobalThread thread = new GlobalThread();
                if (thread.Start(botNick, true))
                {
                    threads = new[] { thread };
                }

            }
            else
            {
                List<GlobalThread> threadTemp = new List<GlobalThread>();
                for (int i = 0; i < threadCount; i++)
                {
                    GlobalThread temp = new GlobalThread();
                    if (temp.Start(botNick + (i + 1), (threadTemp.Count == 0)))
                    {
                        threadTemp.Add(temp);
                    }
                }
                threads = threadTemp.ToArray();
            }

            if (threads.Length > 0)
            {
                //HookUpHandlers();
                return true;
            }
            else
            {
                Logger.Log(LogType.SystemActivity, "GlobalChat functionality disabled.");
                return false;
            }
        }
    }
}