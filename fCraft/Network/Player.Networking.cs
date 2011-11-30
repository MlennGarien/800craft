// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using fCraft.AutoRank;
using fCraft.Drawing;
using fCraft.Events;
using fCraft.MapConversion;
using JetBrains.Annotations;
using System.Text.RegularExpressions;

namespace fCraft {
    /// <summary> Represents a connection to a Minecraft client. Handles low-level interactions (e.g. networking). </summary>
    public sealed partial class Player {
        public static int SocketTimeout { get; set; }
        public static bool RelayAllUpdates { get; set; }
        const int SleepDelay = 5; // milliseconds
        const int SocketPollInterval = 200; // multiples of SleepDelay, approx. 1 second
        const int PingInterval = 3; // multiples of SocketPollInterval, approx. 3 seconds

        const string NoSmpMessage = "This server is for Minecraft Classic only.";

        static Player() {
            MaxBlockPlacementRange = 7 * 32;
            SocketTimeout = 10000;
        }


        public LeaveReason LeaveReason { get; private set; }

        public IPAddress IP { get; private set; }


        bool canReceive = true,
             canSend = true,
             canQueue = true;

        Thread ioThread;
        TcpClient client;
        readonly NetworkStream stream;
        BinaryReader reader;
        PacketWriter writer;
        readonly ConcurrentQueue<Packet> outputQueue = new ConcurrentQueue<Packet>(),
                                         priorityOutputQueue = new ConcurrentQueue<Packet>();


        internal static void StartSession( [NotNull] TcpClient tcpClient ) {
            if( tcpClient == null ) throw new ArgumentNullException( "tcpClient" );
            new Player( tcpClient );
        }

        Player( [NotNull] TcpClient tcpClient ) {
            if( tcpClient == null ) throw new ArgumentNullException( "tcpClient" );
            State = SessionState.Connecting;
            LoginTime = DateTime.UtcNow;
            LastActiveTime = DateTime.UtcNow;
            LastPatrolTime = DateTime.UtcNow;
            LeaveReason = LeaveReason.Unknown;
            LastUsedBlockType = Block.Undefined;

            client = tcpClient;
            client.SendTimeout = SocketTimeout;
            client.ReceiveTimeout = SocketTimeout;

            Brush = NormalBrushFactory.Instance;
            Metadata = new MetadataCollection<object>();

            try {
                IP = ((IPEndPoint)(client.Client.RemoteEndPoint)).Address;
                if( Server.RaiseSessionConnectingEvent( IP ) ) return;

                stream = client.GetStream();
                reader = new BinaryReader( stream );
                writer = new PacketWriter( stream );

                ioThread = new Thread( IoLoop ) {
                    Name = "fCraft.Session",
                    IsBackground = true
                };
                ioThread.Start();

            } catch( SocketException ) {
                // Mono throws SocketException when accessing Client.RemoteEndPoint on disconnected sockets
                Disconnect();

            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Session failed to start", "fCraft", ex, false );
                Disconnect();
            }
        }


        #region I/O Loop

        void IoLoop() {
            try {
                Server.RaiseSessionConnectedEvent( this );

                // try to log the player in, otherwise die.
                if( !LoginSequence() ) return;

                BandwidthUseMode = Info.BandwidthUseMode;

                // set up some temp variables
                Packet packet = new Packet();

                int pollCounter = 0,
                    pingCounter = 0;

                // main i/o loop
                while( canSend ) {
                    int packetsSent = 0;

                    // detect player disconnect
                    if( pollCounter > SocketPollInterval ) {
                        if( !client.Connected ||
                            (client.Client.Poll( 1000, SelectMode.SelectRead ) && client.Client.Available == 0) ) {
                            if( Info != null ) {
                                Logger.Log( LogType.Debug,
                                            "Player.IoLoop: Lost connection to player {0} ({1}).", Name, IP );
                            } else {
                                Logger.Log( LogType.Debug,
                                            "Player.IoLoop: Lost connection to unidentified player at {0}.", IP );
                            }
                            LeaveReason = LeaveReason.ClientQuit;
                            return;
                        }
                        if( pingCounter > PingInterval ) {
                            writer.WritePing();
                            BytesSent++;
                            pingCounter = 0;
                            MeasureBandwidthUseRates();
                        }
                        pingCounter++;
                        pollCounter = 0;
                        if( IsUsingWoM ) {
                            MessageNowPrefixed( "", "^detail.user=" + InfoCommands.GetCompassString( Position.R ) );
                        }
                    }
                    pollCounter++;

                    if( DateTime.UtcNow.Subtract( lastMovementUpdate ) > movementUpdateInterval ) {
                        UpdateVisibleEntities();
                        lastMovementUpdate = DateTime.UtcNow;
                    }

                    // send output to player
                    while( canSend && packetsSent < Server.MaxSessionPacketsPerTick ) {
                        if( !priorityOutputQueue.Dequeue( ref packet ) )
                            if( !outputQueue.Dequeue( ref packet ) ) break;

                        if( IsDeaf && packet.OpCode == OpCode.Message ) continue;

                        writer.Write( packet.Data );
                        BytesSent += packet.Data.Length;
                        packetsSent++;

                        if( packet.OpCode == OpCode.Kick ) {
                            writer.Flush();
                            if( LeaveReason == LeaveReason.Unknown ) LeaveReason = LeaveReason.Kick;
                            return;
                        }

                        if( DateTime.UtcNow.Subtract( lastMovementUpdate ) > movementUpdateInterval ) {
                            UpdateVisibleEntities();
                            lastMovementUpdate = DateTime.UtcNow;
                        }
                    }

                    // check if player needs to change worlds
                    if( canSend ) {
                        lock( joinWorldLock ) {
                            if( forcedWorldToJoin != null ) {
                                while( priorityOutputQueue.Dequeue( ref packet ) ) {
                                    writer.Write( packet.Data );
                                    BytesSent += packet.Data.Length;
                                    packetsSent++;
                                    if( packet.OpCode == OpCode.Kick ) {
                                        writer.Flush();
                                        if( LeaveReason == LeaveReason.Unknown ) LeaveReason = LeaveReason.Kick;
                                        return;
                                    }
                                }
                                if( !JoinWorldNow( forcedWorldToJoin, useWorldSpawn, worldChangeReason ) ) {
                                    Logger.Log( LogType.Warning,
                                                "Player.IoLoop: Player was asked to force-join a world, but it was full." );
                                    KickNow( "World is full.", LeaveReason.ServerFull );
                                }
                                forcedWorldToJoin = null;
                            }
                        }

                        if( DateTime.UtcNow.Subtract( lastMovementUpdate ) > movementUpdateInterval ) {
                            UpdateVisibleEntities();
                            lastMovementUpdate = DateTime.UtcNow;
                        }
                    }


                    // get input from player
                    while( canReceive && stream.DataAvailable ) {
                        byte opcode = reader.ReadByte();
                        switch( (OpCode)opcode ) {

                            case OpCode.Message:
                                if( !ProcessMessagePacket() ) return;
                                break;

                            case OpCode.Teleport:
                                ProcessMovementPacket();
                                break;

                            case OpCode.SetBlockClient:
                                ProcessSetBlockPacket();
                                break;

                            case OpCode.Ping:
                                BytesReceived++;
                                continue;

                            default:
                                Logger.Log( LogType.SuspiciousActivity,
                                            "Player {0} was kicked after sending an invalid opcode ({1}).",
                                            Name, opcode );
                                KickNow( "Unknown packet opcode " + opcode,
                                         LeaveReason.InvalidOpcodeKick );
                                return;
                        }

                        if( DateTime.UtcNow.Subtract( lastMovementUpdate ) > movementUpdateInterval ) {
                            UpdateVisibleEntities();
                            lastMovementUpdate = DateTime.UtcNow;
                        }
                    }

                    Thread.Sleep( SleepDelay );
                }

            } catch( IOException ) {
                LeaveReason = LeaveReason.ClientQuit;

            } catch( SocketException ) {
                LeaveReason = LeaveReason.ClientQuit;
#if !DEBUG
            } catch( Exception ex ) {
                LeaveReason = LeaveReason.ServerError;
                Logger.LogAndReportCrash( "Error in Player.IoLoop", "fCraft", ex, false );
#endif
            } finally {
                canQueue = false;
                canSend = false;
                Disconnect();
            }
        }


