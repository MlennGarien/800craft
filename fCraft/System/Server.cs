// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using fCraft.AutoRank;
using fCraft.Drawing;
using fCraft.Events;
using JetBrains.Annotations;
using ThreadState = System.Threading.ThreadState;
using fCraft.Portals;

namespace fCraft {
    /// <summary> Core of an fCraft server. Manages startup/shutdown, online player
    /// sessions, and global events and scheduled tasks. </summary>
    public static partial class Server {

        /// <summary> Time when the server started (UTC). Used to check uptime. </summary>
        public static DateTime StartTime { get; private set; }

        internal static int MaxUploadSpeed,   // set by Config.ApplyConfig
                            BlockUpdateThrottling; // used when there are no players in a world

        internal const int MaxSessionPacketsPerTick = 128, // used when there are no players in a world
                           MaxBlockUpdatesPerTick = 100000; // used when there are no players in a world
        internal static float TicksPerSecond;

        public static bool IsRestarting = false;
        public static bool HSaverOn = false;

        public static bool Moderation = false;

        public static List<Player> VoicedPlayers = new List<Player>();

        public static List<Player> TempBans = new List<Player>();


        // networking
        static TcpListener listener;
        public static IPAddress InternalIP { get; private set; }
        public static IPAddress ExternalIP { get; private set; }

        public static int Port { get; private set; }

        public static Uri Uri { get; internal set; }


        #region Command-line args

        static readonly Dictionary<ArgKey, string> Args = new Dictionary<ArgKey, string>();

        /// <summary> Returns value of a given command-line argument (if present). Use HasArg to check flag arguments. </summary>
        /// <param name="key"> Command-line argument name (enumerated) </param>
        /// <returns> Value of the command-line argument, or null if this argument was not set or argument is a flag. </returns>
        [CanBeNull]
        public static string GetArg( ArgKey key ) {
            if( Args.ContainsKey( key ) ) {
                return Args[key];
            } else {
                return null;
            }
        }

        /// <summary> Checks whether a command-line argument was set. </summary>
        /// <param name="key"> Command-line argument name (enumerated) </param>
        /// <returns> True if given argument was given. Otherwise false. </returns>
        public static bool HasArg( ArgKey key ) {
            return Args.ContainsKey( key );
        }


        /// <summary> Produces a string containing all recognized arguments that wereset/passed to this instance of fCraft. </summary>
        /// <returns> A string containing all given arguments, or an empty string if none were set. </returns>
        public static string GetArgString() {
            return String.Join( " ", GetArgList() );
        }


        /// <summary> Produces a list of arguments that were passed to this instance of fCraft. </summary>
        /// <returns> An array of strings, formatted as --key="value" (or, for flag arguments, --key).
        /// Returns an empty string array if no arguments were set. </returns>
        public static string[] GetArgList() {
            List<string> argList = new List<string>();
            foreach( var pair in Args ) {
                if( pair.Value != null ) {
                    argList.Add( String.Format( "--{0}=\"{1}\"", pair.Key.ToString().ToLower(), pair.Value ) );
                } else {
                    argList.Add( String.Format( "--{0}", pair.Key.ToString().ToLower() ) );
                }
            }
            return argList.ToArray();
        }

        #endregion


        #region Initialization and Startup

        // flags used to ensure proper initialization order
        static bool libraryInitialized,
                    serverInitialized;
        public static bool IsRunning { get; private set; }

        /// <summary> Reads command-line switches and sets up paths and logging.
        /// This should be called before any other library function.
        /// Note to frontend devs: Subscribe to log-related events before calling this.
        /// Does not raise any events besides Logger.Logged.
        /// Throws exceptions on failure. </summary>
        /// <param name="rawArgs"> string arguments passed to the frontend (if any). </param>
        /// <exception cref="System.InvalidOperationException"> If library is already initialized. </exception>
        /// <exception cref="System.IO.IOException"> Working path, log path, or map path could not be set. </exception>
        public static void InitLibrary( [NotNull] IEnumerable<string> rawArgs ) {
            if( rawArgs == null ) throw new ArgumentNullException( "rawArgs" );
            if( libraryInitialized ) {
                throw new InvalidOperationException( "800Craft library is already initialized" );
            }

            ServicePointManager.Expect100Continue = false;

            // try to parse arguments
            foreach( string arg in rawArgs ) {
                if( arg.StartsWith( "--" ) ) {
                    string argKeyName, argValue;
                    if( arg.Contains( '=' ) ) {
                        argKeyName = arg.Substring( 2, arg.IndexOf( '=' ) - 2 ).ToLower().Trim();
                        argValue = arg.Substring( arg.IndexOf( '=' ) + 1 ).Trim();
                        if( argValue.StartsWith( "\"" ) && argValue.EndsWith( "\"" ) ) {
                            argValue = argValue.Substring( 1, argValue.Length - 2 );
                        }

                    } else {
                        argKeyName = arg.Substring( 2 );
                        argValue = null;
                    }
                    ArgKey key;
                    if( EnumUtil.TryParse( argKeyName, out key, true ) ) {
                        Args.Add( key, argValue );
                    } else {
                        Console.Error.WriteLine( "Unknown argument: {0}", arg );
                    }
                } else {
                    Console.Error.WriteLine( "Unknown argument: {0}", arg );
                }
            }

            // before we do anything, set path to the default location
            Directory.SetCurrentDirectory( Paths.WorkingPath );

            // set custom working path (if specified)
            string path = GetArg( ArgKey.Path );
            if( path != null && Paths.TestDirectory( "WorkingPath", path, true ) ) {
                Paths.WorkingPath = Path.GetFullPath( path );
                Directory.SetCurrentDirectory( Paths.WorkingPath );
            } else if( Paths.TestDirectory( "WorkingPath", Paths.WorkingPathDefault, true ) ) {
                Paths.WorkingPath = Path.GetFullPath( Paths.WorkingPathDefault );
                Directory.SetCurrentDirectory( Paths.WorkingPath );
            } else {
                throw new IOException( "Could not set the working path." );
            }

            // set log path
            string logPath = GetArg( ArgKey.LogPath );
            if( logPath != null && Paths.TestDirectory( "LogPath", logPath, true ) ) {
                Paths.LogPath = Path.GetFullPath( logPath );
            } else if( Paths.TestDirectory( "LogPath", Paths.LogPathDefault, true ) ) {
                Paths.LogPath = Path.GetFullPath( Paths.LogPathDefault );
            } else {
                throw new IOException( "Could not set the log path." );
            }

            if( HasArg( ArgKey.NoLog ) ) {
                Logger.Enabled = false;
            } else {
                Logger.MarkLogStart();
            }

            // set map path
            string mapPath = GetArg( ArgKey.MapPath );
            if( mapPath != null && Paths.TestDirectory( "MapPath", mapPath, true ) ) {
                Paths.MapPath = Path.GetFullPath( mapPath );
                Paths.IgnoreMapPathConfigKey = true;
            } else if( Paths.TestDirectory( "MapPath", Paths.MapPathDefault, true ) ) {
                Paths.MapPath = Path.GetFullPath( Paths.MapPathDefault );
            } else {
                throw new IOException( "Could not set the map path." );
            }

            // set config path
            Paths.ConfigFileName = Paths.ConfigFileNameDefault;
            string configFile = GetArg( ArgKey.Config );
            if( configFile != null ) {
                if( Paths.TestFile( "config.xml", configFile, false, FileAccess.Read ) ) {
                    Paths.ConfigFileName = new FileInfo( configFile ).FullName;
                }
            }

            if( MonoCompat.IsMono ) {
                Logger.Log( LogType.Debug, "Running on Mono {0}", MonoCompat.MonoVersion );
            }

#if DEBUG_EVENTS
            Logger.PrepareEventTracing();
#endif

            Logger.Log( LogType.Debug, "Working directory: {0}", Directory.GetCurrentDirectory() );
            Logger.Log( LogType.Debug, "Log path: {0}", Path.GetFullPath( Paths.LogPath ) );
            Logger.Log( LogType.Debug, "Map path: {0}", Path.GetFullPath( Paths.MapPath ) );
            Logger.Log( LogType.Debug, "Config path: {0}", Path.GetFullPath( Paths.ConfigFileName ) );

            libraryInitialized = true;
        }


