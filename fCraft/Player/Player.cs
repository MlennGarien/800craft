// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Threading;
using fCraft.Drawing;
using fCraft.Events;
using JetBrains.Annotations;
using fCraft.Collections;

namespace fCraft {
    /// <summary> Callback for a player-made selection of one or more blocks on a map.
    /// A command may request a number of marks/blocks to select, and a specify callback
    /// to be executed when the desired number of marks/blocks is reached. </summary>
    /// <param name="player"> Player who made the selection. </param>
    /// <param name="marks"> An array of 3D marks/blocks, in terms of block coordinates. </param>
    /// <param name="tag"> An optional argument to pass to the callback,
    /// the value of player.selectionArgs </param>
    public delegate void SelectionCallback( Player player, Vector3I[] marks, object tag );

    public delegate void ConfirmationCallback( Player player, object tag, bool fromConsole );


    /// <summary> Object representing volatile state ("session") of a connected player.
    /// For persistent state of a known player account, see PlayerInfo. </summary>
    public sealed partial class Player : IClassy {

        /// <summary> The godly pseudo-player for commands called from the server console.
        /// Console has all the permissions granted.
        /// Note that Player.Console.World is always null,
        /// and that prevents console from calling certain commands (like /TP). </summary>
        public static Player Console, AutoRank;


        #region Properties

        public readonly bool IsSuper;


        /// <summary> Whether the player has completed the login sequence. </summary>
        public SessionState State { get; private set; }

        /// <summary> Whether the player has completed the login sequence. </summary>
        public bool HasRegistered { get; internal set; }

        /// <summary> Whether the player registered and then finished loading the world. </summary>
        public bool HasFullyConnected { get; private set; }

        /// <summary> Whether the client is currently connected. </summary>
        public bool IsOnline {
            get {
                return State == SessionState.Online;
            }
        }

        /// <summary> Whether the player name was verified at login. </summary>
        public bool IsVerified { get; private set; }

        /// <summary> Persistent information record associated with this player. </summary>
        public PlayerInfo Info { get; private set; }

        /// <summary> Whether the player is in paint mode (deleting blocks replaces them). Used by /Paint. </summary>
        public bool IsPainting { get; set; }

        /// <summary> Whether player has blocked all incoming chat.
        /// Deaf players can't hear anything. </summary>
        public bool IsDeaf { get; set; }


        /// <summary> The world that the player is currently on. May be null.
        /// Use .JoinWorld() to make players teleport to another world. </summary>
        [CanBeNull]
        public World World { get; private set; }

        /// <summary> Map from the world that the player is on.
        /// Throws PlayerOpException if player does not have a world.
        /// Loads the map if it's not loaded. Guaranteed to not return null. </summary>
        [NotNull]
        public Map WorldMap {
            get {
                World world = World;
                if( world == null ) PlayerOpException.ThrowNoWorld( this );
                return world.LoadMap();
            }
        }

        /// <summary> Player's position in the current world. </summary>
        public Position Position;


        /// <summary> Time when the session connected. </summary>
        public DateTime LoginTime { get; private set; }

        /// <summary> Last time when the player was active (moving/messaging). UTC. </summary>
        public DateTime LastActiveTime { get; private set; }

        /// <summary> Last time when this player was patrolled by someone. </summary>
        public DateTime LastPatrolTime { get; set; }


        /// <summary> Last command called by the player. </summary>
        [CanBeNull]
        public Command LastCommand { get; private set; }


        /// <summary> Plain version of the name (no formatting). </summary>
        [NotNull]
        public string Name {
            get { return Info.Name; }
        }

        /// <summary> Name formatted for display in the player list. </summary>
        [NotNull]
        public string ListName {
            get {
                string displayedName = Name;
                if (iName != null) displayedName = Color.ReplacePercentCodes(iName); //impersonate
                if( ConfigKey.RankPrefixesInList.Enabled() ) {
                    displayedName = Info.Rank.Prefix + displayedName;
                }
                if( ConfigKey.RankColorsInChat.Enabled() && Info.Rank.Color != Color.White && iName == null ) {
                    displayedName = Info.Rank.Color + displayedName;
                }
                return displayedName;
            }
        }

        /// <summary> Name formatted for display in chat. </summary>
        [NotNull]
        public string ClassyName {
            get { return Info.ClassyName; }
        }

        /// <summary> Whether the client supports advanced WoM client functionality. </summary>
        public bool IsUsingWoM { get; private set; }


        /// <summary> Metadata associated with the session/player. </summary>
        [NotNull]
        public MetadataCollection<object> Metadata { get; private set; }

        #endregion
        public bool IsAway;

        public bool IsFlying = false;
        public ConcurrentDictionary<String, Vector3I> FlyCache;
        public readonly object FlyLock = new object();

        public ConcurrentDictionary<String, Vector3I> TowerCache = new ConcurrentDictionary<String, Vector3I>();
        public bool towerMode = false;
        public Vector3I towerOrigin;

        public ConcurrentDictionary<String, Vector3I> GunCache = new ConcurrentDictionary<String, Vector3I>();

        public List<Vector3I> bluePortal = new List<Vector3I>();
        public List<Vector3I> orangePortal = new List<Vector3I>();
        public List<Block> blueOld = new List<Block>();
        public List<Block> orangeOld = new List<Block>();
        public byte blueOut;
        public byte orangeOut;
        public bool GunMode = false;

        public bool fireworkMode = false;

        public DateTime LastTimeKilled;

        //general purpose state storage for plugins
        private readonly ConcurrentDictionary<string, object> _publicAuxStateObjects = new ConcurrentDictionary<string, object>();
        public IDictionary<string, object> PublicAuxStateObjects { get { return _publicAuxStateObjects; } }

        public bool StandingInPortal = false;
        public bool CanUsePortal = true;
        public String PortalWorld;
        public String PortalName;
        public bool BuildingPortal = true;
        public DateTime LastUsedPortal;
        public DateTime LastWarnedPortal;
        public DateTime DrownTime;
        public readonly object PortalLock = new object();
        public bool PortalsEnabled = true;
        public bool CanBeKilled()
        {
            return ((DateTime.UtcNow - LastTimeKilled).TotalSeconds < 15);
        }
        public string iName = null;


