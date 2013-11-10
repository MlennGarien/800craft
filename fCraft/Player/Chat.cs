// Copyright 2009-2013 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {

    /// <summary> Helper class for handling player-generated chat. </summary>
    public static class Chat {
        public static List<string> Swears = new List<string>();
        public static IEnumerable<Regex> badWordMatchers;
        private static System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();

        #region SendGlobal

        /// <summary> Sends a global (white) chat. </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendGlobal( [NotNull] Player player, [NotNull] string rawMessage ) {
            if ( player == null )
                throw new ArgumentNullException( "player" );
            if ( rawMessage == null )
                throw new ArgumentNullException( "rawMessage" );
            string OriginalMessage = rawMessage;
            if ( Server.Moderation && !Server.VoicedPlayers.Contains( player ) && player.World != null ) {
                player.Message( "&WError: Server Moderation is activated. Message failed to send" );
                return false;
            }
            rawMessage = rawMessage.Replace( "$name", "Hello my name is " + player.ClassyName );
            rawMessage = rawMessage.Replace( "$kicks", "I have kicked " + player.Info.TimesKickedOthers.ToString() + " players." );
            rawMessage = rawMessage.Replace( "$bans", "I have banned " + player.Info.TimesBannedOthers.ToString() + " players." );
            rawMessage = rawMessage.Replace( "$awesome", "It is my professional opinion, that " + ConfigKey.ServerName.GetString() + " is the best server on Minecraft" );
            rawMessage = rawMessage.Replace( "$server", ConfigKey.ServerName.GetString() );
            rawMessage = rawMessage.Replace( "$motd", ConfigKey.MOTD.GetString() );
            rawMessage = rawMessage.Replace( "$date", DateTime.UtcNow.ToShortDateString() );
            rawMessage = rawMessage.Replace( "$time", DateTime.UtcNow.ToString() );

            if ( !player.Can( Permission.ChatWithCaps ) ) {
                int caps = 0;
                for ( int i = 0; i < rawMessage.Length; i++ ) {
                    if ( Char.IsUpper( rawMessage[i] ) ) {
                        caps++;
                        if ( caps > ConfigKey.MaxCaps.GetInt() ) {
                            rawMessage = rawMessage.ToLower();
                            player.Message( "Your message was changed to lowercase as it exceeded the maximum amount of capital letters." );
                        }
                    }
                }
            }

            if ( !player.Can( Permission.Swear ) ) {
                if ( !File.Exists( "SwearWords.txt" ) ) {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine( "#This txt file should be filled with bad words that you want to be filtered out" );
                    sb.AppendLine( "#I have included some examples, excuse my language :P" );
                    sb.AppendLine( "fuck" );
                    sb.AppendLine( "fucking" );
                    sb.AppendLine( "fucked" );
                    sb.AppendLine( "dick" );
                    sb.AppendLine( "bitch" );
                    sb.AppendLine( "shit" );
                    sb.AppendLine( "shitting" );
                    sb.AppendLine( "shithead" );
                    sb.AppendLine( "cunt" );
                    sb.AppendLine( "nigger" );
                    sb.AppendLine( "wanker" );
                    sb.AppendLine( "wank" );
                    sb.AppendLine( "wanking" );
                    sb.AppendLine( "piss" );
                    File.WriteAllText( "SwearWords.txt", sb.ToString() );
                }
                string CensoredText = Color.ReplacePercentCodes( ConfigKey.SwearName.GetString() ) + Color.White;
                if ( ConfigKey.SwearName.GetString() == null ) {
                    CensoredText = "&CBlock&F";
                }

                const string PatternTemplate = @"\b({0})(s?)\b";
                const RegexOptions Options = RegexOptions.IgnoreCase;

                if ( Swears.Count == 0 ) {
                    Swears.AddRange( File.ReadAllLines( "SwearWords.txt" ).
                        Where( line => line.StartsWith( "#" ) == false || line.Trim().Equals( String.Empty ) ) );
                }

                if ( badWordMatchers == null ) {
                    badWordMatchers = Swears.
                        Select( x => new Regex( string.Format( PatternTemplate, x ), Options ) );
                }

                string output = badWordMatchers.
                   Aggregate( rawMessage, ( current, matcher ) => matcher.Replace( current, CensoredText ) );
                rawMessage = output;
            }

            var recepientList = Server.Players.NotIgnoring( player );

            string formattedMessage = String.Format( "{0}&F: {1}",
                                                     player.ClassyName,
                                                     rawMessage );

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Global,
                                              recepientList );

            if ( !SendInternal( e ) )
                return false;

            Logger.Log( LogType.GlobalChat,
                        "{0}: {1}", player.Name, OriginalMessage );
            return true;
        }

        #endregion SendGlobal

        #region Emotes

        private static readonly char[] UnicodeReplacements = " ☺☻♥♦♣♠•◘○\n♂♀♪♫☼►◄↕‼¶§▬↨↑↓→←∟↔▲▼".ToCharArray();

        /// <summary> List of chat keywords, and emotes that they stand for. </summary>
        public static readonly Dictionary<string, char> EmoteKeywords = new Dictionary<string, char> {
            { ":)", '\u0001' }, // ☺
            { "smile", '\u0001' },

            { "smile2", '\u0002' }, // ☻

            { "heart", '\u0003' }, // ♥
            { "hearts", '\u0003' },
            { "<3", '\u0003' },

            { "diamond", '\u0004' }, // ♦
            { "diamonds", '\u0004' },
            { "rhombus", '\u0004' },

            { "club", '\u0005' }, // ♣
            { "clubs", '\u0005' },
            { "clover", '\u0005' },
            { "shamrock", '\u0005' },

            { "spade", '\u0006' }, // ♠
            { "spades", '\u0006' },

            { "*", '\u0007' }, // •
            { "bullet", '\u0007' },
            { "dot", '\u0007' },
            { "point", '\u0007' },

            { "hole", '\u0008' }, // ◘

            { "circle", '\u0009' }, // ○
            { "o", '\u0009' },

            { "male", '\u000B' }, // ♂
            { "mars", '\u000B' },

            { "female", '\u000C' }, // ♀
            { "venus", '\u000C' },

            { "8", '\u000D' }, // ♪
            { "note", '\u000D' },
            { "quaver", '\u000D' },

            { "notes", '\u000E' }, // ♫
            { "music", '\u000E' },

            { "sun", '\u000F' }, // ☼
            { "celestia", '\u000F' },

            { ">>", '\u0010' }, // ►
            { "right2", '\u0010' },

            { "<<", '\u0011' }, // ◄
            { "left2", '\u0011' },

            { "updown", '\u0012' }, // ↕
            { "^v", '\u0012' },

            { "!!", '\u0013' }, // ‼

            { "p", '\u0014' }, // ¶
            { "para", '\u0014' },
            { "pilcrow", '\u0014' },
            { "paragraph", '\u0014' },

            { "s", '\u0015' }, // §
            { "sect", '\u0015' },
            { "section", '\u0015' },

            { "-", '\u0016' }, // ▬
            { "_", '\u0016' },
            { "bar", '\u0016' },
            { "half", '\u0016' },

            { "updown2", '\u0017' }, // ↨
            { "^v_", '\u0017' },

            { "^", '\u0018' }, // ↑
            { "up", '\u0018' },

            { "v", '\u0019' }, // ↓
            { "down", '\u0019' },

            { ">", '\u001A' }, // →
            { "->", '\u001A' },
            { "right", '\u001A' },

            { "<", '\u001B' }, // ←
            { "<-", '\u001B' },
            { "left", '\u001B' },

            { "l", '\u001C' }, // ∟
            { "angle", '\u001C' },
            { "corner", '\u001C' },

            { "<>", '\u001D' }, // ↔
            { "<->", '\u001D' },
            { "leftright", '\u001D' },

            { "^^", '\u001E' }, // ▲
            { "up2", '\u001E' },

            { "vv", '\u001F' }, // ▼
            { "down2", '\u001F' },

            { "house", '\u007F' } // ⌂
        };

        private static readonly Regex EmoteSymbols = new Regex( "[\x00-\x1F\x7F☺☻♥♦♣♠•◘○\n♂♀♪♫☼►◄↕‼¶§▬↨↑↓→←∟↔▲▼⌂]" );

        /// <summary> Strips all emote symbols (ASCII control characters). Does not strip UTF-8 equivalents of emotes. </summary>
        /// <param name="message"> Message to strip emotes from. </param>
        /// <returns> Message with its emotes stripped. </returns>
        [NotNull, Pure]
        public static string StripEmotes( [NotNull] string message ) {
            if ( message == null )
                throw new ArgumentNullException( "message" );
            return EmoteSymbols.Replace( message, "" );
        }

        /// <summary> Replaces emote keywords with actual emotes, using Chat.EmoteKeywords mapping.
        /// Keywords are enclosed in curly braces, and are case-insensitive. </summary>
        /// <param name="message"> String to process. </param>
        /// <returns> Processed string. </returns>
        /// <exception cref="ArgumentNullException"> input is null. </exception>
        [NotNull, Pure]
        public static string ReplaceEmoteKeywords( [NotNull] string message ) {
            if ( message == null )
                throw new ArgumentNullException( "message" );
            int startIndex = message.IndexOf( '{' );
            if ( startIndex == -1 ) {
                return message; // break out early if there are no opening braces
            }

            StringBuilder output = new StringBuilder( message.Length );
            int lastAppendedIndex = 0;
            while ( startIndex != -1 ) {
                int endIndex = message.IndexOf( '}', startIndex + 1 );
                if ( endIndex == -1 ) {
                    break; // abort if there are no more closing braces
                }

                // see if emote was escaped (if odd number of backslashes precede it)
                bool escaped = false;
                for ( int i = startIndex - 1; i >= 0 && message[i] == '\\'; i-- ) {
                    escaped = !escaped;
                }
                // extract the keyword
                string keyword = message.Substring( startIndex + 1, endIndex - startIndex - 1 );
                char substitute;
                if ( EmoteKeywords.TryGetValue( keyword.ToLowerInvariant(), out substitute ) ) {
                    if ( escaped ) {
                        // it was escaped; remove escaping character
                        startIndex++;
                        output.Append( message, lastAppendedIndex, startIndex - lastAppendedIndex - 2 );
                        lastAppendedIndex = startIndex - 1;
                    } else {
                        // it was not escaped; insert substitute character
                        output.Append( message, lastAppendedIndex, startIndex - lastAppendedIndex );
                        output.Append( substitute );
                        startIndex = endIndex + 1;
                        lastAppendedIndex = startIndex;
                    }
                } else {
                    startIndex++; // unrecognized macro, keep going
                }
                startIndex = message.IndexOf( '{', startIndex );
            }
            // append the leftovers
            output.Append( message, lastAppendedIndex, message.Length - lastAppendedIndex );
            return output.ToString();
        }

        /// <summary> Substitutes percent color codes (e.g. %C) with equivalent ampersand color codes (&amp;C).
        /// Also replaces newline codes (%N) with actual newlines (\n). </summary>
        /// <param name="message"> Message to process. </param>
        /// <param name="allowNewlines"> Whether newlines are allowed. </param>
        /// <returns> Processed string. </returns>
        /// <exception cref="ArgumentNullException"> message is null. </exception>
        [NotNull, Pure]
        public static string ReplacePercentColorCodes( [NotNull] string message, bool allowNewlines ) {
            if ( message == null )
                throw new ArgumentNullException( "message" );
            int startIndex = message.IndexOf( '%' );
            if ( startIndex == -1 ) {
                return message; // break out early if there are no percent marks
            }

            StringBuilder output = new StringBuilder( message.Length );
            int lastAppendedIndex = 0;
            while ( startIndex != -1 && startIndex < message.Length - 1 ) {
                // see if colorcode was escaped (if odd number of backslashes precede it)
                bool escaped = false;
                for ( int i = startIndex - 1; i >= 0 && message[i] == '\\'; i-- ) {
                    escaped = !escaped;
                }
                // extract the colorcode
                char colorCode = message[startIndex + 1];
                if ( Color.IsValidColorCode( colorCode ) || allowNewlines && ( colorCode == 'n' || colorCode == 'N' ) ) {
                    if ( escaped ) {
                        // it was escaped; remove escaping character
                        startIndex++;
                        output.Append( message, lastAppendedIndex, startIndex - lastAppendedIndex - 2 );
                        lastAppendedIndex = startIndex - 1;
                    } else {
                        // it was not escaped; insert substitute character
                        output.Append( message, lastAppendedIndex, startIndex - lastAppendedIndex );
                        output.Append( '&' );
                        lastAppendedIndex = startIndex + 1;
                        startIndex += 2;
                    }
                } else {
                    startIndex++; // unrecognized colorcode, keep going
                }
                startIndex = message.IndexOf( '%', startIndex );
            }
            // append the leftovers
            output.Append( message, lastAppendedIndex, message.Length - lastAppendedIndex );
            return output.ToString();
        }

        /// <summary> Replaces keywords with appropriate values. </summary>
        [NotNull]
        public static string ReplaceTextKeywords( [NotNull] Player player, [NotNull] string input ) {
            if ( player == null )
                throw new ArgumentNullException( "player" );
            if ( input == null )
                throw new ArgumentNullException( "input" );
            StringBuilder sb = new StringBuilder( input );
            sb.Replace( "{SERVER_NAME}", ConfigKey.ServerName.GetString() );
            sb.Replace( "{RANK}", player.Info.Rank.ClassyName );
            sb.Replace( "{TIME}", DateTime.Now.ToShortTimeString() ); // localized
            if ( player.World == null ) {
                sb.Replace( "{WORLD}", "(No World)" );
            } else {
                sb.Replace( "{WORLD}", player.World.ClassyName );
            }
            sb.Replace( "{WORLDS}", WorldManager.Worlds.Length.ToString() );
            sb.Replace( "{MOTD}", ConfigKey.MOTD.GetString() );
            sb.Replace( "{VERSION}", Updater.CurrentRelease.VersionString );
            if ( input.IndexOf("{PLAYER", System.StringComparison.Ordinal) != -1 ) {
                Player[] playerList = Server.Players.CanBeSeen( player ).Union( player ).ToArray();
                sb.Replace( "{PLAYER_NAME}", player.ClassyName );
                sb.Replace( "{PLAYER_LIST}", playerList.JoinToClassyString() );
                sb.Replace( "{PLAYERS}", playerList.Length.ToString() );
            }
            return sb.ToString();
        }

        /// <summary> Removes newlines (\n) and newline codes (&amp;n and &amp;N). </summary>
        /// <param name="message"> Message to process. </param>
        /// <returns> Processed message. </returns>
        /// <exception cref="ArgumentNullException"> message is null. </exception>
        [NotNull, Pure]
        public static string StripNewlines( [NotNull] string message ) {
            if ( message == null )
                throw new ArgumentNullException( "message" );
            message = message.Replace( "\n", "" );
            message = message.Replace( "&n", "" );
            message = message.Replace( "&N", "" );
            return message;
        }

        /// <summary> Replaces newline codes (&amp;n and &amp;N) with actual newlines (\n). </summary>
        /// <param name="message"> Message to process. </param>
        /// <returns> Processed message. </returns>
        /// <exception cref="ArgumentNullException"> message is null. </exception>
        [NotNull, Pure]
        public static string ReplaceNewlines( [NotNull] string message ) {
            if ( message == null )
                throw new ArgumentNullException( "message" );
            message = message.Replace( "&n", "\n" );
            message = message.Replace( "&N", "\n" );
            return message;
        }

        /// <summary> Unescapes backslashes. Any paid of backslashes (\\) is converted to a single one (\). </summary>
        /// <param name="message"> String to process. </param>
        /// <returns> Processed string. </returns>
        /// <exception cref="ArgumentNullException"> message is null. </exception>
        [NotNull, Pure]
        public static string UnescapeBackslashes( [NotNull] string message ) {
            if ( message == null )
                throw new ArgumentNullException( "message" );
            if ( message.IndexOf( '\\' ) != -1 ) {
                return message.Replace( @"\\", @"\" );
            } else {
                return message;
            }
        }

        /// <summary> Replaces UTF-8 symbol characters with ASCII control characters, matching Code Page 437.
        /// Opposite of ReplaceEmotesWithUncode. </summary>
        /// <param name="input"> String to process. </param>
        /// <returns> Processed string, with its UTF-8 symbol characters replaced. </returns>
        /// <exception cref="ArgumentNullException"> input is null. </exception>
        [NotNull]
        public static string ReplaceUncodeWithEmotes( [NotNull] string input ) {
            if ( input == null )
                throw new ArgumentNullException( "input" );
            StringBuilder sb = new StringBuilder( input );
            for ( int i = 1; i < UnicodeReplacements.Length; i++ ) {
                sb.Replace( UnicodeReplacements[i], ( char )i );
            }
            sb.Replace( '⌂', '\u007F' );
            return sb.ToString();
        }

        /// <summary> Replaces ASCII control characters with UTF-8 symbol characters, matching Code Page 437.
        /// Opposite of ReplaceUncodeWithEmotes. </summary>
        /// <param name="input"> String to process. </param>
        /// <returns> Processed string, with its ASCII control characters replaced. </returns>
        /// <exception cref="ArgumentNullException"> input is null. </exception>
        [NotNull]
        public static string ReplaceEmotesWithUncode( [NotNull] string input ) {
            if ( input == null )
                throw new ArgumentNullException( "input" );
            StringBuilder sb = new StringBuilder( input );
            for ( int i = 1; i < UnicodeReplacements.Length; i++ ) {
                sb.Replace( ( char )i, UnicodeReplacements[i] );
            }
            sb.Replace( '\u007F', '⌂' );
            return sb.ToString();
        }

        #endregion Emotes

        #region SendAdmin

        public static bool SendAdmin( Player player, string rawMessage ) {
            if ( player == null )
                throw new ArgumentNullException( "player" );
            if ( rawMessage == null )
                throw new ArgumentNullException( "rawMessage" );
            var recepientList = Server.Players.Can( Permission.ReadAdminChat )
                                              .NotIgnoring( player );

            string formattedMessage = String.Format( "&9(Admin){0}&b: {1}",
                                                     player.ClassyName,
                                                     rawMessage );

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Staff,
                                              recepientList );

            if ( !SendInternal( e ) )
                return false;

            Logger.Log( LogType.GlobalChat, "(Admin){0}: {1}", player.Name, rawMessage );
            return true;
        }

        #endregion SendAdmin

        #region SendCustom

        public static bool SendCustom( Player player, string rawMessage ) {
            if ( player == null )
                throw new ArgumentNullException( "player" );
            if ( rawMessage == null )
                throw new ArgumentNullException( "rawMessage" );
            var recepientList = Server.Players.Can( Permission.ReadCustomChat )
                                              .NotIgnoring( player );

            string formattedMessage = String.Format( Color.Custom + "({2}){0}&b: {1}",
                                                     player.ClassyName,
                                                     rawMessage, ConfigKey.CustomChatName.GetString() );

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Staff,
                                              recepientList );

            if ( !SendInternal( e ) )
                return false;

            Logger.Log( LogType.GlobalChat, "({2}){0}: {1}", player.Name, rawMessage, ConfigKey.CustomChatName.GetString() );
            return true;
        }

        #endregion SendCustom

        #region SendMe

        /// <summary> Sends an action message (/Me). </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendMe( [NotNull] Player player, [NotNull] string rawMessage ) {
            if ( player == null )
                throw new ArgumentNullException( "player" );
            if ( rawMessage == null )
                throw new ArgumentNullException( "rawMessage" );
            var recepientList = Server.Players.NotIgnoring( player );

            string formattedMessage = String.Format( "&M*{0} {1}",
                                                     player.Name,
                                                     rawMessage );

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Me,
                                              recepientList );

            if ( !SendInternal( e ) )
                return false;

            Logger.Log( LogType.GlobalChat,
                        "(me){0}: {1}", player.Name, rawMessage );
            return true;
        }

        #endregion SendMe

        #region SendPM

        /// <summary> Sends a private message (PM). Does NOT send a copy of the message to the sender. </summary>
        /// <param name="from"> Sender player. </param>
        /// <param name="to"> Recepient player. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendPM( [NotNull] Player from, [NotNull] Player to, [NotNull] string rawMessage ) {
            if ( from == null )
                throw new ArgumentNullException( "from" );
            if ( to == null )
                throw new ArgumentNullException( "to" );
            if ( rawMessage == null )
                throw new ArgumentNullException( "rawMessage" );
            var recepientList = new[] { to };
            string formattedMessage = String.Format( "&Pfrom {0}: {1}",
                                                     from.Name, rawMessage );

            var e = new ChatSendingEventArgs( from,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.PM,
                                              recepientList );

            if ( !SendInternal( e ) )
                return false;

            Logger.Log( LogType.PrivateChat,
                        "{0} to {1}: {2}",
                        from.Name, to.Name, rawMessage );
            return true;
        }

        #endregion SendPM

        #region SendRank

        /// <summary> Sends a rank-wide message (@@Rank message). </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rank"> Target rank. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendRank( [NotNull] Player player, [NotNull] Rank rank, [NotNull] string rawMessage ) {
            if ( player == null )
                throw new ArgumentNullException( "player" );
            if ( rank == null )
                throw new ArgumentNullException( "rank" );
            if ( rawMessage == null )
                throw new ArgumentNullException( "rawMessage" );
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

            if ( !SendInternal( e ) )
                return false;

            Logger.Log( LogType.RankChat,
                        "(rank {0}){1}: {2}",
                        rank.Name, player.Name, rawMessage );
            return true;
        }

        #endregion SendRank

        #region SendSay

        /// <summary> Sends a global announcement (/Say). </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendSay( [NotNull] Player player, [NotNull] string rawMessage ) {
            if ( player == null )
                throw new ArgumentNullException( "player" );
            if ( rawMessage == null )
                throw new ArgumentNullException( "rawMessage" );
            var recepientList = Server.Players.NotIgnoring( player );

            string formattedMessage = Color.Say + rawMessage;

            var e = new ChatSendingEventArgs( player,
                                              rawMessage,
                                              formattedMessage,
                                              ChatMessageType.Say,
                                              recepientList );

            if ( !SendInternal( e ) )
                return false;

            Logger.Log( LogType.GlobalChat,
                        "(say){0}: {1}", player.Name, rawMessage );
            return true;
        }

        #endregion SendSay

        #region SendStaff

        /// <summary> Sends a staff message (/Staff). </summary>
        /// <param name="player"> Player writing the message. </param>
        /// <param name="rawMessage"> Message text. </param>
        /// <returns> True if message was sent, false if it was cancelled by an event callback. </returns>
        public static bool SendStaff( [NotNull] Player player, [NotNull] string rawMessage ) {
            if ( player == null )
                throw new ArgumentNullException( "player" );
            if ( rawMessage == null )
                throw new ArgumentNullException( "rawMessage" );
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

            if ( !SendInternal( e ) )
                return false;

            Logger.Log( LogType.GlobalChat,
                        "(staff){0}: {1}", player.Name, rawMessage );
            return true;
        }

        #endregion SendStaff

        private static bool SendInternal( [NotNull] ChatSendingEventArgs e ) {
            if ( e == null )
                throw new ArgumentNullException( "e" );
            if ( RaiseSendingEvent( e ) )
                return false;

            int recepients = e.RecepientList.Message( e.FormattedMessage );

            // Only increment the MessagesWritten count if someone other than
            // the player was on the recepient list.
            if ( recepients > 1 || ( recepients == 1 && e.RecepientList.First() != e.Player ) ) {
                e.Player.Info.ProcessMessageWritten();
            }

            RaiseSentEvent( e, recepients );
            return true;
        }

        /// <summary> Checks for unprintable or illegal characters in a message. </summary>
        /// <param name="message"> Message to check. </param>
        /// <returns> True if message contains invalid chars. False if message is clean. </returns>
        public static bool ContainsInvalidChars( string message ) {
            return message.Any( t => t < ' ' || t == '&' || t > '~' );
        }

        /// <summary> Determines the type of player-supplies message based on its syntax. </summary>
        internal static RawMessageType GetRawMessageType( string message ) {
            if ( string.IsNullOrEmpty( message ) )
                return RawMessageType.Invalid;
            if ( message == "/" )
                return RawMessageType.RepeatCommand;
            if ( message.Equals( "/ok", StringComparison.OrdinalIgnoreCase ) )
                return RawMessageType.Confirmation;
            if ( message.EndsWith( " /" ) )
                return RawMessageType.PartialMessage;
            if ( message.EndsWith( " //" ) )
                message = message.Substring( 0, message.Length - 1 );

            switch ( message[0] ) {
                case '/':
                    if ( message.Length < 2 ) {
                        // message too short to be a command
                        return RawMessageType.Invalid;
                    }
                    if ( message[1] == '/' ) {
                        // escaped slash in the beginning: "//blah"
                        return RawMessageType.Chat;
                    }
                    if ( message[1] != ' ' ) {
                        // normal command: "/cmd"
                        return RawMessageType.Command;
                    }
                    return RawMessageType.Invalid;

                case '@':
                    if ( message.Length < 4 || message.IndexOf( ' ' ) == -1 ) {
                        // message too short to be a PM or rank chat
                        return RawMessageType.Invalid;
                    }
                    if ( message[1] == '@' ) {
                        return RawMessageType.RankChat;
                    }
                    if ( message[1] == '-' && message[2] == ' ' ) {
                        // name shortcut: "@- blah"
                        return RawMessageType.PrivateChat;
                    }
                    if ( message[1] == ' ' && message.IndexOf( ' ', 2 ) != -1 ) {
                        // alternative PM notation: "@ name blah"
                        return RawMessageType.PrivateChat;
                    }
                    if ( message[1] != ' ' ) {
                        // primary PM notation: "@name blah"
                        return RawMessageType.PrivateChat;
                    }
                    return RawMessageType.Invalid;
            }
            return RawMessageType.Chat;
        }

        #region Events

        private static bool RaiseSendingEvent( ChatSendingEventArgs args ) {
            var h = Sending;
            if ( h == null )
                return false;
            h( null, args );
            return args.Cancel;
        }

        private static void RaiseSentEvent( ChatSendingEventArgs args, int count ) {
            var h = Sent;
            if ( h != null )
                h( null, new ChatSentEventArgs( args.Player, args.Message, args.FormattedMessage,
                                                args.MessageType, args.RecepientList, count ) );
        }

        /// <summary> Occurs when a chat message is about to be sent. Cancellable. </summary>
        public static event EventHandler<ChatSendingEventArgs> Sending;

        /// <summary> Occurs after a chat message has been sent. </summary>
        public static event EventHandler<ChatSentEventArgs> Sent;

        #endregion Events
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

#endregion Events
}