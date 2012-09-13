// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using fCraft.Events;
using JetBrains.Annotations;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace fCraft {
    /// <summary> Helper class for handling player-generated chat. </summary>
    public static class Chat {
        public static List<string> Swears = new List<string>();
        public static IEnumerable<Regex> badWordMatchers;
        static System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();

        #region SendGlobal
        /// <summary> Sends a global (white) chat. </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendGlobal( [NotNull] Player player, [NotNull] string rawMessage ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
            string OriginalMessage = rawMessage;
            if (Server.Moderation && !Server.VoicedPlayers.Contains(player) && player.World != null)
            {
                player.Message("&WError: Server Moderation is activated. Message failed to send");
                return false;
            }
            rawMessage = ParseEmotes(rawMessage, false);
            rawMessage = rawMessage.Replace("$name", "Hello my name is " + player.ClassyName);
            rawMessage = rawMessage.Replace("$kicks", "I have kicked " + player.Info.TimesKickedOthers.ToString() + " players.");
            rawMessage = rawMessage.Replace("$bans", "I have banned " + player.Info.TimesBannedOthers.ToString() + " players.");
            rawMessage = rawMessage.Replace("$awesome", "It is my professional opinion, that " + ConfigKey.ServerName.GetString() + " is the best server on Minecraft");
            rawMessage = rawMessage.Replace("$server", ConfigKey.ServerName.GetString());
            rawMessage = rawMessage.Replace("$motd", ConfigKey.MOTD.GetString());
            rawMessage = rawMessage.Replace("$date", DateTime.UtcNow.ToShortDateString());
            rawMessage = rawMessage.Replace("$time", DateTime.Now.ToString());

            if (!player.Can(Permission.ChatWithCaps))
            {
                int caps = 0;
                for (int i = 0; i < rawMessage.Length; i++)
                {
                    if (Char.IsUpper(rawMessage[i]))
                    {
                        caps++;
                        if (caps > ConfigKey.MaxCaps.GetInt())
                        {
                            rawMessage = rawMessage.ToLower();
                            player.Message("Your message was changed to lowercase as it exceeded the maximum amount of capital letters.");
                        }
                    }
                }
            }

            if (!player.Can(Permission.Swear))
            {
                if (!File.Exists("SwearWords.txt"))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("#This txt file should be filled with bad words that you want to be filtered out");
                    sb.AppendLine("#I have included some examples, excuse my language :P");
                    sb.AppendLine("fuck");
                    sb.AppendLine("fucking");
                    sb.AppendLine("fucked");
                    sb.AppendLine("dick");
                    sb.AppendLine("bitch");
                    sb.AppendLine("shit");
                    sb.AppendLine("shitting");
                    sb.AppendLine("shithead");
                    sb.AppendLine("cunt");
                    sb.AppendLine("nigger");
                    sb.AppendLine("wanker");
                    sb.AppendLine("wank");
                    sb.AppendLine("wanking");
                    sb.AppendLine("piss");
                    File.WriteAllText("SwearWords.txt", sb.ToString());
                }
                string CensoredText = Color.ReplacePercentCodes(ConfigKey.SwearName.GetString()) + Color.White;
                if (ConfigKey.SwearName.GetString() == null)
                {
                    CensoredText = "&CBlock&F";
                }

                const string PatternTemplate = @"\b({0})(s?)\b";
                const RegexOptions Options = RegexOptions.IgnoreCase;

                if (Swears.Count == 0)
                {
                    Swears.AddRange(File.ReadAllLines("SwearWords.txt").
                        Where(line => line.StartsWith("#") == false || line.Trim().Equals(String.Empty)));
                }

                if (badWordMatchers == null)
                {
                    badWordMatchers = Swears.
                        Select(x => new Regex(string.Format(PatternTemplate, x), Options));
                }

                string output = badWordMatchers.
                   Aggregate(rawMessage, (current, matcher) => matcher.Replace(current, CensoredText));
                rawMessage = output;
            }

            var recepientList = Server.Players.NotIgnoring(player);

            string formattedMessage = String.Format( "{0}&F: {1}",
                                                     player.ClassyName,
                                                     rawMessage );

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Global,
                                              recepientList );

            if( !SendInternal( e ) ) return false;

            Logger.Log( LogType.GlobalChat,
                        "{0}: {1}", player.Name, OriginalMessage );
            return true;
        }
        #endregion

        #region Emotes
        public struct EmoteData //Contains all data needed for the emotes. Emote is the trigger word, 
            //example (bullet). ID is the byte value of the char
        {
            public string Emote;
            public byte ID;
        }

        //this list will be loaded with the list of emotes and their replacement values
        static List<EmoteData> EmoteTriggers = null;

        public static EmoteData[] EmotesArray()
        {
            if (EmoteTriggers != null)
            {
                return EmoteTriggers.ToArray();
            }
            else
            {
                AddEmotes();
                return EmoteTriggers.ToArray();
            }
        }

        //a method to add all the emotes into EmoteTriggers. You can add your own triggers here
        static void AddEmotes()
        {
            EmoteTriggers = new List<EmoteData>();
            EmoteTriggers.Add(new EmoteData() { Emote = "(darksmile)", ID = 1 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(:))", ID = 1 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(smile)", ID = 2 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(:P)", ID = 2 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(heart)", ID = 3 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(<3)", ID = 3 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(diamond)", ID = 4 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(bullet)", ID = 7 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(hole)", ID = 8 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(male)", ID = 11 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(female)", ID = 12 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(sun)", ID = 15 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(right)", ID = 16 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(>)", ID = 16 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(left)", ID = 17 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(<)", ID = 17 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(double)", ID = 19 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(half)", ID = 22 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(uparrow)", ID = 24 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(^)", ID = 24 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(downarrow)", ID = 25 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(rightarrow)", ID = 26 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(up)", ID = 30 });
            EmoteTriggers.Add(new EmoteData() { Emote = "(down)", ID = 31 });
        }

        //this is the main method. This parses the emotes, correct glitches and makes the emotes safe to display
        public static string ParseEmotes(string rawMessage, bool SentencePart)
        {
            if (EmoteTriggers == null){ //until a message is made, the list is null, so fill it up here
                AddEmotes();
            }
            byte[] stored = new byte[1]; //this byte represents the emoticon, to be used in GetString(
            foreach (EmoteData ed in EmoteTriggers)
            { //scroll through the emotes, replacing each trigger word with the emote
                string s = ed.Emote; //s is the emote trigger
                stored[0] = (byte)ed.ID; //stored is the byte char for the emote
                string s1 = enc.GetString(stored); //s1 is the emote
                switch (ed.ID)
                { //this fixes glitches. Each ID appends a ' to resolve overlapping of letters
                    case 7:
                    case 12:
                    case 22:
                    case 24:
                    case 25:
                        s1 = s1 + "-";
                        break;
                    case 19:
                        s1 = s1 + "' ";
                        break;
                    default: break;
                }
                if (!SentencePart) //if sentence being parsed definately ends with no following text (not a displayedname, ect)
                {
                    if (rawMessage.EndsWith(s))
                    {
                        s1 = s1 + "."; //append a period. A emoticon ONLY shows if some text follows the emoticon (excluding a blank space)
                    }
                }
                rawMessage = rawMessage.Replace(s, s1); //replace the triggerword with the emoticon
            } 
            return rawMessage; //return the new sentence
        }

        #endregion

        #region SendAdmin
        public static bool SendAdmin(Player player, string rawMessage)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (rawMessage == null) throw new ArgumentNullException("rawMessage");
            rawMessage = ParseEmotes(rawMessage, false);
            var recepientList = Server.Players.Can(Permission.ReadAdminChat)
                                              .NotIgnoring(player);

            string formattedMessage = String.Format("&9(Admin){0}&b: {1}",
                                                     player.ClassyName,
                                                     rawMessage);

            var e = new ChatSendingEventArgs(player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Staff,
                                              recepientList);

            if (!SendInternal(e)) return false;

            Logger.Log(LogType.GlobalChat, "(Admin){0}: {1}", player.Name, rawMessage);
            return true;
        }
        #endregion

        #region SendCustom
        public static bool SendCustom(Player player, string rawMessage)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (rawMessage == null) throw new ArgumentNullException("rawMessage");
            rawMessage = ParseEmotes(rawMessage, false);
            var recepientList = Server.Players.Can(Permission.ReadCustomChat)
                                              .NotIgnoring(player);

            string formattedMessage = String.Format(Color.Custom + "({2}){0}&b: {1}",
                                                     player.ClassyName,
                                                     rawMessage, ConfigKey.CustomChatName.GetString());

            var e = new ChatSendingEventArgs(player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Staff,
                                              recepientList);

            if (!SendInternal(e)) return false;

            Logger.Log(LogType.GlobalChat, "({2}){0}: {1}", player.Name, rawMessage, ConfigKey.CustomChatName.GetString());
            return true;
        }
        #endregion

        #region SendMe
        /// <summary> Sends an action message (/Me). </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendMe( [NotNull] Player player, [NotNull] string rawMessage ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
            rawMessage = ParseEmotes(rawMessage, false);
            var recepientList = Server.Players.NotIgnoring( player );

            string formattedMessage = String.Format( "&M*{0} {1}",
                                                     player.Name,
                                                     rawMessage );

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Me,
                                              recepientList );

            if( !SendInternal( e ) ) return false;

            Logger.Log( LogType.GlobalChat,
                        "(me){0}: {1}", player.Name, rawMessage );
            return true;
        }
        #endregion

        #region SendPM
        /// <summary> Sends a private message (PM). Does NOT send a copy of the message to the sender. </summary>
        /// <param name="from"> Sender player. </param>
        /// <param name="to"> Recepient player. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendPM( [NotNull] Player from, [NotNull] Player to, [NotNull] string rawMessage ) {
            if( from == null ) throw new ArgumentNullException( "from" );
            if( to == null ) throw new ArgumentNullException( "to" );
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
            var recepientList = new[] { to };
            rawMessage = ParseEmotes(rawMessage, false);
            string formattedMessage = String.Format( "&Pfrom {0}: {1}",
                                                     from.Name, rawMessage );

            var e = new ChatSendingEventArgs( from,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.PM,
                                              recepientList );

            if( !SendInternal( e ) ) return false;

            Logger.Log( LogType.PrivateChat,
                        "{0} to {1}: {2}",
                        from.Name, to.Name, rawMessage );
            return true;
        }
        #endregion

        #region SendRank
        /// <summary> Sends a rank-wide message (@@Rank message). </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rank"> Target rank. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendRank( [NotNull] Player player, [NotNull] Rank rank, [NotNull] string rawMessage ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( rank == null ) throw new ArgumentNullException( "rank" );
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
            rawMessage = ParseEmotes(rawMessage, false);
            var recepientList = rank.Players.NotIgnoring( player ).Union( player );

            string formattedMessage = String.Format( "&P({0}&P){1}: {2}",
                                                     rank.ClassyName,
                                                     player.Name,
                                                     rawMessage );

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Rank,
                                              recepientList );

            if( !SendInternal( e ) ) return false;

            Logger.Log( LogType.RankChat,
                        "(rank {0}){1}: {2}",
                        rank.Name, player.Name, rawMessage );
            return true;
        }
        #endregion

        #region SendSay
        /// <summary> Sends a global announcement (/Say). </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendSay( [NotNull] Player player, [NotNull] string rawMessage ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
            rawMessage = ParseEmotes(rawMessage, false);
            var recepientList = Server.Players.NotIgnoring( player );

            string formattedMessage = Color.Say + rawMessage;

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Say,
                                              recepientList );

            if( !SendInternal( e ) ) return false;

            Logger.Log( LogType.GlobalChat,
                        "(say){0}: {1}", player.Name, rawMessage );
            return true;
        }
        #endregion

        #region SendStaff
        /// <summary> Sends a staff message (/Staff). </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendStaff( [NotNull] Player player, [NotNull] string rawMessage ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );
            rawMessage = ParseEmotes(rawMessage, false);
            var recepientList = Server.Players.Can( Permission.ReadStaffChat )
                                              .NotIgnoring( player )
                                              .Union( player );

            string formattedMessage = String.Format( "&P(staff){0}&P: {1}",
                                                     player.ClassyName,
                                                     rawMessage );

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Staff,
                                              recepientList );

            if( !SendInternal( e ) ) return false;

            Logger.Log( LogType.GlobalChat,
                        "(staff){0}: {1}", player.Name, rawMessage );
            return true;
        }
        #endregion


        static bool SendInternal( [NotNull] ChatSendingEventArgs e ) {
            if( e == null ) throw new ArgumentNullException( "e" );
            if( RaiseSendingEvent( e ) ) return false;

            int recepients = e.RecepientList.Message( e.FormattedMessage );

            // Only increment the MessagesWritten count if someone other than
            // the player was on the recepient list.
            if( recepients > 1 || (recepients == 1 && e.RecepientList.First() != e.Player) ) {
                e.Player.Info.ProcessMessageWritten();
            }

            RaiseSentEvent( e, recepients );
            return true;
        }


        /// <summary> Checks for unprintable or illegal characters in a message. </summary>
        /// <param name="message"> Message to check. </param>
        /// <returns> True if message contains invalid chars. False if message is clean. </returns>
        public static bool ContainsInvalidChars( string message ) {
            return message.Any(t => t < ' ' || t == '&' || t > '~');
        }


        /// <summary> Determines the type of player-supplies message based on its syntax. </summary>
        internal static RawMessageType GetRawMessageType( string message ) {
            if( string.IsNullOrEmpty( message ) ) return RawMessageType.Invalid;
            if( message == "/" ) return RawMessageType.RepeatCommand;
            if( message.Equals( "/ok", StringComparison.OrdinalIgnoreCase ) ) return RawMessageType.Confirmation;
            if( message.EndsWith( " /" ) ) return RawMessageType.PartialMessage;
            if( message.EndsWith( " //" ) ) message = message.Substring( 0, message.Length - 1 );

            switch( message[0] ) {
                case '/':
                    if( message.Length < 2 ) {
                        // message too short to be a command
                        return RawMessageType.Invalid;
                    }
                    if( message[1] == '/' ) {
                        // escaped slash in the beginning: "//blah"
                        return RawMessageType.Chat;
                    }
                    if( message[1] != ' ' ) {
                        // normal command: "/cmd"
                        return RawMessageType.Command;
                    }
                    return RawMessageType.Invalid;

                case '@':
                    if( message.Length < 4 || message.IndexOf( ' ' ) == -1 ) {
                        // message too short to be a PM or rank chat
                        return RawMessageType.Invalid;
                    }
                    if( message[1] == '@' ) {
                        return RawMessageType.RankChat;
                    }
                    if( message[1] == '-' && message[2] == ' ' ) {
                        // name shortcut: "@- blah"
                        return RawMessageType.PrivateChat;
                    }
                    if( message[1] == ' ' && message.IndexOf( ' ', 2 ) != -1 ) {
                        // alternative PM notation: "@ name blah"
                        return RawMessageType.PrivateChat;
                    }
                    if( message[1] != ' ' ) {
                        // primary PM notation: "@name blah"
                        return RawMessageType.PrivateChat;
                    }
                    return RawMessageType.Invalid;
            }
            return RawMessageType.Chat;
        }


        #region Events

        static bool RaiseSendingEvent( ChatSendingEventArgs args ) {
            var h = Sending;
            if( h == null ) return false;
            h( null, args );
            return args.Cancel;
        }


        static void RaiseSentEvent( ChatSendingEventArgs args, int count ) {
            var h = Sent;
            if( h != null ) h( null, new ChatSentEventArgs( args.Player, args.Message, args.FormattedMessage,
                                                            args.MessageType, args.RecepientList, count ) );
        }


        /// <summary> Occurs when a chat message is about to be sent. Cancellable. </summary>
        public static event EventHandler<ChatSendingEventArgs> Sending;

        /// <summary> Occurs after a chat message has been sent. </summary>
        public static event EventHandler<ChatSentEventArgs> Sent;

        #endregion
    }


    public enum ChatMessageType {
        Other,

        Global,
        IRC,
        Me,
        PM,
        Rank,
        Say,
        Staff,
        World
    }



    /// <summary> Type of message sent by the player. Determined by CommandManager.GetMessageType() </summary>
    public enum RawMessageType {
        /// <summary> Unparseable chat syntax (rare). </summary>
        Invalid,

        /// <summary> Normal (white) chat. </summary>
        Chat,

        /// <summary> Command. </summary>
        Command,

        /// <summary> Confirmation (/ok) for a previous command. </summary>
        Confirmation,

        /// <summary> Partial message (ends with " /"). </summary>
        PartialMessage,

        /// <summary> Private message. </summary>
        PrivateChat,

        /// <summary> Rank chat. </summary>
        RankChat,

        /// <summary> Repeat of the last command ("/"). </summary>
        RepeatCommand,
    }
}

