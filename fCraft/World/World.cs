// Copyright 2009-2013 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using fCraft.MapConversion;
using JetBrains.Annotations;

namespace fCraft {

    public sealed class World : IClassy {

        /// <summary> World name (no formatting).
        /// Use WorldManager.RenameWorld() method to change this. </summary>
        [NotNull]
        public string Name { get; internal set; }

        private GrassTask _plantTask = null;
        private List<PhysScheduler> _physSchedulers = new List<PhysScheduler>();

        /// <summary> Whether the world shows up on the /Worlds list.
        /// Can be assigned directly. </summary>
        public bool IsHidden { get; set; }

        //custom
        public bool IsRealm { get; set; }

        public float VisitCount { get; set; }

        public bool RealisticEnv = false; //previously getset

        public bool ZombieGame = false;

        public int FireworkCount = 0;

        //physics
        public bool tntPhysics = false;

        public bool fireworkPhysics = false;
        public bool waterPhysics = false;
        public bool plantPhysics = false;
        public bool sandPhysics = false;
        public bool gunPhysics = false;

        //games
        //move all these tings
        public ConcurrentDictionary<String, Vector3I> blockCache = new ConcurrentDictionary<String, Vector3I>();

        public List<Player> redTeam = new List<Player>();
        public List<Player> blueTeam = new List<Player>();
        public int redScore = 0;
        public int blueScore = 0;
        public List<Action> Games;
        public bool GameOn = false;
        public GameMode gameMode = GameMode.NULL;
        public Vector3I footballPos;
        public Player[, ,] positions;

        //feeds
        public Dictionary<int, FeedData> Feeds = new Dictionary<int, FeedData>();

        public string Greeting = null;

        /// <summary> Whether this world is currently pending unload
        /// (waiting for block updates to finish processing before unloading). </summary>
        public bool IsPendingMapUnload { get; private set; }

        #region Realm State Serialization

        public string RealmXMLRootName = "RealmState";

        public XElement SaveRealmState() {
            return SaveRealmState( RealmXMLRootName );
        }

        public XElement SaveRealmState( string rootName ) {
            XElement root = new XElement( rootName );
            if ( IsRealm ) {
                root.Add( new XAttribute( "IsRealm", true ) );
            }
            return root;
        }

        public void LoadRealmState( XElement el ) {
            XAttribute temp;
            if ( ( temp = el.Attribute( "IsRealm" ) ) != null ) {
                bool isRealm;
                if ( bool.TryParse( temp.Value, out isRealm ) ) {
                    IsRealm = true;
                }
            }
        }

        #endregion Realm State Serialization

        [NotNull]
        public SecurityController AccessSecurity { get; internal set; }

        [NotNull]
        public SecurityController BuildSecurity { get; internal set; }

        public DateTime LoadedOn { get; internal set; }

        [CanBeNull]
        public string LoadedBy { get; internal set; }

        [NotNull]
        public string LoadedByClassy {
            get {
                return PlayerDB.FindExactClassyName( LoadedBy );
            }
        }

        public DateTime MapChangedOn { get; private set; }

        [CanBeNull]
        public string MapChangedBy { get; internal set; }

        [NotNull]
        public string MapChangedByClassy {
            get {
                return PlayerDB.FindExactClassyName( MapChangedBy );
            }
        }

        // used to synchronize player joining/parting with map loading/saving
        internal readonly object SyncRoot = new object();

        public BlockDB BlockDB { get; private set; }

        internal World( [NotNull] string name ) {
            if ( name == null )
                throw new ArgumentNullException( "name" );
            if ( !IsValidName( name ) ) {
                throw new ArgumentException( "Unacceptable world name." );
            }
            BlockDB = new BlockDB( this );
            AccessSecurity = new SecurityController();
            BuildSecurity = new SecurityController();
            Name = name;
            UpdatePlayerList();

            for ( int i = 0; i < Enum.GetValues( typeof( TaskCategory ) ).Length; ++i )
                _physSchedulers.Add( new PhysScheduler( this ) );
        }

        public bool TryAddLife( Life2DZone life ) {
            if ( null == life ) {
                Logger.Log( LogType.Error, "trying to add null life instance" );
                return false;
            }
            lock ( SyncRoot ) {
                if ( null == Map )
                    return false;
                if ( map.LifeZones.ContainsKey( life.Name.ToLower() ) )
                    return false;
                map.LifeZones.Add( life.Name.ToLower(), life );
                if ( !_physSchedulers[( int )TaskCategory.Life].Started )
                    _physSchedulers[( int )TaskCategory.Life].Start();
            }
            return true;
        }

        public void DeleteLife( string name ) {
            if ( string.IsNullOrWhiteSpace( name ) ) {
                Logger.Log( LogType.Error, "null or empty string in deleting life" );
                return;
            }
            lock ( SyncRoot ) {
                if ( null == Map )
                    return;
                map.LifeZones.Remove( name.ToLower() );
                if ( map.LifeZones.Count <= 0 )
                    _physSchedulers[( int )TaskCategory.Life].Stop();
            }
        }

        public Life2DZone GetLife( string name ) {
            if ( string.IsNullOrWhiteSpace( name ) ) {
                Logger.Log( LogType.Error, "null or empty string in GetLife" );
                return null;
            }
            lock ( SyncRoot ) {
                if ( null == Map )
                    return null;
                Life2DZone life = null;
                map.LifeZones.TryGetValue( name.ToLower(), out life );
                return life;
            }
        }