        bool ProcessMessagePacket() {
            BytesReceived += 66;
            ResetIdleTimer();
            reader.ReadByte();
            string message = ReadString();

            if( !IsSuper && message.StartsWith( "/womid " ) ) {
                IsUsingWoM = true;
                return true;
            }

            if( Chat.ContainsInvalidChars( message ) ) {
                Logger.Log( LogType.SuspiciousActivity,
                            "Player.ParseMessage: {0} attempted to write illegal characters in chat and was kicked.",
                            Name );
                Server.Message( "{0}&W was kicked for sending invalid chat.", ClassyName );
                KickNow( "Illegal characters in chat.", LeaveReason.InvalidMessageKick );
                return false;
            }
#if DEBUG
                ParseMessage( message, false );
#else
            try {
                ParseMessage( message, false );
            } catch( IOException ) {
                throw;
            } catch( SocketException ) {
                throw;
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Error while parsing player's message", "fCraft", ex, false );
                MessageNow( "&WError while handling your message ({0}: {1})." +
                            "It is recommended that you reconnect to the server.",
                            ex.GetType().Name, ex.Message );
            }
#endif
            return true;
        }


        void ProcessMovementPacket() {
            BytesReceived += 10;
            reader.ReadByte();
            Position newPos = new Position {
                X = IPAddress.NetworkToHostOrder( reader.ReadInt16() ),
                Z = IPAddress.NetworkToHostOrder( reader.ReadInt16() ),
                Y = IPAddress.NetworkToHostOrder( reader.ReadInt16() ),
                R = reader.ReadByte(),
                L = reader.ReadByte()
            };

            Position oldPos = Position;

            // calculate difference between old and new positions
            Position delta = new Position {
                X = (short)(newPos.X - oldPos.X),
                Y = (short)(newPos.Y - oldPos.Y),
                Z = (short)(newPos.Z - oldPos.Z),
                R = (byte)Math.Abs( newPos.R - oldPos.R ),
                L = (byte)Math.Abs( newPos.L - oldPos.L )
            };

            // skip everything if player hasn't moved
            if( delta.IsZero ) return;

            bool rotChanged = (delta.R != 0) || (delta.L != 0);

            // only reset the timer if player rotated
            // if player is just pushed around, rotation does not change (and timer should not reset)
            if( rotChanged ) ResetIdleTimer();

            if( Info.IsFrozen ) {
                // special handling for frozen players
                if( delta.X * delta.X + delta.Y * delta.Y > AntiSpeedMaxDistanceSquared ||
                    Math.Abs( delta.Z ) > 40 ) {
                    SendNow( PacketWriter.MakeSelfTeleport( Position ) );
                }
                newPos.X = Position.X;
                newPos.Y = Position.Y;
                newPos.Z = Position.Z;

                // recalculate deltas
                delta.X = 0;
                delta.Y = 0;
                delta.Z = 0;

            } else if( !Can( Permission.UseSpeedHack ) ) {
                int distSquared = delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z;
                // speedhack detection
                if( DetectMovementPacketSpam() ) {
                    return;

                } else if( (distSquared - delta.Z * delta.Z > AntiSpeedMaxDistanceSquared || delta.Z > AntiSpeedMaxJumpDelta) &&
                           speedHackDetectionCounter >= 0 ) {

                    if( speedHackDetectionCounter == 0 ) {
                        lastValidPosition = Position;
                    } else if( speedHackDetectionCounter > 1 ) {
                        DenyMovement();
                        speedHackDetectionCounter = 0;
                        return;
                    }
                    speedHackDetectionCounter++;

                } else {
                    speedHackDetectionCounter = 0;
                }
            }

            if( RaisePlayerMovingEvent( this, newPos ) ) {
                DenyMovement();
                return;
            }

            Position = newPos;
            RaisePlayerMovedEvent( this, oldPos );
        }


        void ProcessSetBlockPacket() {
            BytesReceived += 9;
            if( World == null || World.Map == null ) return;
            ResetIdleTimer();
            short x = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
            short z = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
            short y = IPAddress.NetworkToHostOrder( reader.ReadInt16() );
            ClickAction action = (reader.ReadByte() == 1) ? ClickAction.Build : ClickAction.Delete;
            byte type = reader.ReadByte();

            // if a player is using InDev or SurvivalTest client, they may try to
            // place blocks that are not found in MC Classic. Convert them!
            if( type > 49 ) {
                type = MapDat.MapBlock( type );
            }

            Vector3I coords = new Vector3I( x, y, z );

            // If block is in bounds, count the click.
            // Sometimes MC allows clicking out of bounds,
            // like at map transitions or at the top layer of the world.
            // Those clicks should be simply ignored.
            if( World.Map.InBounds( coords ) ) {
                var e = new PlayerClickingEventArgs( this, coords, action, (Block)type );
                if( RaisePlayerClickingEvent( e ) ) {
                    RevertBlockNow( coords );
                } else {
                    RaisePlayerClickedEvent( this, coords, e.Action, e.Block );
                    PlaceBlock( coords, e.Action, e.Block );
                }
            }
        }

        #endregion


