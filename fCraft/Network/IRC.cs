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

namespace fCraft
{

    /// <summary> IRC control class. </summary>
    public static class IRC
    {

        /// <summary> Class represents an IRC connection/thread.
        /// There is an undocumented option (IRCThreads) to "load balance" the outgoing
        /// messages between multiple bots. If that's the case, several IRCThread objects
        /// are created. The bots grab messages from IRC.outputQueue whenever they are
        /// not on cooldown (a bit of an intentional race condition). </summary>
        public sealed class IRCThread : IDisposable
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
                desiredBotNick = botNick;
                ResponsibleForInputParsing = parseInput;
                try
                {
                    // start the machinery!
                    thread = new Thread(IoThread)
                    {
                        Name = "fCraft.IRC",
                        IsBackground = true
                    };
                    thread.Start();
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Log(LogType.Error,
                                "IRC: Could not start the bot: {0}", ex);
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


            void Send([NotNull] string msg)
            {
                if (msg == null) throw new ArgumentNullException("msg");
                localQueue.Enqueue(msg);
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
                        Logger.Log(LogType.IRC,
                                    "Connecting to {0}:{1} as {2}",
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
                        Logger.Log(LogType.Warning, "IRC: Disconnected. Will retry in {0} seconds.",
                                    ReconnectDelay / 1000);
                        reconnect = true;

                    }
                    catch (IOException)
                    {
                        Logger.Log(LogType.Warning, "IRC: Disconnected. Will retry in {0} seconds.",
                                    ReconnectDelay / 1000);
                        reconnect = true;
#if !DEBUG
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogType.Error, "IRC: {0}", ex);
                        reconnect = true;
#endif
                    }

                    if (reconnect) Thread.Sleep(ReconnectDelay);
                } while (reconnect);
            }