        public IEnumerable<Life2DZone> GetLifes() {
            lock ( SyncRoot ) {
                return null == Map ? null : Map.LifeZones.Values.ToList();
            }
        }

        #region Physics

        internal void StartScheduler( TaskCategory cat ) {
            try {
                if ( !_physSchedulers[( int )cat].Started )
                    _physSchedulers[( int )cat].Start();
            } catch ( Exception e ) {
                Logger.Log( LogType.Error, "PhySched: " + e );
            }
        }

        internal void AddTask( TaskCategory cat, PhysicsTask task, int Delay ) {
            try {
                _physSchedulers[( int )cat].AddTask( task, Delay );
            } catch ( Exception e ) {
                Logger.Log( LogType.Error, "PhySchedAddTask: " + e );
            }
        }

        internal void AddPhysicsTask( PhysicsTask task, int Delay ) {
            try {
                _physSchedulers[( int )TaskCategory.Physics].AddTask( task, Delay );
            } catch ( Exception e ) {
                Logger.Log( LogType.Error, "AddPhySchedTask: " + e );
            }
        }

        #region Plant

        internal void AddPlantTask( short x, short y, short z ) {
            AddPhysicsTask( new PlantTask( this, x, y, z ), PlantTask.GetRandomDelay() );
        }

        public void EnablePlantPhysics( Player player, bool announce ) {
            try {
                if ( null != _plantTask ) {
                    player.Message( "&WAlready enabled on this world" );
                    return;
                }
                plantPhysics = true;
                CheckIfPhysicsStarted();
                bool NotLoaded = false;
                if ( !this.IsLoaded ) { this.LoadMap(); NotLoaded = true; }
                _plantTask = new GrassTask( this );
                AddPhysicsTask( _plantTask, 0 );
                if ( announce )
                    Server.Message( "{0}&S enabled Plant Physics on {1}", player.ClassyName, ClassyName );
                if ( NotLoaded ) { this.UnloadMap( true ); }
            } catch ( Exception e ) {
                Logger.Log( LogType.Error, "PhyStartGrassPhysics: " + e );
            }
        }

        public void DisablePlantPhysics( Player player, bool announce ) {
            try {
                if ( null == _plantTask ) {
                    player.Message( "&WAlready disabled on this world" );
                    return;
                }
                _plantTask.Deleted = true;
                _plantTask = null;
                CheckIfToStopPhysics();
                plantPhysics = false;
                if ( announce )
                    Server.Message( "{0}&S disabled Plant Physics on {1}", player.ClassyName, ClassyName );
            } catch ( Exception e ) {
                Logger.Log( LogType.Error, "PhyStopGrassPhysics: " + e );
            }
        }

        #endregion Plant

        #region TNT

        public void EnableTNTPhysics( Player player, bool announce ) {
            try {
                if ( tntPhysics == true ) {
                    player.Message( "&WAlready enabled on this world" );
                    return;
                }
                CheckIfPhysicsStarted();
                tntPhysics = true;
                if ( announce )
                    Server.Message( "{0}&S enabled TNT Physics on {1}", player.ClassyName, ClassyName );
            } catch ( Exception e ) {
                Logger.Log( LogType.Error, "PhyStartTNTPhysics: " + e );
            }
        }

        public void DisableTNTPhysics( Player player, bool announce ) {
            try {
                if ( tntPhysics == false ) {
                    player.Message( "&WAlready disabled on this world" );
                    return;
                }
                tntPhysics = false;
                CheckIfToStopPhysics();
                if ( announce )
                    Server.Message( "{0}&S disabled TNT Physics on {1}", player.ClassyName, ClassyName );
            } catch ( Exception e ) {
                Logger.Log( LogType.Error, "PhyStopTNTPhysics: " + e );
            }
        }

        #endregion TNT

        #region Fireworks

        public void EnableFireworkPhysics( Player player, bool announce ) {
            try {
                if ( fireworkPhysics == true ) {
                    player.Message( "&WAlready enabled on this world" );
                    return;
                }
                CheckIfPhysicsStarted();
                fireworkPhysics = true;
                if ( announce )
                    Server.Message( "{0}&S enabled Firework Physics on {1}", player.ClassyName, ClassyName );
            } catch ( Exception e ) {
                Logger.Log( LogType.Error, "PhyStartFireworkPhysics: " + e );
            }
        }

        public void DisableFireworkPhysics( Player player, bool announce ) {
            try {
                if ( fireworkPhysics == false ) {
                    player.Message( "&WAlready disabled on this world" );
                    return;
                }
                fireworkPhysics = false;
                CheckIfToStopPhysics();
                if ( announce )
                    Server.Message( "{0}&S disabled Firework Physics on {1}", player.ClassyName, ClassyName );
            } catch ( Exception e ) {
                Logger.Log( LogType.Error, "PhyStopFireworkPhysics: " + e );
            }
        }

        #endregion Fireworks

        #region Gun

