// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary>
    /// Most commands for server moderation - kick, ban, rank change, etc - are here.
    /// </summary>
    static class ModerationCommands {
        const string BanCommonHelp = "Ban information can be viewed with &H/BanInfo";

        internal static void Init() {
            CdBan.Help += BanCommonHelp;
            CdBanIP.Help += BanCommonHelp;
            CdBanAll.Help += BanCommonHelp;
            CdUnban.Help += BanCommonHelp;
            CdUnbanIP.Help += BanCommonHelp;
            CdUnbanAll.Help += BanCommonHelp;

            CommandManager.RegisterCommand( CdBan );
            CommandManager.RegisterCommand( CdBanIP );
            CommandManager.RegisterCommand( CdBanAll );
            CommandManager.RegisterCommand( CdUnban );
            CommandManager.RegisterCommand( CdUnbanIP );
            CommandManager.RegisterCommand( CdUnbanAll );

            CommandManager.RegisterCommand( CdBanEx );

            CommandManager.RegisterCommand( CdKick );

            CommandManager.RegisterCommand( CdRank );

            CommandManager.RegisterCommand( CdHide );
            CommandManager.RegisterCommand( CdUnhide );

            CommandManager.RegisterCommand( CdSetSpawn );

            CommandManager.RegisterCommand( CdFreeze );
            CommandManager.RegisterCommand( CdUnfreeze );

            CommandManager.RegisterCommand( CdTP );
            CommandManager.RegisterCommand( CdBring );
            CommandManager.RegisterCommand( CdWorldBring );
            CommandManager.RegisterCommand( CdBringAll );

            CommandManager.RegisterCommand( CdPatrol );
            CommandManager.RegisterCommand( CdSpecPatrol );

            CommandManager.RegisterCommand( CdMute );
            CommandManager.RegisterCommand( CdUnmute );

            CommandManager.RegisterCommand( CdSpectate );
            CommandManager.RegisterCommand( CdUnspectate );
        }


        #region Ban / Unban

        static readonly CommandDescriptor CdBan = new CommandDescriptor {
            Name = "Ban",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban },
            Usage = "/Ban PlayerName [Reason]",
            Help = "Bans a specified player by name. Note: Does NOT ban IP. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = BanHandler
        };

        static void BanHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if( targetName == null ) {
                CdBan.PrintUsage( player );
                return;
            }
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetName );
            if( target == null ) return;
            string reason = cmd.NextAll();
            try {
                Player targetPlayer = target.PlayerObject;
                target.Ban( player, reason, true, true );
                WarnIfOtherPlayersOnIP( player, target, targetPlayer );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
                if( ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired ) {
                    FreezeIfAllowed( player, target );
                }
            }
        }



        static readonly CommandDescriptor CdBanIP = new CommandDescriptor {
            Name = "BanIP",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP },
            Usage = "/BanIP PlayerName|IPAddress [Reason]",
            Help = "Bans the player's name and IP. If player is not online, last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. " +
                   "Any text after PlayerName/IP will be saved as a memo. ",
            Handler = BanIPHandler
        };

        static void BanIPHandler( Player player, Command cmd ) {
            string targetNameOrIP = cmd.Next();
            if( targetNameOrIP == null ) {
                CdBanIP.PrintUsage( player );
                return;
            }
            string reason = cmd.NextAll();

            IPAddress targetAddress;
            if( Server.IsIP( targetNameOrIP ) && IPAddress.TryParse( targetNameOrIP, out targetAddress ) ) {
                try {
                    targetAddress.BanIP( player, reason, true, true );
                } catch( PlayerOpException ex ) {
                    player.Message( ex.MessageColored );
                }
            } else {
                PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetNameOrIP );
                if( target == null ) return;
                try {
                    if( target.LastIP.Equals( IPAddress.Any ) || target.LastIP.Equals( IPAddress.None ) ) {
                        target.Ban( player, reason, true, true );
                    } else {
                        target.BanIP( player, reason, true, true );
                    }
                } catch( PlayerOpException ex ) {
                    player.Message( ex.MessageColored );
                    if( ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired ) {
                        FreezeIfAllowed( player, target );
                    }
                }
            }
        }



        static readonly CommandDescriptor CdBanAll = new CommandDescriptor {
            Name = "BanAll",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
            Usage = "/BanAll PlayerName|IPAddress [Reason]",
            Help = "Bans the player's name, IP, and all other names associated with the IP. " +
                   "If player is not online, last known IP associated with the name is used. " +
                   "You can also type in the IP address directly. " +
                   "Any text after PlayerName/IP will be saved as a memo. ",
            Handler = BanAllHandler
        };

        static void BanAllHandler( Player player, Command cmd ) {
            string targetNameOrIP = cmd.Next();
            if( targetNameOrIP == null ) {
                CdBanAll.PrintUsage( player );
                return;
            }
            string reason = cmd.NextAll();

            IPAddress targetAddress;
            if( Server.IsIP( targetNameOrIP ) && IPAddress.TryParse( targetNameOrIP, out targetAddress ) ) {
                try {
                    targetAddress.BanAll( player, reason, true, true );
                } catch( PlayerOpException ex ) {
                    player.Message( ex.MessageColored );
                }
            } else {
                PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetNameOrIP );
                if( target == null ) return;
                try {
                    if( target.LastIP.Equals( IPAddress.Any ) || target.LastIP.Equals( IPAddress.None ) ) {
                        target.Ban( player, reason, true, true );
                    } else {
                        target.BanAll( player, reason, true, true );
                    }
                } catch( PlayerOpException ex ) {
                    player.Message( ex.MessageColored );
                    if( ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired ) {
                        FreezeIfAllowed( player, target );
                    }
                }
            }
        }



        static readonly CommandDescriptor CdUnban = new CommandDescriptor {
            Name = "Unban",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban },
            Usage = "/Unban PlayerName [Reason]",
            Help = "Removes ban for a specified player. Does NOT remove associated IP bans. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = UnbanHandler
        };

        static void UnbanHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if( targetName == null ) {
                CdUnban.PrintUsage( player );
                return;
            }
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetName );
            if( target == null ) return;
            string reason = cmd.NextAll();
            try {
                target.Unban( player, reason, true, true );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }



        static readonly CommandDescriptor CdUnbanIP = new CommandDescriptor {
            Name = "UnbanIP",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP },
            Usage = "/UnbanIP PlayerName|IPaddress [Reason]",
            Help = "Removes ban for a specified player's name and last known IP. " +
                   "You can also type in the IP address directly. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = UnbanIPHandler
        };

        static void UnbanIPHandler( Player player, Command cmd ) {
            string targetNameOrIP = cmd.Next();
            if( targetNameOrIP == null ) {
                CdUnbanIP.PrintUsage( player );
                return;
            }
            string reason = cmd.NextAll();

            try {
                IPAddress targetAddress;
                if( Server.IsIP( targetNameOrIP ) && IPAddress.TryParse( targetNameOrIP, out targetAddress ) ) {
                    targetAddress.UnbanIP( player, reason, true, true );
                } else {
                    PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetNameOrIP );
                    if( target == null ) return;
                    if( target.LastIP.Equals( IPAddress.Any ) || target.LastIP.Equals( IPAddress.None ) ) {
                        target.Unban( player, reason, true, true );
                    } else {
                        target.UnbanIP( player, reason, true, true );
                    }
                }
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }



        static readonly CommandDescriptor CdUnbanAll = new CommandDescriptor {
            Name = "UnbanAll",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP, Permission.BanAll },
            Usage = "/UnbanAll PlayerName|IPaddress [Reason]",
            Help = "Removes ban for a specified player's name, last known IP, and all other names associated with the IP. " +
                   "You can also type in the IP address directly. " +
                   "Any text after the player name will be saved as a memo. ",
            Handler = UnbanAllHandler
        };

        static void UnbanAllHandler( Player player, Command cmd ) {
            string targetNameOrIP = cmd.Next();
            if( targetNameOrIP == null ) {
                CdUnbanAll.PrintUsage( player );
                return;
            }
            string reason = cmd.NextAll();

            try {
                IPAddress targetAddress;
                if( Server.IsIP( targetNameOrIP ) && IPAddress.TryParse( targetNameOrIP, out targetAddress ) ) {
                    targetAddress.UnbanAll( player, reason, true, true );
                } else {
                    PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetNameOrIP );
                    if( target == null ) return;
                    if( target.LastIP.Equals( IPAddress.Any ) || target.LastIP.Equals( IPAddress.None ) ) {
                        target.Unban( player, reason, true, true );
                    } else {
                        target.UnbanAll( player, reason, true, true );
                    }
                }
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }


        static readonly CommandDescriptor CdBanEx = new CommandDescriptor {
            Name = "BanEx",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Ban, Permission.BanIP },
            Usage = "/BanEx +PlayerName&S or &H/BanEx -PlayerName",
            Help = "Adds or removes an IP-ban exemption for an account. " +
                   "Exempt accounts can log in from any IP, including banned ones.",
            Handler = BanExHandler
        };

        static void BanExHandler( Player player, Command cmd ) {
            string playerName = cmd.Next();
            if( playerName == null || playerName.Length < 2 || (playerName[0] != '-' && playerName[0] != '+') ) {
                CdBanEx.PrintUsage( player );
                return;
            }
            bool addExemption = (playerName[0] == '+');
            string targetName = playerName.Substring( 1 );
            PlayerInfo target = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetName );
            if( target == null ) return;

            switch( target.BanStatus ) {
                case BanStatus.Banned:
                    if( addExemption ) {
                        player.Message( "Player {0}&S is currently banned. Unban before adding an exemption.",
                                        target.ClassyName );
                    } else {
                        player.Message( "Player {0}&S is already banned. There is no exemption to remove.",
                                        target.ClassyName );
                    }
                    break;
                case BanStatus.IPBanExempt:
                    if( addExemption ) {
                        player.Message( "IP-Ban exemption already exists for player {0}", target.ClassyName );
                    } else {
                        player.Message( "IP-Ban exemption removed for player {0}",
                                        target.ClassyName );
                        target.BanStatus = BanStatus.NotBanned;
                    }
                    break;
                case BanStatus.NotBanned:
                    if( addExemption ) {
                        player.Message( "IP-Ban exemption added for player {0}",
                                        target.ClassyName );
                        target.BanStatus = BanStatus.IPBanExempt;
                    } else {
                        player.Message( "No IP-Ban exemption exists for player {0}",
                                        target.ClassyName );
                    }
                    break;
            }
        }

        #endregion


        #region Kick

        static readonly CommandDescriptor CdKick = new CommandDescriptor {
            Name = "Kick",
            Aliases = new[] { "k" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Kick },
            Usage = "/Kick PlayerName [Reason]",
            Help = "Kicks the specified player from the server. " +
                   "Optional kick reason/message is shown to the kicked player and logged.",
            Handler = KickHandler
        };

        static void KickHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                player.Message( "Usage: &H/Kick PlayerName [Message]" );
                return;
            }

            // find the target
            Player target = Server.FindPlayerOrPrintMatches( player, name, false, true );
            if( target == null ) return;

            string reason = cmd.NextAll();
            DateTime previousKickDate = target.Info.LastKickDate;
            string previousKickedBy = target.Info.LastKickByClassy;
            string previousKickReason = target.Info.LastKickReason;

            // do the kick
            try {
                Player targetPlayer = target;
                target.Kick( player, reason, LeaveReason.Kick, true, true, true );
                WarnIfOtherPlayersOnIP( player, target.Info, targetPlayer );

            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
                if( ex.ErrorCode == PlayerOpExceptionCode.ReasonRequired ) {
                    FreezeIfAllowed( player, target.Info );
                }
                return;
            }

            // warn player if target has been kicked before
            if( target.Info.TimesKicked > 1 ) {
                player.Message( "Warning: {0}&S has been kicked {1} times before.",
                                target.ClassyName, target.Info.TimesKicked - 1 );
                if( previousKickDate != DateTime.MinValue ) {
                    player.Message( "Most recent kick was {0} ago, by {1}",
                                    DateTime.UtcNow.Subtract( previousKickDate ).ToMiniString(),
                                    previousKickedBy );
                }
                if( !String.IsNullOrEmpty( previousKickReason ) ) {
                    player.Message( "Most recent kick reason was: {0}",
                                    previousKickReason );
                }
            }
        }

        #endregion


        #region Changing Rank (Promotion / Demotion)

        static readonly CommandDescriptor CdRank = new CommandDescriptor {
            Name = "Rank",
            Aliases = new[] { "user", "promote", "demote" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Promote, Permission.Demote },
            AnyPermission = true,
            IsConsoleSafe = true,
            Usage = "/Rank PlayerName RankName [Reason]",
            Help = "Changes the rank of a player to a specified rank. " +
                   "Any text specified after the RankName will be saved as a memo.",
            Handler = RankHandler
        };

        public static void RankHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            string newRankName = cmd.Next();

            // Check arguments
            if( name == null || newRankName == null ) {
                CdRank.PrintUsage( player );
                player.Message( "See &H/Ranks&S for list of ranks." );
                return;
            }

            // Parse rank name
            Rank newRank = RankManager.FindRank( newRankName );
            if( newRank == null ) {
                player.MessageNoRank( newRankName );
                return;
            }

            // Parse player name
            if( name == "-" ) {
                if( player.LastUsedPlayerName != null ) {
                    name = player.LastUsedPlayerName;
                } else {
                    player.Message( "Cannot repeat player name: you haven't used any names yet." );
                    return;
                }
            }
            PlayerInfo targetInfo = PlayerDB.FindPlayerInfoExact( name );

            if( targetInfo == null ) {
                if( !player.Can( Permission.EditPlayerDB ) ) {
                    player.MessageNoPlayer( name );
                    return;
                }
                if( !Player.IsValidName( name ) ) {
                    player.MessageInvalidPlayerName( name );
                    CdRank.PrintUsage( player );
                    return;
                }
                if( cmd.IsConfirmed ) {
                    if( newRank > RankManager.DefaultRank ) {
                        targetInfo = PlayerDB.AddFakeEntry( name, RankChangeType.Promoted );
                    } else {
                        targetInfo = PlayerDB.AddFakeEntry( name, RankChangeType.Demoted );
                    }
                } else {
                    player.Confirm( cmd,
                                    "Warning: Player \"{0}\" is not in the database (possible typo). Type the full name or",
                                    name );
                    return;
                }
            }

            try {
                player.LastUsedPlayerName = targetInfo.Name;
                targetInfo.ChangeRank( player, newRank, cmd.NextAll(), true, true, false );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }


        #endregion


        #region Hide

        static readonly CommandDescriptor CdHide = new CommandDescriptor {
            Name = "Hide",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Hide },
            Usage = "/Hide [silent]",
            Help = "Enables invisible mode. It looks to other players like you left the server, " +
                   "but you can still do anything - chat, build, delete, type commands - as usual. " +
                   "Great way to spy on griefers and scare newbies. " +
                   "Call &H/Unhide&S to reveal yourself.",
            Handler = HideHandler
        };

        static void HideHandler( Player player, Command cmd ) {
            if( player.Info.IsHidden ) {
                player.Message( "You are already hidden." );
                return;
            }

            string silentString = cmd.Next();
            bool silent = false;
            if( silentString != null ) {
                silent = silentString.Equals( "silent", StringComparison.OrdinalIgnoreCase );
            }

            player.Info.IsHidden = true;
            player.Message( "&8You are now hidden." );

            // to make it look like player just logged out in /Info
            player.Info.LastSeen = DateTime.UtcNow;

            if( !silent ) {
                if( ConfigKey.ShowConnectionMessages.Enabled() ) {
                    Server.Players.CantSee( player ).Message( "&SPlayer {0}&S left the server.", player.ClassyName );
                }
                if( ConfigKey.IRCBotAnnounceServerJoins.Enabled() ) {
                    IRC.PlayerDisconnectedHandler( null, new PlayerDisconnectedEventArgs( player, LeaveReason.ClientQuit, true ) );
                }
            }

            // for aware players: notify
            Server.Players.CanSee( player ).Message( "&SPlayer {0}&S is now hidden.", player.ClassyName );

            Player.RaisePlayerHideChangedEvent( player );
        }



        static readonly CommandDescriptor CdUnhide = new CommandDescriptor {
            Name = "Unhide",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Hide },
            Usage = "/Unhide [silent]",
            Help = "Disables the &H/Hide&S invisible mode. " +
                   "It looks to other players like you just joined the server.",
            Handler = UnhideHandler
        };

        static void UnhideHandler( Player player, Command cmd ) {
            if( player.World == null ) PlayerOpException.ThrowNoWorld( player );

            if( !player.Info.IsHidden ) {
                player.Message( "You are not currently hidden." );
                return;
            }

            bool silent = cmd.HasNext;

            // for aware players: notify
            Server.Players.CanSee( player ).Message( "&SPlayer {0}&S is no longer hidden.",
                                                     player.ClassyName );
            player.Message( "&8You are no longer hidden." );
            player.Info.IsHidden = false;
            if( !silent ) {
                if( ConfigKey.ShowConnectionMessages.Enabled() ) {
                    // ReSharper disable AssignNullToNotNullAttribute
                    string msg = Server.MakePlayerConnectedMessage( player, false, player.World );
                    // ReSharper restore AssignNullToNotNullAttribute
                    Server.Players.CantSee( player ).Message( msg );
                }
                if( ConfigKey.IRCBotAnnounceServerJoins.Enabled() ) {
                    IRC.PlayerReadyHandler( null, new PlayerConnectedEventArgs( player, player.World ) );
                }
            }

            Player.RaisePlayerHideChangedEvent( player );
        }

        #endregion


        #region Set Spawn

        static readonly CommandDescriptor CdSetSpawn = new CommandDescriptor {
            Name = "SetSpawn",
            Category = CommandCategory.Moderation | CommandCategory.World,
            Permissions = new[] { Permission.SetSpawn },
            Help = "Assigns your current location to be the spawn point of the map/world. " +
                   "If an optional PlayerName param is given, the spawn point of only that player is changed instead.",
            Usage = "/SetSpawn [PlayerName]",
            Handler = SetSpawnHandler
        };

        static void SetSpawnHandler( Player player, Command cmd ) {
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );


            string playerName = cmd.Next();
            if( playerName == null ) {
                Map map = player.WorldMap;
                map.Spawn = player.Position;
                player.TeleportTo( map.Spawn );
                player.Send( PacketWriter.MakeAddEntity( 255, player.ListName, player.Position ) );
                player.Message( "New spawn point saved." );
                Logger.Log( LogType.UserActivity,
                            "{0} changed the spawned point.",
                            player.Name );

            } else if( player.Can( Permission.Bring ) ) {
                Player[] infos = playerWorld.FindPlayers( player, playerName );
                if( infos.Length == 1 ) {
                    Player target = infos[0];
                    player.LastUsedPlayerName = target.Name;
                    if( player.Can( Permission.Bring, target.Info.Rank ) ) {
                        target.Send( PacketWriter.MakeAddEntity( 255, target.ListName, player.Position ) );
                    } else {
                        player.Message( "You may only set spawn of players ranked {0}&S or lower.",
                                        player.Info.Rank.GetLimit( Permission.Bring ).ClassyName );
                        player.Message( "{0}&S is ranked {1}", target.ClassyName, target.Info.Rank.ClassyName );
                    }

                } else if( infos.Length > 0 ) {
                    player.MessageManyMatches( "player", infos );

                } else {
                    infos = Server.FindPlayers( player, playerName, true );
                    if( infos.Length > 0 ) {
                        player.Message( "You may only set spawn of players on the same world as you." );
                    } else {
                        player.MessageNoPlayer( playerName );
                    }
                }
            } else {
                player.MessageNoAccess( CdSetSpawn );
            }
        }

        #endregion


        #region Freeze

        static readonly CommandDescriptor CdFreeze = new CommandDescriptor {
            Name = "Freeze",
            Aliases = new[] { "f" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Freeze },
            Usage = "/Freeze PlayerName",
            Help = "Freezes the specified player in place. " +
                   "This is usually effective, but not hacking-proof. " +
                   "To release the player, use &H/unfreeze PlayerName",
            Handler = FreezeHandler
        };

        static void FreezeHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdFreeze.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, name, false, true );
            if( target == null ) return;

            try {
                target.Info.Freeze( player, true, true );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }


        static readonly CommandDescriptor CdUnfreeze = new CommandDescriptor {
            Name = "Unfreeze",
            Aliases = new[] { "uf" },
            Category = CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Freeze },
            Usage = "/Unfreeze PlayerName",
            Help = "Releases the player from a frozen state. See &H/Help Freeze&S for more information.",
            Handler = UnfreezeHandler
        };

        static void UnfreezeHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdFreeze.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, name, false, true );
            if( target == null ) return;

            try {
                target.Info.Unfreeze( player, true, true );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }

        #endregion


        #region TP

        static readonly CommandDescriptor CdTP = new CommandDescriptor {
            Name = "TP",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Teleport },
            Usage = "/TP PlayerName&S or &H/TP X Y Z",
            Help = "Teleports you to a specified player's location. " +
                   "If coordinates are given, teleports to that location.",
            Handler = TPHandler
        };

        static void TPHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdTP.PrintUsage( player );
                return;
            }

            if( cmd.Next() != null ) {
                cmd.Rewind();
                int x, y, z;
                if( cmd.NextInt( out x ) && cmd.NextInt( out y ) && cmd.NextInt( out z ) ) {

                    if( x <= -1024 || x >= 1024 || y <= -1024 || y >= 1024 || z <= -1024 || z >= 1024 ) {
                        player.Message( "Coordinates are outside the valid range!" );

                    } else {
                        player.TeleportTo( new Position {
                            X = (short)(x * 32 + 16),
                            Y = (short)(y * 32 + 16),
                            Z = (short)(z * 32 + 16),
                            R = player.Position.R,
                            L = player.Position.L
                        } );
                    }
                } else {
                    CdTP.PrintUsage( player );
                }

            } else {
                if( name == "-" ) {
                    if( player.LastUsedPlayerName != null ) {
                        name = player.LastUsedPlayerName;
                    } else {
                        player.Message( "Cannot repeat player name: you haven't used any names yet." );
                        return;
                    }
                }
                Player[] matches = Server.FindPlayers( player, name, true );
                if( matches.Length == 1 ) {
                    Player target = matches[0];
                    World targetWorld = target.World;
                    if( targetWorld == null ) PlayerOpException.ThrowNoWorld( target );

                    if( targetWorld == player.World ) {
                        player.TeleportTo( target.Position );

                    } else {
                        switch( targetWorld.AccessSecurity.CheckDetailed( player.Info ) ) {
                            case SecurityCheckResult.Allowed:
                            case SecurityCheckResult.WhiteListed:
                                if( targetWorld.IsFull ) {
                                    player.Message( "Cannot teleport to {0}&S because world {1}&S is full.",
                                                    target.ClassyName,
                                                    targetWorld.ClassyName );
                                    return;
                                }
                                player.StopSpectating();
                                player.JoinWorld( targetWorld, WorldChangeReason.Tp, target.Position );
                                break;
                            case SecurityCheckResult.BlackListed:
                                player.Message( "Cannot teleport to {0}&S because you are blacklisted on world {1}",
                                                target.ClassyName,
                                                targetWorld.ClassyName );
                                break;
                            case SecurityCheckResult.RankTooLow:
                                player.Message( "Cannot teleport to {0}&S because world {1}&S requires {2}+&S to join.",
                                                target.ClassyName,
                                                targetWorld.ClassyName,
                                                targetWorld.AccessSecurity.MinRank.ClassyName );
                                break;
                            // TODO: case PermissionType.RankTooHigh:
                        }
                    }

                } else if( matches.Length > 1 ) {
                    player.MessageManyMatches( "player", matches );

                } else {
                    // Try to guess if player typed "/TP" instead of "/Join"
                    World[] worlds = WorldManager.FindWorlds( player, name );

                    if( worlds.Length == 1 ) {
                        player.LastUsedWorldName = worlds[0].Name;
                        player.StopSpectating();
                        player.ParseMessage( "/Join " + worlds[0].Name, false );
                    } else {
                        player.MessageNoPlayer( name );
                    }
                }
            }
        }

        #endregion


        #region Bring / WorldBring / BringAll

        static readonly CommandDescriptor CdBring = new CommandDescriptor {
            Name = "Bring",
            IsConsoleSafe = true,
            Aliases = new[] { "summon", "fetch" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Bring },
            Usage = "/Bring PlayerName [ToPlayer]",
            Help = "Teleports another player to your location. " +
                   "If the optional second parameter is given, teleports player to another player.",
            Handler = BringHandler
        };

        static void BringHandler( Player player, Command cmd ) {
            string name = cmd.Next();
            if( name == null ) {
                CdBring.PrintUsage( player );
                return;
            }

            // bringing someone to another player (instead of to self)
            string toName = cmd.Next();
            Player toPlayer = player;
            if( toName != null ) {
                toPlayer = Server.FindPlayerOrPrintMatches( player, toName, false, true );
                if( toPlayer == null ) return;
            } else if( toPlayer.World == null ) {
                player.Message( "When used from console, /Bring requires both names to be given." );
                return;
            }

            World world = toPlayer.World;
            if( world == null ) PlayerOpException.ThrowNoWorld( toPlayer );

            Player target = Server.FindPlayerOrPrintMatches( player, name, false, true );
            if( target == null ) return;

            if( !player.Can( Permission.Bring, target.Info.Rank ) ) {
                player.Message( "You may only bring players ranked {0}&S or lower.",
                                player.Info.Rank.GetLimit( Permission.Bring ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                target.ClassyName, target.Info.Rank.ClassyName );
                return;
            }

            if( target.World == world ) {
                // teleport within the same world
                target.TeleportTo( toPlayer.Position );

            } else {
                // teleport to a different world
                SecurityCheckResult check = world.AccessSecurity.CheckDetailed( target.Info );
                if( check == SecurityCheckResult.RankTooHigh || check == SecurityCheckResult.RankTooLow ) {
                    if( player.CanJoin( world ) ) {
                        if( cmd.IsConfirmed ) {
                            BringPlayerToWorld( player, target, world, true, true );
                        } else {
                            player.Confirm( cmd,
                                            "Player {0}&S is ranked too low to join {1}&S. Override world permissions?",
                                            target.ClassyName,
                                            world.ClassyName );
                        }
                    } else {
                        player.Message( "Neither you nor {0}&S are allowed to join world {1}",
                                        target.ClassyName, world.ClassyName );
                    }
                } else {
                    BringPlayerToWorld( player, target, world, false, true );
                }
            }
        }


        static readonly CommandDescriptor CdWorldBring = new CommandDescriptor {
            Name = "WBring",
            IsConsoleSafe = true,
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Bring },
            Usage = "/WBring PlayerName WorldName",
            Help = "Teleports a player to the given world's spawn.",
            Handler = WorldBringHandler
        };

        static void WorldBringHandler( Player player, Command cmd ) {
            string playerName = cmd.Next();
            string worldName = cmd.Next();
            if( playerName == null || worldName == null ) {
                CdWorldBring.PrintUsage( player );
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, playerName, false, true );
            World world = WorldManager.FindWorldOrPrintMatches( player, worldName );

            if( target == null || world == null ) return;

            if( target == player ) {
                player.Message( "&WYou cannot &H/WBring&W yourself." );
                return;
            }

            if( !player.Can( Permission.Bring, target.Info.Rank ) ) {
                player.Message( "You may only bring players ranked {0}&S or lower.",
                                player.Info.Rank.GetLimit( Permission.Bring ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                target.ClassyName, target.Info.Rank.ClassyName );
                return;
            }

            if( world == target.World ) {
                player.Message( "Player {0}&S is already in world {1}&S. They were brought to spawn.",
                                target.ClassyName, world.ClassyName );
                target.TeleportTo( target.WorldMap.Spawn );
                return;
            }

            SecurityCheckResult check = world.AccessSecurity.CheckDetailed( target.Info );
            if( check == SecurityCheckResult.RankTooHigh || check == SecurityCheckResult.RankTooLow ) {
                if( player.CanJoin( world ) ) {
                    if( cmd.IsConfirmed ) {
                        BringPlayerToWorld( player, target, world, true, false );
                    } else {
                        player.Confirm( cmd,
                                        "Player {0}&S is ranked too low to join {1}&S. Override world permissions?",
                                        target.ClassyName,
                                        world.ClassyName );
                    }
                } else {
                    player.Message( "Neither you nor {0}&S are allowed to join world {1}",
                                    target.ClassyName, world.ClassyName );
                }
            } else {
                BringPlayerToWorld( player, target, world, false, false );
            }
        }


        static readonly CommandDescriptor CdBringAll = new CommandDescriptor {
            Name = "BringAll",
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Bring, Permission.BringAll },
            Usage = "/BringAll [@Rank [@AnotherRank]] [*|World [AnotherWorld]]",
            Help = "Teleports all players from your world to you. " +
                   "If any world names are given, only teleports players from those worlds. " +
                   "If any rank names are given, only teleports players of those ranks.",
            Handler = BringAllHandler
        };

        static void BringAllHandler( Player player, Command cmd ) {
            if( player.World == null ) PlayerOpException.ThrowNoWorld( player );

            List<World> targetWorlds = new List<World>();
            List<Rank> targetRanks = new List<Rank>();
            bool allWorlds = false;
            bool allRanks = true;

            // Parse the list of worlds and ranks
            string arg;
            while( (arg = cmd.Next()) != null ) {
                if( arg.StartsWith( "@" ) ) {
                    Rank rank = RankManager.FindRank( arg.Substring( 1 ) );
                    if( rank == null ) {
                        player.Message( "Unknown rank: {0}", arg.Substring( 1 ) );
                        return;
                    } else {
                        if( player.Can( Permission.Bring, rank ) ) {
                            targetRanks.Add( rank );
                        } else {
                            player.Message( "&WYou are not allowed to bring players of rank {0}",
                                            rank.ClassyName );
                        }
                        allRanks = false;
                    }
                } else if( arg == "*" ) {
                    allWorlds = true;
                } else {
                    World world = WorldManager.FindWorldOrPrintMatches( player, arg );
                    if( world == null ) return;
                    targetWorlds.Add( world );
                }
            }

            // If no worlds were specified, use player's current world
            if( !allWorlds && targetWorlds.Count == 0 ) {
                targetWorlds.Add( player.World );
            }

            // Apply all the rank and world options
            HashSet<Player> targetPlayers;
            if( allRanks && allWorlds ) {
                targetPlayers = new HashSet<Player>( Server.Players );
            } else if( allWorlds ) {
                targetPlayers = new HashSet<Player>();
                foreach( Rank rank in targetRanks ) {
                    foreach( Player rankPlayer in Server.Players.Ranked( rank ) ) {
                        targetPlayers.Add( rankPlayer );
                    }
                }
            } else if( allRanks ) {
                targetPlayers = new HashSet<Player>();
                foreach( World world in targetWorlds ) {
                    foreach( Player worldPlayer in world.Players ) {
                        targetPlayers.Add( worldPlayer );
                    }
                }
            } else {
                targetPlayers = new HashSet<Player>();
                foreach( Rank rank in targetRanks ) {
                    foreach( World world in targetWorlds ) {
                        foreach( Player rankWorldPlayer in world.Players.Ranked( rank ) ) {
                            targetPlayers.Add( rankWorldPlayer );
                        }
                    }
                }
            }

            Rank bringLimit = player.Info.Rank.GetLimit( Permission.Bring );

            // Remove the player him/herself
            targetPlayers.Remove( player );

            int count = 0;


            // Actually bring all the players
            foreach( Player targetPlayer in targetPlayers.CanBeSeen( player )
                                                         .RankedAtMost( bringLimit ) ) {
                if( targetPlayer.World == player.World ) {
                    // teleport within the same world
                    targetPlayer.TeleportTo( player.Position );
                    targetPlayer.Position = player.Position;
                    if( targetPlayer.Info.IsFrozen ) {
                        targetPlayer.Position = player.Position;
                    }

                } else {
                    // teleport to a different world
                    BringPlayerToWorld( player, targetPlayer, player.World, false, true );
                }
                count++;
            }

            // Check if there's anyone to bring
            if( count == 0 ) {
                player.Message( "No players to bring!" );
            } else {
                player.Message( "Bringing {0} players...", count );
            }
        }



        static void BringPlayerToWorld( [NotNull] Player player, [NotNull] Player target, [NotNull] World world,
                                        bool overridePermissions, bool usePlayerPosition ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( target == null ) throw new ArgumentNullException( "target" );
            if( world == null ) throw new ArgumentNullException( "world" );
            switch( world.AccessSecurity.CheckDetailed( target.Info ) ) {
                case SecurityCheckResult.Allowed:
                case SecurityCheckResult.WhiteListed:
                    if( world.IsFull ) {
                        player.Message( "Cannot bring {0}&S because world {1}&S is full.",
                                        target.ClassyName,
                                        world.ClassyName );
                        return;
                    }
                    target.StopSpectating();
                    if( usePlayerPosition ) {
                        target.JoinWorld( world, WorldChangeReason.Bring, player.Position );
                    } else {
                        target.JoinWorld( world, WorldChangeReason.Bring );
                    }
                    break;

                case SecurityCheckResult.BlackListed:
                    player.Message( "Cannot bring {0}&S because he/she is blacklisted on world {1}",
                                    target.ClassyName,
                                    world.ClassyName );
                    break;

                case SecurityCheckResult.RankTooLow:
                    if( overridePermissions ) {
                        target.StopSpectating();
                        if( usePlayerPosition ) {
                            target.JoinWorld( world, WorldChangeReason.Bring, player.Position );
                        } else {
                            target.JoinWorld( world, WorldChangeReason.Bring );
                        }
                    } else {
                        player.Message( "Cannot bring {0}&S because world {1}&S requires {2}+&S to join.",
                                        target.ClassyName,
                                        world.ClassyName,
                                        world.AccessSecurity.MinRank.ClassyName );
                    }
                    break;
                // TODO: case PermissionType.RankTooHigh:
            }
        }

        #endregion


        #region Patrol & SpecPatrol

        static readonly CommandDescriptor CdPatrol = new CommandDescriptor {
            Name = "Patrol",
            Aliases = new[] { "pat" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Patrol },
            Help = "Teleports you to the next player in need of checking.",
            Handler = PatrolHandler
        };

        static void PatrolHandler( Player player, Command cmd ) {
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );

            Player target = playerWorld.GetNextPatrolTarget( player );
            if( target == null ) {
                player.Message( "Patrol: No one to patrol in this world." );
                return;
            }

            player.TeleportTo( target.Position );
            player.Message( "Patrol: Teleporting to {0}", target.ClassyName );
        }


        static readonly CommandDescriptor CdSpecPatrol = new CommandDescriptor {
            Name = "SpecPatrol",
            Aliases = new[] { "spat" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Patrol, Permission.Spectate },
            Help = "Teleports you to the next player in need of checking.",
            Handler = SpecPatrolHandler
        };

        static void SpecPatrolHandler( Player player, Command cmd ) {
            World playerWorld = player.World;
            if( playerWorld == null ) PlayerOpException.ThrowNoWorld( player );

            Player target = playerWorld.GetNextPatrolTarget( player,
                                                             p => player.Can( Permission.Spectate, p.Info.Rank ),
                                                             true );
            if( target == null ) {
                player.Message( "Patrol: No one to spec-patrol in this world." );
                return;
            }

            target.LastPatrolTime = DateTime.UtcNow;
            player.Spectate( target );
        }

        #endregion


        #region Mute / Unmute

        static readonly TimeSpan MaxMuteDuration = TimeSpan.FromDays( 700 ); // 100w0d

        static readonly CommandDescriptor CdMute = new CommandDescriptor {
            Name = "Mute",
            Category = CommandCategory.Moderation | CommandCategory.Chat,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Mute },
            Help = "Mutes a player for a specified length of time.",
            Usage = "/Mute PlayerName Duration",
            Handler = MuteHandler
        };

        static void MuteHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            string timeString = cmd.Next();
            TimeSpan duration;

            // validate command parameters
            if( String.IsNullOrEmpty( targetName ) || String.IsNullOrEmpty( timeString ) ||
                !timeString.TryParseMiniTimespan( out duration ) || duration <= TimeSpan.Zero ) {
                CdMute.PrintUsage( player );
                return;
            }

            // check if given time exceeds maximum (700 days)
            if( duration > MaxMuteDuration ) {
                player.Message( "Maximum mute duration is {0}.", MaxMuteDuration.ToMiniString() );
                duration = MaxMuteDuration;
            }

            // find the target
            Player target = Server.FindPlayerOrPrintMatches( player, targetName, false, true );
            if( target == null ) return;

            // actually mute
            try {
                target.Info.Mute( player, duration, true, true );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }


        static readonly CommandDescriptor CdUnmute = new CommandDescriptor {
            Name = "Unmute",
            Category = CommandCategory.Moderation | CommandCategory.Chat,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Mute },
            Help = "Unmutes a player.",
            Usage = "/Unmute PlayerName",
            Handler = UnmuteHandler
        };

        static void UnmuteHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if( String.IsNullOrEmpty( targetName ) ) {
                CdUnmute.PrintUsage( player );
                return;
            }

            // find target
            Player target = Server.FindPlayerOrPrintMatches( player, targetName, false, true );
            if( target == null ) return;

            try {
                target.Info.Unmute( player, true, true );
            } catch( PlayerOpException ex ) {
                player.Message( ex.MessageColored );
            }
        }

        #endregion


        #region Spectate / Unspectate

        static readonly CommandDescriptor CdSpectate = new CommandDescriptor {
            Name = "Spectate",
            Aliases = new[] { "follow", "spec" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Spectate },
            Usage = "/Spectate PlayerName",
            Handler = SpectateHandler
        };

        static void SpectateHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if( targetName == null ) {
                PlayerInfo lastSpec = player.LastSpectatedPlayer;
                if( lastSpec != null ) {
                    Player spec = player.SpectatedPlayer;
                    if( spec != null ) {
                        player.Message( "Now spectating {0}", spec.ClassyName );
                    } else {
                        player.Message( "Last spectated {0}", lastSpec.ClassyName );
                    }
                } else {
                    CdSpectate.PrintUsage( player );
                }
                return;
            }

            Player target = Server.FindPlayerOrPrintMatches( player, targetName, false, true );
            if( target == null ) return;

            if( target == player ) {
                player.Message( "You cannot spectate yourself." );
                return;
            }

            if( !player.Can( Permission.Spectate, target.Info.Rank ) ) {
                player.Message( "You may only spectate players ranked {0}&S or lower.",
                player.Info.Rank.GetLimit( Permission.Spectate ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                target.ClassyName, target.Info.Rank.ClassyName );
                return;
            }

            if( !player.Spectate( target ) ) {
                player.Message( "Already spectating {0}", target.ClassyName );
            }
        }


        static readonly CommandDescriptor CdUnspectate = new CommandDescriptor {
            Name = "Unspectate",
            Aliases = new[] { "unfollow", "unspec" },
            Category = CommandCategory.Moderation,
            Permissions = new[] { Permission.Spectate },
            NotRepeatable = true,
            Handler = UnspectateHandler
        };

        static void UnspectateHandler( Player player, Command cmd ) {
            if( !player.StopSpectating() ) {
                player.Message( "You are not currently spectating anyone." );
            }
        }

        #endregion


        // freeze target if player is allowed to do so
        static void FreezeIfAllowed( Player player, PlayerInfo targetInfo ) {
            if( targetInfo.IsOnline && !targetInfo.IsFrozen && player.Can( Permission.Freeze, targetInfo.Rank ) ) {
                try {
                    targetInfo.Freeze( player, true, true );
                    player.Message( "Player {0}&S has been frozen while you retry.", targetInfo.ClassyName );
                } catch( PlayerOpException ) { }
            }
        }


        // warn player if others are still online from target's IP
        static void WarnIfOtherPlayersOnIP( Player player, PlayerInfo targetInfo, Player except ) {
            Player[] otherPlayers = Server.Players.FromIP( targetInfo.LastIP )
                                                  .Except( except )
                                                  .ToArray();
            if( otherPlayers.Length > 0 ) {
                player.Message( "&WWarning: Other player(s) share IP with {0}&W: {1}",
                                targetInfo.ClassyName,
                                otherPlayers.JoinToClassyString() );
            }
        }
    }
}