#region Events

namespace fCraft.Events {
    public sealed class ChatSendingEventArgs : EventArgs, IPlayerEvent, ICancellableEvent {
        internal ChatSendingEventArgs( Player player, string message, string formattedMessage,
                                       ChatMessageType messageType, IEnumerable<Player> recepientList ) {
            Player = player;
            Message = message;
            MessageType = messageType;
            RecepientList = recepientList;
            FormattedMessage = formattedMessage;
        }

        public Player Player { get; private set; }
        public string Message { get; private set; }
        public string FormattedMessage { get; set; }
        public ChatMessageType MessageType { get; private set; }
        public readonly IEnumerable<Player> RecepientList;
        public bool Cancel { get; set; }
    }


    public sealed class ChatSentEventArgs : EventArgs, IPlayerEvent {
        internal ChatSentEventArgs( Player player, string message, string formattedMessage,
                                    ChatMessageType messageType, IEnumerable<Player> recepientList, int recepientCount ) {
            Player = player;
            Message = message;
            MessageType = messageType;
            RecepientList = recepientList;
            FormattedMessage = formattedMessage;
            RecepientCount = recepientCount;
        }

        public Player Player { get; private set; }
        public string Message { get; private set; }
        public string FormattedMessage { get; private set; }
        public ChatMessageType MessageType { get; private set; }
        public IEnumerable<Player> RecepientList { get; private set; }
        public int RecepientCount { get; private set; }
    }
#endregion
}