        public void EnableGunPhysics( Player player, bool announce ) {
            try {
                if ( gunPhysics == true ) {
                    player.Message( "&WAlready enabled on this world" );
                    return;
                }
                CheckIfPhysicsStarted();
                gunPhysics = true;
                if ( announce )
                    Server.Message( "{0}&S enabled Gun Physics on {1}", player.ClassyName, ClassyName );
            } catch ( Exception e ) {
                Logger.Log( LogType.Error, "PhyStartGunPhysics: " + e );
            }
        }

        public void DisableGunPhysics( Player player, bool announce ) {
            try {
                if ( gunPhysics == false ) {
                    player.Message( "&WAlready disabled on this world" );
                    return;
                }
                gunPhysics = false;
                CheckIfToStopPhysics();
                if ( announce )
                    Server.Message( "{0}&S disabled Gun Physics on {1}", player.ClassyName, ClassyName );
            } catch ( Exception e ) {
                Logger.Log( LogType.Error, "PhyStopGunPhysics: " + e );
            }
        }

        #endregion Gun

        #region Water

        public void EnableWaterPhysics( Player player, bool announce ) {
            if ( waterPhysics == true ) {
                player.Message( "&WAlready enabled on this world" );
                return;
            }
            CheckIfPhysicsStarted();
            waterPhysics = true;
            if ( announce )
                Server.Message( "{0}&S enabled Water Physics on {1}", player.ClassyName, ClassyName );
        }

        public void DisableWaterPhysics( Player player, bool announce ) {
            if ( waterPhysics == false ) {
                player.Message( "&WAlready disabled on this world" );
                return;
            }
            waterPhysics = false;
            CheckIfToStopPhysics();
            if ( announce )
                Server.Message( "{0}&S disabled Water Physics on {1}", player.ClassyName, ClassyName );
        }

        #endregion Water

        #region Sand

        public void EnableSandPhysics( Player player, bool announce ) {
            if ( sandPhysics == true ) {
                player.Message( "&WAlready enabled on this world" );
                return;
            }
            CheckIfPhysicsStarted();
            sandPhysics = true;
            if ( announce )
                Server.Message( "{0}&S enabled Sand Physics on {1}", player.ClassyName, ClassyName );
        }

        public void DisableSandPhysics( Player player, bool announce ) {
            if ( sandPhysics == false ) {
                player.Message( "&WAlready disabled on this world" );
                return;
            }
            sandPhysics = false;
            CheckIfToStopPhysics();
            if ( announce )
                Server.Message( "{0}&S disabled Sand Physics on {1}", player.ClassyName, ClassyName );
        }

        #endregion Sand

        private void CheckIfPhysicsStarted() {
            if ( !_physSchedulers[( int )TaskCategory.Physics].Started )
                _physSchedulers[( int )TaskCategory.Physics].Start();
        }

        private void CheckIfToStopPhysics() {
            if ( null == _plantTask && !tntPhysics
                && !gunPhysics && !plantPhysics
                && !sandPhysics && !fireworkPhysics
                && !waterPhysics )  //must be extended to further phys types
                _physSchedulers[( int )TaskCategory.Physics].Stop();
        }

        private void StopSchedulers() {
            foreach ( PhysScheduler sch in _physSchedulers )
                sch.Stop();
        }

        private void AddTasksFromNewMap() {
            //assuming lock(SyncRoot)
            foreach ( Life2DZone life in map.LifeZones.Values )
                life.Resume();
            if ( map.LifeZones.Count > 0 )
                _physSchedulers[( int )TaskCategory.Life].Start();
        }

        #endregion Physics

        #region Map

        /// <summary> Map of this world. May be null if world is not loaded. </summary>
        [CanBeNull]
        public Map Map {
            get { return map; }
            set {
                bool changed = !ReferenceEquals( map, value );
                if ( map != null && value == null )
                    StopTasks();
                if ( map == null && value != null )
                    StartTasks();
                if ( value != null )
                    value.World = this;
                map = value;

                if ( changed ) {
                    if ( null == map )
                        StopSchedulers();
                    else
                        AddTasksFromNewMap();
                }
            }
        }

        public Map map;

        /// <summary> Whether the map is currently loaded. </summary>
        public bool IsLoaded {
            get { return Map != null; }
        }

        /// <summary> Loads the map file, if needed.
        /// Generates a default map if mapfile is missing or not loadable.
        /// Guaranteed to return a Map object. </summary>
        [NotNull]
        public Map LoadMap() {
            var tempMap = Map;
            if ( tempMap != null )
                return tempMap;

            lock ( SyncRoot ) {
                if ( Map != null )
                    return Map;

                if ( File.Exists( MapFileName ) ) {
                    try {
                        Map = MapUtility.Load( MapFileName );
                    } catch ( Exception ex ) {
                        Logger.Log( LogType.Error,
                                    "World.LoadMap: Failed to load map ({0}): {1}",
                                    MapFileName, ex );
                    }
                }

                // or generate a default one
                if ( Map == null ) {
                    Server.Message( "&WMapfile is missing for world {0}&W. A new map has been created.", ClassyName );
                    Logger.Log( LogType.Warning,
                                "World.LoadMap: Map file missing for world {0}. Generating default flatgrass map.",
                                Name );
                    Map = MapGenerator.GenerateFlatgrass( 128, 128, 64 );
                }
                return Map;
            }
        }