        /// <summary> Initialized various server subsystems. This should be called after InitLibrary and before StartServer.
        /// Loads config, PlayerDB, IP bans, AutoRank settings, builds a list of commands, and prepares the IRC bot.
        /// Raises Server.Initializing and Server.Initialized events, and possibly Logger.Logged events.
        /// Throws exceptions on failure. </summary>
        /// <exception cref="System.InvalidOperationException"> Library is not initialized, or server is already initialzied. </exception>
        public static void InitServer() {
            if( serverInitialized ) {
                throw new InvalidOperationException( "Server is already initialized" );
            }
            if( !libraryInitialized ) {
                throw new InvalidOperationException( "Server.InitLibrary must be called before Server.InitServer" );
            }
            RaiseEvent( Initializing );

            // Instantiate DeflateStream to make sure that libMonoPosix is present.
            // This allows catching misconfigured Mono installs early, and stopping the server.
            using( var testMemStream = new MemoryStream() ) {
                using( new DeflateStream( testMemStream, CompressionMode.Compress ) ) {
                }
            }

            // warnings/disclaimers
            if( Updater.CurrentRelease.IsFlagged( ReleaseFlags.Dev ) ) {
                Logger.Log( LogType.Warning,
                            "You are using an unreleased developer version of 800Craft. " +
                            "Do not use this version unless you are ready to deal with bugs and potential data loss. " +
                            "Consider using the lastest stable version instead, available from http://github.com/glennmr/800craft" );
            }

            if( Updater.CurrentRelease.IsFlagged( ReleaseFlags.Unstable ) ) {
                const string unstableMessage = "This build has been marked as UNSTABLE. " +
                                               "Do not use except for debugging purposes. " +
                                               "Latest non-broken build is " + Updater.LatestStable;
#if DEBUG
                Logger.Log( LogType.Warning, unstableMessage );
#else
                throw new Exception( unstableMessage );
#endif
            }

            if( MonoCompat.IsMono && !MonoCompat.IsSGenCapable ) {
                Logger.Log( LogType.Warning,
                            "You are using a relatively old version of the Mono runtime ({0}). " +
                            "It is recommended that you upgrade to at least 2.8+",
                            MonoCompat.MonoVersion );
            }

#if DEBUG
            Config.RunSelfTest();
#else
            // delete the old updater, if exists
            File.Delete( Paths.UpdaterFileName );
            File.Delete( "fCraftUpdater.exe" ); // pre-0.600
#endif

            // try to load the config
            if( !Config.Load( false, false ) ) {
                throw new Exception( "800Craft Config failed to initialize" );
            }

            if( ConfigKey.VerifyNames.GetEnum<NameVerificationMode>() == NameVerificationMode.Never ) {
                Logger.Log( LogType.Warning,
                            "Name verification is currently OFF. Your server is at risk of being hacked. " +
                            "Enable name verification as soon as possible." );
            }

            // load player DB
            PlayerDB.Load();
            IPBanList.Load();

            // prepare the list of commands
            CommandManager.Init();
            PluginManager.GetInstance(); //2nd means plugins crash and not 800Craft
            // prepare the brushes
            BrushManager.Init();

            // Init IRC
            IRC.Init();
            GunClass.Init();
            Physics.Physics.Load();
            HeartbeatSaverUtil.Init();

            if( ConfigKey.AutoRankEnabled.Enabled() ) {
                AutoRankManager.Init();
            }

            RaiseEvent( Initialized );

            serverInitialized = true;
        }