        // This constructor is used to create pseudoplayers (such as Console and /dummy).
        // Such players have unlimited permissions, but no world.
        // This should be replaced by a more generic solution, like an IEntity interface.
        internal Player( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            Info = new PlayerInfo( name, RankManager.HighestRank, true, RankChangeType.AutoPromoted );
            spamBlockLog = new Queue<DateTime>( Info.Rank.AntiGriefBlocks );
            IP = IPAddress.Loopback;
            ResetAllBinds();
            State = SessionState.Offline;
            IsSuper = true;
        }
        public void BassKick([NotNull] Player player, [NotNull] string reason, LeaveReason context,
                        bool announce, bool raiseEvents, bool recordToPlayerDB)
        {
            if (player == null) throw new ArgumentNullException("player");
            if (reason == null) throw new ArgumentNullException("reason");
            if (!Enum.IsDefined(typeof(LeaveReason), context))
            {
                throw new ArgumentOutOfRangeException("context");
            }


            // Check if player can ban/unban in general
            if (!player.Can(Permission.Kick))
            {
                PlayerOpException.ThrowPermissionMissing(player, Info, "kick", Permission.Kick);
            }

            // Check if player is trying to ban/unban self
            if (player == this)
            {
                PlayerOpException.ThrowCannotTargetSelf(player, Info, "kick");
            }

            // Check if player has sufficiently high permission limit
            if (!player.Can(Permission.Kick, Info.Rank))
            {
                PlayerOpException.ThrowPermissionLimit(player, Info, "kick", Permission.Kick);
            }

            // check if kick reason is missing but required
            PlayerOpException.CheckKickReason(reason, player, Info);

            // raise Player.BeingKicked event
            if (raiseEvents)
            {
                var e = new PlayerBeingKickedEventArgs(this, player, reason, announce, recordToPlayerDB, context);
                RaisePlayerBeingKickedEvent(e);
                if (e.Cancel) PlayerOpException.ThrowCancelled(player, Info);
                recordToPlayerDB = e.RecordToPlayerDB;
            }

            // actually kick
            string kickReason;
            if (reason.Length > 0)
            {
                kickReason = String.Format("Got blasted out of the server with the BASSCANNON executed by {0}: {1}", player.Name, reason);
            }
            else
            {
                kickReason = String.Format("Got blasted out of the server with the BASSCANNON executed by {0}", player.Name);
            }
            Kick(kickReason, context);

            // log and record kick to PlayerDB
            Logger.Log(LogType.UserActivity, "{0} was kicked by {1}. Reason: {2} (Basscannon)",
                        Name, player.Name, reason);
            if (recordToPlayerDB)
            {
                Info.ProcessKick(player, reason);
            }

            // announce kick
            if (announce)
            {
                if (reason.Length > 0 && ConfigKey.AnnounceKickAndBanReasons.Enabled())
                {
                    Server.Message("{0}&W Got blasted out of the server with the BASSCANNON executed by {1}&W: {2}",
                                    ClassyName, player.ClassyName, reason);
                }
                else
                {
                    Server.Message("{0}&W Got blasted out of the server with the BASSCANNON executed by {1}",
                                    ClassyName, player.ClassyName);
                }
            }

            // raise Player.Kicked event
            if (raiseEvents)
            {
                var e = new PlayerKickedEventArgs(this, player, reason, announce, recordToPlayerDB, context);
                RaisePlayerKickedEvent(e);
            }
        }

        #region Chat and Messaging

        static readonly TimeSpan ConfirmationTimeout = TimeSpan.FromSeconds( 60 );

        int muteWarnings;
        [CanBeNull]
        string partialMessage;

        // Parses message incoming from the player
        public void ParseMessage( [NotNull] string rawMessage, bool fromConsole ) {
            if( rawMessage == null ) throw new ArgumentNullException( "rawMessage" );

            if( rawMessage.Equals( "/nvm", StringComparison.OrdinalIgnoreCase ) ) {
                if( partialMessage != null ) {
                    MessageNow( "Partial message cancelled." );
                    partialMessage = null;
                } else {
                    MessageNow( "No partial message to cancel." );
                }
                return;
            }

            if( partialMessage != null ) {
                rawMessage = partialMessage + rawMessage;
                partialMessage = null;
            }
            
            // replace %-codes with &-codes
            if( Can( Permission.UseColorCodes ) ) {
                rawMessage = Color.ReplacePercentCodes( rawMessage );
            }

            switch( Chat.GetRawMessageType( rawMessage ) ) {
                case RawMessageType.Chat: {
                        if( !Can( Permission.Chat ) ) return;

                        if( Info.IsMuted ) {
                            MessageMuted();
                            return;
                        }

                        if( DetectChatSpam() ) return;

                        // Escaped slash removed AFTER logging, to avoid confusion with real commands
                        if( rawMessage.StartsWith( "//" ) ) {
                            rawMessage = rawMessage.Substring( 1 );
                        }

                        if( rawMessage.EndsWith( "//" ) ) {
                            rawMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                        }

                        Chat.SendGlobal( this, rawMessage );
                    } break;


                case RawMessageType.Command: {
                        if( rawMessage.EndsWith( "//" ) ) {
                            rawMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                        }
                        Command cmd = new Command( rawMessage );
                        CommandDescriptor commandDescriptor = CommandManager.GetDescriptor( cmd.Name, true );

                        if( commandDescriptor == null ) {
                            MessageNow( "Unknown command \"{0}\". See &H/Commands", cmd.Name );
                        } else if( Info.IsFrozen && !commandDescriptor.UsableByFrozenPlayers ) {
                            MessageNow( "&WYou cannot use this command while frozen." );
                        } else {
                            if( !commandDescriptor.DisableLogging ) {
                                Logger.Log( LogType.UserCommand,
                                            "{0}: {1}", Name, rawMessage );
                            }
                            if( commandDescriptor.RepeatableSelection ) {
                                selectionRepeatCommand = cmd;
                            }
                            SendToSpectators( cmd.RawMessage );
                            CommandManager.ParseCommand( this, cmd, fromConsole );
                            if( !commandDescriptor.NotRepeatable ) {
                                LastCommand = cmd;
                            }
                        }
                    } break;


                case RawMessageType.RepeatCommand: {
                        if( LastCommand == null ) {
                            Message( "No command to repeat." );
                        } else {
                            if( Info.IsFrozen && !LastCommand.Descriptor.UsableByFrozenPlayers ) {
                                MessageNow( "&WYou cannot use this command while frozen." );
                                return;
                            }
                            LastCommand.Rewind();
                            Logger.Log( LogType.UserCommand,
                                        "{0} repeated: {1}",
                                        Name, LastCommand.RawMessage );
                            Message( "Repeat: {0}", LastCommand.RawMessage );
                            SendToSpectators( LastCommand.RawMessage );
                            CommandManager.ParseCommand( this, LastCommand, fromConsole );
                        }
                    } break;


                case RawMessageType.PrivateChat: {
                        if( !Can( Permission.Chat ) ) return;

                        if( Info.IsMuted ) {
                            MessageMuted();
                            return;
                        }

                        if( DetectChatSpam() ) return;

                        if( rawMessage.EndsWith( "//" ) ) {
                            rawMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                        }

                        string otherPlayerName, messageText;
                        if( rawMessage[1] == ' ' ) {
                            otherPlayerName = rawMessage.Substring( 2, rawMessage.IndexOf( ' ', 2 ) - 2 );
                            messageText = rawMessage.Substring( rawMessage.IndexOf( ' ', 2 ) + 1 );
                        } else {
                            otherPlayerName = rawMessage.Substring( 1, rawMessage.IndexOf( ' ' ) - 1 );
                            messageText = rawMessage.Substring( rawMessage.IndexOf( ' ' ) + 1 );
                        }

                        if( otherPlayerName == "-" ) {
                            if( LastUsedPlayerName != null ) {
                                otherPlayerName = LastUsedPlayerName;
                            } else {
                                Message( "Cannot repeat player name: you haven't used any names yet." );
                                return;
                            }
                        }

                        // first, find ALL players (visible and hidden)
                        Player[] allPlayers = Server.FindPlayers( otherPlayerName, true );

                        // if there is more than 1 target player, exclude hidden players
                        if( allPlayers.Length > 1 ) {
                            allPlayers = Server.FindPlayers( this, otherPlayerName, true );
                        }

                        if( allPlayers.Length == 1 ) {
                            Player target = allPlayers[0];
                            if( target == this ) {
                                MessageNow( "Trying to talk to yourself?" );
                                return;
                            }
                            if( !target.IsIgnoring( Info ) && !target.IsDeaf ) {
                                Chat.SendPM( this, target, messageText );
                                SendToSpectators( "to {0}&F: {1}", target.ClassyName, messageText );
                            }

                            if( !CanSee( target ) ) {
                                // message was sent to a hidden player
                                MessageNoPlayer( otherPlayerName );

                            } else {
                                // message was sent normally
                                LastUsedPlayerName = target.Name;
                                if( target.IsIgnoring( Info ) ) {
                                    if( CanSee( target ) ) {
                                        MessageNow( "&WCannot PM {0}&W: you are ignored.", target.ClassyName );
                                    }
                                } else if( target.IsDeaf ) {
                                    MessageNow( "&SCannot PM {0}&S: they are currently deaf.", target.ClassyName );
                                } else {
                                    MessageNow( "&Pto {0}: {1}",
                                                target.Name, messageText );
                                }
                            }

                        } else if( allPlayers.Length == 0 ) {
                            MessageNoPlayer( otherPlayerName );

                        } else {
                            MessageManyMatches( "player", allPlayers );
                        }
                    } break;


                case RawMessageType.RankChat: {
                        if( !Can( Permission.Chat ) ) return;

                        if( Info.IsMuted ) {
                            MessageMuted();
                            return;
                        }

                        if( DetectChatSpam() ) return;

                        if( rawMessage.EndsWith( "//" ) ) {
                            rawMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                        }

                        Rank rank;
                        if( rawMessage[2] == ' ' ) {
                            rank = Info.Rank;
                        } else {
                            string rankName = rawMessage.Substring( 2, rawMessage.IndexOf( ' ' ) - 2 );
                            rank = RankManager.FindRank( rankName );
                            if( rank == null ) {
                                MessageNoRank( rankName );
                                break;
                            }
                        }

                        string messageText = rawMessage.Substring( rawMessage.IndexOf( ' ' ) + 1 );

                        Player[] spectators = Server.Players.NotRanked( Info.Rank )
                                                            .Where( p => p.spectatedPlayer == this )
                                                            .ToArray();
                        if( spectators.Length > 0 ) {
                            spectators.Message( "[Spectate]: &Fto rank {0}&F: {1}", rank.ClassyName, messageText );
                        }

                        Chat.SendRank( this, rank, messageText );
                    } break;


                case RawMessageType.Confirmation: {
                        if( Info.IsFrozen ) {
                            MessageNow( "&WYou cannot use any commands while frozen." );
                            return;
                        }
                        if( ConfirmCallback != null ) {
                            if( DateTime.UtcNow.Subtract( ConfirmRequestTime ) < ConfirmationTimeout ) {
                                SendToSpectators( "/ok" );
                                ConfirmCallback( this, ConfirmArgument, fromConsole );
                                ConfirmCallback = null;
                                ConfirmArgument = null;
                            } else {
                                MessageNow( "Confirmation timed out. Enter the command again." );
                            }
                        } else {
                            MessageNow( "There is no command to confirm." );
                        }
                    } break;


                case RawMessageType.PartialMessage:
                    partialMessage = rawMessage.Substring( 0, rawMessage.Length - 1 );
                    MessageNow( "Partial: &F{0}", partialMessage );
                    break;

                case RawMessageType.Invalid:
                    MessageNow( "Could not parse message." );
                    break;
            }
        }