        internal void UnloadMap( bool expectedPendingFlag ) {
            lock ( SyncRoot ) {
                if ( expectedPendingFlag != IsPendingMapUnload )
                    return;
                SaveMap();
                Map = null;
                IsPendingMapUnload = false;
            }
            Server.RequestGC();
        }

        /// <summary> Returns the map filename, including MapPath. </summary>
        public string MapFileName {
            get {
                return Path.Combine( Paths.MapPath, Name + ".fcm" );
            }
        }

        public void SaveMap() {
            lock ( SyncRoot ) {
                if ( Map != null ) {
                    Map.Save( MapFileName );
                }
            }
        }

        public void ChangeMap( [NotNull] Map newMap ) {
            if ( newMap == null )
                throw new ArgumentNullException( "newMap" );
            MapChangedOn = DateTime.UtcNow;
            lock ( SyncRoot ) {
                World newWorld = new World( Name ) {
                    AccessSecurity = ( SecurityController )AccessSecurity.Clone(),
                    BuildSecurity = ( SecurityController )BuildSecurity.Clone(),
                    IsHidden = IsHidden,
                    IsRealm = IsRealm,
                    fireworkPhysics = fireworkPhysics,
                    plantPhysics = plantPhysics,
                    waterPhysics = waterPhysics,
                    tntPhysics = tntPhysics,
                    gunPhysics = gunPhysics,
                    sandPhysics = sandPhysics,
                    BlockDB = BlockDB,
                    Terrain = Terrain,
                    lastBackup = lastBackup,
                    BackupInterval = BackupInterval,
                    IsLocked = IsLocked,
                    LockedBy = LockedBy,
                    UnlockedBy = UnlockedBy,
                    LockedDate = LockedDate,
                    UnlockedDate = UnlockedDate,
                    LoadedBy = LoadedBy,
                    LoadedOn = LoadedOn,
                    MapChangedBy = MapChangedBy,
                    MapChangedOn = MapChangedOn,
                    FogColor = FogColor,
                    CloudColor = CloudColor,
                    SkyColor = SkyColor,
                    EdgeLevel = EdgeLevel,
                    SideBlock = SideBlock,
                    EdgeBlock = EdgeBlock
                };
                newWorld.Map = newMap;
                newWorld.NeverUnload = neverUnload;
                WorldManager.ReplaceWorld( this, newWorld );
                using ( BlockDB.GetWriteLock() ) {
                    BlockDB.Clear();
                    BlockDB.World = newWorld;
                }
                foreach ( Player player in Players ) {
                    player.JoinWorld( newWorld, WorldChangeReason.Rejoin );
                }
            }
        }

        private bool neverUnload;

        public bool NeverUnload {
            get {
                return neverUnload;
            }
            set {
                lock ( SyncRoot ) {
                    if ( neverUnload == value )
                        return;
                    neverUnload = value;
                    if ( neverUnload ) {
                        if ( Map == null )
                            LoadMap();
                    } else {
                        if ( Map != null && playerIndex.Count == 0 )
                            UnloadMap( false );
                    }
                }
            }
        }

        #endregion Map

        #region Flush

        public bool IsFlushing { get; private set; }

        public void Flush() {
            lock ( SyncRoot ) {
                if ( Map == null )
                    return;
                Players.Message( "&WMap is being flushed. Stay put, world will reload shortly." );
                IsFlushing = true;
            }
        }

        internal void EndFlushMapBuffer() {
            lock ( SyncRoot ) {
                IsFlushing = false;
                Players.Message( "&WMap flushed. Reloading..." );
                foreach ( Player player in Players ) {
                    player.JoinWorld( this, WorldChangeReason.Rejoin, player.Position );
                }
            }
        }

        #endregion Flush

        #region PlayerList

        private readonly Dictionary<string, Player> playerIndex = new Dictionary<string, Player>();

        public Player[] Players { get; private set; }