        void Disconnect() {
            State = SessionState.Disconnected;
            Server.UnregisterSession( this );
            Server.RaiseSessionDisconnectedEvent( this, LeaveReason );

            if( HasRegistered ) {
                Server.UnregisterPlayer( this );
                RaisePlayerDisconnectedEvent( this, LeaveReason );
            }

            if( reader != null ) {
                reader.Close();
                reader = null;
            }

            if( writer != null ) {
                writer.Close();
                writer = null;
            }

            if( client != null ) {
                client.Close();
                client = null;
            }

            ioThread = null;
        }


        bool LoginSequence() {
            byte opcode = reader.ReadByte();

            switch( opcode ) {
                case (byte)OpCode.Handshake:
                    break;

                case 2:
                    GentlyKickBetaClients();
                    return false;

                case (byte)'G':
                    ServeCfg();
                    return false;

                default:
                    Logger.Log( LogType.Error,
                                "Player.LoginSequence: Unexpected opcode in the first packet from {0}: {1}.",
                                IP, opcode );
                    KickNow( "Incompatible client, or a network error.", LeaveReason.ProtocolViolation );
                    return false;
            }

            // Check protocol version
            int clientProtocolVersion = reader.ReadByte();
            if( clientProtocolVersion != Config.ProtocolVersion ) {
                Logger.Log( LogType.Error,
                            "Player.LoginSequence: Wrong protocol version: {0}.",
                            clientProtocolVersion );
                KickNow( "Incompatible protocol version!", LeaveReason.ProtocolViolation );
                return false;
            }

            string playerName = ReadString();

            // Check name for nonstandard characters
            if( !IsValidName( playerName ) ) {
                Logger.Log( LogType.SuspiciousActivity,
                            "Player.LoginSequence: Unacceptible player name: {0} ({1})",
                            playerName, IP );
                KickNow( "Invalid characters in player name!", LeaveReason.ProtocolViolation );
                return false;
            }

            string verificationCode = ReadString();
            reader.ReadByte(); // unused
            BytesReceived += 131;

            // ReSharper disable PossibleNullReferenceException
            Position = WorldManager.MainWorld.Map.Spawn;
            // ReSharper restore PossibleNullReferenceException
            Info = PlayerDB.FindOrCreateInfoForPlayer( playerName, IP );
            ResetAllBinds();

            if( Server.VerifyName( Name, verificationCode, Heartbeat.Salt ) ) {
                IsVerified = true;

            } else {
                NameVerificationMode nameVerificationMode = ConfigKey.VerifyNames.GetEnum<NameVerificationMode>();

                string standardMessage = String.Format( "Player.LoginSequence: Could not verify player name for {0} ({1}).",
                                                        Name, IP );
                if( IP.Equals( IPAddress.Loopback ) && nameVerificationMode != NameVerificationMode.Always ) {
                    Logger.Log( LogType.SuspiciousActivity,
                                "{0} Player was identified as connecting from localhost and allowed in.",
                                standardMessage );
                    IsVerified = true;

                } else if( IP.IsLAN() && ConfigKey.AllowUnverifiedLAN.Enabled() ) {
                    Logger.Log( LogType.SuspiciousActivity,
                                "{0} Player was identified as connecting from LAN and allowed in.",
                                standardMessage );
                    IsVerified = true;

                } else if( Info.TimesVisited > 1 && Info.LastIP.Equals( IP ) ) {
                    switch( nameVerificationMode ) {
                        case NameVerificationMode.Always:
                            Info.ProcessFailedLogin( this );
                            Logger.Log( LogType.SuspiciousActivity,
                                        "{0} IP matched previous records for that name. " +
                                        "Player was kicked anyway because VerifyNames is set to Always.",
                                        standardMessage );
                            KickNow( "Could not verify player name!", LeaveReason.UnverifiedName );
                            return false;

                        case NameVerificationMode.Balanced:
                        case NameVerificationMode.Never:
                            Logger.Log( LogType.SuspiciousActivity,
                                        "{0} IP matched previous records for that name. Player was allowed in.",
                                        standardMessage );
                            IsVerified = true;
                            break;
                    }

                } else {
                    switch( nameVerificationMode ) {
                        case NameVerificationMode.Always:
                        case NameVerificationMode.Balanced:
                            Info.ProcessFailedLogin( this );
                            Logger.Log( LogType.SuspiciousActivity,
                                        "{0} IP did not match. Player was kicked.",
                                        standardMessage );
                            KickNow( "Could not verify player name!", LeaveReason.UnverifiedName );
                            return false;

                        case NameVerificationMode.Never:
                            Logger.Log( LogType.SuspiciousActivity,
                                        "{0} IP did not match. Player was allowed in anyway because VerifyNames is set to Never.",
                                        standardMessage );
                            Message( "&WYour name could not be verified." );
                            break;
                    }
                }
            }


            // Check if player is banned
            if( Info.IsBanned ) {
                Info.ProcessFailedLogin( this );
                Logger.Log( LogType.SuspiciousActivity,
                            "Banned player {0} tried to log in from {1}",
                            Name, IP );
                string bannedMessage;
                if( Info.BannedBy != null ) {
                    if( Info.BanReason != null ) {
                        bannedMessage = String.Format( "Banned {0} ago by {1}: {2}",
                                                       Info.TimeSinceBan.ToMiniString(),
                                                       Info.BannedBy,
                                                       Info.BanReason );
                    } else {
                        bannedMessage = String.Format( "Banned {0} ago by {1}",
                                                       Info.TimeSinceBan.ToMiniString(),
                                                       Info.BannedBy );
                    }
                } else {
                    if( Info.BanReason != null ) {
                        bannedMessage = String.Format( "Banned {0} ago: {1}",
                                                       Info.TimeSinceBan.ToMiniString(),
                                                       Info.BanReason );
                    } else {
                        bannedMessage = String.Format( "Banned {0} ago",
                                                       Info.TimeSinceBan.ToMiniString() );
                    }
                }
                KickNow( bannedMessage, LeaveReason.LoginFailed );
                return false;
            }


            // Check if player's IP is banned
            IPBanInfo ipBanInfo = IPBanList.Get( IP );
            if( ipBanInfo != null && Info.BanStatus != BanStatus.IPBanExempt ) {
                Info.ProcessFailedLogin( this );
                ipBanInfo.ProcessAttempt( this );
                Logger.Log( LogType.SuspiciousActivity,
                            "{0} tried to log in from a banned IP.", Name );
                string bannedMessage = String.Format( "IP-banned {0} ago by {1}: {2}",
                                                      DateTime.UtcNow.Subtract( ipBanInfo.BanDate ).ToMiniString(),
                                                      ipBanInfo.BannedBy,
                                                      ipBanInfo.BanReason );
                KickNow( bannedMessage, LeaveReason.LoginFailed );
                return false;
            }


            // Check if max number of connections is reached for IP
            if( !Server.RegisterSession( this ) ) {
                Info.ProcessFailedLogin( this );
                Logger.Log( LogType.SuspiciousActivity,
                            "Player.LoginSequence: Denied player {0}: maximum number of connections was reached for {1}",
                            playerName, IP );
                KickNow( String.Format( "Max connections reached for {0}", IP ), LeaveReason.LoginFailed );
                return false;
            }


            // Check if player is paid (if required)
            if( ConfigKey.PaidPlayersOnly.Enabled() ) {
                SendNow( PacketWriter.MakeHandshake( this,
                                                     ConfigKey.ServerName.GetString(),
                                                     "Please wait; Checking paid status..." ) );
                writer.Flush();

                if( !CheckPaidStatus( Name ) ) {
                    Logger.Log( LogType.SystemActivity,
                                "Player {0} was kicked because their account is not paid, and PaidOnly setting is enabled.", Name );
                    KickNow( "Paid players allowed only.", LeaveReason.LoginFailed );
                    return false;
                }
            }


            // Any additional security checks should be done right here
            if( RaisePlayerConnectingEvent( this ) ) return false;


            // ----==== beyond this point, player is considered connecting (allowed to join) ====----

            // Register player for future block updates
            if( !Server.RegisterPlayer( this ) ) {
                Logger.Log( LogType.SystemActivity,
                            "Player {0} was kicked because server is full.", Name );
                string kickMessage = String.Format( "Sorry, server is full ({0}/{1})",
                                        Server.Players.Length, ConfigKey.MaxPlayers.GetInt() );
                KickNow( kickMessage, LeaveReason.ServerFull );
                return false;
            }
            Info.ProcessLogin( this );
            State = SessionState.LoadingMain;


            // ----==== Beyond this point, player is considered connected (authenticated and registered) ====----


            // Figure out what the starting world should be
            World startingWorld = Info.Rank.MainWorld ?? WorldManager.MainWorld;
            startingWorld = RaisePlayerConnectedEvent( this, startingWorld );

            // Send server information
            string serverName = ConfigKey.ServerName.GetString();
            string motd;
            if( ConfigKey.WoMEnableEnvExtensions.Enabled() ) {
                if( IP.Equals( IPAddress.Loopback ) ) {
                    motd = "&0cfg=localhost:" + Server.Port + "/" + startingWorld.Name + "~motd";
                } else {
                    motd = "&0cfg=" + Server.ExternalIP + ":" + Server.Port + "/" + startingWorld.Name + "~motd";
                }
            } else {
                motd = ConfigKey.MOTD.GetString();
            }
            SendNow( PacketWriter.MakeHandshake( this, serverName, motd ) );

            // AutoRank
            if( ConfigKey.AutoRankEnabled.Enabled() ) {
                Rank newRank = AutoRankManager.Check( Info );
                if( newRank != null ) {
                    try {
                        Info.ChangeRank( AutoRank, newRank, "~AutoRank", true, true, true );
                    } catch( PlayerOpException ex ) {
                        Logger.Log( LogType.Error,
                                    "AutoRank failed on player {0}: {1}",
                                    ex.Player.Name, ex.Message );
                    }
                }
            }

            bool firstTime = (Info.TimesVisited == 1);
            if( !JoinWorldNow( startingWorld, true, WorldChangeReason.FirstWorld ) ) {
                Logger.Log( LogType.Warning,
                            "Could not load main world ({0}) for connecting player {1} (from {2}): " +
                            "Either main world is full, or an error occured.",
                            startingWorld.Name, Name, IP );
                KickNow( "Either main world is full, or an error occured.", LeaveReason.WorldFull );
                return false;
            }


            // ==== Beyond this point, player is considered ready (has a world) ====

            var canSee = Server.Players.CanSee( this );

            // Announce join
            if( ConfigKey.ShowConnectionMessages.Enabled() ) {
                // ReSharper disable AssignNullToNotNullAttribute
                string message = Server.MakePlayerConnectedMessage( this, firstTime, World );
                // ReSharper restore AssignNullToNotNullAttribute
                canSee.Message( message );
            }

            if( !IsVerified ) {
                canSee.Message( "&WName and IP of {0}&W are unverified!", ClassyName );
            }

            if( Info.IsHidden ) {
                if( Can( Permission.Hide ) ) {
                    canSee.Message( "&8Player {0}&8 logged in hidden. Pssst.", ClassyName );
                } else {
                    Info.IsHidden = false;
                }
            }

            // Check if other banned players logged in from this IP
            PlayerInfo[] bannedPlayerNames = PlayerDB.FindPlayers( IP, 25 )
                                                     .Where( playerFromSameIP => playerFromSameIP.IsBanned )
                                                     .ToArray();
            if( bannedPlayerNames.Length > 0 ) {
                canSee.Message( "&WPlayer {0}&W logged in from an IP shared by banned players: {1}",
                                ClassyName, bannedPlayerNames.JoinToClassyString() );
                Logger.Log( LogType.SuspiciousActivity,
                            "Player {0} logged in from an IP shared by banned players: {1}",
                                ClassyName, bannedPlayerNames.JoinToString( info => info.Name ) );
            }

            // check if player is still muted
            if( Info.MutedUntil > DateTime.UtcNow ) {
                Message( "&WYou were previously muted by {0}&W, {1} left.",
                         Info.MutedByClassy, Info.TimeMutedLeft.ToMiniString() );
                canSee.Message( "&WPlayer {0}&W was previously muted by {1}&W, {2} left.",
                                ClassyName, Info.MutedByClassy, Info.TimeMutedLeft.ToMiniString() );
            }

            // check if player is still frozen
            if( Info.IsFrozen ) {
                if( Info.FrozenOn != DateTime.MinValue ) {
                    Message( "&WYou were previously frozen {0} ago by {1}",
                             Info.TimeSinceFrozen.ToMiniString(),
                             Info.FrozenByClassy );
                    canSee.Message( "&WPlayer {0}&W was previously frozen {1} ago by {2}",
                                    ClassyName,
                                    Info.TimeSinceFrozen.ToMiniString(),
                                    Info.FrozenByClassy );
                } else {
                    Message( "&WYou were previously frozen by {0}",
                             Info.FrozenByClassy );
                    canSee.Message( "&WPlayer {0}&W was previously frozen by {1}",
                                    ClassyName, Info.FrozenByClassy );
                }
            }

            // Welcome message
            if( File.Exists( Paths.GreetingFileName ) ) {
                string[] greetingText = File.ReadAllLines( Paths.GreetingFileName );
                foreach( string greetingLine in greetingText ) {
                    MessageNow( Server.ReplaceTextKeywords( this, greetingLine ) );
                }
            } else {
                if( firstTime ) {
                    MessageNow( "Welcome to {0}", ConfigKey.ServerName.GetString() );
                } else {
                    MessageNow( "Welcome back to {0}", ConfigKey.ServerName.GetString() );
                }

                MessageNow( "Your rank is {0}&S. Type &H/Help&S for help.",
                            Info.Rank.ClassyName );
            }

            // A reminder for first-time users
            if( PlayerDB.Size == 1 && Info.Rank != RankManager.HighestRank ) {
                Message( "Type &H/Rank {0} {1}&S in console to promote yourself",
                         Name, RankManager.HighestRank.Name );
            }

            InitCopySlots();

            HasFullyConnected = true;
            State = SessionState.Online;
            Server.UpdatePlayerList();
            RaisePlayerReadyEvent( this );

            return true;
        }