        /// <summary> Starts the server:
        /// Creates Console pseudoplayer, loads the world list, starts listening for incoming connections,
        /// sets up scheduled tasks and starts the scheduler, starts the heartbeat, and connects to IRC.
        /// Raises Server.Starting and Server.Started events.
        /// May throw an exception on hard failure. </summary>
        /// <returns> True if server started normally, false on soft failure. </returns>
        /// <exception cref="System.InvalidOperationException"> Server is already running, or server/library have not been initailized. </exception>
        public static bool StartServer() {
            if( IsRunning ) {
                throw new InvalidOperationException( "Server is already running" );
            }
            if( !libraryInitialized || !serverInitialized ) {
                throw new InvalidOperationException( "Server.InitLibrary and Server.InitServer must be called before Server.StartServer" );
            }

            StartTime = DateTime.UtcNow;
            cpuUsageStartingOffset = Process.GetCurrentProcess().TotalProcessorTime;
            Players = new Player[0];

            RaiseEvent( Starting );

            if( ConfigKey.BackupDataOnStartup.Enabled() ) {
                BackupData();
            }

            Player.Console = new Player( ConfigKey.ConsoleName.GetString() );
            Player.AutoRank = new Player( "(AutoRank)" );

            if( ConfigKey.BlockDBEnabled.Enabled() ) BlockDB.Init();

            // try to load the world list
            if( !WorldManager.LoadWorldList() ) return false;
            WorldManager.SaveWorldList();

            // open the port
            Port = ConfigKey.Port.GetInt();
            InternalIP = IPAddress.Parse( ConfigKey.IP.GetString() );

            try {
                listener = new TcpListener( InternalIP, Port );
                listener.Start();

            } catch( Exception ex ) {
                // if the port is unavailable, try next one
                Logger.Log( LogType.Error,
                            "Could not start listening on port {0}, stopping. ({1})",
                            Port, ex.Message );
                if( !ConfigKey.IP.IsDefault() ) {
                    Logger.Log( LogType.Warning,
                                "Do not use the \"Designated IP\" setting unless you have multiple NICs or IPs." );
                }
                return false;
            }

            InternalIP = ((IPEndPoint)listener.LocalEndpoint).Address;
            ExternalIP = CheckExternalIP();

            if( ExternalIP == null ) {
                Logger.Log( LogType.SystemActivity,
                            "Server.Run: now accepting connections on port {0}", Port );
            } else {
                Logger.Log( LogType.SystemActivity,
                            "Server.Run: now accepting connections at {0}:{1}",
                            ExternalIP, Port );
            }


            // list loaded worlds
            WorldManager.UpdateWorldList();
            Logger.Log( LogType.SystemActivity,
                        "All available worlds: {0}",
                        WorldManager.Worlds.JoinToString( ", ", w => w.Name ) );

            Logger.Log( LogType.SystemActivity,
                        "Main world: {0}; default rank: {1}",
                        WorldManager.MainWorld.Name, RankManager.DefaultRank.Name );

            // Check for incoming connections (every 250ms)
            checkConnectionsTask = Scheduler.NewTask( CheckConnections ).RunForever( CheckConnectionsInterval );

            // Check for idles (every 30s)
            checkIdlesTask = Scheduler.NewTask( CheckIdles ).RunForever( CheckIdlesInterval );

            // Monitor CPU usage (every 30s)
            try {
                MonitorProcessorUsage( null );
                Scheduler.NewTask( MonitorProcessorUsage ).RunForever( MonitorProcessorUsageInterval,
                                                                       MonitorProcessorUsageInterval );
            } catch( Exception ex ) {
                Logger.Log( LogType.Error,
                            "Server.StartServer: Could not start monitoring CPU use: {0}", ex );
            }


            PlayerDB.StartSaveTask();

            // Announcements
            if( ConfigKey.AnnouncementInterval.GetInt() > 0 ) {
                TimeSpan announcementInterval = TimeSpan.FromMinutes( ConfigKey.AnnouncementInterval.GetInt() );
                Scheduler.NewTask( ShowRandomAnnouncement ).RunForever( announcementInterval );
            }

            // garbage collection
            gcTask = Scheduler.NewTask( DoGC ).RunForever( GCInterval, TimeSpan.FromSeconds( 45 ) );

            Heartbeat.Start();
            if( ConfigKey.HeartbeatToWoMDirect.Enabled() ) {
                //Heartbeat.SetWoMDirectSettings();
                if( ExternalIP == null ) {
                    Logger.Log( LogType.SystemActivity,
                                "WoM Direct heartbeat is enabled. To edit your server's appearence on the server list, " +
                                "see https://direct.worldofminecraft.com/server.php?port={0}&salt={1}",
                                Port, Heartbeat.Salt );
                } else {
                    Logger.Log( LogType.SystemActivity,
                                "WoM Direct heartbeat is enabled. To edit your server's appearence on the server list, " +
                                "see https://direct.worldofminecraft.com/server.php?ip={0}&port={1}&salt={2}",
                                ExternalIP, Port, Heartbeat.Salt );
                }
            }

            if( ConfigKey.RestartInterval.GetInt() > 0 ) {
                TimeSpan restartIn = TimeSpan.FromSeconds( ConfigKey.RestartInterval.GetInt() );
                Shutdown( new ShutdownParams( ShutdownReason.Restarting, restartIn, true, true ), false );
                ChatTimer.Start( restartIn, "Automatic Server Restart", Player.Console.Name );
            }

            if( ConfigKey.IRCBotEnabled.Enabled() ) IRC.Start();

            Scheduler.NewTask( AutoRankManager.TaskCallback ).RunForever( AutoRankManager.TickInterval );

            // start the main loop - server is now connectible
            Scheduler.Start();
            PortalHandler.GetInstance();
            PortalDB.Load();

            IsRunning = true;
            RaiseEvent( Started );
            return true;
        }

        #endregion


        #region Shutdown

        static readonly object ShutdownLock = new object();
        public static bool IsShuttingDown;
        static readonly AutoResetEvent ShutdownWaiter = new AutoResetEvent( false );
        static Thread shutdownThread;
        static ChatTimer shutdownTimer;