        [CanBeNull]
        public Map AcceptPlayer( [NotNull] Player player, bool announce ) {
            if ( player == null )
                throw new ArgumentNullException( "player" );

            lock ( SyncRoot ) {
                if ( IsFull ) {
                    if ( player.Info.Rank.ReservedSlot ) {
                        Player idlestPlayer = Players.Where( p => p.Info.Rank.IdleKickTimer != 0 )
                                                     .OrderBy( p => p.LastActiveTime )
                                                     .FirstOrDefault();
                        if ( idlestPlayer != null ) {
                            idlestPlayer.Kick( Player.Console, "Auto-kicked to make room (idle).",
                                               LeaveReason.IdleKick, false, false, false );

                            Server.Players
                                  .CanSee( player )
                                  .Message( "&SPlayer {0}&S was auto-kicked to make room for {1}",
                                            idlestPlayer.ClassyName, player.ClassyName );
                            Server.Players
                                  .CantSee( player )
                                  .Message( "{0}&S was kicked for being idle for {1} min",
                                            player.ClassyName, player.Info.Rank.IdleKickTimer );
                        } else {
                            return null;
                        }
                    } else {
                        return null;
                    }
                }

                if ( playerIndex.ContainsKey( player.Name.ToLower() ) ) {
                    Logger.Log( LogType.Error,
                                "This world already contains the player by name ({0}). " +
                                "Some sort of state corruption must have occured.",
                                player.Name );
                    playerIndex.Remove( player.Name.ToLower() );
                }

                playerIndex.Add( player.Name.ToLower(), player );

                // load the map, if it's not yet loaded
                IsPendingMapUnload = false;
                Map = LoadMap();

                if ( ConfigKey.BackupOnJoin.Enabled() && ( Map.HasChangedSinceBackup || !ConfigKey.BackupOnlyWhenChanged.Enabled() ) ) {
                    string backupFileName = String.Format( JoinBackupFormat,
                                                         Name, DateTime.Now, player.Name ); // localized
                    SaveBackup( MapFileName );
                }

                UpdatePlayerList();

                if ( !IsRealm && announce && ConfigKey.ShowJoinedWorldMessages.Enabled() ) {
                    Server.Players.CanSee( player )
                                  .Message( "&SPlayer {0}&S joined world {1}",
                                            player.ClassyName, ClassyName );
                }

                //realm joining announcer
                if ( IsRealm && announce && ConfigKey.ShowJoinedWorldMessages.Enabled() ) {
                    Server.Players.CanSee( player )
                                  .Message( "&SPlayer {0}&S joined realm {1}",
                                            player.ClassyName, ClassyName );
                }

                if ( IsRealm ) {
                    Logger.Log( LogType.ChangedWorld,
                    "Player {0} joined realm {1}.",
                    player.Name, Name );
                }

                if ( !IsRealm ) {
                    Logger.Log( LogType.ChangedWorld,
                    "Player {0} joined world {1}.",
                    player.Name, Name );
                }

                if ( IsLocked ) {
                    player.Message( "&WThis map is currently locked (read-only)." );
                }

                if ( player.Info.IsHidden ) {
                    player.Message( "&8Reminder: You are still hidden." );
                }

                return Map;
            }
        }

        public bool ReleasePlayer( [NotNull] Player player ) {
            if ( player == null )
                throw new ArgumentNullException( "player" );
            lock ( SyncRoot ) {
                if ( !playerIndex.Remove( player.Name.ToLower() ) ) {
                    return false;
                }

                // clear undo & selection
                player.LastDrawOp = null;
                player.UndoClear();
                player.RedoClear();
                player.IsRepeatingSelection = false;
                player.SelectionCancel();

                // update player list
                UpdatePlayerList();

                // unload map (if needed)
                if ( playerIndex.Count == 0 && !neverUnload ) {
                    IsPendingMapUnload = true;
                }
                return true;
            }
        }

        public Player[] FindPlayers( [NotNull] Player player, [NotNull] string playerName ) {
            if ( player == null )
                throw new ArgumentNullException( "player" );
            if ( playerName == null )
                throw new ArgumentNullException( "playerName" );
            Player[] tempList = Players;
            List<Player> results = new List<Player>();
            for ( int i = 0; i < tempList.Length; i++ ) {
                if ( tempList[i] != null && player.CanSee( tempList[i] ) ) {
                    if ( tempList[i].Name.Equals( playerName, StringComparison.OrdinalIgnoreCase ) ) {
                        results.Clear();
                        results.Add( tempList[i] );
                        break;
                    } else if ( tempList[i].Name.StartsWith( playerName, StringComparison.OrdinalIgnoreCase ) ) {
                        results.Add( tempList[i] );
                    }
                }
            }
            return results.ToArray();
        }

        /// <summary> Gets player by name (without autocompletion) </summary>
        [CanBeNull]
        public Player FindPlayerExact( [NotNull] string playerName ) {
            if ( playerName == null )
                throw new ArgumentNullException( "playerName" );
            Player[] tempList = Players;
            // ReSharper disable LoopCanBeConvertedToQuery
            for ( int i = 0; i < tempList.Length; i++ ) {
                if ( tempList[i] != null && tempList[i].Name.Equals( playerName, StringComparison.OrdinalIgnoreCase ) ) {
                    return tempList[i];
                }
            }
            // ReSharper restore LoopCanBeConvertedToQuery
            return null;
        }

        /// <summary> Caches the player list to an array (Players -> PlayerList) </summary>
        public void UpdatePlayerList() {
            lock ( SyncRoot ) {
                Players = playerIndex.Values.ToArray();
            }
        }

        /// <summary> Counts all players (optionally includes all hidden players). </summary>
        public int CountPlayers( bool includeHiddenPlayers ) {
            if ( includeHiddenPlayers ) {
                return Players.Length;
            } else {
                return Players.Count( player => !player.Info.IsHidden );
            }
        }

        /// <summary> Counts only the players who are not hidden from a given observer. </summary>
        public int CountVisiblePlayers( [NotNull] Player observer ) {
            if ( observer == null )
                throw new ArgumentNullException( "observer" );
            return Players.Count( observer.CanSee );
        }

        public bool IsFull {
            get {
                return ( Players.Length >= ConfigKey.MaxPlayersPerWorld.GetInt() );
            }
        }

        #endregion PlayerList

        #region Lock / Unlock

        /// <summary> Whether the world is currently locked (in read-only mode). </summary>
        public bool IsLocked { get; private set; }

        public string LockedBy, UnlockedBy;
        public DateTime LockedDate, UnlockedDate;

        private readonly object lockLock = new object();