        public void SendToSpectators( [NotNull] string message, [NotNull] params object[] args ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            Player[] spectators = Server.Players.Where( p => p.spectatedPlayer == this ).ToArray();
            if( spectators.Length > 0 ) {
                spectators.Message( "[Spectate]: &F" + message, args );
            }
        }


        const string WoMAlertPrefix = "^detail.user.alert=";
        public void MessageAlt( [NotNull] string message ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( this == Console ) {
                Logger.LogToConsole( message );
            } else if( IsUsingWoM ) {
                foreach( Packet p in LineWrapper.WrapPrefixed( WoMAlertPrefix, WoMAlertPrefix + Color.Sys + message ) ) {
                    Send( p );
                }
            } else {
                foreach( Packet p in LineWrapper.Wrap( Color.Sys + message ) ) {
                    Send( p );
                }
            }
        }

        [StringFormatMethod( "message" )]
        public void MessageAlt( [NotNull] string message, [NotNull] params object[] args ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            MessageAlt( String.Format( message, args ) );
        }


        public void Message( [NotNull] string message ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( IsSuper ) {
                Logger.LogToConsole( message );
            } else {
                foreach( Packet p in LineWrapper.Wrap( Color.Sys + message ) ) {
                    Send( p );
                }
            }
        }


        [StringFormatMethod( "message" )]
        public void Message( [NotNull] string message, [NotNull] object arg ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( arg == null ) throw new ArgumentNullException( "arg" );
            Message( String.Format( message, arg ) );
        }

        [StringFormatMethod( "message" )]
        public void Message( [NotNull] string message, [NotNull] params object[] args ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            Message( String.Format( message, args ) );
        }