        void GentlyKickBetaClients() {
            // This may be someone connecting with an SMP client
            int strLen = IPAddress.NetworkToHostOrder( reader.ReadInt16() );

            if( strLen >= 2 && strLen <= 16 ) {
                string smpPlayerName = Encoding.BigEndianUnicode.GetString( reader.ReadBytes( strLen*2 ) );

                Logger.Log( LogType.Warning,
                            "Player.LoginSequence: Player \"{0}\" tried connecting with Minecraft Beta client from {1}. " +
                            "fCraft does not support Minecraft Beta.", 
                            smpPlayerName, IP );

                // send SMP KICK packet
                writer.Write( (byte)255 );
                byte[] stringData = Encoding.BigEndianUnicode.GetBytes( NoSmpMessage );
                writer.Write( (short)NoSmpMessage.Length );
                writer.Write( stringData );
                BytesSent += (1 + stringData.Length);
                writer.Flush();

            } else {
                // Not SMP client (invalid player name length)
                Logger.Log( LogType.Error,
                            "Player.LoginSequence: Unexpected opcode in the first packet from {0}: 2.", IP );
                KickNow( "Unexpected handshake message - possible protocol mismatch!", LeaveReason.ProtocolViolation );
            }
        }


        static readonly Regex HttpFirstLine = new Regex( "GET /([a-zA-Z0-9_]{1,16})(~motd)? .+", RegexOptions.Compiled );
        void ServeCfg() {
            using( StreamReader textReader = new StreamReader( stream ) ) {
                using( StreamWriter textWriter = new StreamWriter( stream ) ) {
                    string firstLine = "G" + textReader.ReadLine();
                    var match = HttpFirstLine.Match( firstLine );
                    if( match.Success ) {
                        string worldName = match.Groups[1].Value;
                        bool firstTime = match.Groups[2].Success;
                        World world = WorldManager.FindWorldExact( worldName );
                        if( world != null ) {
                            string cfg = world.GenerateWoMConfig( firstTime );
                            byte[] content = Encoding.UTF8.GetBytes( cfg );
                            textWriter.WriteLine( "HTTP/1.1 200 OK" );
                            textWriter.WriteLine( "Date: " + DateTime.UtcNow.ToString( "R" ) );
                            textWriter.WriteLine( "Content-Type: text/plain" );
                            textWriter.WriteLine( "Content-Length: " + content.Length );
                            textWriter.WriteLine();
                            textWriter.WriteLine( cfg );
                        } else {
                            textWriter.WriteLine( "HTTP/1.1 404 Not Found" );
                        }
                    } else {
                        textWriter.WriteLine( "HTTP/1.1 400 Bad Request" );
                    }
                }
            }
        }