        public bool Lock( [NotNull] Player player ) {
            if ( player == null )
                throw new ArgumentNullException( "player" );
            lock ( lockLock ) {
                if ( IsLocked ) {
                    return false;
                } else {
                    LockedBy = player.Name;
                    LockedDate = DateTime.UtcNow;
                    IsLocked = true;
                    Map mapCache = Map;
                    if ( mapCache != null ) {
                        mapCache.ClearUpdateQueue();
                        mapCache.StopAllDrawOps();
                    }
                    Players.Message( "&WWorld was locked by {0}", player.ClassyName );
                    Logger.Log( LogType.UserActivity,
                                "World {0} was locked by {1}",
                                Name, player.Name );
                    return true;
                }
            }
        }

        public bool Unlock( [NotNull] Player player ) {
            if ( player == null )
                throw new ArgumentNullException( "player" );
            lock ( lockLock ) {
                if ( IsLocked ) {
                    UnlockedBy = player.Name;
                    UnlockedDate = DateTime.UtcNow;
                    IsLocked = false;
                    Players.Message( "&WMap was unlocked by {0}", player.ClassyName );
                    Logger.Log( LogType.UserActivity,
                                "World \"{0}\" was unlocked by {1}",
                                Name, player.Name );
                    return true;
                } else {
                    return false;
                }
            }
        }

        #endregion Lock / Unlock

        #region Backups

        private DateTime lastBackup = DateTime.UtcNow;
        private static readonly object BackupLock = new object();

        /// <summary> Whether timed backups are enabled (either manually or by default) on this world. </summary>
        public bool BackupsEnabled {
            get {
                switch ( BackupEnabledState ) {
                    case YesNoAuto.Yes:
                        return BackupInterval > TimeSpan.Zero;
                    case YesNoAuto.No:
                        return false;
                    default: //case YesNoAuto.Auto:
                        return DefaultBackupsEnabled;
                }
            }
        }

        /// <summary> Whether the map was saved since last time it was backed up. </summary>
        public bool HasChangedSinceBackup { get; set; }

        /// <summary> Backup state. Use "Yes" to enable, "No" to disable, and "Auto" to use default settings.
        /// If setting to "Yes", make sure to set BackupInterval property value first. </summary>
        public YesNoAuto BackupEnabledState {
            get { return backupEnabledState; }
            set {
                lock ( BackupLock ) {
                    if ( value == backupEnabledState )
                        return;
                    if ( value == YesNoAuto.Yes && backupInterval <= TimeSpan.Zero ) {
                        throw new InvalidOperationException( "To set BackupEnabledState to 'Yes,' set BackupInterval to the desired time interval." );
                    }
                    backupEnabledState = value;
                }
            }
        }

        private YesNoAuto backupEnabledState = YesNoAuto.Auto;

        /// <summary> Timed backup interval.
        /// If BackupEnabledState is set to "Yes", value must be positive.
        /// If BackupEnabledState is set to "No" or "Auto", this property has no effect. </summary>
        public TimeSpan BackupInterval {
            get {
                switch ( backupEnabledState ) {
                    case YesNoAuto.Yes:
                        return backupInterval;
                    case YesNoAuto.No:
                        return TimeSpan.Zero;
                    default: // case YesNoAuto.Auto:
                        return WorldManager.DefaultBackupInterval;
                }
            }
            set {
                lock ( BackupLock ) {
                    backupInterval = value;
                    if ( value > TimeSpan.Zero ) {
                        BackupEnabledState = YesNoAuto.Yes;
                    } else {
                        BackupEnabledState = YesNoAuto.No;
                    }
                }
            }
        }

        private TimeSpan backupInterval;

        /// <summary> Whether timed backups are enabled by default for worlds that have BackupEnabledState set to "Auto". </summary>
        public static bool DefaultBackupsEnabled {
            get { return WorldManager.DefaultBackupInterval > TimeSpan.Zero; }
        }

        internal string BackupSettingDescription {
            get {
                switch ( backupEnabledState ) {
                    case YesNoAuto.No:
                        return "disabled (manual)";
                    case YesNoAuto.Yes:
                        return String.Format( "every {0} (manual)", backupInterval.ToMiniString() );
                    default: //case YesNoAuto.Auto:
                        if ( DefaultBackupsEnabled ) {
                            return String.Format( "every {0} (default)", WorldManager.DefaultBackupInterval.ToMiniString() );
                        } else {
                            return "disabled (default)";
                        }
                }
            }
        }

        /// <summary> Makes a copy of the current map file associated with this world.
        /// This does NOT save map to disk, and does NOT guarantee that the most up-to-date copy of the map was backed up. </summary>
        /// <param name="targetName"> Target file name. </param>
        /// <returns> Whether a backup was created or not. </returns>
        /// <exception cref="ArgumentNullException"> targetName is null. </exception>
        public bool SaveBackup( [NotNull] string targetName ) {
            if ( targetName == null )
                throw new ArgumentNullException( "targetName" );

            if ( !File.Exists( MapFileName ) )
                return false;
            lock ( BackupLock ) {
                DirectoryInfo directory = new DirectoryInfo( Paths.BackupPath );

                if ( !directory.Exists ) {
                    try {
                        directory.Create();
                    } catch ( Exception ex ) {
                        Logger.Log( LogType.Error,
                                    "Map.SaveBackup: Error occurred while trying to create backup directory: {0}", ex );
                        return false;
                    }
                }

                try {
                    HasChangedSinceBackup = false;
                    File.Copy( MapFileName, targetName, true );
                } catch ( Exception ex ) {
                    HasChangedSinceBackup = true;
                    Logger.Log( LogType.Error,
                                "Map.SaveBackup: Error occurred while trying to save backup to \"{0}\": {1}",
                                targetName, ex );
                    return false;
                }

                if ( ConfigKey.MaxBackups.GetInt() > 0 || ConfigKey.MaxBackupSize.GetInt() > 0 ) {
                    DeleteOldBackups( directory );
                }
            }

            Logger.Log( LogType.SystemActivity,
                        "Saved a backup of world {0} to \"{1}\"", Name, targetName );
            return true;
        }