            void HandleMessage([NotNull] string message)
            {
                if (message == null) throw new ArgumentNullException("message");

                IRCMessage msg = MessageParser(message, ActualBotNick);
#if DEBUG_IRC
                Logger.Log( LogType.IRC,
                            "[{0}]: {1}",
                            msg.Type, msg.RawMessage );
#endif

                switch (msg.Type)
                {
                    case IRCMessageType.Login:
                        if (ConfigKey.IRCRegisteredNick.Enabled())
                        {
                            Send(IRCCommands.Privmsg(ConfigKey.IRCNickServ.GetString(),
                                                       ConfigKey.IRCNickServMessage.GetString()));
                        }
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
                        if (!ResponsibleForInputParsing) return;
                        if (!IsBotNick(msg.Nick))
                        {
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
                            processedMessage = NonPrintableChars.Replace(processedMessage, "");
                            if (processedMessage.Length > 0)
                            {
                                if (ConfigKey.IRCBotForwardFromIRC.Enabled())
                                {
                                    if (msg.Type == IRCMessageType.ChannelAction)
                                    {
                                        Server.Message("&i(IRC) * {0} {1}",
                                                        msg.Nick, processedMessage);
                                    }
                                    else
                                    {
                                        Server.Message("&i(IRC) {0}{1}: {2}",
                                                        msg.Nick, Color.White, processedMessage);
                                    }
                                }
                                else if (msg.Message.StartsWith("#"))
                                {
                                    Server.Message("&i(IRC) {0}{1}: {2}",
                                                    msg.Nick, Color.White, processedMessage.Substring(1));
                                }
                            }
                        }
                        return;


                    case IRCMessageType.Join:
                        if (!ResponsibleForInputParsing) return;
                        if (ConfigKey.IRCBotAnnounceIRCJoins.Enabled())
                        {
                            Server.Message("&i(IRC) {0} joined {1}",
                                            msg.Nick, msg.Channel);
                        }
                        return;


                    case IRCMessageType.Kick:
                        string kicked = msg.RawMessageArray[3];
                        if (kicked == ActualBotNick)
                        {
                            Logger.Log(LogType.IRC,
                                        "Bot was kicked from {0} by {1} ({2}), rejoining.",
                                        msg.Channel, msg.Nick, msg.Message);
                            Thread.Sleep(ReconnectDelay);
                            Send(IRCCommands.Join(msg.Channel));
                        }
                        else
                        {
                            if (!ResponsibleForInputParsing) return;
                            Server.Message("&i(IRC) {0} kicked {1} from {2} ({3})",
                                            msg.Nick, kicked, msg.Channel, msg.Message);
                        }
                        return;


                    case IRCMessageType.Part:
                    case IRCMessageType.Quit:
                        if (!ResponsibleForInputParsing) return;
                        if (ConfigKey.IRCBotAnnounceIRCJoins.Enabled())
                        {
                            Server.Message("&i(IRC) {0} left {1}",
                                            msg.Nick, msg.Channel);
                        }
                        return;


                    case IRCMessageType.NickChange:
                        if (!ResponsibleForInputParsing) return;
                        Server.Message("&i(IRC) {0} is now known as {1}",
                                        msg.Nick, msg.Message);
                        return;


                    case IRCMessageType.ErrorMessage:
                    case IRCMessageType.Error:
                        bool die = false;
                        switch (msg.ReplyCode)
                        {
                            case IRCReplyCode.ErrorNicknameInUse:
                            case IRCReplyCode.ErrorNicknameCollision:
                                Logger.Log(LogType.IRC,
                                            "Error: Nickname \"{0}\" is already in use. Trying \"{0}_\"",
                                            ActualBotNick);
                                ActualBotNick += "_";
                                Send(IRCCommands.Nick(ActualBotNick));
                                break;

                            case IRCReplyCode.ErrorBannedFromChannel:
                            case IRCReplyCode.ErrorNoSuchChannel:
                                Logger.Log(LogType.IRC,
                                            "Error: {0} ({1})",
                                            msg.ReplyCode, msg.Channel);
                                die = true;
                                break;

                            case IRCReplyCode.ErrorBadChannelKey:
                                Logger.Log(LogType.IRC,
                                            "Error: Channel password required for {0}. fCraft does not currently support passworded channels.",
                                            msg.Channel);
                                die = true;
                                break;

                            default:
                                Logger.Log(LogType.IRC,
                                            "Error ({0}): {1}",
                                            msg.ReplyCode, msg.RawMessage);
                                break;
                        }

                        if (die)
                        {
                            Logger.Log(LogType.IRC, "Error: Disconnecting.");
                            reconnect = false;
                            DisconnectThread();
                        }

                        return;


                    case IRCMessageType.QueryAction:
                        // TODO: PMs
                        Logger.Log(LogType.IRC,
                                    "Query: {0}", msg.RawMessage);
                        break;


                    case IRCMessageType.Kill:
                        Logger.Log(LogType.IRC,
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


        static IRCThread[] threads;

        const int Timeout = 10000; // socket timeout (ms)
        internal static int SendDelay; // set by ApplyConfig
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
                        Logger.Log(LogType.IRC,
                                    "Bot \"{0}\" is now responsible for parsing input.",
                                    threads[i].ActualBotNick);
                        return;
                    }
                }
                Logger.Log(LogType.IRC, "All IRC bots have disconnected.");
            }
        }

        // includes IRC color codes and non-printable ASCII
        public static readonly Regex NonPrintableChars = new Regex("\x03\\d{1,2}(,\\d{1,2})?|[\x00-\x1F\x7E-\xFF]", RegexOptions.Compiled);

        public static void Init()
        {
            if (!ConfigKey.IRCBotEnabled.Enabled()) return;

            hostName = ConfigKey.IRCBotNetwork.GetString();
            port = ConfigKey.IRCBotPort.GetInt();
            channelNames = ConfigKey.IRCBotChannels.GetString().Split(',');
            for (int i = 0; i < channelNames.Length; i++)
            {
                channelNames[i] = channelNames[i].Trim();
                if (!channelNames[i].StartsWith("#"))
                {
                    channelNames[i] = '#' + channelNames[i].Trim();
                }
            }
            botNick = ConfigKey.IRCBotNick.GetString();
        }


        public static bool Start()
        {
            int threadCount = ConfigKey.IRCThreads.GetInt();

            if (threadCount == 1)
            {
                IRCThread thread = new IRCThread();
                if (thread.Start(botNick, true))
                {
                    threads = new[] { thread };
                }

            }
            else
            {
                List<IRCThread> threadTemp = new List<IRCThread>();
                for (int i = 0; i < threadCount; i++)
                {
                    IRCThread temp = new IRCThread();
                    if (temp.Start(botNick + (i + 1), (threadTemp.Count == 0)))
                    {
                        threadTemp.Add(temp);
                    }
                }
                threads = threadTemp.ToArray();
            }

            if (threads.Length > 0)
            {
                HookUpHandlers();
                return true;
            }
            else
            {
                Logger.Log(LogType.IRC, "IRC functionality disabled.");
                return false;
            }
        }

        public static void SendChannelMessage([NotNull] string line)
        {
            if (line == null) throw new ArgumentNullException("line");
            if (channelNames == null) return; // in case IRC bot is disabled.
            if (ConfigKey.IRCUseColor.Enabled())
            {
                line = Color.ToIRCColorCodes(line);
            }
            else
            {
                line = NonPrintableChars.Replace(line, "").Trim();
            }
            for (int i = 0; i < channelNames.Length; i++)
            {
                SendRawMessage(IRCCommands.Privmsg(channelNames[i], line));
            }
        }


        public static void SendAction([NotNull] string line)
        {
            if (line == null) throw new ArgumentNullException("line");
            SendChannelMessage(String.Format("\u0001ACTION {0}\u0001", line));
        }


        public static void SendNotice([NotNull] string line)
        {
            if (line == null) throw new ArgumentNullException("line");
            if (channelNames == null) return; // in case IRC bot is disabled.
            if (ConfigKey.IRCUseColor.Enabled())
            {
                line = Color.ToIRCColorCodes(line);
            }
            else
            {
                line = NonPrintableChars.Replace(line, "").Trim();
            }
            for (int i = 0; i < channelNames.Length; i++)
            {
                SendRawMessage(IRCCommands.Notice(channelNames[i], line));
            }
        }


        public static void SendRawMessage([NotNull] string line)
        {
            if (line == null) throw new ArgumentNullException("line");
            OutputQueue.Enqueue(line);
        }


        public static bool IsBotNick([NotNull] string str)
        {
            if (str == null) throw new ArgumentNullException("str");
            return threads.Any(t => t.ActualBotNick == str);
        }


        public static void Disconnect()
        {
            if (threads != null && threads.Length > 0)
            {
                foreach (IRCThread thread in threads)
                {
                    thread.DisconnectThread();
                }
            }
        }


        #region Server Event Handlers

        static void HookUpHandlers()
        {
            Chat.Sent += ChatSentHandler;
            Player.Ready += PlayerReadyHandler;
            Player.Disconnected += PlayerDisconnectedHandler;
            Player.Kicked += PlayerKickedHandler;
            PlayerInfo.BanChanged += PlayerInfoBanChangedHandler;
            PlayerInfo.RankChanged += PlayerInfoRankChangedHandler;
        }


        internal static void ChatSentHandler(object sender, ChatSentEventArgs args)
        {
            bool enabled = ConfigKey.IRCBotForwardFromServer.Enabled();
            switch (args.MessageType)
            {
                case ChatMessageType.Global:
                    if (enabled)
                    {
                        SendChannelMessage(args.Player.ClassyName + Color.IRCReset + ": " + args.Message);
                    }
                    else if (args.Message.StartsWith("#"))
                    {
                        SendChannelMessage(args.Player.ClassyName + Color.IRCReset + ": " + args.Message.Substring(1));
                    }
                    break;

                case ChatMessageType.Me:
                case ChatMessageType.Say:
                    if (enabled) SendAction(args.FormattedMessage);
                    break;
            }
        }


        internal static void PlayerReadyHandler(object sender, IPlayerEvent e)
        {
            if (ConfigKey.IRCBotAnnounceServerJoins.Enabled() && !e.Player.Info.IsHidden)
            {
                string message = String.Format("\u0001ACTION {0}&S* {1}&S connected.\u0001",
                                                Color.IRCBold,
                                                e.Player.ClassyName);
                SendChannelMessage(message);
            }
        }


        internal static void PlayerDisconnectedHandler(object sender, PlayerDisconnectedEventArgs e)
        {
            if (e.Player.HasFullyConnected && ConfigKey.IRCBotAnnounceServerJoins.Enabled() && (e.IsFake || !e.Player.Info.IsHidden))
            {
                string message = String.Format("{0}&S* {1}&S left the server ({2})",
                                 Color.IRCBold,
                                 e.Player.ClassyName,
                                 e.LeaveReason);
                SendAction(message);
            }
        }


        static void PlayerKickedHandler(object sender, PlayerKickedEventArgs e)
        {
            if (e.Announce && e.Context == LeaveReason.Kick)
            {
                PlayerSomethingMessage(e.Kicker, "kicked", e.Player.Info, e.Reason);
            }
        }


        static void PlayerInfoBanChangedHandler(object sender, PlayerInfoBanChangedEventArgs e)
        {
            if (e.Announce)
            {
                if (e.IsBeingUnbanned)
                {
                    PlayerSomethingMessage(e.Banner, "unbanned", e.PlayerInfo, e.Reason);
                }
                else
                {
                    PlayerSomethingMessage(e.Banner, "banned", e.PlayerInfo, e.Reason);
                }
            }
        }


        static void PlayerInfoRankChangedHandler(object sender, PlayerInfoRankChangedEventArgs e)
        {
            if (e.Announce)
            {
                string actionString = String.Format("{0} from {1}&W to {2}&W",
                                                     e.RankChangeType,
                                                     e.OldRank.ClassyName,
                                                     e.NewRank.ClassyName);
                PlayerSomethingMessage(e.RankChanger, actionString, e.PlayerInfo, e.Reason);
            }
        }

        public static void PlayerSomethingMessage([NotNull] IClassy player, [NotNull] string action, [NotNull] IClassy target, [CanBeNull] string reason)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (action == null) throw new ArgumentNullException("action");
            if (target == null) throw new ArgumentNullException("target");
            string message = String.Format("{0}&W* {1}&W was {2} by {3}&W",
                    Color.IRCBold,
                    target.ClassyName,
                    action,
                    player.ClassyName);
            if (!String.IsNullOrEmpty(reason))
            {
                message += " Reason: " + reason;
            }
            if (ConfigKey.IRCBotAnnounceServerEvents.Enabled())
            {
                SendAction(message);
            }
        }

        #endregion


        #region Parsing

        static readonly IRCReplyCode[] ReplyCodes = (IRCReplyCode[])Enum.GetValues(typeof(IRCReplyCode));


        static IRCMessageType GetMessageType([NotNull] string rawline, [NotNull] string actualBotNick)
        {
            if (rawline == null) throw new ArgumentNullException("rawline");
            if (actualBotNick == null) throw new ArgumentNullException("actualBotNick");

            Match found = ReplyCodeRegex.Match(rawline);
            if (found.Success)
            {
                string code = found.Groups[1].Value;
                IRCReplyCode replycode = (IRCReplyCode)int.Parse(code);

                // check if this replycode is known in the RFC
                if (Array.IndexOf(ReplyCodes, replycode) == -1)
                {
                    return IRCMessageType.Unknown;
                }

                switch (replycode)
                {
                    case IRCReplyCode.Welcome:
                    case IRCReplyCode.YourHost:
                    case IRCReplyCode.Created:
                    case IRCReplyCode.MyInfo:
                    case IRCReplyCode.Bounce:
                        return IRCMessageType.Login;
                    case IRCReplyCode.LuserClient:
                    case IRCReplyCode.LuserOp:
                    case IRCReplyCode.LuserUnknown:
                    case IRCReplyCode.LuserMe:
                    case IRCReplyCode.LuserChannels:
                        return IRCMessageType.Info;
                    case IRCReplyCode.MotdStart:
                    case IRCReplyCode.Motd:
                    case IRCReplyCode.EndOfMotd:
                        return IRCMessageType.Motd;
                    case IRCReplyCode.NamesReply:
                    case IRCReplyCode.EndOfNames:
                        return IRCMessageType.Name;
                    case IRCReplyCode.WhoReply:
                    case IRCReplyCode.EndOfWho:
                        return IRCMessageType.Who;
                    case IRCReplyCode.ListStart:
                    case IRCReplyCode.List:
                    case IRCReplyCode.ListEnd:
                        return IRCMessageType.List;
                    case IRCReplyCode.BanList:
                    case IRCReplyCode.EndOfBanList:
                        return IRCMessageType.BanList;
                    case IRCReplyCode.Topic:
                    case IRCReplyCode.TopicSetBy:
                    case IRCReplyCode.NoTopic:
                        return IRCMessageType.Topic;
                    case IRCReplyCode.WhoIsUser:
                    case IRCReplyCode.WhoIsServer:
                    case IRCReplyCode.WhoIsOperator:
                    case IRCReplyCode.WhoIsIdle:
                    case IRCReplyCode.WhoIsChannels:
                    case IRCReplyCode.EndOfWhoIs:
                        return IRCMessageType.WhoIs;
                    case IRCReplyCode.WhoWasUser:
                    case IRCReplyCode.EndOfWhoWas:
                        return IRCMessageType.WhoWas;
                    case IRCReplyCode.UserModeIs:
                        return IRCMessageType.UserMode;
                    case IRCReplyCode.ChannelModeIs:
                        return IRCMessageType.ChannelMode;
                    default:
                        if (((int)replycode >= 400) &&
                            ((int)replycode <= 599))
                        {
                            return IRCMessageType.ErrorMessage;
                        }
                        else
                        {
                            return IRCMessageType.Unknown;
                        }
                }
            }

            found = PingRegex.Match(rawline);
            if (found.Success)
            {
                return IRCMessageType.Ping;
            }

            found = ErrorRegex.Match(rawline);
            if (found.Success)
            {
                return IRCMessageType.Error;
            }

            found = ActionRegex.Match(rawline);
            if (found.Success)
            {
                switch (found.Groups[1].Value)
                {
                    case "#":
                    case "!":
                    case "&":
                    case "+":
                        return IRCMessageType.ChannelAction;
                    default:
                        return IRCMessageType.QueryAction;
                }
            }

            found = CtcpRequestRegex.Match(rawline);
            if (found.Success)
            {
                return IRCMessageType.CtcpRequest;
            }

            found = MessageRegex.Match(rawline);
            if (found.Success)
            {
                switch (found.Groups[1].Value)
                {
                    case "#":
                    case "!":
                    case "&":
                    case "+":
                        return IRCMessageType.ChannelMessage;
                    default:
                        return IRCMessageType.QueryMessage;
                }
            }

            found = CtcpReplyRegex.Match(rawline);
            if (found.Success)
            {
                return IRCMessageType.CtcpReply;
            }

            found = NoticeRegex.Match(rawline);
            if (found.Success)
            {
                switch (found.Groups[1].Value)
                {
                    case "#":
                    case "!":
                    case "&":
                    case "+":
                        return IRCMessageType.ChannelNotice;
                    default:
                        return IRCMessageType.QueryNotice;
                }
            }

            found = InviteRegex.Match(rawline);
            if (found.Success)
            {
                return IRCMessageType.Invite;
            }

            found = JoinRegex.Match(rawline);
            if (found.Success)
            {
                return IRCMessageType.Join;
            }

            found = TopicRegex.Match(rawline);
            if (found.Success)
            {
                return IRCMessageType.TopicChange;
            }

            found = NickRegex.Match(rawline);
            if (found.Success)
            {
                return IRCMessageType.NickChange;
            }

            found = KickRegex.Match(rawline);
            if (found.Success)
            {
                return IRCMessageType.Kick;
            }

            found = PartRegex.Match(rawline);
            if (found.Success)
            {
                return IRCMessageType.Part;
            }

            found = ModeRegex.Match(rawline);
            if (found.Success)
            {
                if (found.Groups[1].Value == actualBotNick)
                {
                    return IRCMessageType.UserModeChange;
                }
                else
                {
                    return IRCMessageType.ChannelModeChange;
                }
            }

            found = QuitRegex.Match(rawline);
            if (found.Success)
            {
                return IRCMessageType.Quit;
            }

            found = KillRegex.Match(rawline);
            return found.Success ? IRCMessageType.Kill : IRCMessageType.Unknown;
        }


        public static IRCMessage MessageParser([NotNull] string rawline, [NotNull] string actualBotNick)
        {
            if (rawline == null) throw new ArgumentNullException("rawline");
            if (actualBotNick == null) throw new ArgumentNullException("actualBotNick");

            string line;
            string nick = null;
            string ident = null;
            string host = null;
            string channel = null;
            string message = null;
            IRCReplyCode replycode;

            if (rawline[0] == ':')
            {
                line = rawline.Substring(1);
            }
            else
            {
                line = rawline;
            }

            string[] linear = line.Split(new[] { ' ' });

            // conform to RFC 2812
            string from = linear[0];
            string messagecode = linear[1];
            int exclamationpos = from.IndexOf("!");
            int atpos = from.IndexOf("@");
            int colonpos = line.IndexOf(" :");
            if (colonpos != -1)
            {
                // we want the exact position of ":" not beginning from the space
                colonpos += 1;
            }
            if (exclamationpos != -1)
            {
                nick = from.Substring(0, exclamationpos);
            }
            if ((atpos != -1) &&
                (exclamationpos != -1))
            {
                ident = from.Substring(exclamationpos + 1, (atpos - exclamationpos) - 1);
            }
            if (atpos != -1)
            {
                host = from.Substring(atpos + 1);
            }

            try
            {
                replycode = (IRCReplyCode)int.Parse(messagecode);
            }
            catch (FormatException)
            {
                replycode = IRCReplyCode.Null;
            }
            IRCMessageType type = GetMessageType(rawline, actualBotNick);
            if (colonpos != -1)
            {
                message = line.Substring(colonpos + 1);
            }

            switch (type)
            {
                case IRCMessageType.Join:
                case IRCMessageType.Kick:
                case IRCMessageType.Part:
                case IRCMessageType.TopicChange:
                case IRCMessageType.ChannelModeChange:
                case IRCMessageType.ChannelMessage:
                case IRCMessageType.ChannelAction:
                case IRCMessageType.ChannelNotice:
                    channel = linear[2];
                    break;
                case IRCMessageType.Who:
                case IRCMessageType.Topic:
                case IRCMessageType.Invite:
                case IRCMessageType.BanList:
                case IRCMessageType.ChannelMode:
                    channel = linear[3];
                    break;
                case IRCMessageType.Name:
                    channel = linear[4];
                    break;
            }

            if ((channel != null) &&
                (channel[0] == ':'))
            {
                channel = channel.Substring(1);
            }

            return new IRCMessage(from, nick, ident, host, channel, message, rawline, type, replycode);
        }


        static readonly Regex ReplyCodeRegex = new Regex("^:[^ ]+? ([0-9]{3}) .+$", RegexOptions.Compiled);
        static readonly Regex PingRegex = new Regex("^PING :.*", RegexOptions.Compiled);
        static readonly Regex ErrorRegex = new Regex("^ERROR :.*", RegexOptions.Compiled);
        static readonly Regex ActionRegex = new Regex("^:.*? PRIVMSG (.).* :" + "\x1" + "ACTION .*" + "\x1" + "$", RegexOptions.Compiled);
        static readonly Regex CtcpRequestRegex = new Regex("^:.*? PRIVMSG .* :" + "\x1" + ".*" + "\x1" + "$", RegexOptions.Compiled);
        static readonly Regex MessageRegex = new Regex("^:.*? PRIVMSG (.).* :.*$", RegexOptions.Compiled);
        static readonly Regex CtcpReplyRegex = new Regex("^:.*? NOTICE .* :" + "\x1" + ".*" + "\x1" + "$", RegexOptions.Compiled);
        static readonly Regex NoticeRegex = new Regex("^:.*? NOTICE (.).* :.*$", RegexOptions.Compiled);
        static readonly Regex InviteRegex = new Regex("^:.*? INVITE .* .*$", RegexOptions.Compiled);
        static readonly Regex JoinRegex = new Regex("^:.*? JOIN .*$", RegexOptions.Compiled);
        static readonly Regex TopicRegex = new Regex("^:.*? TOPIC .* :.*$", RegexOptions.Compiled);
        static readonly Regex NickRegex = new Regex("^:.*? NICK .*$", RegexOptions.Compiled);
        static readonly Regex KickRegex = new Regex("^:.*? KICK .* .*$", RegexOptions.Compiled);
        static readonly Regex PartRegex = new Regex("^:.*? PART .*$", RegexOptions.Compiled);
        static readonly Regex ModeRegex = new Regex("^:.*? MODE (.*) .*$", RegexOptions.Compiled);
        static readonly Regex QuitRegex = new Regex("^:.*? QUIT :.*$", RegexOptions.Compiled);
        static readonly Regex KillRegex = new Regex("^:.*? KILL (.*) :.*$", RegexOptions.Compiled);

        #endregion
    }


    // ReSharper disable UnusedMember.Global
    /// <summary> IRC protocol reply codes. </summary>
    public enum IRCReplyCode
    {
        Null = 000,
        Welcome = 001,
        YourHost = 002,
        Created = 003,
        MyInfo = 004,
        Bounce = 005,
        TraceLink = 200,
        TraceConnecting = 201,
        TraceHandshake = 202,
        TraceUnknown = 203,
        TraceOperator = 204,
        TraceUser = 205,
        TraceServer = 206,
        TraceService = 207,
        TraceNewType = 208,
        TraceClass = 209,
        TraceReconnect = 210,
        StatsLinkInfo = 211,
        StatsCommands = 212,
        EndOfStats = 219,
        UserModeIs = 221,
        ServiceList = 234,
        ServiceListEnd = 235,
        StatsUptime = 242,
        StatsOLine = 243,
        LuserClient = 251,
        LuserOp = 252,
        LuserUnknown = 253,
        LuserChannels = 254,
        LuserMe = 255,
        AdminMe = 256,
        AdminLocation1 = 257,
        AdminLocation2 = 258,
        AdminEmail = 259,
        TraceLog = 261,
        TraceEnd = 262,
        TryAgain = 263,
        Away = 301,
        UserHost = 302,
        IsOn = 303,
        UnAway = 305,
        NowAway = 306,
        WhoIsUser = 311,
        WhoIsServer = 312,
        WhoIsOperator = 313,
        WhoWasUser = 314,
        EndOfWho = 315,
        WhoIsIdle = 317,
        EndOfWhoIs = 318,
        WhoIsChannels = 319,
        ListStart = 321,
        List = 322,
        ListEnd = 323,
        ChannelModeIs = 324,
        UniqueOpIs = 325,
        NoTopic = 331,
        Topic = 332,
        TopicSetBy = 333,
        Inviting = 341,
        Summoning = 342,
        InviteList = 346,
        EndOfInviteList = 347,
        ExceptionList = 348,
        EndOfExceptionList = 349,
        Version = 351,
        WhoReply = 352,
        NamesReply = 353,
        Links = 364,
        EndOfLinks = 365,
        EndOfNames = 366,
        BanList = 367,
        EndOfBanList = 368,
        EndOfWhoWas = 369,
        Info = 371,
        Motd = 372,
        EndOfInfo = 374,
        MotdStart = 375,
        EndOfMotd = 376,
        YouAreOper = 381,
        Rehashing = 382,
        YouAreService = 383,
        Time = 391,
        UsersStart = 392,
        Users = 393,
        EndOfUsers = 394,
        NoUsers = 395,
        ErrorNoSuchNickname = 401,
        ErrorNoSuchServer = 402,
        ErrorNoSuchChannel = 403,
        ErrorCannotSendToChannel = 404,
        ErrorTooManyChannels = 405,
        ErrorWasNoSuchNickname = 406,
        ErrorTooManyTargets = 407,
        ErrorNoSuchService = 408,
        ErrorNoOrigin = 409,
        ErrorNoRecipient = 411,
        ErrorNoTextToSend = 412,
        ErrorNoTopLevel = 413,
        ErrorWildTopLevel = 414,
        ErrorBadMask = 415,
        ErrorUnknownCommand = 421,
        ErrorNoMotd = 422,
        ErrorNoAdminInfo = 423,
        ErrorFileError = 424,
        ErrorNoNicknameGiven = 431,
        ErrorErroneusNickname = 432,
        ErrorNicknameInUse = 433,
        ErrorNicknameCollision = 436,
        ErrorUnavailableResource = 437,
        ErrorUserNotInChannel = 441,
        ErrorNotOnChannel = 442,
        ErrorUserOnChannel = 443,
        ErrorNoLogin = 444,
        ErrorSummonDisabled = 445,
        ErrorUsersDisabled = 446,
        ErrorNotRegistered = 451,
        ErrorNeedMoreParams = 461,
        ErrorAlreadyRegistered = 462,
        ErrorNoPermissionForHost = 463,
        ErrorPasswordMismatch = 464,
        ErrorYouAreBannedCreep = 465,
        ErrorYouWillBeBanned = 466,
        ErrorKeySet = 467,
        ErrorChannelIsFull = 471,
        ErrorUnknownMode = 472,
        ErrorInviteOnlyChannel = 473,
        ErrorBannedFromChannel = 474,
        ErrorBadChannelKey = 475,
        ErrorBadChannelMask = 476,
        ErrorNoChannelModes = 477,
        ErrorBanListFull = 478,
        ErrorNoPrivileges = 481,
        ErrorChannelOpPrivilegesNeeded = 482,
        ErrorCannotKillServer = 483,
        ErrorRestricted = 484,
        ErrorUniqueOpPrivilegesNeeded = 485,
        ErrorNoOperHost = 491,
        ErrorUserModeUnknownFlag = 501,
        ErrorUsersDoNotMatch = 502
    }


    /// <summary> IRC message types. </summary>
    public enum IRCMessageType
    {
        Ping,
        Info,
        Login,
        Motd,
        List,
        Join,
        Kick,
        Part,
        Invite,
        Quit,
        Kill,
        Who,
        WhoIs,
        WhoWas,
        Name,
        Topic,
        BanList,
        NickChange,
        TopicChange,
        UserMode,
        UserModeChange,
        ChannelMode,
        ChannelModeChange,
        ChannelMessage,
        ChannelAction,
        ChannelNotice,
        QueryMessage,
        QueryAction,
        QueryNotice,
        CtcpReply,
        CtcpRequest,
        Error,
        ErrorMessage,
        Unknown
    }
}