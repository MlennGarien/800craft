// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;

namespace fCraft {
    static class ChatCommands {

        public static void Init() {
            CommandManager.RegisterCommand( CdSay );
            CommandManager.RegisterCommand( CdStaff );

            CommandManager.RegisterCommand( CdIgnore );
            CommandManager.RegisterCommand( CdUnignore );

            CommandManager.RegisterCommand( CdMe );

            CommandManager.RegisterCommand( CdRoll );

            CommandManager.RegisterCommand( CdDeafen );

            CommandManager.RegisterCommand( CdClear );

            CommandManager.RegisterCommand( CdTimer );
        }


        #region Say

        static readonly CommandDescriptor CdSay = new CommandDescriptor {
            Name = "Say",
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            NotRepeatable = true,
            DisableLogging = true,
            Permissions = new[] { Permission.Chat, Permission.Say },
            Usage = "/Say Message",
            Help = "Shows a message in special color, without the player name prefix. " +
                   "Can be used for making announcements.",
            Handler = SayHandler
        };

        static void SayHandler( Player player, Command cmd ) {
            if( player.Info.IsMuted ) {
                player.MessageMuted();
                return;
            }

            if( player.DetectChatSpam() ) return;

            if( player.Can( Permission.Say ) ) {
                string msg = cmd.NextAll().Trim();
                if( msg.Length > 0 ) {
                    Chat.SendSay( player, msg );
                } else {
                    CdSay.PrintUsage( player );
                }
            } else {
                player.MessageNoAccess( Permission.Say );
            }
        }

        #endregion


        #region Staff

        static readonly CommandDescriptor CdStaff = new CommandDescriptor {
            Name = "Staff",
            Aliases = new[] { "st" },
            Category = CommandCategory.Chat | CommandCategory.Moderation,
            Permissions = new[] { Permission.Chat },
            NotRepeatable = true,
            IsConsoleSafe = true,
            DisableLogging = true,
            Usage = "/Staff Message",
            Help = "Broadcasts your message to all operators/moderators on the server at once.",
            Handler = StaffHandler
        };

        static void StaffHandler( Player player, Command cmd ) {
            if( player.Info.IsMuted ) {
                player.MessageMuted();
                return;
            }

            if( player.DetectChatSpam() ) return;

            string message = cmd.NextAll().Trim();
            if( message.Length > 0 ) {
                Chat.SendStaff( player, message );
            }
        }

        #endregion


        #region Ignore / Unignore

        static readonly CommandDescriptor CdIgnore = new CommandDescriptor {
            Name = "Ignore",
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            Usage = "/Ignore [PlayerName]",
            Help = "Temporarily blocks the other player from messaging you. " +
                   "If no player name is given, lists all ignored players.",
            Handler = IgnoreHandler
        };