        [StringFormatMethod( "message" )]
        public void MessagePrefixed( [NotNull] string prefix, [NotNull] string message, [NotNull] params object[] args ) {
            if( prefix == null ) throw new ArgumentNullException( "prefix" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            if( args.Length > 0 ) {
                message = String.Format( message, args );
            }
            if( this == Console ) {
                Logger.LogToConsole( message );
            } else {
                foreach( Packet p in LineWrapper.WrapPrefixed( prefix, message ) ) {
                    Send( p );
                }
            }
        }


        [StringFormatMethod( "message" )]
        internal void MessageNow( [NotNull] string message, [NotNull] params object[] args ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            if( IsDeaf ) return;
            if( args.Length > 0 ) {
                message = String.Format( message, args );
            }
            if( this == Console ) {
                Logger.LogToConsole( message );
            } else {
                if( Thread.CurrentThread != ioThread ) {
                    throw new InvalidOperationException( "SendNow may only be called from player's own thread." );
                }
                foreach( Packet p in LineWrapper.Wrap( Color.Sys + message ) ) {
                    SendNow( p );
                }
            }
        }


        [StringFormatMethod( "message" )]
        internal void MessageNowPrefixed( [NotNull] string prefix, [NotNull] string message, [NotNull] params object[] args ) {
            if( prefix == null ) throw new ArgumentNullException( "prefix" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            if( IsDeaf ) return;
            if( args.Length > 0 ) {
                message = String.Format( message, args );
            }
            if( this == Console ) {
                Logger.LogToConsole( message );
            } else {
                if( Thread.CurrentThread != ioThread ) {
                    throw new InvalidOperationException( "SendNow may only be called from player's own thread." );
                }
                foreach( Packet p in LineWrapper.WrapPrefixed( prefix, message ) ) {
                    Send( p );
                }
            }
        }


        #region Macros

        public void MessageNoPlayer( [NotNull] string playerName ) {
            if( playerName == null ) throw new ArgumentNullException( "playerName" );
            Message( "No players found matching \"{0}\"", playerName );
        }


        public void MessageNoWorld( [NotNull] string worldName ) {
            if( worldName == null ) throw new ArgumentNullException( "worldName" );
            Message( "No worlds found matching \"{0}\". See &H/Worlds", worldName );
        }


        public void MessageManyMatches( [NotNull] string itemType, [NotNull] IEnumerable<IClassy> names ) {
            if( itemType == null ) throw new ArgumentNullException( "itemType" );
            if( names == null ) throw new ArgumentNullException( "names" );

            string nameList = names.JoinToString( ", ", p => p.ClassyName );
            Message( "More than one {0} matched: {1}",
                     itemType, nameList );
        }


        public void MessageNoAccess( [NotNull] params Permission[] permissions ) {
            if( permissions == null ) throw new ArgumentNullException( "permissions" );
            Rank reqRank = RankManager.GetMinRankWithAllPermissions( permissions );
            if( reqRank == null ) {
                Message( "None of the ranks have permissions for this command." );
            } else {
                Message( "This command requires {0}+&S rank.",
                         reqRank.ClassyName );
            }
        }


        public void MessageNoAccess( [NotNull] CommandDescriptor cmd ) {
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            Rank reqRank = cmd.MinRank;
            if( reqRank == null ) {
                Message( "This command is disabled on the server." );
            } else {
                Message( "This command requires {0}+&S rank.",
                         reqRank.ClassyName );
            }
        }


        public void MessageNoRank( [NotNull] string rankName ) {
            if( rankName == null ) throw new ArgumentNullException( "rankName" );
            Message( "Unrecognized rank \"{0}\". See &H/Ranks", rankName );
        }


        public void MessageUnsafePath() {
            Message( "&WYou cannot access files outside the map folder." );
        }


        public void MessageNoZone( [NotNull] string zoneName ) {
            if( zoneName == null ) throw new ArgumentNullException( "zoneName" );
            Message( "No zones found matching \"{0}\". See &H/Zones", zoneName );
        }


        public void MessageInvalidWorldName( [NotNull] string worldName ) {
            Message( "Unacceptible world name: \"{0}\"", worldName );
            Message( "World names must be 1-16 characters long, and only contain letters, numbers, and underscores." );
        }


        public void MessageInvalidPlayerName( [NotNull] string playerName ) {
            Message( "\"{0}\" is not a valid player name.", playerName );
        }


        public void MessageMuted() {
            Message( "You are muted for {0} longer.",
                     Info.TimeMutedLeft.ToMiniString() );
        }


        public void MessageMaxTimeSpan() {
            Message( "Specify a time range up to {0:0}d.", DateTimeUtil.MaxTimeSpan.TotalDays );
        }

        #endregion


        #region Ignore

        readonly HashSet<PlayerInfo> ignoreList = new HashSet<PlayerInfo>();
        readonly object ignoreLock = new object();


        /// <summary> Checks whether this player is currently ignoring a given PlayerInfo.</summary>
        public bool IsIgnoring( [NotNull] PlayerInfo other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            lock( ignoreLock ) {
                return ignoreList.Contains( other );
            }
        }


        /// <summary> Adds a given PlayerInfo to the ignore list.
        /// Not that ignores are not persistent, and are reset when a player disconnects. </summary>
        /// <param name="other"> Player to ignore. </param>
        /// <returns> True if the player is now ignored,
        /// false is the player has already been ignored previously. </returns>
        public bool Ignore( [NotNull] PlayerInfo other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            lock( ignoreLock ) {
                if( !ignoreList.Contains( other ) ) {
                    ignoreList.Add( other );
                    return true;
                } else {
                    return false;
                }
            }
        }


        /// <summary> Removes a given PlayerInfo from the ignore list. </summary>
        /// <param name="other"> PlayerInfo to unignore. </param>
        /// <returns> True if the player is no longer ignored,
        /// false if the player was already not ignored. </returns>
        public bool Unignore( [NotNull] PlayerInfo other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            lock( ignoreLock ) {
                return ignoreList.Remove( other );
            }
        }


        /// <summary> Returns a list of all currently-ignored players. </summary>
        [NotNull]
        public PlayerInfo[] IgnoreList {
            get {
                lock( ignoreLock ) {
                    return ignoreList.ToArray();
                }
            }
        }

        #endregion


        #region Confirmation

        [CanBeNull]
        public ConfirmationCallback ConfirmCallback { get; private set; }

        [CanBeNull]
        public object ConfirmArgument { get; private set; }

        static void ConfirmCommandCallback( [NotNull] Player player, object tag, bool fromConsole ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            Command cmd = (Command)tag;
            cmd.Rewind();
            cmd.IsConfirmed = true;
            CommandManager.ParseCommand( player, cmd, fromConsole );
        }

        /// <summary> Time when the confirmation was requested. UTC. </summary>
        public DateTime ConfirmRequestTime { get; private set; }

        /// <summary> Request player to confirm continuing with the command.
        /// Player is prompted to type "/ok", and when he/she does,
        /// the command is called again with IsConfirmed flag set. </summary>
        /// <param name="cmd"> Command that needs confirmation. </param>
        /// <param name="message"> Message to print before "Type /ok to continue". </param>
        /// <param name="args"> Optional String.Format() arguments, for the message. </param>
        [StringFormatMethod( "message" )]
        public void Confirm( [NotNull] Command cmd, [NotNull] string message, [NotNull] params object[] args ) {
            Confirm( ConfirmCommandCallback, cmd, message, args );
        }

        [StringFormatMethod( "message" )]
        public void Confirm( [NotNull] ConfirmationCallback callback, [CanBeNull] object arg, [NotNull] string message, [NotNull] params object[] args ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            if( message == null ) throw new ArgumentNullException( "message" );
            if( args == null ) throw new ArgumentNullException( "args" );
            ConfirmCallback = callback;
            ConfirmArgument = arg;
            ConfirmRequestTime = DateTime.UtcNow;
            Message( "{0} Type &H/ok&S to continue.", String.Format( message, args ) );
        }

        #endregion


        #region AntiSpam

        public static int AntispamMessageCount = 3;
        public static int AntispamInterval = 4;
        readonly Queue<DateTime> spamChatLog = new Queue<DateTime>( AntispamMessageCount );

        internal bool DetectChatSpam() {
            if( IsSuper ) return false;
            if( spamChatLog.Count >= AntispamMessageCount ) {
                DateTime oldestTime = spamChatLog.Dequeue();
                if( DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds < AntispamInterval ) {
                    muteWarnings++;
                    if( muteWarnings > ConfigKey.AntispamMaxWarnings.GetInt() ) {
                        KickNow( "You were kicked for repeated spamming.", LeaveReason.MessageSpamKick );
                        Server.Message( "&W{0} was kicked for repeated spamming.", ClassyName );
                    } else {
                        TimeSpan autoMuteDuration = TimeSpan.FromSeconds( ConfigKey.AntispamMuteDuration.GetInt() );
                        Info.Mute( Console, autoMuteDuration, false, true );
                        Message( "You have been muted for {0} seconds. Slow down.", autoMuteDuration );
                    }
                    return true;
                }
            }
            spamChatLog.Enqueue( DateTime.UtcNow );
            return false;
        }

        #endregion

        #endregion


        #region Placing Blocks

        // for grief/spam detection
        readonly Queue<DateTime> spamBlockLog = new Queue<DateTime>();

        /// <summary> Last blocktype used by the player.
        /// Make sure to use in conjunction with Player.GetBind() to ensure that bindings are properly applied. </summary>
        public Block LastUsedBlockType { get; private set; }

        /// <summary> Max distance that player may be from a block to reach it (hack detection). </summary>
        public static int MaxBlockPlacementRange { get; set; }


        /// <summary> Handles manually-placed/deleted blocks.
        /// Returns true if player's action should result in a kick. </summary>
        public bool PlaceBlock( Vector3I coord, ClickAction action, Block type ) {
            if( World == null ) PlayerOpException.ThrowNoWorld( this );
            Map map = WorldMap;
            LastUsedBlockType = type;

            Vector3I coordBelow = new Vector3I( coord.X, coord.Y, coord.Z - 1 );

            // check if player is frozen or too far away to legitimately place a block
            if( Info.IsFrozen ||
                Math.Abs( coord.X * 32 - Position.X ) > MaxBlockPlacementRange ||
                Math.Abs( coord.Y * 32 - Position.Y ) > MaxBlockPlacementRange ||
                Math.Abs( coord.Z * 32 - Position.Z ) > MaxBlockPlacementRange ) {
                RevertBlockNow( coord );
                return false;
            }

            if( IsSpectating ) {
                Message( "You cannot build or delete while spectating." );
                RevertBlockNow( coord );
                return false;
            }

            if( World.IsLocked ) {
                RevertBlockNow( coord );
                Message( "This map is currently locked (read-only)." );
                return false;
            }

            if( CheckBlockSpam() ) return true;

            BlockChangeContext context = BlockChangeContext.Manual;
            if( IsPainting && action == ClickAction.Delete ) {
                context = BlockChangeContext.Replaced;
            }

            // bindings
            bool requiresUpdate = (type != bindings[(byte)type] || IsPainting);
            if( action == ClickAction.Delete && !IsPainting ) {
                type = Block.Air;
            }
            type = bindings[(byte)type];

            // selection handling
            if( SelectionMarksExpected > 0 ) {
                RevertBlockNow( coord );
                SelectionAddMark( coord, true );
                return false;
            }

            CanPlaceResult canPlaceResult;
            if( type == Block.Stair && coord.Z > 0 && map.GetBlock( coordBelow ) == Block.Stair ) {
                // stair stacking
                canPlaceResult = CanPlace( map, coordBelow, Block.DoubleStair, context );
            } else {
                // normal placement
                canPlaceResult = CanPlace( map, coord, type, context );
            }

            // if all is well, try placing it
            switch( canPlaceResult ) {
                case CanPlaceResult.Allowed:
                    BlockUpdate blockUpdate;
                    if( type == Block.Stair && coord.Z > 0 && map.GetBlock( coordBelow ) == Block.Stair ) {
                        // handle stair stacking
                        blockUpdate = new BlockUpdate( this, coordBelow, Block.DoubleStair );
                        Info.ProcessBlockPlaced( (byte)Block.DoubleStair );
                        map.QueueUpdate( blockUpdate );
                        RaisePlayerPlacedBlockEvent( this, World.Map, coordBelow, Block.Stair, Block.DoubleStair, context );
                        SendNow( PacketWriter.MakeSetBlock( coordBelow, Block.DoubleStair ) );
                        RevertBlockNow( coord );
                        break;

                    } else {
                        // handle normal blocks
                        blockUpdate = new BlockUpdate( this, coord, type );
                        Info.ProcessBlockPlaced( (byte)type );
                        Block old = map.GetBlock( coord );
                        map.QueueUpdate( blockUpdate );
                        RaisePlayerPlacedBlockEvent( this, World.Map, coord, old, type, context );
                        if( requiresUpdate || RelayAllUpdates ) {
                            SendNow( PacketWriter.MakeSetBlock( coord, type ) );
                        }
                    }
                    break;

                case CanPlaceResult.BlocktypeDenied:
                    Message( "&WYou are not permitted to affect this block type." );
                    RevertBlockNow( coord );
                    break;

                case CanPlaceResult.RankDenied:
                    Message( "&WYour rank is not allowed to build." );
                    RevertBlockNow( coord );
                    break;

                case CanPlaceResult.Revert:
                    RevertBlockNow(coord);
                    break;

                case CanPlaceResult.WorldDenied:
                    switch( World.BuildSecurity.CheckDetailed( Info ) ) {
                        case SecurityCheckResult.RankTooLow:
                        case SecurityCheckResult.RankTooHigh:
                            Message( "&WYour rank is not allowed to build in this world." );
                            break;
                        case SecurityCheckResult.BlackListed:
                            Message( "&WYou are not allowed to build in this world." );
                            break;
                    }
                    RevertBlockNow( coord );
                    break;

                case CanPlaceResult.ZoneDenied:
                    Zone deniedZone = WorldMap.Zones.FindDenied( coord, this );
                    if( deniedZone != null ) {
                        Message( "&WYou are not allowed to build in zone \"{0}\".", deniedZone.Name );
                    } else {
                        Message( "&WYou are not allowed to build here." );
                    }
                    RevertBlockNow( coord );
                    break;

                case CanPlaceResult.PluginDenied:
                    RevertBlockNow( coord );
                    break;

                //case CanPlaceResult.PluginDeniedNoUpdate:
                //    break;
            }
            return false;
        }


        /// <summary>  Gets the block from given location in player's world,
        /// and sends it (async) to the player.
        /// Used to undo player's attempted block placement/deletion. </summary>
        public void RevertBlock( Vector3I coords ) {
            SendLowPriority( PacketWriter.MakeSetBlock( coords, WorldMap.GetBlock( coords ) ) );
        }

        public void Kill(World inWorld, string message)
        {
            LastTimeKilled = DateTime.UtcNow;
            inWorld.Players.Message(message);
            TeleportTo(inWorld.Map.Spawn);
        } 


        /// <summary>  Gets the block from given location in player's world, and sends it (sync) to the player.
        /// Used to undo player's attempted block placement/deletion.
        /// To avoid threading issues, only use this from this player's IoThread. </summary>
        void RevertBlockNow( Vector3I coords ) {
            SendNow( PacketWriter.MakeSetBlock( coords, WorldMap.GetBlock( coords ) ) );
        }


        // returns true if the player is spamming and should be kicked.
        bool CheckBlockSpam() {
            if( Info.Rank.AntiGriefBlocks == 0 || Info.Rank.AntiGriefSeconds == 0 ) return false;
            if( spamBlockLog.Count >= Info.Rank.AntiGriefBlocks ) {
                DateTime oldestTime = spamBlockLog.Dequeue();
                double spamTimer = DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds;
                if( spamTimer < Info.Rank.AntiGriefSeconds ) {
                    KickNow( "You were kicked by antigrief system. Slow down.", LeaveReason.BlockSpamKick );
                    Server.Message( "{0}&W was kicked for suspected griefing.", ClassyName );
                    Logger.Log( LogType.SuspiciousActivity,
                                "{0} was kicked for block spam ({1} blocks in {2} seconds)",
                                Name, Info.Rank.AntiGriefBlocks, spamTimer );
                    return true;
                }
            }
            spamBlockLog.Enqueue( DateTime.UtcNow );
            return false;
        }

        #endregion


        #region Binding

        readonly Block[] bindings = new Block[50];

        public void Bind( Block type, Block replacement ) {
            bindings[(byte)type] = replacement;
        }

        public void ResetBind( Block type ) {
            bindings[(byte)type] = type;
        }

        public void ResetBind( [NotNull] params Block[] types ) {
            if( types == null ) throw new ArgumentNullException( "types" );
            foreach( Block type in types ) {
                ResetBind( type );
            }
        }

        public Block GetBind( Block type ) {
            return bindings[(byte)type];
        }

        public void ResetAllBinds() {
            foreach( Block block in Enum.GetValues( typeof( Block ) ) ) {
                if( block != Block.Undefined ) {
                    ResetBind( block );
                }
            }
        }

        #endregion


        #region Permission Checks

        /// <summary> Returns true if player has ALL of the given permissions. </summary>
        public bool Can( [NotNull] params Permission[] permissions ) {
            if( permissions == null ) throw new ArgumentNullException( "permissions" );
            return IsSuper || permissions.All( Info.Rank.Can );
        }


        /// <summary> Returns true if player has ANY of the given permissions. </summary>
        public bool CanAny( [NotNull] params Permission[] permissions ) {
            if( permissions == null ) throw new ArgumentNullException( "permissions" );
            return IsSuper || permissions.Any( Info.Rank.Can );
        }


        /// <summary> Returns true if player has the given permission. </summary>
        public bool Can( Permission permission ) {
            return IsSuper || Info.Rank.Can( permission );
        }


        /// <summary> Returns true if player has the given permission,
        /// and is allowed to affect players of the given rank. </summary>
        public bool Can( Permission permission, [NotNull] Rank other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return IsSuper || Info.Rank.Can( permission, other );
        }


        /// <summary> Returns true if player is allowed to run
        /// draw commands that affect a given number of blocks. </summary>
        public bool CanDraw( int volume ) {
            if( volume < 0 ) throw new ArgumentOutOfRangeException( "volume" );
            return IsSuper || (Info.Rank.DrawLimit == 0) || (volume <= Info.Rank.DrawLimit);
        }


        /// <summary> Returns true if player is allowed to join a given world. </summary>
        public bool CanJoin( [NotNull] World worldToJoin ) {
            if( worldToJoin == null ) throw new ArgumentNullException( "worldToJoin" );
            return IsSuper || worldToJoin.AccessSecurity.Check( Info );
        }


        /// <summary> Checks whether player is allowed to place a block on the current world at given coordinates.
        /// Raises the PlayerPlacingBlock event. </summary>
        public CanPlaceResult CanPlace( [NotNull] Map map, Vector3I coords, Block newBlock, BlockChangeContext context ) {
            if( map == null ) throw new ArgumentNullException( "map" );
            CanPlaceResult result;

            // check whether coordinate is in bounds
            Block oldBlock = map.GetBlock( coords );
            if( oldBlock == Block.Undefined ) {
                result = CanPlaceResult.OutOfBounds;
                goto eventCheck;
            }

            // check special blocktypes
            if( newBlock == Block.Admincrete && !Can( Permission.PlaceAdmincrete ) ) {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            } else if( (newBlock == Block.Water || newBlock == Block.StillWater) && !Can( Permission.PlaceWater ) ) {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            } else if( (newBlock == Block.Lava || newBlock == Block.StillLava) && !Can( Permission.PlaceLava ) ) {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            }

            // check admincrete-related permissions
            if( oldBlock == Block.Admincrete && !Can( Permission.DeleteAdmincrete ) ) {
                result = CanPlaceResult.BlocktypeDenied;
                goto eventCheck;
            }

            // check zones & world permissions
            PermissionOverride zoneCheckResult = map.Zones.Check( coords, this );
            if( zoneCheckResult == PermissionOverride.Allow ) {
                result = CanPlaceResult.Allowed;
                goto eventCheck;
            } else if( zoneCheckResult == PermissionOverride.Deny ) {
                result = CanPlaceResult.ZoneDenied;
                goto eventCheck;
            }

            // Check world permissions
            World mapWorld = map.World;
            if( mapWorld != null ) {
                switch( mapWorld.BuildSecurity.CheckDetailed( Info ) ) {
                    case SecurityCheckResult.Allowed:
                        // Check world's rank permissions
                        if( (Can( Permission.Build ) || newBlock == Block.Air) &&
                            (Can( Permission.Delete ) || oldBlock == Block.Air) ) {
                            result = CanPlaceResult.Allowed;
                        } else {
                            result = CanPlaceResult.RankDenied;
                        }
                        break;

                    case SecurityCheckResult.WhiteListed:
                        result = CanPlaceResult.Allowed;
                        break;

                    default:
                        result = CanPlaceResult.WorldDenied;
                        break;
                }
            } else {
                result = CanPlaceResult.Allowed;
            }

        eventCheck:
            var handler = PlacingBlock;
            if( handler == null ) return result;

            var e = new PlayerPlacingBlockEventArgs( this, map, coords, oldBlock, newBlock, context, result );
            handler( null, e );
            return e.Result;
        }


        /// <summary> Whether this player can currently see another player as being online.
        /// Visibility is determined by whether the other player is hiding or spectating.
        /// Players can always see themselves. Super players (e.g. Console) can see all.
        /// Hidden players can only be seen by those of sufficient rank. </summary>
        public bool CanSee( [NotNull] Player other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return other == this ||
                   IsSuper ||
                   !other.Info.IsHidden ||
                   Info.Rank.CanSee( other.Info.Rank );
        }


        /// <summary> Whether this player can currently see another player moving.
        /// Behaves very similarly to CanSee method, except when spectating:
        /// Players can never see someone who's spectating them. If other player is spectating
        /// someone else, they are treated as hidden and can only be seen by those of sufficient rank. </summary>
        public bool CanSeeMoving( [NotNull] Player other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            return other == this ||
                   IsSuper ||
                   other.spectatedPlayer == null && !other.Info.IsHidden ||
                   (other.spectatedPlayer != this && Info.Rank.CanSee( other.Info.Rank ));
        }


        /// <summary> Whether this player should see a given world on the /Worlds list by default. </summary>
        public bool CanSee( [NotNull] World world ) {
            if( world == null ) throw new ArgumentNullException( "world" );
            return CanJoin( world ) && !world.IsHidden;
        }

        #endregion


        #region Undo / Redo

        readonly LinkedList<UndoState> undoStack = new LinkedList<UndoState>();
        readonly LinkedList<UndoState> redoStack = new LinkedList<UndoState>();

        internal UndoState RedoPop() {
            if( redoStack.Count > 0 ) {
                var lastNode = redoStack.Last;
                redoStack.RemoveLast();
                return lastNode.Value;
            } else {
                return null;
            }
        }

        internal UndoState RedoBegin( DrawOperation op ) {
            LastDrawOp = op;
            UndoState newState = new UndoState( op );
            undoStack.AddLast( newState );
            return newState;
        }

        internal UndoState UndoBegin( DrawOperation op ) {
            LastDrawOp = op;
            UndoState newState = new UndoState( op );
            redoStack.AddLast( newState );
            return newState;
        }

        public UndoState UndoPop() {
            if( undoStack.Count > 0 ) {
                var lastNode = undoStack.Last;
                undoStack.RemoveLast();
                return lastNode.Value;
            } else {
                return null;
            }
        }

        public UndoState DrawBegin( DrawOperation op ) {
            LastDrawOp = op;
            UndoState newState = new UndoState( op );
            undoStack.AddLast( newState );
            if( undoStack.Count > ConfigKey.MaxUndoStates.GetInt() ) {
                undoStack.RemoveFirst();
            }
            redoStack.Clear();
            return newState;
        }

        public void UndoClear() {
            undoStack.Clear();
        }

        public void RedoClear() {
            redoStack.Clear();
        }

        #endregion


        #region Drawing, Selection

        [NotNull]
        public IBrush Brush { get; set; }

        [CanBeNull]
        public DrawOperation LastDrawOp { get; set; }


        /// <summary> Whether player is currently making a selection. </summary>
        public bool IsMakingSelection {
            get { return SelectionMarksExpected > 0; }
        }

        /// <summary> Number of selection marks so far. </summary>
        public int SelectionMarkCount {
            get { return selectionMarks.Count; }
        }

        /// <summary> Number of marks expected to complete the selection. </summary>
        public int SelectionMarksExpected { get; private set; }

        /// <summary> Whether player is repeating a selection (/static) </summary>
        public bool IsRepeatingSelection { get; set; }

        [CanBeNull]
        Command selectionRepeatCommand;

        [CanBeNull]
        SelectionCallback selectionCallback;

        readonly Queue<Vector3I> selectionMarks = new Queue<Vector3I>();

        [CanBeNull]
        object selectionArgs;

        [CanBeNull]
        Permission[] selectionPermissions;


        public void SelectionAddMark( Vector3I pos, bool executeCallbackIfNeeded ) {
            if( !IsMakingSelection ) throw new InvalidOperationException( "No selection in progress." );
            selectionMarks.Enqueue( pos );
            if( SelectionMarkCount >= SelectionMarksExpected ) {
                if( executeCallbackIfNeeded ) {
                    SelectionExecute();
                } else {
                    Message( "Last block marked at {0}. Type &H/Mark&S or click any block to continue.", pos );
                }
            } else {
                Message( "Block #{0} marked at {1}. Place mark #{2}.",
                         SelectionMarkCount, pos, SelectionMarkCount + 1 );
            }
        }


        public void SelectionExecute() {
            if( !IsMakingSelection || selectionCallback == null ) {
                throw new InvalidOperationException( "No selection in progress." );
            }
            SelectionMarksExpected = 0;
            // check if player still has the permissions required to complete the selection.
            if( selectionPermissions == null || Can( selectionPermissions ) ) {
                selectionCallback( this, selectionMarks.ToArray(), selectionArgs );
                if( IsRepeatingSelection && selectionRepeatCommand != null ) {
                    selectionRepeatCommand.Rewind();
                    CommandManager.ParseCommand( this, selectionRepeatCommand, this == Console );
                }
                selectionMarks.Clear();
            } else {
                // More complex permission checks can be done in the callback function itself.
                Message( "&WYou are no longer allowed to complete this action." );
                MessageNoAccess( selectionPermissions );
            }
        }


        public void SelectionStart( int marksExpected,
                                    [NotNull] SelectionCallback callback,
                                    [CanBeNull] object args,
                                    [CanBeNull] params Permission[] requiredPermissions ) {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            selectionArgs = args;
            SelectionMarksExpected = marksExpected;
            selectionMarks.Clear();
            selectionCallback = callback;
            selectionPermissions = requiredPermissions;
        }


        public void SelectionResetMarks() {
            selectionMarks.Clear();
        }


        public void SelectionCancel() {
            selectionMarks.Clear();
            SelectionMarksExpected = 0;
            selectionCallback = null;
            selectionArgs = null;
            selectionPermissions = null;
        }

        #endregion


        #region Copy/Paste

        CopyState[] copyInformation;
        public CopyState[] CopyInformation {
            get { return copyInformation; }
        }

        int copySlot;
        public int CopySlot {
            get { return copySlot; }
            set {
                if( value < 0 || value > Info.Rank.CopySlots ) {
                    throw new ArgumentOutOfRangeException( "value" );
                }
                copySlot = value;
            }
        }

        internal void InitCopySlots() {
            Array.Resize( ref copyInformation, Info.Rank.CopySlots );
            CopySlot = Math.Min( CopySlot, Info.Rank.CopySlots - 1 );
        }

        [CanBeNull]
        public CopyState GetCopyInformation() {
            return CopyInformation[copySlot];
        }

        public void SetCopyInformation( [CanBeNull] CopyState info ) {
            if( info != null ) info.Slot = copySlot;
            CopyInformation[copySlot] = info;
        }

        #endregion


        #region Spectating

        [CanBeNull]
        Player spectatedPlayer;

        /// <summary> Player currently being spectated. Use Spectate/StopSpectate methods to set. </summary>
        [CanBeNull]
        public Player SpectatedPlayer {
            get { return spectatedPlayer; }
        }

        [CanBeNull]
        public PlayerInfo LastSpectatedPlayer { get; private set; }

        readonly object spectateLock = new object();

        public bool IsSpectating {
            get { return (spectatedPlayer != null); }
        }


        public bool Spectate( [NotNull] Player target ) {
            if( target == null ) throw new ArgumentNullException( "target" );
            lock( spectateLock ) {
                if( target == this ) {
                    PlayerOpException.ThrowCannotTargetSelf( this, Info, "spectate" );
                }

                if( !Can( Permission.Spectate, target.Info.Rank ) ) {
                    PlayerOpException.ThrowPermissionLimit( this, target.Info, "spectate", Permission.Spectate );
                }

                if( spectatedPlayer == target ) return false;

                spectatedPlayer = target;
                LastSpectatedPlayer = target.Info;
                Message( "Now spectating {0}&S. Type &H/unspec&S to stop.", target.ClassyName );
                return true;
            }
        }


        public bool StopSpectating() {
            lock( spectateLock ) {
                if( spectatedPlayer == null ) return false;
                Message( "Stopped spectating {0}", spectatedPlayer.ClassyName );
                spectatedPlayer = null;
                return true;
            }
        }

        #endregion


        #region Static Utilities

        static readonly Uri PaidCheckUri = new Uri( "http://www.minecraft.net/haspaid.jsp?user=" );
        const int PaidCheckTimeout = 5000;


        /// <summary> Checks whether a given player has a paid minecraft.net account. </summary>
        /// <returns> True if the account is paid. False if it is not paid, or if information is unavailable. </returns>
        public static bool CheckPaidStatus( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create( PaidCheckUri + Uri.EscapeDataString( name ) );
            request.ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint( Server.BindIPEndPointCallback );
            request.Timeout = PaidCheckTimeout;
            request.CachePolicy = new RequestCachePolicy( RequestCacheLevel.NoCacheNoStore );

            try {
                using( WebResponse response = request.GetResponse() ) {
                    // ReSharper disable AssignNullToNotNullAttribute
                    using( StreamReader responseReader = new StreamReader( response.GetResponseStream() ) ) {
                        // ReSharper restore AssignNullToNotNullAttribute
                        string paidStatusString = responseReader.ReadToEnd();
                        bool isPaid;
                        return Boolean.TryParse( paidStatusString, out isPaid ) && isPaid;
                    }
                }
            } catch( WebException ex ) {
                Logger.Log( LogType.Warning,
                            "Could not check paid status of player {0}: {1}",
                            name, ex.Message );
                return false;
            }
        }


        /// <summary> Ensures that a player name has the correct length and character set. </summary>
        public static bool IsValidName( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( name.Length < 2 || name.Length > 16 ) return false;
            return ContainsValidCharacters(name);
        }
        
        /// <summary> Ensures that a player name has the correct length and character set. </summary>
        public static bool ContainsValidCharacters( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            // ReSharper disable LoopCanBeConvertedToQuery
            for( int i = 0; i < name.Length; i++ ) {
                char ch = name[i];
                if( (ch < '0' && ch != '.') || (ch > '9' && ch < 'A') || (ch > 'Z' && ch < '_') || (ch > '_' && ch < 'a') || ch > 'z' ) {
                    return false;
                }
            }
            // ReSharper restore LoopCanBeConvertedToQuery
            return true;
        }

        #endregion


        public void TeleportTo( Position pos ) {
            StopSpectating();
            Send( PacketWriter.MakeSelfTeleport( pos ) );
            Position = pos;
        }


        /// <summary> Time since the player was last active (moved, talked, or clicked). </summary>
        public TimeSpan IdleTime {
            get {
                return DateTime.UtcNow.Subtract( LastActiveTime );
            }
        }


        /// <summary> Resets the IdleTimer to 0. </summary>
        public void ResetIdleTimer() {
            LastActiveTime = DateTime.UtcNow;
        }


        #region Kick

        /// <summary> Advanced kick command. </summary>
        /// <param name="player"> Player who is kicking. </param>
        /// <param name="reason"> Reason for kicking. May be null or blank if allowed by server configuration. </param>
        /// <param name="context"> Classification of kick context. </param>
        /// <param name="announce"> Whether the kick should be announced publicly on the server and IRC. </param>
        /// <param name="raiseEvents"> Whether Player.BeingKicked and Player.Kicked events should be raised. </param>
        /// <param name="recordToPlayerDB"> Whether the kick should be counted towards player's record.</param>
        public void Kick( [NotNull] Player player, [CanBeNull] string reason, LeaveReason context,
                          bool announce, bool raiseEvents, bool recordToPlayerDB ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( !Enum.IsDefined( typeof( LeaveReason ), context ) ) {
                throw new ArgumentOutOfRangeException( "context" );
            }
            if( reason != null && reason.Trim().Length == 0 ) reason = null;

            // Check if player can ban/unban in general
            if( !player.Can( Permission.Kick ) ) {
                PlayerOpException.ThrowPermissionMissing( player, Info, "kick", Permission.Kick );
            }

            // Check if player is trying to ban/unban self
            if( player == this ) {
                PlayerOpException.ThrowCannotTargetSelf( player, Info, "kick" );
            }

            // Check if player has sufficiently high permission limit
            if( !player.Can( Permission.Kick, Info.Rank ) ) {
                PlayerOpException.ThrowPermissionLimit( player, Info, "kick", Permission.Kick );
            }

            // check if kick reason is missing but required
            PlayerOpException.CheckKickReason( reason, player, Info );

            // raise Player.BeingKicked event
            if( raiseEvents ) {
                var e = new PlayerBeingKickedEventArgs( this, player, reason, announce, recordToPlayerDB, context );
                RaisePlayerBeingKickedEvent( e );
                if( e.Cancel ) PlayerOpException.ThrowCancelled( player, Info );
                recordToPlayerDB = e.RecordToPlayerDB;
            }

            // actually kick
            string kickReason;
            if( reason != null ) {
                kickReason = String.Format( "Kicked by {0}: {1}", player.Name, reason );
            } else {
                kickReason = String.Format( "Kicked by {0}", player.Name );
            }
            Kick( kickReason, context );

            // log and record kick to PlayerDB
            Logger.Log( LogType.UserActivity,
                        "{0} kicked {1}. Reason: {2}",
                        player.Name, Name, reason ?? "" );
            if( recordToPlayerDB ) {
                Info.ProcessKick( player, reason );
            }

            // announce kick
            if( announce ) {
                if( reason != null && ConfigKey.AnnounceKickAndBanReasons.Enabled() ) {
                    Server.Message( "{0}&W was kicked by {1}&W: {2}",
                                    ClassyName, player.ClassyName, reason );
                } else {
                    Server.Message( "{0}&W was kicked by {1}",
                                    ClassyName, player.ClassyName );
                }
            }

            // raise Player.Kicked event
            if( raiseEvents ) {
                var e = new PlayerKickedEventArgs( this, player, reason, announce, recordToPlayerDB, context );
                RaisePlayerKickedEvent( e );
            }
        }

        #endregion


        [CanBeNull]
        public string LastUsedPlayerName { get; set; }

        [CanBeNull]
        public string LastUsedWorldName { get; set; }


        /// <summary> Name formatted for the debugger. </summary>
        public override string ToString() {
            if( Info != null ) {
                return String.Format( "Player({0})", Info.Name );
            } else {
                return String.Format( "Player({0})", IP );
            }
        }
    }


    sealed class PlayerListSorter : IComparer<Player> {
        public static readonly PlayerListSorter Instance = new PlayerListSorter();

        public int Compare( Player x, Player y ) {
            if( x.Info.Rank == y.Info.Rank ) {
                return StringComparer.OrdinalIgnoreCase.Compare( x.Name, y.Name );
            } else {
                return x.Info.Rank.Index - y.Info.Rank.Index;
            }
        }
    }
}