        static void ShutdownNow( [NotNull] ShutdownParams shutdownParams ) {
            if( shutdownParams == null ) throw new ArgumentNullException( "shutdownParams" );
            if( IsShuttingDown ) return; // to avoid starting shutdown twice
            IsShuttingDown = true;
#if !DEBUG
            try {
#endif
                Heartbeat.HbSave();
                RaiseShutdownBeganEvent( shutdownParams );

                Scheduler.BeginShutdown();

                Logger.Log( LogType.SystemActivity,
                            "Server shutting down ({0})",
                            shutdownParams.ReasonString );

                // stop accepting new players
                if( listener != null ) {
                    listener.Stop();
                    listener = null;
                }

                // kick all players
                lock( SessionLock ) {
                    if( Sessions.Count > 0 ) {
                        foreach( Player p in Sessions ) {
                            // NOTE: kick packet delivery here is not currently guaranteed
                            p.Kick( "Server shutting down (" + shutdownParams.ReasonString + Color.White + ")", LeaveReason.ServerShutdown );
                        }
                        // increase the chances of kick packets being delivered
                        Thread.Sleep( 1000 );
                    }
                }

                // kill IRC bot
                IRC.Disconnect();

                if( WorldManager.Worlds != null ) {
                    lock( WorldManager.SyncRoot ) {
                        // unload all worlds (includes saving)
                        foreach( World world in WorldManager.Worlds ) {
                            if( world.BlockDB.IsEnabled ) world.BlockDB.Flush();
                            world.SaveMap();
                        }
                    }
                }

                if (Server.TempBans.Count() > 0)
                {
                    foreach (Player p in Server.TempBans)
                    {
                        if (p.Info.IsBanned)
                        {
                            p.Info.Unban(Player.Console, "Shutdown: Tempban cancelled", false, true);
                            Logger.Log(LogType.SystemActivity, "Unbanning {0}: Was tempbanned", p.Name);
                        }
                    }
                }

                Scheduler.EndShutdown();

                if( PlayerDB.IsLoaded ) PlayerDB.Save();
                if( IPBanList.IsLoaded ) IPBanList.Save();

                RaiseShutdownEndedEvent( shutdownParams );
#if !DEBUG
            } catch( Exception ex ) {
                Logger.LogAndReportCrash( "Error in Server.Shutdown", "800Craft", ex, true );
            }
#endif
        }


        /// <summary> Initiates the server shutdown with given parameters. </summary>
        /// <param name="shutdownParams"> Shutdown parameters </param>
        /// <param name="waitForShutdown"> If true, blocks the calling thread until shutdown is complete or cancelled. </param>
        public static void Shutdown( [NotNull] ShutdownParams shutdownParams, bool waitForShutdown ) {
            if( shutdownParams == null ) throw new ArgumentNullException( "shutdownParams" );
            lock( ShutdownLock ) {
                if( !CancelShutdown() ) return;
                shutdownThread = new Thread( ShutdownThread ) {
                    Name = "fCraft.Shutdown"
                };
                if( shutdownParams.Delay >= ChatTimer.MinDuration ) {
                    string timerMsg = String.Format( "Server {0} ({1})",
                                                     shutdownParams.Restart ? "restart" : "shutdown",
                                                     shutdownParams.ReasonString );
                    string nameOnTimer;
                    if( shutdownParams.InitiatedBy == null ) {
                        nameOnTimer = Player.Console.Name;
                    } else {
                        nameOnTimer = shutdownParams.InitiatedBy.Name;
                    }
                    shutdownTimer = ChatTimer.Start( shutdownParams.Delay, timerMsg, nameOnTimer );
                }
                shutdownThread.Start( shutdownParams );
            }
            if( waitForShutdown ) {
                ShutdownWaiter.WaitOne();
            }
        }


        /// <summary> Attempts to cancel the shutdown timer. </summary>
        /// <returns> True if a shutdown timer was cancelled, false if no shutdown is in progress.
        /// Also returns false if it's too late to cancel (shutdown has begun). </returns>
        public static bool CancelShutdown() {
            lock( ShutdownLock ) {
                if( shutdownThread != null ) {
                    if( IsShuttingDown || shutdownThread.ThreadState != ThreadState.WaitSleepJoin ) {
                        return false;
                    }
                    if( shutdownTimer != null ) {
                        shutdownTimer.Stop();
                        shutdownTimer = null;
                    }
                    ShutdownWaiter.Set();
                    shutdownThread.Abort();
                    shutdownThread = null;
                }
            }
            return true;
        }


        static void ShutdownThread( [NotNull] object obj ) {
            if( obj == null ) throw new ArgumentNullException( "obj" );
            ShutdownParams param = (ShutdownParams)obj;
            Thread.Sleep( param.Delay );
            ShutdownNow( param );
            ShutdownWaiter.Set();

            bool doRestart = (param.Restart && !HasArg( ArgKey.NoRestart ));
            string assemblyExecutable = Assembly.GetEntryAssembly().Location;

            if( Updater.RunAtShutdown && doRestart ) {
                string args = String.Format( "--restart=\"{0}\" {1}",
                                             MonoCompat.PrependMono( assemblyExecutable ),
                                             GetArgString() );

                MonoCompat.StartDotNetProcess( Paths.UpdaterFileName, args, true );

            } else if( Updater.RunAtShutdown ) {
                MonoCompat.StartDotNetProcess( Paths.UpdaterFileName, GetArgString(), true );

            } else if( doRestart ) {
                MonoCompat.StartDotNetProcess( assemblyExecutable, GetArgString(), true );
            }

            if( param.KillProcess ) {
                Process.GetCurrentProcess().Kill();
            }
        }

        #endregion


        #region Messaging / Packet Sending

        /// <summary> Broadcasts a message to all online players.
        /// Shorthand for Server.Players.Message </summary>
        public static void Message( [NotNull] string message ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            Players.Message( message );
        }


        /// <summary> Broadcasts a message to all online players.
        /// Shorthand for Server.Players.Message </summary>
        [StringFormatMethod( "message" )]
        public static void Message( [NotNull] string message, [NotNull] params object[] formatArgs ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( formatArgs == null ) throw new ArgumentNullException( "formatArgs" );
            Players.Message( message, formatArgs );
        }