        #region Joining Worlds

        readonly object joinWorldLock = new object();

        [CanBeNull]
        World forcedWorldToJoin;
        WorldChangeReason worldChangeReason;
        Position postJoinPosition;
        bool useWorldSpawn;

        public void JoinWorld( [NotNull] World newWorld, WorldChangeReason reason ) {
            if( newWorld == null ) throw new ArgumentNullException( "newWorld" );
            lock( joinWorldLock ) {
                useWorldSpawn = true;
                postJoinPosition = Position.Zero;
                forcedWorldToJoin = newWorld;
                worldChangeReason = reason;
            }
        }


        public void JoinWorld( [NotNull] World newWorld, WorldChangeReason reason, Position position ) {
            if( newWorld == null ) throw new ArgumentNullException( "newWorld" );
            if( !Enum.IsDefined( typeof( WorldChangeReason ), reason ) ) {
                throw new ArgumentOutOfRangeException( "reason" );
            }
            lock( joinWorldLock ) {
                useWorldSpawn = false;
                postJoinPosition = position;
                forcedWorldToJoin = newWorld;
                worldChangeReason = reason;
            }
        }


        internal bool JoinWorldNow( [NotNull] World newWorld, bool doUseWorldSpawn, WorldChangeReason reason ) {
            if( newWorld == null ) throw new ArgumentNullException( "newWorld" );
            if( !Enum.IsDefined( typeof( WorldChangeReason ), reason ) ) {
                throw new ArgumentOutOfRangeException( "reason" );
            }
            if( Thread.CurrentThread != ioThread ) {
                throw new InvalidOperationException( "Player.JoinWorldNow may only be called from player's own thread. " +
                                                     "Use Player.JoinWorld instead." );
            }

            string textLine1 = ConfigKey.ServerName.GetString();
            string textLine2;

            if( IsUsingWoM && ConfigKey.WoMEnableEnvExtensions.Enabled() ) {
                if( IP.Equals( IPAddress.Loopback ) ) {
                    textLine2 = "cfg=localhost:" + Server.Port + "/" + newWorld.Name;
                } else {
                    textLine2 = "cfg=" + Server.ExternalIP + ":" + Server.Port + "/" + newWorld.Name;
                }
            } else {
                textLine2 = "Loading world " + newWorld.ClassyName;
            }

            if( RaisePlayerJoiningWorldEvent( this, newWorld, reason, textLine1, textLine2 ) ) {
                Logger.Log( LogType.Warning,
                            "Player.JoinWorldNow: Player {0} was prevented from joining world {1} by an event callback.",
                            Name, newWorld.Name );
                return false;
            }

            World oldWorld = World;

            // remove player from the old world
            if( oldWorld != null && oldWorld != newWorld ) {
                if( !oldWorld.ReleasePlayer( this ) ) {
                    Logger.Log( LogType.Error,
                                "Player.JoinWorldNow: Player asked to be released from its world, " +
                                "but the world did not contain the player." );
                }
            }

            ResetVisibleEntities();

            ClearLowPriotityOutputQueue();

            Map map;

            // try to join the new world
            if( oldWorld != newWorld ) {
                bool announce = (oldWorld != null) && (oldWorld.Name != newWorld.Name);
                map = newWorld.AcceptPlayer( this, announce );
                if( map == null ) {
                    return false;
                }
            } else {
                map = newWorld.LoadMap();
            }
            World = newWorld;

            // Set spawn point
            if( doUseWorldSpawn ) {
                Position = map.Spawn;
            } else {
                Position = postJoinPosition;
            }

            // Start sending over the level copy
            if( oldWorld != null ) {
                SendNow( PacketWriter.MakeHandshake( this, textLine1, textLine2 ) );
            }

            writer.WriteMapBegin();
            BytesSent++;

            // enable Nagle's algorithm (in case it was turned off by LowLatencyMode)
            // to avoid wasting bandwidth for map transfer
            client.NoDelay = false;

            // Fetch compressed map copy
            byte[] buffer = new byte[1024];
            int mapBytesSent = 0;
            byte[] blockData;
            using( MemoryStream mapStream = new MemoryStream() ) {
                map.GetCompressedCopy( mapStream, true );
                blockData = mapStream.ToArray();
            }
            Logger.Log( LogType.Debug,
                        "Player.JoinWorldNow: Sending compressed map ({0} bytes) to {1}.",
                        blockData.Length, Name );

            // Transfer the map copy
            while( mapBytesSent < blockData.Length ) {
                int chunkSize = blockData.Length - mapBytesSent;
                if( chunkSize > 1024 ) {
                    chunkSize = 1024;
                } else {
                    // CRC fix for ManicDigger
                    for( int i = 0; i < buffer.Length; i++ ) {
                        buffer[i] = 0;
                    }
                }
                Array.Copy( blockData, mapBytesSent, buffer, 0, chunkSize );
                byte progress = (byte)(100 * mapBytesSent / blockData.Length);

                // write in chunks of 1024 bytes or less
                writer.WriteMapChunk( buffer, chunkSize, progress );
                BytesSent += 1028;
                mapBytesSent += chunkSize;
            }

            // Turn off Nagel's algorithm again for LowLatencyMode
            client.NoDelay = ConfigKey.LowLatencyMode.Enabled();

            // Done sending over level copy
            writer.WriteMapEnd( map );
            BytesSent += 7;

            // Sets player's spawn point to map spawn
            writer.WriteAddEntity( 255, this, map.Spawn );
            BytesSent += 74;

            // Teleport player to the target location
            // This allows preserving spawn rotation/look, and allows
            // teleporting player to a specific location (e.g. /TP or /Bring)
            writer.WriteTeleport( 255, Position );
            BytesSent += 10;

            if (World.IsRealm && oldWorld == newWorld)
            {
                Message("Rejoined Realm {0}", newWorld.ClassyName);
                if (World.DummyCount > 0)
                {
                    foreach (Player d in World.Dummys)
                    {
                        this.Send(PacketWriter.MakeAddEntity(d.Info.DummyID, d.Info.DummyName, d.Info.DummyPos));
                    }
                }
                
            }
            else if(World.IsRealm)
            {
                Message("Joined Realm {0}", newWorld.ClassyName);
            }


            if( !World.IsRealm && oldWorld == newWorld ) {
                Message( "Rejoined world {0}", newWorld.ClassyName );
                if (World.DummyCount > 0)
                {
                    foreach (Player d in World.Dummys)
                    {
                        this.Send(PacketWriter.MakeAddEntity(d.Info.DummyID, d.Info.DummyName, d.Info.DummyPos));
                    }
                }
            } else if(!World.IsRealm) {
                Message( "Joined world {0}", newWorld.ClassyName );
            }

            RaisePlayerJoinedWorldEvent( this, oldWorld, reason );

            // Done.
            Server.RequestGC();

            return true;
        }