        private static void DeleteOldBackups( [NotNull] DirectoryInfo directory ) {
            if ( directory == null )
                throw new ArgumentNullException( "directory" );
            var backupList = directory.GetFiles( "*.fcm" ).OrderBy( fi => -fi.CreationTimeUtc.Ticks ).ToList();

            int maxFileCount = ConfigKey.MaxBackups.GetInt();

            if ( maxFileCount > 0 ) {
                while ( backupList.Count > maxFileCount ) {
                    FileInfo info = backupList[backupList.Count - 1];
                    backupList.RemoveAt( backupList.Count - 1 );
                    try {
                        File.Delete( info.FullName );
                    } catch ( Exception ex ) {
                        Logger.Log( LogType.Error,
                                    "Map.SaveBackup: Error occurred while trying delete old backup \"{0}\": {1}",
                                    info.FullName, ex );
                        break;
                    }
                    Logger.Log( LogType.SystemActivity,
                                "Map.SaveBackup: Deleted old backup \"{0}\"", info.Name );
                }
            }

            int maxFileSize = ConfigKey.MaxBackupSize.GetInt();

            if ( maxFileSize > 0 ) {
                while ( true ) {
                    FileInfo[] fis = directory.GetFiles();
                    long size = fis.Sum( fi => fi.Length );

                    if ( size / 1024 / 1024 > maxFileSize ) {
                        FileInfo info = backupList[backupList.Count - 1];
                        backupList.RemoveAt( backupList.Count - 1 );
                        try {
                            File.Delete( info.FullName );
                        } catch ( Exception ex ) {
                            Logger.Log( LogType.Error,
                                        "Map.SaveBackup: Error occurred while trying delete old backup \"{0}\": {1}",
                                        info.Name, ex );
                            break;
                        }
                        Logger.Log( LogType.SystemActivity,
                                    "Map.SaveBackup: Deleted old backup \"{0}\"", info.Name );
                    } else {
                        break;
                    }
                }
            }
        }

        #endregion Backups

        #region Patrol

        private readonly object patrolLock = new object();
        private static readonly TimeSpan MinPatrolInterval = TimeSpan.FromSeconds( 20 );

        public Player GetNextPatrolTarget( [NotNull] Player observer ) {
            if ( observer == null )
                throw new ArgumentNullException( "observer" );
            lock ( patrolLock ) {
                Player candidate = Players.RankedAtMost( RankManager.PatrolledRank )
                                          .CanBeSeen( observer )
                                          .Where( p => p.LastActiveTime > p.LastPatrolTime &&
                                                       p.HasFullyConnected &&
                                                       DateTime.UtcNow.Subtract( p.LastPatrolTime ) > MinPatrolInterval )
                                          .OrderBy( p => p.LastPatrolTime.Ticks )
                                          .FirstOrDefault();
                if ( candidate != null ) {
                    candidate.LastPatrolTime = DateTime.UtcNow;
                }
                return candidate;
            }
        }

        public Player GetNextPatrolTarget( [NotNull] Player observer,
                                           [NotNull] Predicate<Player> predicate,
                                           bool setLastPatrolTime ) {
            if ( observer == null )
                throw new ArgumentNullException( "observer" );
            if ( predicate == null )
                throw new ArgumentNullException( "predicate" );
            lock ( patrolLock ) {
                Player candidate = Players.RankedAtMost( RankManager.PatrolledRank )
                                          .CanBeSeen( observer )
                                          .Where( p => p.LastActiveTime > p.LastPatrolTime &&
                                                       p.HasFullyConnected &&
                                                       DateTime.UtcNow.Subtract( p.LastPatrolTime ) > MinPatrolInterval )
                                          .Where( p => predicate( p ) )
                                          .OrderBy( p => p.LastPatrolTime.Ticks )
                                          .FirstOrDefault();
                if ( setLastPatrolTime && candidate != null ) {
                    candidate.LastPatrolTime = DateTime.UtcNow;
                }
                return candidate;
            }
        }

        #endregion Patrol

        #region Scheduled Tasks

        private SchedulerTask updateTask, saveTask;
        private readonly object taskLock = new object();

        private void StopTasks() {
            lock ( taskLock ) {
                if ( updateTask != null ) {
                    updateTask.Stop();
                    updateTask = null;
                }
                if ( saveTask != null ) {
                    saveTask.Stop();
                    saveTask = null;
                }
            }
        }