        /// <summary> Broadcasts a message to all online players except one.
        /// Shorthand for Server.Players.Except(except).Message </summary>
        public static void Message( [CanBeNull] Player except, [NotNull] string message ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            Players.Except( except ).Message( message );
        }


        /// <summary> Broadcasts a message to all online players except one.
        /// Shorthand for Server.Players.Except(except).Message </summary>
        [StringFormatMethod( "message" )]
        public static void Message( [CanBeNull] Player except, [NotNull] string message, [NotNull] params object[] formatArgs ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            if( formatArgs == null ) throw new ArgumentNullException( "formatArgs" );
            Players.Except( except ).Message( message, formatArgs );
        }

        #endregion


        #region Scheduled Tasks

        // checks for incoming connections
        static SchedulerTask checkConnectionsTask;
        static TimeSpan checkConnectionsInterval = TimeSpan.FromMilliseconds( 250 );
        public static TimeSpan CheckConnectionsInterval {
            get { return checkConnectionsInterval; }
            set {
                if( value.Ticks < 0 ) throw new ArgumentException( "CheckConnectionsInterval may not be negative." );
                checkConnectionsInterval = value;
                if( checkConnectionsTask != null ) checkConnectionsTask.Interval = value;
            }
        }

        static void CheckConnections( SchedulerTask param ) {
            TcpListener listenerCache = listener;
            if( listenerCache != null && listenerCache.Pending() ) {
                try {
                    Player.StartSession( listenerCache.AcceptTcpClient() );
                } catch( Exception ex ) {
                    Logger.Log( LogType.Error,
                                "Server.CheckConnections: Could not accept incoming connection: {0}", ex );
                }
            }
        }


        // checks for idle players
        static SchedulerTask checkIdlesTask;
        static TimeSpan checkIdlesInterval = TimeSpan.FromSeconds( 30 );
        public static TimeSpan CheckIdlesInterval {
            get { return checkIdlesInterval; }
            set {
                if( value.Ticks < 0 ) throw new ArgumentException( "CheckIdlesInterval may not be negative." );
                checkIdlesInterval = value;
                if( checkIdlesTask != null ) checkIdlesTask.Interval = checkIdlesInterval;
            }
        }

        static void CheckIdles( SchedulerTask task ) {
            Player[] tempPlayerList = Players;
            for( int i = 0; i < tempPlayerList.Length; i++ ) {
                Player player = tempPlayerList[i];
                if( player.Info.Rank.IdleKickTimer <= 0 ) continue;

                if( player.IdleTime.TotalMinutes >= player.Info.Rank.IdleKickTimer ) {
                    Message( "{0}&S was kicked for being idle for {1} min",
                             player.ClassyName,
                             player.Info.Rank.IdleKickTimer );
                    string kickReason = "Idle for " + player.Info.Rank.IdleKickTimer + " minutes";
                    player.Kick( Player.Console, kickReason, LeaveReason.IdleKick, false, true, false );
                    player.ResetIdleTimer(); // to prevent kick from firing more than once
                }
            }
        }


        // collects garbage (forced collection is necessary under Mono)
        static SchedulerTask gcTask;
        static TimeSpan gcInterval = TimeSpan.FromSeconds( 60 );
        public static TimeSpan GCInterval {
            get { return gcInterval; }
            set {
                if( value.Ticks < 0 ) throw new ArgumentException( "GCInterval may not be negative." );
                gcInterval = value;
                if( gcTask != null ) gcTask.Interval = gcInterval;
            }
        }

        static void DoGC( SchedulerTask task ) {
            if( !gcRequested ) return;
            gcRequested = false;

            Process proc = Process.GetCurrentProcess();
            proc.Refresh();
            long usageBefore = proc.PrivateMemorySize64 / (1024 * 1024);

            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );

            proc.Refresh();
            long usageAfter = proc.PrivateMemorySize64 / (1024 * 1024);

            Logger.Log( LogType.Debug,
                        "Server.DoGC: Collected on schedule ({0}->{1} MB).",
                        usageBefore, usageAfter );
        }


        // shows announcements
        static void ShowRandomAnnouncement( SchedulerTask task ) {
            if( !File.Exists( Paths.AnnouncementsFileName ) ) return;
            string[] lines = File.ReadAllLines( Paths.AnnouncementsFileName );
            if( lines.Length == 0 ) return;
            string line = lines[new Random().Next( 0, lines.Length )].Trim();
            if( line.Length == 0 ) return;
            foreach( Player player in Players.Where( player => player.World != null ) ) {
                player.Message( "&R" + ReplaceTextKeywords( player, line ) );
            }
        }


        // measures CPU usage
        public static bool IsMonitoringCPUUsage { get; private set; }
        static TimeSpan cpuUsageStartingOffset;
        public static double CPUUsageTotal { get; private set; }
        public static double CPUUsageLastMinute { get; private set; }

        static TimeSpan oldCPUTime = new TimeSpan( 0 );
        static readonly TimeSpan MonitorProcessorUsageInterval = TimeSpan.FromSeconds( 30 );
        static DateTime lastMonitorTime = DateTime.UtcNow;

        static void MonitorProcessorUsage( SchedulerTask task ) {
            TimeSpan newCPUTime = Process.GetCurrentProcess().TotalProcessorTime - cpuUsageStartingOffset;
            CPUUsageLastMinute = (newCPUTime - oldCPUTime).TotalSeconds /
                                 (Environment.ProcessorCount * DateTime.UtcNow.Subtract( lastMonitorTime ).TotalSeconds);
            lastMonitorTime = DateTime.UtcNow;
            CPUUsageTotal = newCPUTime.TotalSeconds /
                            (Environment.ProcessorCount * DateTime.UtcNow.Subtract( StartTime ).TotalSeconds);
            oldCPUTime = newCPUTime;
            IsMonitoringCPUUsage = true;
        }

        #endregion


        #region Utilities

        static bool gcRequested;

        public static void RequestGC() {
            gcRequested = true;
        }