        #endregion


        #region Sending

        /// <summary> Send packet to player (not thread safe, sync, immediate).
        /// Should NEVER be used from any thread other than this session's ioThread.
        /// Not thread-safe (for performance reason). </summary>
        public void SendNow( Packet packet ) {
            if( Thread.CurrentThread != ioThread ) {
                throw new InvalidOperationException( "SendNow may only be called from player's own thread." );
            }
            writer.Write( packet.Data );
            BytesSent += packet.Data.Length;
        }


        /// <summary> Send packet (thread-safe, async, priority queue).
        /// This is used for most packets (movement, chat, etc). </summary>
        public void Send( Packet packet ) {
            if( canQueue ) priorityOutputQueue.Enqueue( packet );
        }


        /// <summary> Send packet (thread-safe, asynchronous, delayed queue).
        /// This is currently only used for block updates. </summary>
        public void SendLowPriority( Packet packet ) {
            if( canQueue ) outputQueue.Enqueue( packet );
        }

        #endregion


        string ReadString() {
            return Encoding.ASCII.GetString( reader.ReadBytes( 64 ) ).TrimEnd();
        }


        public void ClearLowPriotityOutputQueue() {
            outputQueue.Clear();
        }


        public void ClearPriorityOutputQueue() {
            priorityOutputQueue.Clear();
        }


        #region Kicking

        /// <summary> Kick (asynchronous). Immediately blocks all client input, but waits
        /// until client thread has sent the kick packet. </summary>
        public void Kick( [NotNull] string message, LeaveReason leaveReason ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( !Enum.IsDefined( typeof( LeaveReason ), leaveReason ) ) {
                throw new ArgumentOutOfRangeException( "leaveReason" );
            }
            State = SessionState.PendingDisconnect;
            LeaveReason = leaveReason;

            canReceive = false;
            canQueue = false;

            // clear all pending output to be written to client (it won't matter after the kick)
            ClearLowPriotityOutputQueue();
            ClearPriorityOutputQueue();

            // bypassing Send() because canQueue is false
            priorityOutputQueue.Enqueue( PacketWriter.MakeDisconnect( message ) );
        }


        /// <summary> Kick (synchronous). Immediately sends the kick packet.
        /// Can only be used from IoThread (this is not thread-safe). </summary>
        void KickNow( [NotNull] string message, LeaveReason leaveReason ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( !Enum.IsDefined( typeof( LeaveReason ), leaveReason ) ) {
                throw new ArgumentOutOfRangeException( "leaveReason" );
            }
            if( Thread.CurrentThread != ioThread ) {
                throw new InvalidOperationException( "KickNow may only be called from player's own thread." );
            }
            State = SessionState.PendingDisconnect;
            LeaveReason = leaveReason;

            canQueue = false;
            canReceive = false;
            canSend = false;
            SendNow( PacketWriter.MakeDisconnect( message ) );
            writer.Flush();
        }


        /// <summary> Blocks the calling thread until this session disconnects. </summary>
        public void WaitForDisconnect() {
            if( Thread.CurrentThread == ioThread ) {
                throw new InvalidOperationException( "Cannot call WaitForDisconnect from IoThread." );
            }
            if( ioThread != null && ioThread.IsAlive ) {
                try {
                    ioThread.Join();
                } catch( NullReferenceException ) {
                } catch( ThreadStateException ) { }
            }
        }

        #endregion


        #region Movement

        // visible entities
        readonly Dictionary<Player, VisibleEntity> entities = new Dictionary<Player, VisibleEntity>();
        readonly Stack<Player> playersToRemove = new Stack<Player>( 127 );
        readonly Stack<sbyte> freePlayerIDs = new Stack<sbyte>( 127 );