        static void IgnoreHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name != null ) {
                if( cmd.HasNext ) {
                    CdIgnore.PrintUsage( player );
                    return;
                }
                PlayerInfo targetInfo = PlayerDB.FindPlayerInfoOrPrintMatches( player, name );
                if( targetInfo == null ) return;

                if( player.Ignore( targetInfo ) ) {
                    player.MessageNow( "You are now ignoring {0}", targetInfo.ClassyName );
                } else {
                    player.MessageNow( "You are already ignoring {0}", targetInfo.ClassyName );
                }

            } else {
                PlayerInfo[] ignoreList = player.IgnoreList;
                if( ignoreList.Length > 0 ) {
                    player.MessageNow( "Ignored players: {0}", ignoreList.JoinToClassyString() );
                } else {
                    player.MessageNow( "You are not currently ignoring anyone." );
                }
                return;
            }
        }


        static readonly CommandDescriptor CdUnignore = new CommandDescriptor {
            Name = "Unignore",
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            Usage = "/Unignore PlayerName",
            Help = "Unblocks the other player from messaging you.",
            Handler = UnignoreHandler
        };

        static void UnignoreHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name != null ) {
                if( cmd.HasNext ) {
                    CdUnignore.PrintUsage( player );
                    return;
                }
                PlayerInfo targetInfo = PlayerDB.FindPlayerInfoOrPrintMatches( player, name );
                if( targetInfo == null ) return;

                if( player.Unignore( targetInfo ) ) {
                    player.MessageNow( "You are no longer ignoring {0}", targetInfo.ClassyName );
                } else {
                    player.MessageNow( "You are not currently ignoring {0}", targetInfo.ClassyName );
                }
            } else {
                PlayerInfo[] ignoreList = player.IgnoreList;
                if( ignoreList.Length > 0 ) {
                    player.MessageNow( "Ignored players: {0}", ignoreList.JoinToClassyString() );
                } else {
                    player.MessageNow( "You are not currently ignoring anyone." );
                }
                return;
            }
        }

        #endregion


        #region Me

        static readonly CommandDescriptor CdMe = new CommandDescriptor {
            Name = "Me",
            Category = CommandCategory.Chat,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = true,
            NotRepeatable = true,
            DisableLogging = true,
            Usage = "/Me Message",
            Help = "Sends IRC-style action message prefixed with your name.",
            Handler = MeHandler
        };

        static void MeHandler( Player player, Command cmd ) {
            if( player.Info.IsMuted ) {
                player.MessageMuted();
                return;
            }

            if( player.DetectChatSpam() ) return;

            string msg = cmd.NextAll().Trim();
            if( msg.Length > 0 ) {
                Chat.SendMe( player, msg );
            } else {
                CdMe.PrintUsage( player );
            }
        }

        #endregion


        #region Roll

        static readonly CommandDescriptor CdRoll = new CommandDescriptor {
            Name = "Roll",
            Category = CommandCategory.Chat,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = true,
            Help = "Gives random number between 1 and 100.\n" +
                   "&H/Roll MaxNumber\n" +
                   "&S  Gives number between 1 and max.\n" +
                   "&H/Roll MinNumber MaxNumber\n" +
                   "&S  Gives number between min and max.",
            Handler = RollHandler
        };

        static void RollHandler( Player player, Command cmd ) {
            if( player.Info.IsMuted ) {
                player.MessageMuted();
                return;
            }

            if( player.DetectChatSpam() ) return;

            Random rand = new Random();
            int n1;
            int min, max;
            if( cmd.NextInt( out n1 ) ) {
                int n2;
                if( !cmd.NextInt( out n2 ) ) {
                    n2 = 1;
                }
                min = Math.Min( n1, n2 );
                max = Math.Max( n1, n2 );
            } else {
                min = 1;
                max = 100;
            }

            int num = rand.Next( min, max + 1 );
            Server.Message( player,
                            "{0}{1} rolled {2} ({3}...{4})",
                            player.ClassyName, Color.Silver, num, min, max );
            player.Message( "{0}You rolled {1} ({2}...{3})",
                            Color.Silver, num, min, max );
        }

        #endregion


        #region Deafen

        static readonly CommandDescriptor CdDeafen = new CommandDescriptor {
            Name = "Deafen",
            Aliases = new[] { "deaf" },
            Category = CommandCategory.Chat,
            IsConsoleSafe = true,
            Help = "Blocks all chat messages from being sent to you.",
            Handler = DeafenHandler
        };

        static void DeafenHandler( Player player, Command cmd ) {
            if( cmd.HasNext ) {
                CdDeafen.PrintUsage( player );
                return;
            }
            if( !player.IsDeaf ) {
                for( int i = 0; i < LinesToClear; i++ ) {
                    player.MessageNow( "" );
                }
                player.MessageNow( "Deafened mode: ON" );
                player.MessageNow( "You will not see ANY messages until you type &H/Deafen&S again." );
                player.IsDeaf = true;
            } else {
                player.IsDeaf = false;
                player.MessageNow( "Deafened mode: OFF" );
            }
        }

        #endregion


        #region Clear

        const int LinesToClear = 30;
        static readonly CommandDescriptor CdClear = new CommandDescriptor {
            Name = "Clear",
            UsableByFrozenPlayers = true,
            Category = CommandCategory.Chat,
            Help = "Clears the chat screen.",
            Handler = ClearHandler
        };

        static void ClearHandler( Player player, Command cmd ) {
            if( cmd.HasNext ) {
                CdClear.PrintUsage( player );
                return;
            }
            for( int i = 0; i < LinesToClear; i++ ) {
                player.Message( "" );
            }
        }

        #endregion


        #region Timer

        static readonly CommandDescriptor CdTimer = new CommandDescriptor {
            Name = "Timer",
            Permissions = new[] { Permission.Say },
            IsConsoleSafe = true,
            Category = CommandCategory.Chat,
            Usage = "/Timer <Duration> <Message>",
            Help = "Starts a timer with a given duration and message. " +
                   "As the timer counts down, announcements are shown globally. See also: &H/Help Timer Abort",
            HelpSections = new Dictionary<string, string> {
                { "abort",  "&H/Timer Abort <TimerID>\n&S" +
                            "Aborts a timer with the given ID number. " +
                            "To see a list of timers and their IDs, type &H/Timer&S (without any parameters)." }
            },
            Handler = TimerHandler
        };

        static void TimerHandler( Player player, Command cmd ) {
            string param = cmd.Next();

            // List timers
            if( param == null ) {
                ChatTimer[] list = ChatTimer.TimerList.OrderBy( timer => timer.TimeLeft ).ToArray();
                if( list.Length == 0 ) {
                    player.Message( "No timers running." );
                } else {
                    player.Message( "There are {0} timers running:", list.Length );
                    foreach( ChatTimer timer in list ) {
                        player.Message( "  #{0} \"{1}&S\" (started by {2}, {3} left)",
                                        timer.Id, timer.Message, timer.StartedBy, timer.TimeLeft.ToMiniString() );
                    }
                }
                return;
            }

            // Abort a timer
            if( param.Equals( "abort", StringComparison.OrdinalIgnoreCase ) ) {
                int timerId;
                if( cmd.NextInt( out timerId ) ) {
                    ChatTimer timer = ChatTimer.FindTimerById( timerId );
                    if( timer == null || !timer.IsRunning ) {
                        player.Message( "Given timer (#{0}) does not exist.", timerId );
                    } else {
                        timer.Stop();
                        string abortMsg = String.Format( "&Y(Timer) {0}&Y aborted a timer with {1} left: {2}",
                                                         player.ClassyName, timer.TimeLeft.ToMiniString(), timer.Message );
                        Chat.SendSay( player, abortMsg );
                    }
                } else {
                    CdTimer.PrintUsage( player );
                }
                return;
            }

            // Start a timer
            if( player.Info.IsMuted ) {
                player.MessageMuted();
                return;
            }
            if( player.DetectChatSpam() ) return;
            TimeSpan duration;
            if( !param.TryParseMiniTimespan( out duration ) ) {
                CdTimer.PrintUsage( player );
                return;
            }
            if( duration > DateTimeUtil.MaxTimeSpan ) {
                player.MessageMaxTimeSpan();
                return;
            }
            if( duration < ChatTimer.MinDuration ) {
                player.Message( "Timer: Must be at least 1 second." );
                return;
            }

            string sayMessage;
            string message = cmd.NextAll();
            if( String.IsNullOrEmpty( message ) ) {
                sayMessage = String.Format( "&Y(Timer) {0}&Y started a {1} timer",
                                            player.ClassyName,
                                            duration.ToMiniString() );
            } else {
                sayMessage = String.Format( "&Y(Timer) {0}&Y started a {1} timer: {2}",
                                            player.ClassyName,
                                            duration.ToMiniString(),
                                            message );
            }
            Chat.SendSay( player, sayMessage );
            ChatTimer.Start( duration, message, player.Name );
        }

        #endregion
    }
}