        public static bool VerifyName( [NotNull] string name, [NotNull] string hash, [NotNull] string salt ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( hash == null ) throw new ArgumentNullException( "hash" );
            if( salt == null ) throw new ArgumentNullException( "salt" );
            while( hash.Length < 32 ) {
                hash = "0" + hash;
            }
            MD5 hasher = MD5.Create();
            StringBuilder sb = new StringBuilder( 32 );
            foreach( byte b in hasher.ComputeHash( Encoding.ASCII.GetBytes( salt + name ) ) ) {
                sb.AppendFormat( "{0:x2}", b );
            }
            return sb.ToString().Equals( hash, StringComparison.OrdinalIgnoreCase );
        }


        public static int CalculateMaxPacketsPerUpdate( [NotNull] World world ) {
            if( world == null ) throw new ArgumentNullException( "world" );
            int packetsPerTick = (int)(BlockUpdateThrottling / TicksPerSecond);
            int maxPacketsPerUpdate = (int)(MaxUploadSpeed / TicksPerSecond * 128);

            int playerCount = world.Players.Length;
            if( playerCount > 0 && !world.IsFlushing ) {
                maxPacketsPerUpdate /= playerCount;
                if( maxPacketsPerUpdate > packetsPerTick ) {
                    maxPacketsPerUpdate = packetsPerTick;
                }
            } else {
                maxPacketsPerUpdate = MaxBlockUpdatesPerTick;
            }

            return maxPacketsPerUpdate;
        }


        static readonly Regex RegexIP = new Regex( @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b",
                                                   RegexOptions.Compiled );

        public static bool IsIP( [NotNull] string ipString ) {
            if( ipString == null ) throw new ArgumentNullException( "ipString" );
            return RegexIP.IsMatch( ipString );
        }



        public static void BackupData()
        {
            if (!Paths.TestDirectory("DataBackup", Paths.DataBackupDirectory, true))
            {
                Logger.Log(LogType.Error, "Unable to create a data backup.");
                return;
            }
            string backupFileName = String.Format(Paths.DataBackupFileNameFormat, DateTime.Now); // localized
            backupFileName = Path.Combine(Paths.DataBackupDirectory, backupFileName);
            using (FileStream fs = File.Create(backupFileName))
            {
                try
                {
                    string fileComment = String.Format("Backup of 800Craft data for server \"{0}\", saved on {1}",
                                                        ConfigKey.ServerName.GetString(),
                                                        DateTime.Now);
                    using (ZipStorer backupZip = ZipStorer.Create(fs, fileComment))
                    {
                        foreach (string dataFileName in Paths.DataFilesToBackup)
                        {
                            if (File.Exists(dataFileName))
                            {
                                backupZip.AddFile(ZipStorer.Compression.Deflate,
                                                   dataFileName,
                                                   dataFileName,
                                                   "");
                            }
                        }
                    }
                }
                catch (IOException)
                {
                    Logger.Log(LogType.Error, "Unable to create a data backup.");
                    return;
                }
                finally
                {
                    if (fs != null)
                        fs.Close();
                }
                Logger.Log(LogType.SystemActivity,
                            "Backed up server data to \"{0}\"",
                            backupFileName);
            }
        }


        public static string ReplaceTextKeywords( [NotNull] Player player, [NotNull] string input ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( input == null ) throw new ArgumentNullException( "input" );
            StringBuilder sb = new StringBuilder( input );
            sb.Replace( "{SERVER_NAME}", ConfigKey.ServerName.GetString() );
            sb.Replace( "{RANK}", player.Info.Rank.ClassyName );
            sb.Replace( "{PLAYER_NAME}", player.ClassyName );
            sb.Replace( "{TIME}", DateTime.Now.ToShortTimeString() ); // localized
            if( player.World == null ) {
                sb.Replace( "{WORLD}", "(No World)" );
            } else {
                sb.Replace( "{WORLD}", player.World.ClassyName );
            }
            sb.Replace( "{PLAYERS}", CountVisiblePlayers( player ).ToString() );
            sb.Replace( "{WORLDS}", WorldManager.Worlds.Length.ToString() );
            sb.Replace( "{MOTD}", ConfigKey.MOTD.GetString() );
            sb.Replace( "{VERSION}", Updater.CurrentRelease.VersionString );
            return sb.ToString();
        }



        public static string GetRandomString( int chars ) {
            RandomNumberGenerator prng = RandomNumberGenerator.Create();
            StringBuilder sb = new StringBuilder();
            byte[] oneChar = new byte[1];
            while( sb.Length < chars ) {
                prng.GetBytes( oneChar );
                if( oneChar[0] >= 48 && oneChar[0] <= 57 ||
                    oneChar[0] >= 65 && oneChar[0] <= 90 ||
                    oneChar[0] >= 97 && oneChar[0] <= 122 ) {
                    //if( oneChar[0] >= 33 && oneChar[0] <= 126 ) {
                    sb.Append( (char)oneChar[0] );
                }
            }
            return sb.ToString();
        }

        static readonly Uri IPCheckUri = new Uri( "http://checkip.dyndns.org/" );
        const int IPCheckTimeout = 30000;

        /// <summary> Checks server's external IP, as reported by checkip.dyndns.org. </summary>
        [CanBeNull]
        static IPAddress CheckExternalIP() {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create( IPCheckUri );
            request.ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint( BindIPEndPointCallback );
            request.Timeout = IPCheckTimeout;
            request.CachePolicy = new RequestCachePolicy( RequestCacheLevel.NoCacheNoStore );

            try {
                using( WebResponse response = request.GetResponse() ) {
                    // ReSharper disable AssignNullToNotNullAttribute
                    using( StreamReader responseReader = new StreamReader( response.GetResponseStream() ) ) {
                        // ReSharper restore AssignNullToNotNullAttribute
                        string responseString = responseReader.ReadToEnd();
                        int startIndex = responseString.IndexOf( ":" ) + 2;
                        int endIndex = responseString.IndexOf( '<', startIndex ) - startIndex;
                        IPAddress result;
                        if( IPAddress.TryParse( responseString.Substring( startIndex, endIndex ), out result ) ) {
                            return result;
                        } else {
                            return null;
                        }
                    }
                }
            } catch( WebException ex ) {
                Logger.Log( LogType.Warning,
                            "Could not check external IP: {0}", ex );
                return null;
            }
        }