        // movement optimization
        int fullUpdateCounter;
        public const int FullPositionUpdateIntervalDefault = 20;
        public static int FullPositionUpdateInterval = FullPositionUpdateIntervalDefault;
        const int SkipMovementThresholdSquared = 64,
                  SkipRotationThresholdSquared = 1500;

        // anti-speedhack vars
        int speedHackDetectionCounter;
        const int AntiSpeedMaxJumpDelta = 25, // 16 for normal client, 25 for WoM
                  AntiSpeedMaxDistanceSquared = 1024, // 32 * 32
                  AntiSpeedMaxPacketCount = 200,
                  AntiSpeedMaxPacketInterval = 5;

        // anti-speedhack vars: packet spam
        readonly Queue<DateTime> antiSpeedPacketLog = new Queue<DateTime>();
        DateTime antiSpeedLastNotification = DateTime.UtcNow;


        void ResetVisibleEntities() {
            foreach( var pos in entities.Values ) {
                SendNow( PacketWriter.MakeRemoveEntity( pos.Id ) );
            }
            freePlayerIDs.Clear();
            for( int i = 1; i <= sbyte.MaxValue; i++ ) {
                freePlayerIDs.Push( (sbyte)i );
            }
            playersToRemove.Clear();
            entities.Clear();
        }


        void UpdateVisibleEntities() {
            if( World == null ) PlayerOpException.ThrowNoWorld( this );
            if( spectatedPlayer != null ) {
                if( !spectatedPlayer.IsOnline || !CanSee( spectatedPlayer ) ) {
                    Message( "Stopped spectating {0}&S (disconnected)", spectatedPlayer.ClassyName );
                    spectatedPlayer = null;
                } else {
                    Position spectatePos = spectatedPlayer.Position;
                    World spectateWorld = spectatedPlayer.World;
                    if( spectateWorld == null ) {
                        throw new InvalidOperationException( "Trying to spectate player without a world." );
                    }
                    if( spectateWorld != World ) {
                        if( CanJoin( spectateWorld ) ) {
                            postJoinPosition = spectatePos;
                            Message( "Joined {0}&S to continue spectating {1}",
                                     spectateWorld.ClassyName,
                                     spectatedPlayer.ClassyName );
                            JoinWorldNow( spectateWorld, false, WorldChangeReason.SpectateTargetJoined );
                        } else {
                            Message( "Stopped spectating {0}&S (cannot join {1}&S)",
                                     spectatedPlayer.ClassyName,
                                     spectateWorld.ClassyName );
                            spectatedPlayer = null;
                        }
                    } else if( spectatePos != Position ) {
                        SendNow( PacketWriter.MakeSelfTeleport( spectatePos ) );
                    }
                }
            }

            Player[] worldPlayerList = World.Players;
            Position pos = Position;

            for( int i = 0; i < worldPlayerList.Length; i++ ) {
                Player otherPlayer = worldPlayerList[i];
                if( otherPlayer == this ||
                    !CanSee( otherPlayer ) ||
                    SpectatedPlayer == otherPlayer ||
                    otherPlayer.SpectatedPlayer == this ) continue;

                Position otherPos = otherPlayer.Position;
                int distance = pos.DistanceSquaredTo( otherPos );

                VisibleEntity entity;
                // if Player has a corresponding VisibleEntity
                if( entities.TryGetValue( otherPlayer, out entity ) ) {
                    entity.MarkedForRetention = true;

                    if( entity.LastKnownRank != otherPlayer.Info.Rank ) {
                        ReAddEntity( entity, otherPlayer, otherPos );
                        entity.LastKnownRank = otherPlayer.Info.Rank;

                    } else if( entity.Hidden ) {
                        if( distance < entityShowingThreshold ) {
                            ShowEntity( entity, otherPos );
                        }

                    } else {
                        if( distance > entityHidingThreshold ) {
                            HideEntity( entity );

                        } else if( entity.LastKnownPosition != otherPos ) {
                            MoveEntity( entity, otherPos );
                        }
                    }
                } else {
                    AddEntity( otherPlayer, otherPos );
                }
            }


            // Find entities to remove (not marked for retention).
            foreach( var pair in entities ) {
                if( pair.Value.MarkedForRetention ) {
                    pair.Value.MarkedForRetention = false;
                } else {
                    playersToRemove.Push( pair.Key );
                }
            }

            // Remove non-retained entities
            while( playersToRemove.Count > 0 ) {
                RemoveEntity( playersToRemove.Pop() );
            }

            fullUpdateCounter++;
            if( fullUpdateCounter >= FullPositionUpdateInterval ) {
                fullUpdateCounter = 0;
            }
        }


        void AddEntity( [NotNull] Player player, Position newPos ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( freePlayerIDs.Count > 0 ) {
                var pos = new VisibleEntity( newPos, freePlayerIDs.Pop(), player.Info.Rank );
                entities.Add( player, pos );
                SendNow( PacketWriter.MakeAddEntity( pos.Id, player.ListName, newPos ) );
            }
        }


        void HideEntity( [NotNull] VisibleEntity entity ) {
            if( entity == null ) throw new ArgumentNullException( "entity" );
            entity.Hidden = true;
            entity.LastKnownPosition = VisibleEntity.HiddenPosition;
            SendNow( PacketWriter.MakeTeleport( entity.Id, VisibleEntity.HiddenPosition ) );
        }


        void ShowEntity( [NotNull] VisibleEntity entity, Position newPos ) {
            if( entity == null ) throw new ArgumentNullException( "entity" );
            entity.Hidden = false;
            entity.LastKnownPosition = newPos;
            SendNow( PacketWriter.MakeTeleport( entity.Id, newPos ) );
        }


        void ReAddEntity( [NotNull] VisibleEntity entity, [NotNull] Player player, Position newPos ) {
            if( entity == null ) throw new ArgumentNullException( "entity" );
            if( player == null ) throw new ArgumentNullException( "player" );
            SendNow( PacketWriter.MakeRemoveEntity( entity.Id ) );
            SendNow( PacketWriter.MakeAddEntity( entity.Id, player.ListName, newPos ) );
            entity.LastKnownPosition = newPos;
        }


        void RemoveEntity( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            SendNow( PacketWriter.MakeRemoveEntity( entities[player].Id ) );
            freePlayerIDs.Push( entities[player].Id );
            entities.Remove( player );
        }


