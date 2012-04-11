// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
//#define DEBUG_BLOCKDB
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    public unsafe sealed class BlockDB {
        internal BlockDB( [NotNull] World world ) {
            if( world == null ) throw new ArgumentNullException( "world" );
            World = world;
        }

        public readonly object SyncRoot = new object();

        [NotNull]
        public World World { get; internal set; }


        YesNoAuto enabledState;
        public YesNoAuto EnabledState {
            get { return enabledState; }
            set {
                lock( SyncRoot ) {
                    if( IsEnabledGlobally ) {
#if DEBUG_BLOCKDB
                        if( value != enabledState ) {
                            Logger.Log( "BlockDB({0}): Enabled={1}", LogType.Debug, World.Name, value );
                        }
#endif
                        if( value == YesNoAuto.No && IsEnabled ) {
                            // going from enabled/auto-enabled to disabled
                            Flush();
                            CacheClear();
                            IsEnabled = false;

                        } else if( !IsEnabled && (value == YesNoAuto.Yes || value == YesNoAuto.Auto && ShouldBeAutoEnabled) ) {
                            // going from disabled to enabled/auto-enabled
                            cacheStore = new BlockDBEntry[MinCacheSize];
                            if( isPreloaded ) {
                                Preload();
                            }
                            IsEnabled = true;
                        }
                    }
                    enabledState = value;
                }
            }
        }


        public bool AutoToggleIfNeeded() {
            bool oldEnabled = IsEnabled;
            EnabledState = enabledState;
            return (oldEnabled != IsEnabled);
        }


        public bool ShouldBeAutoEnabled {
            get {
                return (World.BuildSecurity.MinRank <= RankManager.BlockDBAutoEnableRank);
            }
        }


        /// <summary> Checks whether this BlockDB is enabled (either automatically or manually).
        /// Set EnabledState to enable/disable. </summary>
        public bool IsEnabled { get; private set; }


        /// <summary> Full path to the file where BlockDB data is stored. </summary>
        [NotNull]
        public string FileName {
            get {
                return Path.Combine( Paths.BlockDBPath, World.Name + ".fbdb" );
            }
        }


        #region Cache

        const int BufferSize = 64 * 1024; // 64 KB (at 16 bytes/entry)
        readonly byte[] ioBuffer = new byte[BufferSize];

        BlockDBEntry[] cacheStore = new BlockDBEntry[MinCacheSize];
        internal int CacheSize;
        const int MinCacheSize = 2 * 1024, // 32 KB (at 16 bytes/entry)
                  CacheLinearResizeThreshold = 64 * 1024; // 1 MB (at 16 bytes/entry)

        void AddEntry( BlockDBEntry item ) {
            lock( SyncRoot ) {
                if( CacheSize == cacheStore.Length ) {
                    if( !isPreloaded && CacheSize >= CacheLinearResizeThreshold ) {
                        // Avoid bloating the cacheStore if we are not preloaded.
                        // This might cause lag spikes, since it's ran from main scheduler thread.
                        Flush();
                    } else {
                        // resize cache to fit
                        EnsureCapacity( CacheSize + 1 );
                    }
                }
                cacheStore[CacheSize++] = item;
            }
        }


        void CacheClear() {
            CacheSize = 0;
            if( IsEnabled ) {
                cacheStore = new BlockDBEntry[MinCacheSize];
            } else {
                cacheStore = null;
            }
            LastFlushedIndex = 0;
        }


        void EnsureCapacity( int min ) {
            if( cacheStore.Length < min ) {
                int num = cacheStore.Length;
                while( num < min ) {
                    if( num <= CacheLinearResizeThreshold ) {
                        num *= 2;
                    } else {
                        num += CacheLinearResizeThreshold;
                    }
                }
                CacheCapacity = num;
            }
        }


        void LimitCapacity( int max ) {
            if( cacheStore.Length > max ) {
                int newCapacity = max;
                if( newCapacity < MinCacheSize ) {
                    // minimum capacity
                    newCapacity = MinCacheSize;

                } else if( newCapacity < CacheLinearResizeThreshold ) {
                    // exponential resizing (x2 each time)
                    newCapacity = 1 << (int)(1 + Math.Floor( Math.Log( newCapacity, 2 ) ));

                } else {
                    // linear resizing (in 1 MB increments)
                    newCapacity = (newCapacity / CacheLinearResizeThreshold + 1) * CacheLinearResizeThreshold;
                }

                CacheCapacity = newCapacity;
                if( max < CacheSize ) {
                    Array.Copy( cacheStore, CacheSize - max, cacheStore, 0, max );
                    LastFlushedIndex -= (CacheSize - max);
                    CacheSize = max;
                }
            }
        }


        internal int CacheCapacity {
            get {
                return cacheStore.Length;
            }
            set {
                if( value < MinCacheSize ) {
                    throw new ArgumentOutOfRangeException( "value", "MinCacheSize may not be negative" );
                }
                if( value != cacheStore.Length ) {
                    BlockDBEntry[] destinationArray = new BlockDBEntry[value];
                    if( value < CacheSize ) {
                        // downsizing the cache
                        Array.Copy( cacheStore, CacheSize - value, destinationArray, 0, value );
                        LastFlushedIndex -= (CacheSize - value);
                        CacheSize = value;

                    } else {
                        // upsizing the cache
                        Array.Copy( cacheStore, 0, destinationArray, 0, Math.Min( cacheStore.Length, CacheSize ) );
                    }
                    cacheStore = destinationArray;
#if DEBUG_BLOCKDB
                    Logger.Log( "BlockDB({0}): CacheCapacity={1}", LogType.Debug, World.Name, value );
#endif
                }
            }
        }

        #endregion


        #region Preload

        bool isPreloaded;
        public bool IsPreloaded {
            get {
                return isPreloaded;
            }
            set {
                lock( SyncRoot ) {
                    if( IsEnabledGlobally ) {
                        if( value == isPreloaded ) return;
                        Flush();
                        if( value && File.Exists( FileName ) ) {
                            Preload();
                        } else if( value == false ) {
                            CacheClear();
                        }
#if DEBUG_BLOCKDB
                        Logger.Log( "BlockDB({0}): Preloaded={1}", LogType.Debug, World.Name, value );
#endif
                    }
                    isPreloaded = value;
                }
            }
        }


        void Preload() {
            using( FileStream fs = OpenRead() ) {
                CacheSize = (int)(fs.Length / BlockDBEntry.Size);
                EnsureCapacity( CacheSize );
                LastFlushedIndex = CacheSize;

                // Converting from byte[] to BlockDBEntry[] on the fly
                // This is possible because BlockDBEntry is a sequentially packed struct
                fixed( BlockDBEntry* pCacheStart = cacheStore ) {
                    fixed( byte* pBuffer = ioBuffer ) {
                        byte* pCache = (byte*)pCacheStart;
                        while( fs.Position < fs.Length ) {
                            int bytesToRead = Math.Min( BufferSize, (int)(fs.Length - fs.Position) );
                            int bytesInBuffer = 0;
                            do {
                                int bytesRead = fs.Read( ioBuffer, bytesInBuffer, BufferSize - bytesInBuffer );
                                bytesInBuffer += bytesRead;
                            } while( bytesInBuffer < bytesToRead );
                            BufferUtil.MemCpy( pBuffer, pCache, bytesInBuffer );
                            pCache += bytesInBuffer;
                        }
                    }
                }
            }
        }

        #endregion


        #region Limiting

        void TrimFile( int maxCapacity ) {
            if( maxCapacity == 0 ) {
                using( File.Create( FileName ) ) { }
                return;
            }
            string tempFileName = FileName + ".tmp";
            using( FileStream source = File.OpenRead( FileName ) ) {
                int entries = (int)(source.Length / BlockDBEntry.Size);
                if( entries <= maxCapacity ) return;

                // skip beginning of the file (that's where old entries are)
                source.Seek( (entries - maxCapacity) * BlockDBEntry.Size, SeekOrigin.Begin );

                // copy end of the existing file to a new one
                using( FileStream destination = File.Create( tempFileName ) ) {
                    while( source.Position < source.Length ) {
                        int bytesRead = source.Read( ioBuffer, 0, ioBuffer.Length );
                        destination.Write( ioBuffer, 0, bytesRead );
                    }
                }
            }
            Paths.MoveOrReplace( tempFileName, FileName );
        }


        /// <summary> Counts entries that are newer tha the given age. </summary>
        /// <param name="age"> Maximum age of entry </param>
        /// <returns> Number of entries newer than given age. May be 0 if all entries are older than given age. </returns>
        int CountNewerEntries( TimeSpan age ) {
            if( age < TimeSpan.Zero ) {
                throw new ArgumentOutOfRangeException( "age", "Age must be non-negative." );
            }
            int minTimestamp = (int)DateTime.UtcNow.Subtract( age ).ToUnixTime();

            if( isPreloaded ) {
                fixed( BlockDBEntry* ptr = cacheStore ) {
                    for( int i = 0; i < CacheSize; i++ ) {
                        if( ptr[i].Timestamp >= minTimestamp ) {
                            return CacheSize - i;
                        }
                    }
                }
                return 0;

            } else {
                byte[] bytes = Load();
                int entryCount = bytes.Length / BlockDBEntry.Size;
                fixed( byte* parr = bytes ) {
                    BlockDBEntry* entries = (BlockDBEntry*)parr;
                    for( int i = entryCount - 1; i >= 0; i-- ) {
                        if( entries[i].Timestamp >= minTimestamp ) {
                            return entryCount - i;
                        }
                    }
                }
                return 0;
            }
        }


        internal int LastFlushedIndex;


        int limit;
        public int Limit {
            get { return limit; }
            set {
                if( value < 0 ) {
                    throw new ArgumentOutOfRangeException( "value", "Limit may not be negative." );
                }
                lock( SyncRoot ) {
                    int oldLimit = limit;
                    limit = value;
                    if( oldLimit == 0 && value != 0 ||
                        oldLimit != 0 && value < oldLimit ) {
                        EnforceLimit();
                    }
#if DEBUG_BLOCKDB
                    Logger.Log( "BlockDB({0}): Limit={1}", LogType.Debug, World.Name, value );
#endif
                }
            }
        }

        public bool HasLimit {
            get { return limit > 0; }
        }

        void EnforceLimit() {
            if( IsEnabled && limit > 0 ) {
#if DEBUG_BLOCKDB
                int oldCap = CacheCapacity;
                int oldSize = CacheSize;
#endif
                if( isPreloaded ) {
                    LimitCapacity( limit );
                }
                TrimFile( limit );
                lastLimit = DateTime.UtcNow;
#if DEBUG_BLOCKDB
                Logger.Log( "BlockDB({0}): Enforce Limit, CC {1}->{2}, CS {3}->{4}", LogType.Debug,
                            World.Name, oldCap, CacheCapacity, oldSize, CacheSize );
#endif
            }
        }


        TimeSpan timeLimit;
        public TimeSpan TimeLimit {
            get { return timeLimit; }
            set {
                if( value < TimeSpan.Zero ) {
                    throw new ArgumentOutOfRangeException( "value", "TimeLimit may not be negative." );
                }
                lock( SyncRoot ) {
                    TimeSpan oldTimeLimit = timeLimit;
                    timeLimit = value;
                    if( oldTimeLimit == TimeSpan.Zero && value != TimeSpan.Zero ||
                        oldTimeLimit != TimeSpan.Zero && value < oldTimeLimit ) {
                        EnforceTimeLimit();
                    }
#if DEBUG_BLOCKDB
                    Logger.Log( "BlockDB({0}): TimeLimit={1}", LogType.Debug, World.Name, value );
#endif
                }
            }
        }

        public bool HasTimeLimit {
            get { return timeLimit > TimeSpan.Zero; }
        }

        void EnforceTimeLimit() {
            if( IsEnabled && timeLimit > TimeSpan.Zero ) {
#if DEBUG_BLOCKDB
                int oldCap = CacheCapacity;
                int oldSize = CacheSize;
#endif
                int newCapacity = CountNewerEntries( timeLimit );
                if( isPreloaded ) {
                    LimitCapacity( newCapacity );
                }
                TrimFile( newCapacity );
                lastTimeLimit = DateTime.UtcNow;
#if DEBUG_BLOCKDB
                Logger.Log( "BlockDB({0}): Enforce TimeLimit, CC {1}->{2}, CS {3}->{4}", LogType.Debug,
                            World.Name, oldCap, CacheCapacity, oldSize, CacheSize );
#endif
            }
        }


        int changesSinceLimitEnforcement;
        const double LimitEnforcementThreshold = 1.15; // 15%
        DateTime lastLimit, lastTimeLimit;

        void EnforceLimitsIfNeeded() {
            if( limit > 0 ) {
                bool limitingAllowed = DateTime.UtcNow.Subtract( lastLimit ) > MinLimitDelay ||
                                       (CacheSize - limit) > CacheLinearResizeThreshold;
                if( changesSinceLimitEnforcement > limit * LimitEnforcementThreshold && limitingAllowed ) {
                    changesSinceLimitEnforcement = 0;
                    EnforceLimit();
                    LimitCapacity( CacheSize );
                }
            }

            if( timeLimit > TimeSpan.Zero ) {
                if( DateTime.UtcNow.Subtract( lastTimeLimit ) > MinTimeLimitDelay ) {
                    EnforceTimeLimit();
                    LimitCapacity( CacheSize );
                }
            }
        }

        #endregion


        /// <summary> Clears cache and deletes the BlockDB file. </summary>
        public void Clear() {
            lock( SyncRoot ) {
                CacheClear();
                if( File.Exists( FileName ) ) {
                    File.Delete( FileName );
                }
            }
        }


        static readonly TimeSpan MinLimitDelay = TimeSpan.FromMinutes( 5 ),
                                 MinTimeLimitDelay = TimeSpan.FromMinutes( 10 );

        public void Flush() {
            lock( SyncRoot ) {
                if( LastFlushedIndex < CacheSize ) {
#if DEBUG_BLOCKDB
                    Logger.Log( "BlockDB({0}): Flushing. CC={1} CS={2} LFI={3}", LogType.Debug,
                                World.Name, CacheCapacity, CacheSize, LastFlushedIndex );
#endif
                    int count = 0;
                    using( FileStream stream = OpenAppend() ) {
                        BinaryWriter writer = new BinaryWriter( stream );
                        for( int i = LastFlushedIndex; i < CacheSize; i++ ) {
                            cacheStore[i].Serialize( writer );
                            count++;
                        }
                    }
                    if( !isPreloaded ) CacheSize = 0;
                    LastFlushedIndex = CacheSize;
                    changesSinceLimitEnforcement += count;
#if DEBUG_BLOCKDB
                    Logger.Log( "BlockDB({0}): Flushed {1} entries. CC={2} CS={3} LFI={4}", LogType.Debug,
                                World.Name, count, CacheCapacity, CacheSize, LastFlushedIndex );
#endif
                }
                EnforceLimitsIfNeeded();
            }
        }


        FileStream OpenRead() {
            return new FileStream( FileName, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan );
        }


        FileStream OpenAppend() {
            return new FileStream( FileName, FileMode.Append, FileAccess.Write, FileShare.None, BufferSize );
        }


        byte[] Load() {
            lock( SyncRoot ) {
                if( File.Exists( FileName ) ) {
                    return File.ReadAllBytes( FileName );
                } else {
                    return new byte[0];
                }
            }
        }


        public BlockDBEntry[] Lookup( Vector3I coords ) {
            if( !IsEnabled || !IsEnabledGlobally ) {
                throw new InvalidOperationException( "Trying to lookup on disabled BlockDB." );
            }
            List<BlockDBEntry> results = new List<BlockDBEntry>();
            if( !World.LoadMap().InBounds( coords ) ) {
                return results.ToArray();
            }

            if( isPreloaded ) {
                lock( SyncRoot ) {
                    fixed( BlockDBEntry* entries = cacheStore ) {
                        for( int i = 0; i < CacheSize; i++ ) {
                            if( entries[i].X == coords.X && entries[i].Y == coords.Y && entries[i].Z == coords.Z ) {
                                results.Add( entries[i] );
                            }
                        }
                    }
                }
            } else {
                Flush();
                byte[] bytes = Load();
                int entryCount = bytes.Length / BlockDBEntry.Size;
                fixed( byte* parr = bytes ) {
                    BlockDBEntry* entries = (BlockDBEntry*)parr;
                    for( int i = 0; i < entryCount; i++ ) {
                        if( entries[i].X == coords.X && entries[i].Y == coords.Y && entries[i].Z == coords.Z ) {
                            results.Add( entries[i] );
                        }
                    }
                }
            }

            return results.ToArray();
        }


        public BlockDBEntry[] Lookup( [NotNull] PlayerInfo info, int max ) {
            if( !IsEnabled || !IsEnabledGlobally ) {
                throw new InvalidOperationException( "Trying to lookup on disabled BlockDB." );
            }
            if( info == null ) throw new ArgumentNullException( "info" );
            Dictionary<int, BlockDBEntry> results = new Dictionary<int, BlockDBEntry>();
            int count = 0;

            Map map = World.LoadMap();
            if( isPreloaded ) {
                lock( SyncRoot ) {
                    fixed( BlockDBEntry* entries = cacheStore ) {
                        for( int i = CacheSize - 1; i >= 0; i-- ) {
                            if( entries[i].PlayerID == info.ID ) {
                                int index = map.Index( entries[i].X, entries[i].Y, entries[i].Z );
                                results[index] = entries[i];
                                count++;
                                if( count >= max ) break;
                            }
                        }
                    }
                }
            } else {
                Flush();
                byte[] bytes = Load();
                int entryCount = bytes.Length / BlockDBEntry.Size;
                fixed( byte* parr = bytes ) {
                    BlockDBEntry* entries = (BlockDBEntry*)parr;
                    for( int i = entryCount - 1; i >= 0; i-- ) {
                        if( entries[i].PlayerID == info.ID ) {
                            int index = map.Index( entries[i].X, entries[i].Y, entries[i].Z );
                            results[index] = entries[i];
                            count++;
                            if( count >= max ) break;
                        }
                    }
                }
            }
            return results.Values.ToArray();
        }


        public BlockDBEntry[] Lookup( [NotNull] PlayerInfo info, TimeSpan span ) {
            if( !IsEnabled || !IsEnabledGlobally ) {
                throw new InvalidOperationException( "Trying to lookup on disabled BlockDB." );
            }
            if( info == null ) throw new ArgumentNullException( "info" );
            long ticks = DateTime.UtcNow.Subtract( span ).ToUnixTime();
            Dictionary<int, BlockDBEntry> results = new Dictionary<int, BlockDBEntry>();
            Map map = World.LoadMap();

            if( isPreloaded ) {
                lock( SyncRoot ) {
                    fixed( BlockDBEntry* entries = cacheStore ) {
                        for( int i = CacheSize - 1; i >= 0; i-- ) {
                            if( entries[i].Timestamp < ticks ) break;
                            if( entries[i].PlayerID == info.ID ) {
                                int index = map.Index( entries[i].X, entries[i].Y, entries[i].Z );
                                results[index] = entries[i];
                            }
                        }
                    }
                }
            } else {
                Flush();
                byte[] bytes = Load();
                int entryCount = bytes.Length / BlockDBEntry.Size;
                fixed( byte* parr = bytes ) {
                    BlockDBEntry* entries = (BlockDBEntry*)parr;
                    for( int i = entryCount - 1; i >= 0; i-- ) {
                        if( entries[i].Timestamp < ticks ) break;
                        if( entries[i].PlayerID != info.ID ) continue;
                        int index = map.Index( entries[i].X, entries[i].Y, entries[i].Z );
                        results[index] = entries[i];
                    }
                }
            }
            return results.Values.ToArray();
        }


        public BlockDBEntry[] Lookup( [NotNull] PlayerInfo info, [NotNull] BoundingBox area, int max ) {
            if( !IsEnabled || !IsEnabledGlobally ) {
                throw new InvalidOperationException( "Trying to lookup on disabled BlockDB." );
            }
            if( info == null ) throw new ArgumentNullException( "info" );
            if( area == null ) throw new ArgumentNullException( "area" );
            Dictionary<int, BlockDBEntry> results = new Dictionary<int, BlockDBEntry>();
            int count = 0;
            Map map = World.LoadMap();

            if( isPreloaded ) {
                lock( SyncRoot ) {
                    fixed( BlockDBEntry* entries = cacheStore ) {
                        for( int i = CacheSize - 1; i >= 0; i-- ) {
                            if( entries[i].PlayerID == info.ID && area.Contains( entries[i].X, entries[i].Y, entries[i].Z ) ) {
                                int index = map.Index( entries[i].X, entries[i].Y, entries[i].Z );
                                results[index] = entries[i];
                                count++;
                                if( count >= max ) break;
                            }
                        }
                    }
                }
            } else {
                Flush();
                byte[] bytes = Load();
                int entryCount = bytes.Length / BlockDBEntry.Size;
                fixed( byte* parr = bytes ) {
                    BlockDBEntry* entries = (BlockDBEntry*)parr;
                    for( int i = entryCount - 1; i >= 0; i-- ) {
                        if( entries[i].PlayerID == info.ID && area.Contains( entries[i].X, entries[i].Y, entries[i].Z ) ) {
                            int index = map.Index( entries[i].X, entries[i].Y, entries[i].Z );
                            results[index] = entries[i];
                            count++;
                            if( count >= max ) break;
                        }
                    }
                }
            }
            return results.Values.ToArray();
        }


        public BlockDBEntry[] Lookup( [NotNull] PlayerInfo info, [NotNull] BoundingBox area, TimeSpan span ) {
            if( !IsEnabled || !IsEnabledGlobally ) {
                throw new InvalidOperationException( "Trying to lookup on disabled BlockDB." );
            }
            if( info == null ) throw new ArgumentNullException( "info" );
            if( area == null ) throw new ArgumentNullException( "area" );
            long ticks = DateTime.UtcNow.Subtract( span ).ToUnixTime();
            Dictionary<int, BlockDBEntry> results = new Dictionary<int, BlockDBEntry>();
            Map map = World.LoadMap();

            if( isPreloaded ) {
                lock( SyncRoot ) {
                    fixed( BlockDBEntry* entries = cacheStore ) {
                        for( int i = CacheSize - 1; i >= 0; i-- ) {
                            if( entries[i].Timestamp < ticks ) break;
                            if( entries[i].PlayerID == info.ID && area.Contains( entries[i].X, entries[i].Y, entries[i].Z ) ) {
                                int index = map.Index( entries[i].X, entries[i].Y, entries[i].Z );
                                results[index] = entries[i];
                            }
                        }
                    }
                }
            } else {
                Flush();
                byte[] bytes = Load();
                int entryCount = bytes.Length / BlockDBEntry.Size;
                fixed( byte* parr = bytes ) {
                    BlockDBEntry* entries = (BlockDBEntry*)parr;
                    for( int i = entryCount - 1; i >= 0; i-- ) {
                        if( entries[i].Timestamp < ticks ) break;
                        if( entries[i].PlayerID == info.ID && area.Contains( entries[i].X, entries[i].Y, entries[i].Z ) ) {
                            int index = map.Index( entries[i].X, entries[i].Y, entries[i].Z );
                            results[index] = entries[i];
                        }
                    }
                }
            }
            return results.Values.ToArray();
        }


        #region Serialization

        public const string XmlRootName = "BlockDB";

        public XElement SaveSettings() {
            return SaveSettings( XmlRootName );
        }

        public XElement SaveSettings( string rootName ) {
            XElement root = new XElement( rootName );
            root.Add( new XAttribute( "enabled", EnabledState ) );
            root.Add( new XAttribute( "preload", IsPreloaded ) );
            if( HasLimit ) {
                root.Add( new XAttribute( "limit", Limit ) );
            }
            if( HasTimeLimit ) {
                root.Add( new XAttribute( "timeLimit", (int)TimeLimit.TotalSeconds ) );
            }
            return root;
        }

        public void LoadSettings( XElement el ) {
            XAttribute temp;
            if( (temp = el.Attribute( "enabled" )) != null ) {
                YesNoAuto enabledStateTemp;
                if( EnumUtil.TryParse( temp.Value, out enabledStateTemp, true ) ) {
                    EnabledState = enabledStateTemp;
                } else {
                    Logger.Log( LogType.Warning,
                                "WorldManager: Could not parse BlockDB \"enabled\" attribute of world \"{0}\", assuming \"Auto\"",
                                World.Name );
                    EnabledState = YesNoAuto.Auto;
                }
            }

            if( (temp = el.Attribute( "preload" )) != null ) {
                bool isPreloadedTemp;
                if( Boolean.TryParse( temp.Value, out isPreloadedTemp ) ) {
                    IsPreloaded = isPreloadedTemp;
                } else {
                    Logger.Log( LogType.Warning,
                                "WorldManager: Could not parse BlockDB \"preload\" attribute of world \"{0}\", assuming NOT preloaded.",
                                World.Name );
                }
            }
            if( (temp = el.Attribute( "limit" )) != null ) {
                int limitTemp;
                if( Int32.TryParse( temp.Value, out limitTemp ) ) {
                    Limit = limitTemp;
                } else {
                    Logger.Log( LogType.Warning,
                                "WorldManager: Could not parse BlockDB \"limit\" attribute of world \"{0}\", assuming NO limit.",
                                World.Name );
                }
            }
            if( (temp = el.Attribute( "timeLimit" )) != null ) {
                int timeLimitSeconds;
                if( Int32.TryParse( temp.Value, out timeLimitSeconds ) ) {
                    TimeLimit = TimeSpan.FromSeconds( timeLimitSeconds );
                } else {
                    Logger.Log( LogType.Warning,
                                "WorldManager: Could not parse BlockDB \"timeLimit\" attribute of world \"{0}\", assuming NO time limit.",
                                World.Name );
                }
            }
        }

        #endregion


        #region Static

        static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds( 90 );

        /// <summary> Whether BlockDB was enabled at startup.
        /// Changing this setting currently requires a server restart. </summary>
        public static bool IsEnabledGlobally { get; private set; }

        internal static void Init() {
            Paths.TestDirectory( "BlockDB", Paths.BlockDBPath, true );
            Player.PlacedBlock += OnPlayerPlacedBlock;
            Scheduler.NewBackgroundTask( FlushAll ).RunForever( FlushInterval, FlushInterval );
            IsEnabledGlobally = true;
        }


        static void OnPlayerPlacedBlock( object sender, [NotNull] PlayerPlacedBlockEventArgs e ) {
            if( e == null ) throw new ArgumentNullException( "e" );
            World world = e.Map.World;
            if( world != null && world.BlockDB.IsEnabled ) {
                BlockDBEntry newEntry = new BlockDBEntry( (int)DateTime.UtcNow.ToUnixTime(),
                                                          e.Player.Info.ID,
                                                          e.Coords,
                                                          e.OldBlock,
                                                          e.NewBlock,
                                                          e.Context );
                world.BlockDB.AddEntry( newEntry );
            }
        }


        static void FlushAll( SchedulerTask task ) {
            lock( WorldManager.SyncRoot ) {
                foreach( World w in WorldManager.Worlds.Where( w => w.BlockDB.IsEnabled ) ) {
                    w.BlockDB.Flush();
                }
            }
        }

        #endregion
    }
}