        // Callback for setting the local IP binding. Implements System.Net.BindIPEndPoint delegate.
        public static IPEndPoint BindIPEndPointCallback( ServicePoint servicePoint, IPEndPoint remoteEndPoint, int retryCount ) {
            return new IPEndPoint( InternalIP, 0 );
        }

        #endregion


        #region Player and Session Management

        // list of registered players
        static readonly SortedDictionary<string, Player> PlayerIndex = new SortedDictionary<string, Player>();
        public static Player[] Players { get; private set; }
        static readonly object PlayerListLock = new object();

        // list of all connected sessions
        static readonly List<Player> Sessions = new List<Player>();
        static readonly object SessionLock = new object();


        // Registers a new session, and checks the number of connections from this IP.
        // Returns true if the session was registered succesfully.
        // Returns false if the max number of connections was reached.
        internal static bool RegisterSession( [NotNull] Player session ) {
            if( session == null ) throw new ArgumentNullException( "session" );
            int maxSessions = ConfigKey.MaxConnectionsPerIP.GetInt();
            lock( SessionLock ) {
                if( !session.IP.Equals( IPAddress.Loopback ) && maxSessions > 0 ) {
                    int sessionCount = 0;
                    for( int i = 0; i < Sessions.Count; i++ ) {
                        Player p = Sessions[i];
                        if( p.IP.Equals( session.IP ) ) {
                            sessionCount++;
                            if( sessionCount >= maxSessions ) {
                                return false;
                            }
                        }
                    }
                }
                Sessions.Add( session );
            }
            return true;
        }


        // Registers a player and checks if the server is full.
        // Also kicks any existing connections for this player account.
        // Returns true if player was registered succesfully.
        // Returns false if the server was full.
        internal static bool RegisterPlayer( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            // Kick other sessions with same player name
            List<Player> sessionsToKick = new List<Player>();
            lock( SessionLock ) {
                foreach( Player s in Sessions ) {
                    if( s == player ) continue;
                    if( s.Name.Equals( player.Name, StringComparison.OrdinalIgnoreCase ) ) {
                        sessionsToKick.Add( s );
                        Logger.Log( LogType.SuspiciousActivity,
                                    "Server.RegisterPlayer: Player {0} logged in twice. Ghost from {1} was kicked.",
                                    s.Name, s.IP );
                        s.Kick( "Connected from elsewhere!", LeaveReason.ClientReconnect );
                    }
                }
            }

            // Wait for other sessions to exit/unregister (if any)
            foreach( Player ses in sessionsToKick ) {
                ses.WaitForDisconnect();
            }

            // Add player to the list
            lock( PlayerListLock ) {
                if( PlayerIndex.Count >= ConfigKey.MaxPlayers.GetInt() && !player.Info.Rank.ReservedSlot ) {
                    return false;
                }
                PlayerIndex.Add( player.Name, player );
                player.HasRegistered = true;
            }
            return true;
        }


        public static string MakePlayerConnectedMessage( [NotNull] Player player, bool firstTime, [NotNull] World world ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( world == null ) throw new ArgumentNullException( "world" );
            if (firstTime){
                return String.Format("&S{0} &Sconnected, joined {1}",
                                      player.ClassyName,
                                      world.ClassyName);
            }
            //use this if you want to show original names for people with displayednames
            if (!firstTime && player.Info.DisplayedName != null){

                return String.Format("&S{0} &S({1}&S) connected again, joined {2}",
                                      player.ClassyName,
                                      player.Name,
                                      world.ClassyName);
            }else{
                return String.Format("&S{0} &Sconnected again, joined {1}",
                                      player.ClassyName,
                                      world.ClassyName);
            }
        }


        // Removes player from the list, and announced them leaving
        public static void UnregisterPlayer( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            lock( PlayerListLock ) {
                if( !player.HasRegistered ) return;
                player.Info.ProcessLogout( player );

                Logger.Log( LogType.UserActivity,
                            "{0} left the server ({1}).", player.Name, player.LeaveReason );
                if( player.HasRegistered && ConfigKey.ShowConnectionMessages.Enabled() ) {
                    Players.CanSee(player).Message("&SPlayer {0}&S {1}.",
                                                      player.ClassyName, player.Info.LeaveMsg);
                    player.Info.LeaveMsg = "left the server";
                }

                if( player.World != null ) {
                    player.World.ReleasePlayer( player );
                }
                PlayerIndex.Remove( player.Name );
                UpdatePlayerList();
            }
        }


        // Removes a session from the list
        internal static void UnregisterSession( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            lock( SessionLock ) {
                Sessions.Remove( player );
            }
        }


        internal static void UpdatePlayerList() {
            lock( PlayerListLock ) {
                Players = PlayerIndex.Values.Where( p => p.IsOnline )
                                            .OrderBy( player => player.Name )
                                            .ToArray();
                RaiseEvent( PlayerListChanged );
            }
        }


