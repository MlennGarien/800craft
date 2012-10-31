// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
//#define DEBUG_BLOCKDB
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Database of block changes. Each BlockDB object is associated with a world.
    /// Provides controls for storage/retention, and methods to look up data. </summary>
    public sealed unsafe class BlockDB {
        internal BlockDB ( [NotNull] World world ) {
            if ( world == null ) throw new ArgumentNullException( "world" );
            World = world;
        }


        /// <summary> World associated with this BlockDB. </summary>
        [NotNull]
        public World World { get; internal set; }


        /// <summary> Sets this BlockDB's enabled state to Yes (always on), No (aways off),
        /// or Auto (on or off depending on BlockDBAutoEnableRank config key and World's build permissions). </summary>
        public YesNoAuto EnabledState {
            get { return enabledState; }
            set {
                IDisposable writeLockHandle = null;
                try {
                    // make sure we dont acquire the write lock twice
                    if ( !locker.IsWriteLockHeld ) {
                        writeLockHandle = locker.WriteLock();
                    }

                    if ( IsEnabledGlobally ) {
#if DEBUG_BLOCKDB
                        if( value != enabledState ) {
                            Logger.Log( LogType.Debug, "BlockDB({0}): Enabled={1}", World.Name, value );
                        }
#endif
                        if ( value == YesNoAuto.No && IsEnabled ) {
                            // going from enabled/auto-enabled to disabled
                            Flush( true );
                            CacheClear();
                            IsEnabled = false;

                        } else if ( !IsEnabled &&
                                   ( value == YesNoAuto.Yes || value == YesNoAuto.Auto && ShouldBeAutoEnabled ) ) {
                            // going from disabled to enabled/auto-enabled
                            CheckAlignment();
                            cacheStore = new BlockDBEntry[MinCacheSize];
                            if ( isPreloaded ) {
                                Preload();
                            }
                            IsEnabled = true;
                        }
                    }
                    enabledState = value;

                } finally {
                    // release write lock, if we were holding it
                    if ( writeLockHandle != null ) {
                        writeLockHandle.Dispose();
                    }
                }
            }
        }

        YesNoAuto enabledState;


        /// <summary> Checks whether this BlockDB is enabled (either automatically or manually).
        /// Set EnabledState to enable/disable. </summary>
        public bool IsEnabled { get; private set; }


        // BlockDB files are supposed to be aligned to 20 bytes.
        // Misalignment might happen if writing didnt finish properly - e.g. power outage, or ungraceful shutdown.
        // If that's not the case, the last few bytes are trimmed off the BlockDB file.
        void CheckAlignment () {
            FileInfo fi = new FileInfo( FileName );
            if ( fi.Exists ) {
                long length = fi.Length;
                if ( length % sizeof( BlockDBEntry ) != 0 ) {
                    Logger.Log( LogType.Error,
                                "BlockDB: Misaligned data detected in \"{0}\". " +
                                "This might have been caused by a power outage or ungraceful shutdown. Attempting recovery.",
                                fi.Name );
                    using ( FileStream fs = File.OpenWrite( fi.FullName ) ) {
                        fs.SetLength( length - ( length % sizeof( BlockDBEntry ) ) );
                    }
                }
            }
        }


        /// <summary> If EnabledState is set to Auto, calling this auto-enables / auto-disables
        /// BlockDB depending on world's BuildSecurity.MinRank and BlockDBAutoEnableRank config key. </summary>
        /// <returns> True if call resulted in IsEnabled being changed; otherwise false. </returns>
        public bool AutoToggleIfNeeded () {
            bool oldEnabled = IsEnabled;
            EnabledState = enabledState;
            return ( oldEnabled != IsEnabled );
        }


        bool ShouldBeAutoEnabled {
            get { return ( World.BuildSecurity.MinRank <= RankManager.BlockDBAutoEnableRank ); }
        }


        /// <summary> Full path to the file where BlockDB data is stored. </summary>
        [NotNull]
        public string FileName {
            get { return Path.Combine( Paths.BlockDBPath, World.Name + ".fbdb" ); }
        }


        void AddEntry ( BlockDBEntry item ) {
            try {
                locker.EnterUpgradeableReadLock();
                if ( CacheSize == cacheStore.Length ) {
                    using ( locker.WriteLock() ) {
                        if ( !isPreloaded && CacheSize >= CacheLinearResizeThreshold ) {
                            // Avoid bloating the cacheStore if we are not preloaded.
                            // This might cause lag spikes, since it's ran from main scheduler thread.
                            Flush( false );
                        } else {
                            // resize cache to fit
                            EnsureCapacity( CacheSize + 1 );
                        }
                    }
                }
                cacheStore[CacheSize++] = item;
            } finally {
                locker.ExitUpgradeableReadLock();
            }
        }


        #region Cache

        const int BufferSize = 64 * 1024; // 64 KB (at 16 bytes/entry)
        readonly byte[] ioBuffer = new byte[BufferSize];

        BlockDBEntry[] cacheStore = new BlockDBEntry[MinCacheSize];
        internal int CacheSize;

        const int MinCacheSize = 2 * 1024, // 32 KB (at 16 bytes/entry)
                  CacheLinearResizeThreshold = 64 * 1024; // 1 MB (at 16 bytes/entry)


        void CacheClear () {
            CacheSize = 0;
            if ( IsEnabled ) {
                cacheStore = new BlockDBEntry[MinCacheSize];
            } else {
                cacheStore = null;
            }
            LastFlushedIndex = 0;
        }


        void EnsureCapacity ( int min ) {
            if ( cacheStore.Length < min ) {
                int num = cacheStore.Length;
                while ( num < min ) {
                    if ( num <= CacheLinearResizeThreshold ) {
                        num *= 2;
                    } else {
                        num += CacheLinearResizeThreshold;
                    }
                }
                CacheCapacity = num;
            }
        }


        void LimitCapacity ( int max ) {
            if ( cacheStore.Length > max ) {
                int newCapacity = max;
                if ( newCapacity < MinCacheSize ) {
                    // minimum capacity
                    newCapacity = MinCacheSize;

                } else if ( newCapacity < CacheLinearResizeThreshold ) {
                    // exponential resizing (x2 each time)
                    newCapacity = 1 << ( int )( 1 + Math.Floor( Math.Log( newCapacity, 2 ) ) );

                } else {
                    // linear resizing (in 1 MB increments)
                    newCapacity = ( newCapacity / CacheLinearResizeThreshold + 1 ) * CacheLinearResizeThreshold;
                }

                CacheCapacity = newCapacity;
                if ( max < CacheSize ) {
                    Array.Copy( cacheStore, CacheSize - max, cacheStore, 0, max );
                    LastFlushedIndex -= ( CacheSize - max );
                    CacheSize = max;
                }
            }
        }


        internal int CacheCapacity {
            get { return cacheStore.Length; }
            set {
                if ( value < MinCacheSize ) {
                    throw new ArgumentOutOfRangeException( "value", "MinCacheSize may not be negative" );
                }
                if ( value != cacheStore.Length ) {
                    BlockDBEntry[] destinationArray = new BlockDBEntry[value];
                    if ( value < CacheSize ) {
                        // downsizing the cache
                        Array.Copy( cacheStore, CacheSize - value, destinationArray, 0, value );
                        LastFlushedIndex -= ( CacheSize - value );
                        CacheSize = value;

                    } else {
                        // upsizing the cache
                        Array.Copy( cacheStore, 0, destinationArray, 0, Math.Min( cacheStore.Length, CacheSize ) );
                    }
                    cacheStore = destinationArray;
#if DEBUG_BLOCKDB
                    Logger.Log( LogType.Debug, "BlockDB({0}): CacheCapacity={1}", World.Name, value );
#endif
                }
            }
        }

        #endregion


        #region Preload

        bool isPreloaded;

        public bool IsPreloaded {
            get { return isPreloaded; }
            set {
                IDisposable writeLockHandle = null;
                try {
                    if ( !locker.IsWriteLockHeld ) {
                        writeLockHandle = locker.WriteLock();
                    }

                    if ( IsEnabledGlobally ) {
                        if ( value == isPreloaded ) return;
                        Flush( true );
                        if ( value ) {
                            Preload();
                        } else {
                            CacheClear();
                        }
#if DEBUG_BLOCKDB
                        Logger.Log( LogType.Debug, "BlockDB({0}): Preloaded={1}", World.Name, value );
#endif
                    }
                    isPreloaded = value;
                } finally {
                    if ( writeLockHandle != null ) {
                        writeLockHandle.Dispose();
                    }
                }
            }
        }


        void Preload () {
            if ( !File.Exists( FileName ) ) return;
            using ( FileStream fs = OpenRead() ) {
                CacheSize = ( int )( fs.Length / sizeof( BlockDBEntry ) );
                EnsureCapacity( CacheSize );
                LastFlushedIndex = CacheSize;

                // Converting from byte[] to BlockDBEntry[] on the fly
                // This is possible because BlockDBEntry is a sequentially packed struct
                fixed ( BlockDBEntry* pCacheStart = cacheStore ) {
                    fixed ( byte* pBuffer = ioBuffer ) {
                        byte* pCache = ( byte* )pCacheStart;
                        while ( fs.Position < fs.Length ) {
                            int bytesToRead = Math.Min( BufferSize, ( int )( fs.Length - fs.Position ) );
                            int bytesInBuffer = 0;
                            do {
                                int bytesRead = fs.Read( ioBuffer, bytesInBuffer, BufferSize - bytesInBuffer );
                                bytesInBuffer += bytesRead;
                            } while ( bytesInBuffer < bytesToRead );
                            BufferUtil.MemCpy( pBuffer, pCache, bytesInBuffer );
                            pCache += bytesInBuffer;
                        }
                    }
                }
            }
        }

        #endregion


        #region Limiting

        static readonly TimeSpan MinLimitDelay = TimeSpan.FromMinutes( 5 ),
                                 MinTimeLimitDelay = TimeSpan.FromMinutes( 10 );
        const double LimitEnforcementThreshold = 1.15; // 15%
        internal int LastFlushedIndex;
        int changesSinceLimitEnforcement;
        DateTime lastLimit,
                 lastTimeLimit;


        void TrimFile ( int maxCapacity ) {
            if ( maxCapacity == 0 ) {
                using ( File.Create( FileName ) ) { }
                return;
            }
            if ( !File.Exists( FileName ) ) return;

            string tempFileName = FileName + ".tmp";
            using ( FileStream source = File.OpenRead( FileName ) ) {
                int entries = ( int )( source.Length / sizeof( BlockDBEntry ) );
                if ( entries <= maxCapacity ) return;

                // skip beginning of the file (that's where old entries are)
                source.Seek( ( entries - maxCapacity ) * sizeof( BlockDBEntry ), SeekOrigin.Begin );

                // copy end of the existing file to a new one
                using ( FileStream destination = File.Create( tempFileName ) ) {
                    while ( source.Position < source.Length ) {
                        int bytesRead = source.Read( ioBuffer, 0, ioBuffer.Length );
                        destination.Write( ioBuffer, 0, bytesRead );
                    }
                }
            }
            Paths.MoveOrReplace( tempFileName, FileName );
        }


        /// <summary> Counts entries that are newer than the given age. </summary>
        /// <param name="age"> Maximum age of entry </param>
        /// <returns> Number of entries newer than given age.
        /// 0 if all entries are older than given age.
        /// -1 if all entries are newer than given age. </returns>
        int CountNewerEntries ( TimeSpan age ) {
            if ( age < TimeSpan.Zero ) {
                throw new ArgumentOutOfRangeException( "age", "Age must be non-negative." );
            }
            int minTimestamp = ( int )DateTime.UtcNow.Subtract( age ).ToUnixTime();

            if ( isPreloaded ) {
                fixed ( BlockDBEntry* ptr = cacheStore ) {
                    for ( int i = 0; i < CacheSize; i++ ) {
                        if ( ptr[i].Timestamp < minTimestamp ) {
                            return CacheSize - i;
                        }
                    }
                }

            } else {
                Flush( false );
                if ( !File.Exists( FileName ) ) return -1;

                using ( FileStream fs = OpenRead() ) {
                    long length = fs.Length;
                    long bytesReadTotal = 0;
                    int bufferSize = ( int )Math.Min( SearchBufferSize, length );
                    byte[] buffer = new byte[bufferSize];

                    while ( bytesReadTotal < length ) {
                        long offset = Math.Max( 0, length - bytesReadTotal - SearchBufferSize );
                        fs.Seek( offset, SeekOrigin.Begin );

                        int bytesToRead = ( int )Math.Min( length - bytesReadTotal, SearchBufferSize );
                        int bytesLeft = bytesToRead;
                        int bytesRead = 0;
                        while ( bytesLeft > 0 ) {
                            int readPass = fs.Read( buffer, bytesRead, bytesLeft );
                            if ( readPass == 0 ) throw new EndOfStreamException();
                            bytesRead += readPass;
                            bytesLeft -= readPass;
                        }
                        bytesReadTotal += bytesRead;

                        fixed ( byte* parr = buffer ) {
                            BlockDBEntry* entries = ( BlockDBEntry* )parr;
                            int entryCount = bytesRead / sizeof( BlockDBEntry );
                            if ( bytesRead % sizeof( BlockDBEntry ) != 0 ) throw new DataMisalignedException();
                            for ( int i = entryCount - 1; i >= 0; i-- ) {
                                if ( entries[i].Timestamp < minTimestamp ) {
                                    return entryCount - i;
                                }
                            }
                        }
                    }
                }
            }
            return -1;
        }


        public int Limit {
            get { return limit; }
            set {
                if ( value < 0 ) {
                    throw new ArgumentOutOfRangeException( "value", "Limit may not be negative." );
                }

                IDisposable writeLockHandle = null;
                try {
                    if ( !locker.IsWriteLockHeld ) {
                        writeLockHandle = locker.WriteLock();
                    }

                    int oldLimit = limit;
                    limit = value;
                    if ( oldLimit == 0 && value != 0 ||
                        oldLimit != 0 && value < oldLimit ) {
                        EnforceLimit();
                    }
#if DEBUG_BLOCKDB
                    Logger.Log( LogType.Debug, "BlockDB({0}): Limit={1}", World.Name, value );
#endif

                } finally {
                    if ( writeLockHandle != null ) {
                        writeLockHandle.Dispose();
                    }
                }
            }
        }

        int limit;

        /// <summary> Whether this BlockDB instance has a limit on the number of entries. </summary>
        public bool HasLimit {
            get { return limit > 0; }
        }


        void EnforceLimit () {
            if ( IsEnabled && limit > 0 ) {
#if DEBUG_BLOCKDB
                int oldCap = CacheCapacity;
                int oldSize = CacheSize;
#endif
                Flush( false );
                if ( isPreloaded ) {
                    LimitCapacity( limit );
                }
                TrimFile( limit );
                lastLimit = DateTime.UtcNow;
#if DEBUG_BLOCKDB
                Logger.Log( LogType.Debug,
                            "BlockDB({0}): Enforce Limit, CC {1}->{2}, CS {3}->{4}",
                            World.Name, oldCap, CacheCapacity, oldSize, CacheSize );
#endif
            }
        }


        public TimeSpan TimeLimit {
            get { return timeLimit; }
            set {
                if ( value < TimeSpan.Zero ) {
                    throw new ArgumentOutOfRangeException( "value", "TimeLimit may not be negative." );
                }

                IDisposable writeLockHandle = null;
                try {
                    if ( !locker.IsWriteLockHeld ) {
                        writeLockHandle = locker.WriteLock();
                    }

                    TimeSpan oldTimeLimit = timeLimit;
                    timeLimit = value;
                    if ( oldTimeLimit == TimeSpan.Zero && value != TimeSpan.Zero ||
                        oldTimeLimit != TimeSpan.Zero && value < oldTimeLimit ) {
                        EnforceTimeLimit();
                    }
#if DEBUG_BLOCKDB
                    Logger.Log( LogType.Debug,
                                "BlockDB({0}): TimeLimit={1}",
                                World.Name, value );
#endif
                } finally {
                    if ( writeLockHandle != null ) {
                        writeLockHandle.Dispose();
                    }
                }
            }
        }

        TimeSpan timeLimit;

        public bool HasTimeLimit {
            get { return timeLimit > TimeSpan.Zero; }
        }


        void EnforceTimeLimit () {
            if ( IsEnabled && timeLimit > TimeSpan.Zero ) {
#if DEBUG_BLOCKDB
                int oldCap = CacheCapacity;
                int oldSize = CacheSize;
#endif
                Flush( false );
                int newCapacity = CountNewerEntries( timeLimit );
                if ( newCapacity != -1 ) {
                    if ( isPreloaded ) {
                        LimitCapacity( newCapacity );
                    }
                    TrimFile( newCapacity );
                }
                lastTimeLimit = DateTime.UtcNow;
#if DEBUG_BLOCKDB
                Logger.Log( LogType.Debug,
                            "BlockDB({0}): Enforce TimeLimit, CC {1}->{2}, CS {3}->{4}",
                            World.Name, oldCap, CacheCapacity, oldSize, CacheSize );
#endif
            }
        }

        #endregion


        /// <summary> Clears cache and deletes the .fbdb file. </summary>
        public void Clear () {
            IDisposable writeLockHandle = null;
            try {
                if ( !locker.IsWriteLockHeld ) {
                    writeLockHandle = locker.WriteLock();
                }

                CacheClear();
                if ( File.Exists( FileName ) ) {
                    File.Delete( FileName );
                }

            } finally {
                if ( writeLockHandle != null ) {
                    writeLockHandle.Dispose();
                }
            }
        }


        internal void Flush ( bool enforceLimits ) {
            IDisposable writeLockHandle = null;
            try {
                if ( !locker.IsWriteLockHeld ) {
                    writeLockHandle = locker.WriteLock();
                }

                if ( LastFlushedIndex < CacheSize ) {
#if DEBUG_BLOCKDB
                    Logger.Log( LogType.Debug,
                                "BlockDB({0}): Flushing({1}) CC={2} CS={3} LFI={4}",
                                World.Name, enforceLimits, CacheCapacity, CacheSize, LastFlushedIndex );
#endif
                    int count = 0;
                    using ( FileStream stream = OpenAppend() ) {
                        BinaryWriter writer = new BinaryWriter( stream );
                        for ( int i = LastFlushedIndex; i < CacheSize; i++ ) {
                            cacheStore[i].Serialize( writer );
                            count++;
                        }
                    }
                    if ( !isPreloaded ) CacheSize = 0;
                    LastFlushedIndex = CacheSize;
                    changesSinceLimitEnforcement += count;
#if DEBUG_BLOCKDB
                    Logger.Log( LogType.Debug,
                                "BlockDB({0}): Flushed {1} entries. CC={2} CS={3} LFI={4}",
                                World.Name, count, CacheCapacity, CacheSize, LastFlushedIndex );
#endif
                }

                if ( !enforceLimits ) return;

                // enforce size limit, if needed
                if ( limit > 0 ) {
                    bool limitingAllowed = DateTime.UtcNow.Subtract( lastLimit ) > MinLimitDelay ||
                                           ( CacheSize - limit ) > CacheLinearResizeThreshold;
                    if ( changesSinceLimitEnforcement > limit * LimitEnforcementThreshold && limitingAllowed ) {
                        changesSinceLimitEnforcement = 0;
                        EnforceLimit();
                        LimitCapacity( CacheSize );
                    }
                }

                // enforce time limit, if needed
                if ( timeLimit > TimeSpan.Zero ) {
                    if ( DateTime.UtcNow.Subtract( lastTimeLimit ) > MinTimeLimitDelay ) {
                        EnforceTimeLimit();
                        LimitCapacity( CacheSize );
                    }
                }
            } finally {
                if ( writeLockHandle != null ) {
                    writeLockHandle.Dispose();
                }
            }
        }


        FileStream OpenRead () {
            return new FileStream( FileName, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize,
                                   FileOptions.SequentialScan );
        }


        FileStream OpenAppend () {
            return new FileStream( FileName, FileMode.Append, FileAccess.Write, FileShare.Read, BufferSize );
        }


        #region Lookup

        /// <summary> Searches the database for BlockDBEntries, based on given criteria. </summary>
        /// <param name="max"> Maximum number of entries to check. </param>
        /// <param name="searchType"> Type of search (ReturnAll, ReturnOldest, or ReturnNewest). See BlockDBSearchType enum. </param>
        /// <param name="selector"> Function used to select BlockDBEntries to be returned. </param>
        /// <returns> Array of BlockDBEntry structs. </returns>
        /// <exception cref="ArgumentOutOfRangeException"> max is less than 0. </exception>
        /// <exception cref="ArgumentNullException"> selector is null. </exception>
        /// <exception cref="InvalidOperationException"> BlockDB is disabled. </exception>
        /// <exception cref="EndOfStreamException"> The end of FBDB file is reached prematurely (corrupted file, or outside interference). </exception>
        /// <exception cref="DataMisalignedException"> FBDB file is not aligned to 20 bytes (likely corrupted). </exception>
        /// <exception cref="IOException"> An I/O error occurrs while trying to read FBDB file from disk. </exception>
        [NotNull]
        public BlockDBEntry[] Lookup ( int max, BlockDBSearchType searchType, [NotNull] Func<BlockDBEntry, bool> selector ) {
            if ( !IsEnabled || !IsEnabledGlobally ) {
                throw new InvalidOperationException( "Trying to lookup on disabled BlockDB." );
            }
            if ( max == 0 ) return new BlockDBEntry[0];
            if ( max < 0 ) throw new ArgumentOutOfRangeException( "max" );
            if ( selector == null ) throw new ArgumentNullException( "selector" );

            IBlockDBQueryProcessor processor;

            switch ( searchType ) {
                case BlockDBSearchType.ReturnAll:
                    processor = new ReturnAllProcessor( max, selector );
                    break;
                case BlockDBSearchType.ReturnNewest:
                    processor = new ReturnNewestProcessor( World.LoadMap(), max, selector );
                    break;
                case BlockDBSearchType.ReturnOldest:
                    processor = new ReturnOldestProcessor( World.LoadMap(), max, selector );
                    break;
                default:
                    throw new ArgumentOutOfRangeException( "searchType" );
            }

            Traverse( processor );
            return processor.GetResults();
        }


        /// <summary> Traverses the database, newest to oldest entries, processing each entry with the given IBlockDBQueryProcessor. </summary>
        /// <param name="processor"> Processor to use for each BlockDBEntry. </param>
        /// <exception cref="ArgumentNullException"> processor is null. </exception>
        /// <exception cref="InvalidOperationException"> BlockDB is disabled. </exception>
        /// <exception cref="EndOfStreamException"> The end of FBDB file is reached prematurely (corrupted file, or outside interference). </exception>
        /// <exception cref="DataMisalignedException"> FBDB file is not aligned to 20 bytes (likely corrupted). </exception>
        /// <exception cref="IOException"> An I/O error occurrs while trying to read FBDB file from disk. </exception>
        public void Traverse ( [NotNull] IBlockDBQueryProcessor processor ) {
            if ( processor == null ) throw new ArgumentNullException( "processor" );
            if ( !IsEnabled || !IsEnabledGlobally ) {
                throw new InvalidOperationException( "Trying to lookup on disabled BlockDB." );
            }

            IDisposable readLockHandle = null;
            try {
                if ( !locker.IsReadLockHeld ) {
                    readLockHandle = locker.WriteLock();
                }

                if ( isPreloaded ) {
                    // Entire database is in memory
                    fixed ( BlockDBEntry* entries = cacheStore ) {
                        for ( int i = CacheSize - 1; i >= 0; i-- ) {
                            if ( !processor.ProcessEntry( entries[i] ) ) {
                                return;
                            }
                        }
                    }

                } else {
                    // Search unflushed cache for matches
                    if ( LastFlushedIndex < CacheSize ) {
                        fixed ( BlockDBEntry* entries = cacheStore ) {
                            for ( int i = CacheSize - 1; i >= LastFlushedIndex; i-- ) {
                                if ( !processor.ProcessEntry( entries[i] ) ) {
                                    return;
                                }
                            }
                        }
                    }

                    // If we still have some searching to do, read the file
                    if ( !File.Exists( FileName ) ) return;
                    using ( FileStream fs = OpenRead() ) {
                        long length = fs.Length;
                        long bytesReadTotal = 0;
                        int bufferSize = ( int )Math.Min( SearchBufferSize, length );
                        byte[] buffer = new byte[bufferSize];

                        while ( bytesReadTotal < length ) {
                            // The file is scanned from the end of file towards the beginning
                            long offset = Math.Max( 0, length - bytesReadTotal - SearchBufferSize );
                            fs.Seek( offset, SeekOrigin.Begin );

                            // read a chunk of the .fbdb file into memory, linearly
                            int bytesToRead = ( int )Math.Min( length - bytesReadTotal, SearchBufferSize );
                            int bytesLeft = bytesToRead;
                            int bytesRead = 0;
                            while ( bytesLeft > 0 ) {
                                int readPass = fs.Read( buffer, bytesRead, bytesLeft );
                                if ( readPass == 0 ) throw new EndOfStreamException();
                                bytesRead += readPass;
                                bytesLeft -= readPass;
                            }
                            bytesReadTotal += bytesRead;

                            // search a chunk
                            fixed ( byte* parr = buffer ) {
                                BlockDBEntry* entries = ( BlockDBEntry* )parr;
                                int entryCount = bytesRead / sizeof( BlockDBEntry );
                                if ( bytesRead % sizeof( BlockDBEntry ) != 0 ) {
                                    throw new DataMisalignedException();
                                }
                                // iterate over the chunk backwards (newest to oldest)
                                for ( int i = entryCount - 1; i >= 0; i-- ) {
                                    if ( !processor.ProcessEntry( entries[i] ) ) {
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            } finally {
                if ( readLockHandle != null ) {
                    readLockHandle.Dispose();
                }
            }
        }

        #endregion


        #region Lookup processors

        sealed class ReturnAllProcessor : IBlockDBQueryProcessor {
            int count;
            readonly int max;
            readonly Func<BlockDBEntry, bool> selector;
            readonly List<BlockDBEntry> result = new List<BlockDBEntry>();


            public ReturnAllProcessor ( int max, Func<BlockDBEntry, bool> selector ) {
                this.max = max;
                this.selector = selector;
            }


            public bool ProcessEntry ( BlockDBEntry entry ) {
                if ( selector( entry ) ) {
                    result.Add( entry );
                    count++;
                    if ( count >= max ) {
                        return false;
                    }
                }
                return true;
            }


            public BlockDBEntry[] GetResults () {
                return result.ToArray();
            }
        }


        sealed class ReturnOldestProcessor : IBlockDBQueryProcessor {
            readonly Map map;
            int count;
            readonly int max;
            readonly Func<BlockDBEntry, bool> selector;
            readonly Dictionary<int, BlockDBEntry> result = new Dictionary<int, BlockDBEntry>();


            public ReturnOldestProcessor ( Map map, int max, Func<BlockDBEntry, bool> selector ) {
                this.max = max;
                this.selector = selector;
                this.map = map;
            }


            public bool ProcessEntry ( BlockDBEntry entry ) {
                if ( selector( entry ) ) {
                    int index = map.Index( entry.X, entry.Y, entry.Z );
                    result[index] = entry;
                    count++;
                    if ( count >= max ) {
                        return false;
                    }
                }
                return true;
            }

            public BlockDBEntry[] GetResults () {
                return result.Values.ToArray();
            }
        }


        sealed class ReturnNewestProcessor : IBlockDBQueryProcessor {
            readonly Map map;
            int count;
            readonly int max;
            readonly Func<BlockDBEntry, bool> selector;
            readonly Dictionary<int, BlockDBEntry> result = new Dictionary<int, BlockDBEntry>();


            public ReturnNewestProcessor ( Map map, int max, Func<BlockDBEntry, bool> selector ) {
                this.max = max;
                this.selector = selector;
                this.map = map;
            }


            public bool ProcessEntry ( BlockDBEntry entry ) {
                if ( selector( entry ) ) {
                    int index = map.Index( entry.X, entry.Y, entry.Z );
                    if ( !result.ContainsKey( index ) ) {
                        result.Add( index, entry );
                    }
                    count++;
                    if ( count >= max ) {
                        return false;
                    }
                }
                return true;
            }
            public BlockDBEntry[] GetResults () {
                return result.Values.ToArray();
            }
        }


        sealed class ExcludingReturnOldestProcessor : IBlockDBQueryProcessor {
            readonly Map map;
            int count;
            readonly int max;
            readonly Func<BlockDBEntry, bool> inclusionSelector;
            readonly Func<BlockDBEntry, bool> exclusionSelector;
            readonly HashSet<int> excluded = new HashSet<int>();
            readonly Dictionary<int, BlockDBEntry> result = new Dictionary<int, BlockDBEntry>();


            public ExcludingReturnOldestProcessor ( Map map, int max,
                                                   Func<BlockDBEntry, bool> inclusionSelector,
                                                   Func<BlockDBEntry, bool> exclusionSelector ) {
                this.max = max;
                this.inclusionSelector = inclusionSelector;
                this.exclusionSelector = exclusionSelector;
                this.map = map;
            }


            public bool ProcessEntry ( BlockDBEntry entry ) {
                if ( inclusionSelector( entry ) ) {
                    int index = map.Index( entry.X, entry.Y, entry.Z );
                    if ( !excluded.Contains( index ) ) {
                        if ( exclusionSelector( entry ) ) {
                            excluded.Add( index );
                        } else {
                            result[index] = entry;
                        }
                        count++;
                        if ( count >= max ) {
                            return false;
                        }
                    }
                }
                return true;
            }


            public BlockDBEntry[] GetResults () {
                return result.Values.ToArray();
            }
        }

        #endregion


        #region Lookup shortcuts

        /// <summary> Returns list of all changes done to the map at the given coordinate, newest to oldest. </summary>
        /// <param name="max"> Maximum number of changes to return. </param>
        /// <param name="coords"> Coordinate to search at. </param>
        /// <exception cref="ArgumentOutOfRangeException"> Given coordinates are outside the map. </exception>
        public BlockDBEntry[] Lookup ( int max, Vector3I coords ) {
            if ( !World.LoadMap().InBounds( coords ) ) {
                throw new ArgumentOutOfRangeException( "coords" );
            }
            return Lookup( max, BlockDBSearchType.ReturnAll,
                           entry => ( entry.X == coords.X && entry.Y == coords.Y && entry.Z == coords.Z ) );
        }


        public BlockDBEntry[] Lookup ( int max ) {
            return Lookup( max, BlockDBSearchType.ReturnOldest,
                           entry => true );
        }


        public BlockDBEntry[] Lookup ( int max, TimeSpan span ) {
            if ( span < TimeSpan.Zero ) throw new ArgumentOutOfRangeException( "span" );
            long ticks = DateTime.UtcNow.Subtract( span ).ToUnixTime();
            return Lookup( max, BlockDBSearchType.ReturnOldest,
                           entry => entry.Timestamp >= ticks );
        }


        public BlockDBEntry[] Lookup ( int max, [NotNull] BoundingBox area ) {
            if ( area == null ) throw new ArgumentNullException( "area" );
            return Lookup( max, BlockDBSearchType.ReturnOldest,
                           entry => area.Contains( entry.X, entry.Y, entry.Z ) );
        }


        public BlockDBEntry[] Lookup ( int max, [NotNull] BoundingBox area, TimeSpan span ) {
            if ( area == null ) throw new ArgumentNullException( "area" );
            if ( span < TimeSpan.Zero ) throw new ArgumentOutOfRangeException( "span" );
            long ticks = DateTime.UtcNow.Subtract( span ).ToUnixTime();
            return Lookup( max, BlockDBSearchType.ReturnOldest,
                           entry => entry.Timestamp >= ticks && area.Contains( entry.X, entry.Y, entry.Z ) );
        }


        public BlockDBEntry[] Lookup ( int max, [NotNull] PlayerInfo info, bool exclude ) {
            if ( info == null ) throw new ArgumentNullException( "info" );
            int pid = info.ID;
            Map map = World.LoadMap();

            IBlockDBQueryProcessor processor;
            if ( exclude ) {
                processor = new ExcludingReturnOldestProcessor( map, max,
                                                                entry => true,
                                                                entry => entry.PlayerID == pid );
            } else {
                processor = new ReturnOldestProcessor( map, max,
                                                       entry => entry.PlayerID == pid );
            }
            Traverse( processor );
            return processor.GetResults();
        }


        public BlockDBEntry[] Lookup ( int max, [NotNull] PlayerInfo info, bool exclude, TimeSpan span ) {
            if ( info == null ) throw new ArgumentNullException( "info" );
            if ( span < TimeSpan.Zero ) throw new ArgumentOutOfRangeException( "span" );
            int pid = info.ID;
            long ticks = DateTime.UtcNow.Subtract( span ).ToUnixTime();
            Map map = World.LoadMap();

            IBlockDBQueryProcessor processor;
            if ( exclude ) {
                processor = new ExcludingReturnOldestProcessor( map, max,
                                                                entry => entry.Timestamp >= ticks,
                                                                entry => entry.PlayerID == pid );
            } else {
                processor = new ReturnOldestProcessor( map, max,
                                                       entry => entry.Timestamp >= ticks && entry.PlayerID == pid );
            }
            Traverse( processor );
            return processor.GetResults();
        }


        public BlockDBEntry[] Lookup ( int max, [NotNull] PlayerInfo[] infos, bool exclude ) {
            if ( infos == null ) throw new ArgumentNullException( "infos" );
            if ( infos.Length == 0 ) throw new ArgumentException( "At least one PlayerInfo must be given", "infos" );
            if ( infos.Length == 1 ) return Lookup( max, infos[0], exclude );
            Map map = World.LoadMap();

            IBlockDBQueryProcessor processor;
            if ( exclude ) {
                processor = new ExcludingReturnOldestProcessor( map, max,
                                                                entry => true,
                                                                entry => infos.Any( t => entry.PlayerID == t.ID ) );
            } else {
                processor = new ReturnOldestProcessor( map, max,
                                                       entry => infos.Any( t => entry.PlayerID == t.ID ) );
            }
            Traverse( processor );
            return processor.GetResults();
        }


        public BlockDBEntry[] Lookup ( int max, [NotNull] PlayerInfo[] infos, bool exclude, TimeSpan span ) {
            if ( infos == null ) throw new ArgumentNullException( "infos" );
            if ( infos.Length == 0 ) throw new ArgumentException( "At least one PlayerInfo must be given", "infos" );
            if ( span < TimeSpan.Zero ) throw new ArgumentOutOfRangeException( "span" );
            if ( infos.Length == 1 ) return Lookup( max, infos[0], exclude, span );
            long ticks = DateTime.UtcNow.Subtract( span ).ToUnixTime();
            Map map = World.LoadMap();

            IBlockDBQueryProcessor processor;
            if ( exclude ) {
                // ReSharper disable ImplicitlyCapturedClosure
                processor = new ExcludingReturnOldestProcessor( map, max,
                                                                entry => entry.Timestamp >= ticks,
                                                                entry => infos.Any( t => entry.PlayerID == t.ID ) );
                // ReSharper restore ImplicitlyCapturedClosure
            } else {
                processor = new ReturnOldestProcessor( map, max,
                                                       entry => entry.Timestamp >= ticks &&
                                                                infos.Any( t => entry.PlayerID == t.ID ) );
            }
            Traverse( processor );
            return processor.GetResults();
        }


        public BlockDBEntry[] Lookup ( int max, [NotNull] BoundingBox area, [NotNull] PlayerInfo info, bool exclude ) {
            if ( area == null ) throw new ArgumentNullException( "area" );
            if ( info == null ) throw new ArgumentNullException( "info" );
            int pid = info.ID;
            Map map = World.LoadMap();

            IBlockDBQueryProcessor processor;
            if ( exclude ) {
                // ReSharper disable ImplicitlyCapturedClosure
                processor = new ExcludingReturnOldestProcessor( map, max,
                                                                entry => area.Contains( entry.X, entry.Y, entry.Z ),
                                                                entry => entry.PlayerID == pid );
                // ReSharper restore ImplicitlyCapturedClosure
            } else {
                processor = new ReturnOldestProcessor( map, max,
                                                       entry => area.Contains( entry.X, entry.Y, entry.Z ) &&
                                                                entry.PlayerID == pid );
            }
            Traverse( processor );
            return processor.GetResults();
        }


        public BlockDBEntry[] Lookup ( int max, [NotNull] BoundingBox area, [NotNull] PlayerInfo info, bool exclude, TimeSpan span ) {
            if ( area == null ) throw new ArgumentNullException( "area" );
            if ( info == null ) throw new ArgumentNullException( "info" );
            if ( span < TimeSpan.Zero ) throw new ArgumentOutOfRangeException( "span" );
            int pid = info.ID;
            long ticks = DateTime.UtcNow.Subtract( span ).ToUnixTime();
            Map map = World.LoadMap();

            IBlockDBQueryProcessor processor;
            if ( exclude ) {
                // ReSharper disable ImplicitlyCapturedClosure
                processor = new ExcludingReturnOldestProcessor( map, max,
                                                                entry => entry.Timestamp >= ticks &&
                                                                         area.Contains( entry.X, entry.Y, entry.Z ),
                                                                entry => entry.PlayerID == pid );
                // ReSharper restore ImplicitlyCapturedClosure
            } else {
                processor = new ReturnOldestProcessor( map, max,
                                                       entry => entry.Timestamp >= ticks &&
                                                                entry.PlayerID == pid &&
                                                                area.Contains( entry.X, entry.Y, entry.Z ) );
            }
            Traverse( processor );
            return processor.GetResults();
        }


        public BlockDBEntry[] Lookup ( int max, [NotNull] BoundingBox area, [NotNull] PlayerInfo[] infos, bool exclude ) {
            if ( area == null ) throw new ArgumentNullException( "area" );
            if ( infos == null ) throw new ArgumentNullException( "infos" );
            if ( infos.Length == 0 ) throw new ArgumentException( "At least one PlayerInfo must be given", "infos" );
            if ( infos.Length == 1 ) return Lookup( max, area, infos[0], exclude );
            Map map = World.LoadMap();

            IBlockDBQueryProcessor processor;
            if ( exclude ) {
                // ReSharper disable ImplicitlyCapturedClosure
                processor = new ExcludingReturnOldestProcessor( map, max,
                                                                entry => area.Contains( entry.X, entry.Y, entry.Z ),
                                                                entry => infos.Any( t => entry.PlayerID == t.ID ) );
                // ReSharper restore ImplicitlyCapturedClosure
            } else {
                processor = new ReturnOldestProcessor( map, max,
                                                       entry => area.Contains( entry.X, entry.Y, entry.Z ) &&
                                                                infos.Any( t => entry.PlayerID == t.ID ) );
            }
            Traverse( processor );
            return processor.GetResults();
        }


        public BlockDBEntry[] Lookup ( int max, [NotNull] BoundingBox area, [NotNull] PlayerInfo[] infos, bool exclude, TimeSpan span ) {
            if ( area == null ) throw new ArgumentNullException( "area" );
            if ( infos == null ) throw new ArgumentNullException( "infos" );
            if ( infos.Length == 0 ) throw new ArgumentException( "At least one PlayerInfo must be given", "infos" );
            if ( span < TimeSpan.Zero ) throw new ArgumentOutOfRangeException( "span" );
            if ( infos.Length == 1 ) return Lookup( max, area, infos[0], exclude, span );
            long ticks = DateTime.UtcNow.Subtract( span ).ToUnixTime();
            Map map = World.LoadMap();

            IBlockDBQueryProcessor processor;
            if ( exclude ) {
                // ReSharper disable ImplicitlyCapturedClosure
                processor = new ExcludingReturnOldestProcessor( map, max,
                                                                entry => entry.Timestamp >= ticks &&
                                                                         area.Contains( entry.X, entry.Y, entry.Z ),
                                                                entry => infos.Any( t => entry.PlayerID == t.ID ) );
                // ReSharper restore ImplicitlyCapturedClosure
            } else {
                processor = new ReturnOldestProcessor( map, max,
                                                       entry => entry.Timestamp >= ticks &&
                                                                area.Contains( entry.X, entry.Y, entry.Z ) &&
                                                                infos.Any( t => entry.PlayerID == t.ID ) );
            }
            Traverse( processor );
            return processor.GetResults();
        }

        #endregion


        /// <summary> Acquires and returns an exclusive (write) lock for this BlockDB.
        /// Disposing the object returned by this method releases the lock. </summary>
        public IDisposable GetWriteLock () {
            return locker.WriteLock();
        }


        /// <summary> Acquires and returns a shared (read) lock for this BlockDB.
        /// Disposing the object returned by this method releases the lock. </summary>
        public IDisposable GetReadLock () {
            return locker.ReadLock();
        }


        readonly ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
        const int SearchBufferSize = 1000000; // in bytes


        #region Serialization

        public const string XmlRootName = "BlockDB";


        public XElement SaveSettings () {
            return SaveSettings( XmlRootName );
        }


        public XElement SaveSettings ( string rootName ) {
            XElement root = new XElement( rootName );
            root.Add( new XAttribute( "enabled", EnabledState ) );
            root.Add( new XAttribute( "preload", IsPreloaded ) );
            if ( HasLimit ) {
                root.Add( new XAttribute( "limit", Limit ) );
            }
            if ( HasTimeLimit ) {
                root.Add( new XAttribute( "timeLimit", ( int )TimeLimit.TotalSeconds ) );
            }
            return root;
        }


        public void LoadSettings ( XElement el ) {
            XAttribute temp;
            if ( ( temp = el.Attribute( "enabled" ) ) != null ) {
                YesNoAuto enabledStateTemp;
                if ( EnumUtil.TryParse( temp.Value, out enabledStateTemp, true ) ) {
                    EnabledState = enabledStateTemp;
                } else {
                    Logger.Log( LogType.Warning,
                                "WorldManager: Could not parse BlockDB \"enabled\" attribute of world \"{0}\", assuming \"Auto\"",
                                World.Name );
                    EnabledState = YesNoAuto.Auto;
                }
            }

            if ( ( temp = el.Attribute( "preload" ) ) != null ) {
                bool isPreloadedTemp;
                if ( Boolean.TryParse( temp.Value, out isPreloadedTemp ) ) {
                    IsPreloaded = isPreloadedTemp;
                } else {
                    Logger.Log( LogType.Warning,
                                "WorldManager: Could not parse BlockDB \"preload\" attribute of world \"{0}\", assuming NOT preloaded.",
                                World.Name );
                }
            }
            if ( ( temp = el.Attribute( "limit" ) ) != null ) {
                int limitTemp;
                if ( Int32.TryParse( temp.Value, out limitTemp ) ) {
                    Limit = limitTemp;
                } else {
                    Logger.Log( LogType.Warning,
                                "WorldManager: Could not parse BlockDB \"limit\" attribute of world \"{0}\", assuming NO limit.",
                                World.Name );
                }
            }
            if ( ( temp = el.Attribute( "timeLimit" ) ) != null ) {
                int timeLimitSeconds;
                if ( Int32.TryParse( temp.Value, out timeLimitSeconds ) ) {
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

        /// <summary> Whether BlockDB was enabled at startup.
        /// Changing this setting currently requires a server restart. </summary>
        public static bool IsEnabledGlobally { get; private set; }


        internal static void Init () {
            Paths.TestDirectory( "BlockDB", Paths.BlockDBPath, true );
            Player.PlacedBlock += OnPlayerPlacedBlock;
            IsEnabledGlobally = true;
        }


        static void OnPlayerPlacedBlock ( object sender, [NotNull] PlayerPlacedBlockEventArgs e ) {
            if ( e == null ) throw new ArgumentNullException( "e" );
            World world = e.Map.World;
            if ( world != null && world.BlockDB.IsEnabled ) {
                BlockDBEntry newEntry = new BlockDBEntry( ( int )DateTime.UtcNow.ToUnixTime(),
                                                          e.Player.Info.ID,
                                                          e.Coords,
                                                          e.OldBlock,
                                                          e.NewBlock,
                                                          e.Context );
                world.BlockDB.AddEntry( newEntry );
            }
        }

        #endregion
    }


    /// <summary> Class used to process results of traversing the BlockDB. </summary>
    public interface IBlockDBQueryProcessor {
        /// <summary> Callback for each BlockDBEntry. </summary>
        /// <param name="entry"> Current entry. </param>
        /// <returns> True if traversal should continue. False if traversal should end. </returns>
        bool ProcessEntry ( BlockDBEntry entry );
        /// <summary> Returns the gathered results, as an array of BlockDBEntry structs. </summary>
        BlockDBEntry[] GetResults ();
    }
}