        void MoveEntity( [NotNull] VisibleEntity entity, Position newPos ) {
            if( entity == null ) throw new ArgumentNullException( "entity" );
            Position oldPos = entity.LastKnownPosition;

            // calculate difference between old and new positions
            Position delta = new Position {
                X = (short)(newPos.X - oldPos.X),
                Y = (short)(newPos.Y - oldPos.Y),
                Z = (short)(newPos.Z - oldPos.Z),
                R = (byte)Math.Abs( newPos.R - oldPos.R ),
                L = (byte)Math.Abs( newPos.L - oldPos.L )
            };

            bool posChanged = (delta.X != 0) || (delta.Y != 0) || (delta.Z != 0);
            bool rotChanged = (delta.R != 0) || (delta.L != 0);

            if( skipUpdates ) {
                int distSquared = delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z;
                // movement optimization
                if( distSquared < SkipMovementThresholdSquared &&
                    (delta.R * delta.R + delta.L * delta.L) < SkipRotationThresholdSquared &&
                    !entity.SkippedLastMove ) {

                    entity.SkippedLastMove = true;
                    return;
                }
                entity.SkippedLastMove = false;
            }

            Packet packet;
            // create the movement packet
            if( partialUpdates && delta.FitsIntoMoveRotatePacket && fullUpdateCounter < FullPositionUpdateInterval ) {
                if( posChanged && rotChanged ) {
                    // incremental position + rotation update
                    packet = PacketWriter.MakeMoveRotate( entity.Id, new Position {
                        X = delta.X,
                        Y = delta.Y,
                        Z = delta.Z,
                        R = newPos.R,
                        L = newPos.L
                    } );

                } else if( posChanged ) {
                    // incremental position update
                    packet = PacketWriter.MakeMove( entity.Id, delta );

                } else if( rotChanged ) {
                    // absolute rotation update
                    packet = PacketWriter.MakeRotate( entity.Id, newPos );
                } else {
                    return;
                }

            } else {
                // full (absolute position + rotation) update
                packet = PacketWriter.MakeTeleport( entity.Id, newPos );
            }

            entity.LastKnownPosition = newPos;
            SendNow( packet );
        }


        sealed class VisibleEntity {
            public static readonly Position HiddenPosition = new Position( 0, 0, short.MinValue );

            public VisibleEntity( Position newPos, sbyte newId, Rank newRank ) {
                Id = newId;
                LastKnownPosition = newPos;
                MarkedForRetention = true;
                Hidden = true;
                LastKnownRank = newRank;
            }

            public readonly sbyte Id;
            public Position LastKnownPosition;
            public Rank LastKnownRank;
            public bool Hidden;
            public bool MarkedForRetention;
            public bool SkippedLastMove;
        }


        Position lastValidPosition; // used in speedhack detection

        bool DetectMovementPacketSpam() {
            if( antiSpeedPacketLog.Count >= AntiSpeedMaxPacketCount ) {
                DateTime oldestTime = antiSpeedPacketLog.Dequeue();
                double spamTimer = DateTime.UtcNow.Subtract( oldestTime ).TotalSeconds;
                if( spamTimer < AntiSpeedMaxPacketInterval ) {
                    DenyMovement();
                    return true;
                }
            }
            antiSpeedPacketLog.Enqueue( DateTime.UtcNow );
            return false;
        }


        void DenyMovement() {
            SendNow( PacketWriter.MakeSelfTeleport( lastValidPosition ) );
            if( DateTime.UtcNow.Subtract( antiSpeedLastNotification ).Seconds > 1 ) {
                Message( "&WYou are not allowed to speedhack." );
                antiSpeedLastNotification = DateTime.UtcNow;
            }
        }

        #endregion


        #region Bandwidth Use Tweaks

        BandwidthUseMode bandwidthUseMode;
        int entityShowingThreshold, entityHidingThreshold;
        bool partialUpdates, skipUpdates;

        DateTime lastMovementUpdate;
        TimeSpan movementUpdateInterval;


        public BandwidthUseMode BandwidthUseMode {
            get {
                return bandwidthUseMode;
            }

            set {
                bandwidthUseMode = value;
                BandwidthUseMode actualValue = value;
                if( value == BandwidthUseMode.Default ) {
                    actualValue = ConfigKey.BandwidthUseMode.GetEnum<BandwidthUseMode>();
                }
                switch( actualValue ) {
                    case BandwidthUseMode.VeryLow:
                        entityShowingThreshold = (40 * 32) * (40 * 32);
                        entityHidingThreshold = (42 * 32) * (42 * 32);
                        partialUpdates = true;
                        skipUpdates = true;
                        movementUpdateInterval = TimeSpan.FromMilliseconds( 100 );
                        break;

                    case BandwidthUseMode.Low:
                        entityShowingThreshold = (50 * 32) * (50 * 32);
                        entityHidingThreshold = (52 * 32) * (52 * 32);
                        partialUpdates = true;
                        skipUpdates = true;
                        movementUpdateInterval = TimeSpan.FromMilliseconds( 50 );
                        break;

                    case BandwidthUseMode.Normal:
                        entityShowingThreshold = (68 * 32) * (68 * 32);
                        entityHidingThreshold = (70 * 32) * (70 * 32);
                        partialUpdates = true;
                        skipUpdates = false;
                        movementUpdateInterval = TimeSpan.FromMilliseconds( 50 );
                        break;

                    case BandwidthUseMode.High:
                        entityShowingThreshold = (128 * 32) * (128 * 32);
                        entityHidingThreshold = (130 * 32) * (130 * 32);
                        partialUpdates = true;
                        skipUpdates = false;
                        movementUpdateInterval = TimeSpan.FromMilliseconds( 50 );
                        break;

                    case BandwidthUseMode.VeryHigh:
                        entityShowingThreshold = int.MaxValue;
                        entityHidingThreshold = int.MaxValue;
                        partialUpdates = false;
                        skipUpdates = false;
                        movementUpdateInterval = TimeSpan.FromMilliseconds( 25 );
                        break;
                }
            }
        }

        #endregion


        #region Bandwidth Use Metering

        DateTime lastMeasurementDate = DateTime.UtcNow;
        int lastBytesSent, lastBytesReceived;


        /// <summary> Total bytes sent (to the client) this session. </summary>
        public int BytesSent { get; private set; }

        /// <summary> Total bytes received (from the client) this session. </summary>
        public int BytesReceived { get; private set; }

        /// <summary> Bytes sent (to the client) per second, averaged over the last several seconds. </summary>
        public double BytesSentRate { get; private set; }

        /// <summary> Bytes received (from the client) per second, averaged over the last several seconds. </summary>
        public double BytesReceivedRate { get; private set; }


        void MeasureBandwidthUseRates() {
            int sentDelta = BytesSent - lastBytesSent;
            int receivedDelta = BytesReceived - lastBytesReceived;
            TimeSpan timeDelta = DateTime.UtcNow.Subtract( lastMeasurementDate );
            BytesSentRate = sentDelta / timeDelta.TotalSeconds;
            BytesReceivedRate = receivedDelta / timeDelta.TotalSeconds;
            lastBytesSent = BytesSent;
            lastBytesReceived = BytesReceived;
            lastMeasurementDate = DateTime.UtcNow;
        }

        #endregion
    }
}