        /// <summary> Finds a player by name, using autocompletion.
        /// Returns ALL matching players, including hidden ones. </summary>
        /// <returns> An array of matches. List length of 0 means "no matches";
        /// 1 is an exact match; over 1 for multiple matches. </returns>
        public static Player[] FindPlayers( [NotNull] string name, bool raiseEvent ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            Player[] tempList = Players;
            List<Player> results = new List<Player>();
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] == null ) continue;
                if( tempList[i].Name.Equals( name, StringComparison.OrdinalIgnoreCase ) ) {
                    results.Clear();
                    results.Add( tempList[i] );
                    break;
                } else if( tempList[i].Name.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
                    results.Add( tempList[i] );
                }
            }
            if( raiseEvent ) {
                var h = SearchingForPlayer;
                if( h != null ) {
                    var e = new SearchingForPlayerEventArgs( null, name, results );
                    h( null, e );
                }
            }
            return results.ToArray();
        }


        /// <summary> Finds a player by name, using autocompletion. Does not include hidden players. </summary>
        /// <param name="player"> Player who initiated the search.
        /// Used to determine whether others are hidden or not. </param>
        /// <param name="name"> Full or partial name of the search target. </param>
        /// <param name="raiseEvent"> Whether to raise Server.SearchingForPlayer event. </param>
        /// <returns> An array of matches. List length of 0 means "no matches";
        /// 1 is an exact match; over 1 for multiple matches. </returns>
        public static Player[] FindPlayers( [NotNull] Player player, [NotNull] string name, bool raiseEvent ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( name == null ) throw new ArgumentNullException( "name" );
            if( name == "-" ) {
                if( player.LastUsedPlayerName != null ) {
                    name = player.LastUsedPlayerName;
                } else {
                    return new Player[0];
                }
            }
            player.LastUsedPlayerName = name;
            List<Player> results = new List<Player>();
            Player[] tempList = Players;
            for( int i = 0; i < tempList.Length; i++ ) {
                if( tempList[i] == null || !player.CanSee( tempList[i] ) ) continue;
                if( tempList[i].Name.Equals( name, StringComparison.OrdinalIgnoreCase ) ) {
                    results.Clear();
                    results.Add( tempList[i] );
                    break;
                } else if( tempList[i].Name.StartsWith( name, StringComparison.OrdinalIgnoreCase ) ) {
                    results.Add( tempList[i] );
                }
            }
            if( raiseEvent ) {
                var h = SearchingForPlayer;
                if( h != null ) {
                    var e = new SearchingForPlayerEventArgs( player, name, results );
                    h( null, e );
                }
            }
            if( results.Count == 1 ) {
                player.LastUsedPlayerName = results[0].Name;
            }
            return results.ToArray();
        }


        /// <summary> Find player by name using autocompletion.
        /// Returns null and prints message if none or multiple players matched.
        /// Raises Player.SearchingForPlayer event, which may modify search results. </summary>
        /// <param name="player"> Player who initiated the search. This is where messages are sent. </param>
        /// <param name="name"> Full or partial name of the search target. </param>
        /// <param name="includeHidden"> Whether to include hidden players in the search. </param>
        /// <param name="raiseEvent"> Whether to raise Server.SearchingForPlayer event. </param>
        /// <returns> Player object, or null if no player was found. </returns>
        [CanBeNull]
        public static Player FindPlayerOrPrintMatches( [NotNull] Player player, [NotNull] string name, bool includeHidden, bool raiseEvent ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( name == null ) throw new ArgumentNullException( "name" );
            if( name == "-" ) {
                if( player.LastUsedPlayerName != null ) {
                    name = player.LastUsedPlayerName;
                } else {
                    player.Message( "Cannot repeat player name: you haven't used any names yet." );
                    return null;
                }
            }
            Player[] matches;
            if( includeHidden ) {
                matches = FindPlayers( name, raiseEvent );
            } else {
                matches = FindPlayers( player, name, raiseEvent );
            }

            if( matches.Length == 0 ) {
                player.MessageNoPlayer( name );
                return null;

            } else if( matches.Length > 1 ) {
                player.MessageManyMatches( "player", matches );
                return null;

            } else {
                player.LastUsedPlayerName = matches[0].Name;
                return matches[0];
            }
        }


        /// <summary> Counts online players, optionally including hidden ones. </summary>
        public static int CountPlayers( bool includeHiddenPlayers ) {
            if( includeHiddenPlayers ) {
                return Players.Length;
            } else {
                return Players.Count( player => !player.Info.IsHidden );
            }
        }


        /// <summary> Counts online players whom the given observer can see. </summary>
        public static int CountVisiblePlayers( [NotNull] Player observer ) {
            if( observer == null ) throw new ArgumentNullException( "observer" );
            return Players.Count( observer.CanSee );
        }

        #endregion
    }


    /// <summary> Describes the circumstances of server shutdown. </summary>
    public sealed class ShutdownParams {
        public ShutdownParams( ShutdownReason reason, TimeSpan delay, bool killProcess, bool restart ) {
            Reason = reason;
            Delay = delay;
            KillProcess = killProcess;
            Restart = restart;
        }

        public ShutdownParams( ShutdownReason reason, TimeSpan delay, bool killProcess,
                               bool restart, [CanBeNull] string customReason, [CanBeNull] Player initiatedBy ) :
            this( reason, delay, killProcess, restart ) {
            customReasonString = customReason;
            InitiatedBy = initiatedBy;
        }

        public ShutdownReason Reason { get; private set; }

        readonly string customReasonString;
        [NotNull]
        public string ReasonString {
            get {
                return customReasonString ?? Reason.ToString();
            }
        }

        /// <summary> Delay before shutting down. </summary>
        public TimeSpan Delay { get; private set; }

        /// <summary> Whether 800Craft should try to forcefully kill the current process. </summary>
        public bool KillProcess { get; private set; }

        /// <summary> Whether the server is expected to restart itself after shutting down. </summary>
        public bool Restart { get; private set; }

        /// <summary> Player who initiated the shutdown. May be null or Console. </summary>
        [CanBeNull]
        public Player InitiatedBy { get; private set; }
    }


    /// <summary> Categorizes conditions that lead to server shutdowns. </summary>
    public enum ShutdownReason {
        Unknown,

        /// <summary> Use for mod- or plugin-triggered shutdowns. </summary>
        Other,

        /// <summary> InitLibrary or InitServer failed. </summary>
        FailedToInitialize,

        /// <summary> StartServer failed. </summary>
        FailedToStart,

        /// <summary> Server is restarting, usually because someone called /Restart. </summary>
        Restarting,

        /// <summary> Server has experienced a non-recoverable crash. </summary>
        Crashed,

        /// <summary> Server is shutting down, usually because someone called /Shutdown. </summary>
        ShuttingDown,

        /// <summary> Server process is being closed/killed. </summary>
        ProcessClosing
    }
}