        private void StartTasks() {
            lock ( taskLock ) {
                updateTask = Scheduler.NewTask( UpdateTask );
                updateTask.RunForever( this,
                                       TimeSpan.FromMilliseconds( ConfigKey.TickInterval.GetInt() ),
                                       TimeSpan.Zero );

                if ( ConfigKey.SaveInterval.GetInt() > 0 ) {
                    saveTask = Scheduler.NewBackgroundTask( SaveTask );
                    saveTask.RunForever( this,
                                         TimeSpan.FromSeconds( ConfigKey.SaveInterval.GetInt() ),
                                         TimeSpan.FromSeconds( ConfigKey.SaveInterval.GetInt() ) );
                }
            }
        }

        private void UpdateTask( SchedulerTask task ) {
            Map tempMap = Map;
            if ( tempMap != null ) {
                tempMap.ProcessUpdates();
            }
        }

        private const string TimedBackupFormat = "{0}_{1:yyyy-MM-dd_HH-mm}.fcm",
                     JoinBackupFormat = "{0}_{1:yyyy-MM-dd_HH-mm}_{2}.fcm";

        private void SaveTask( SchedulerTask task ) {
            if ( !IsLoaded )
                return;
            lock ( SyncRoot ) {
                if ( Map == null )
                    return;

                lock ( BackupLock ) {
                    if ( BackupsEnabled &&
                        DateTime.UtcNow.Subtract( lastBackup ) > BackupInterval &&
                        ( HasChangedSinceBackup || !ConfigKey.BackupOnlyWhenChanged.Enabled() ) ) {
                        string backupFileName = String.Format( TimedBackupFormat, Name, DateTime.Now ); // localized
                        SaveBackup( Path.Combine( Paths.BackupPath, backupFileName ) );
                        lastBackup = DateTime.UtcNow;
                    }
                }

                if ( Map.HasChangedSinceSave ) {
                    SaveMap();
                }

                if ( BlockDB.IsEnabledGlobally && BlockDB.IsEnabled ) {
                    BlockDB.Flush( true );
                }
            }
        }

        #endregion Scheduled Tasks

        #region WoM Extensions

        public int CloudColor = -1,
                   FogColor = -1,
                   SkyColor = -1,
                   EdgeLevel = -1;

        public string Terrain { get; set; }

        public Block EdgeBlock = Block.Water;
        public Block SideBlock = Block.Admincrete;

        public string GenerateWoMConfig( bool sendMotd ) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine( "server.name = " + ConfigKey.ServerName.GetString() );
            if ( sendMotd ) {
                sb.AppendLine( "server.detail = " + ConfigKey.MOTD.GetString() );
            } else {
                sb.AppendLine( "server.detail = " + ClassyName );
            }
            sb.AppendLine( "user.detail = World " + ClassyName );
            if ( CloudColor > -1 )
                sb.AppendLine( "environment.cloud = " + CloudColor );
            if ( FogColor > -1 )
                sb.AppendLine( "environment.fog = " + FogColor );
            if ( SkyColor > -1 )
                sb.AppendLine( "environment.sky = " + SkyColor );
            if ( EdgeLevel > -1 )
                sb.AppendLine( "environment.level = " + EdgeLevel );
            if ( Terrain != null )
                sb.AppendLine( "environment.terrain = " + Terrain );
            if ( EdgeBlock != Block.Water ) {
                string edgeTexture = Map.GetEdgeTexture( EdgeBlock );
                if ( edgeTexture != null ) {
                    sb.AppendLine( "environment.edge = " + edgeTexture );
                }
            }
            if ( SideBlock != Block.Admincrete ) {
                string sideTexture = Map.GetEdgeTexture( SideBlock );
                if ( sideTexture != null ) {
                    sb.AppendLine( "environment.side = " + sideTexture );
                }
            }
            sb.AppendLine( "server.sendwomid = true" );
            return sb.ToString();
        }

        #endregion WoM Extensions

        /// <summary> Ensures that player name has the correct length (2-16 characters)
        /// and character set (alphanumeric chars and underscores allowed). </summary>
        public static bool IsValidName( [NotNull] string name ) {
            if ( name == null )
                throw new ArgumentNullException( "name" );
            if ( name.Length < 2 || name.Length > 16 )
                return false;
            // ReSharper disable LoopCanBeConvertedToQuery
            for ( int i = 0; i < name.Length; i++ ) {
                char ch = name[i];
                if ( ch != '-' ) {
                    if ( ch < '0' ||
                        ch > '9' && ch < 'A' ||
                        ch > 'Z' && ch < '_' ||
                        ch > '_' && ch < 'a' ||
                        ch > 'z' ) {
                        return false;
                    }
                }
            }
            // ReSharper restore LoopCanBeConvertedToQuery
            return true;
        }

        /// <summary> Returns a nicely formatted name, with optional color codes. </summary>
        public string ClassyName {
            get {
                if ( ConfigKey.RankColorsInWorldNames.Enabled() ) {
                    Rank maxRank;
                    if ( BuildSecurity.MinRank >= AccessSecurity.MinRank ) {
                        maxRank = BuildSecurity.MinRank;
                    } else {
                        maxRank = AccessSecurity.MinRank;
                    }
                    if ( ConfigKey.RankPrefixesInChat.Enabled() ) {
                        return maxRank.Color + maxRank.Prefix + Name;
                    } else {
                        return maxRank.Color + Name;
                    }
                } else {
                    return Name;
                }
            }
        }

        public override string ToString() {
            return String.Format( "World({0})", Name );
        }
    }
}