// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using fCraft.AutoRank;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Several yet-undocumented commands, mostly related to AutoRank. </summary>
    static class MaintenanceCommands {

        internal static void Init() {
            CommandManager.RegisterCommand( CdDumpStats );

            CommandManager.RegisterCommand( CdMassRank );
            CommandManager.RegisterCommand( CdAutoRankAll );
            CommandManager.RegisterCommand( CdSetInfo );

            CommandManager.RegisterCommand( CdReload );

            CommandManager.RegisterCommand( CdShutdown );
            CommandManager.RegisterCommand( CdRestart );

            CommandManager.RegisterCommand( CdPruneDB );

            CommandManager.RegisterCommand( CdImport );

            CommandManager.RegisterCommand( CdInfoSwap );

#if DEBUG
            CommandManager.RegisterCommand( new CommandDescriptor {
                Name = "BUM",
                IsHidden = true,
                Category = CommandCategory.Maintenance | CommandCategory.Debug,
                Help = "Bandwidth Use Mode statistics.",
                Handler = delegate( Player player, Command cmd ) {
                    string newModeName = cmd.Next();
                    if( newModeName == null ) {
                        player.Message( "{0}: S: {1}  R: {2}  S/s: {3:0.0}  R/s: {4:0.0}",
                                        player.BandwidthUseMode,
                                        player.BytesSent,
                                        player.BytesReceived,
                                        player.BytesSentRate,
                                        player.BytesReceivedRate );
                    } else {
                        var newMode = (BandwidthUseMode)Enum.Parse( typeof( BandwidthUseMode ), newModeName, true );
                        player.BandwidthUseMode = newMode;
                        player.Info.BandwidthUseMode = newMode;
                    }
                }
            } );

            CommandManager.RegisterCommand( new CommandDescriptor {
                Name = "BDBDB",
                IsHidden = true,
                Category = CommandCategory.Maintenance | CommandCategory.Debug,
                Help = "BlockDB Debug",
                Handler = delegate( Player player, Command cmd ) {
                    if( player.World == null ) PlayerOpException.ThrowNoWorld( player );
                    BlockDB db = player.World.BlockDB;
                    lock( db.SyncRoot ) {
                        player.Message( "BlockDB: CAP={0} SZ={1} FI={2}",
                                        db.CacheCapacity, db.CacheSize, db.LastFlushedIndex );
                    }
                }
            } );
#endif
        }


        #region DumpStats

        static readonly CommandDescriptor CdDumpStats = new CommandDescriptor {
            Name = "DumpStats",
            Category = CommandCategory.Maintenance,
            IsConsoleSafe = true,
            IsHidden = true,
            Permissions = new[] { Permission.EditPlayerDB },
            Help = "Writes out a number of statistics about the server. " +
                   "Only non-banned players active in the last 30 days are counted.",
            Usage = "/DumpStats FileName",
            Handler = DumpStatsHandler
        };

        const int TopPlayersToList = 5;

        static void DumpStatsHandler( Player player, Command cmd ) {
            string fileName = cmd.Next();
            if( fileName == null ) {
                CdDumpStats.PrintUsage( player );
                return;
            }

            if( !Paths.Contains( Paths.WorkingPath, fileName ) ) {
                player.MessageUnsafePath();
                return;
            }

            // ReSharper disable AssignNullToNotNullAttribute
            if( Paths.IsProtectedFileName( Path.GetFileName( fileName ) ) ) {
                // ReSharper restore AssignNullToNotNullAttribute
                player.Message( "You may not use this file." );
                return;
            }

            string extension = Path.GetExtension( fileName );
            if( extension == null || !extension.Equals( ".txt", StringComparison.OrdinalIgnoreCase ) ) {
                player.Message( "Stats filename must end with .txt" );
                return;
            }

            if( File.Exists( fileName ) && !cmd.IsConfirmed ) {
                player.Confirm( cmd, "File \"{0}\" already exists. Overwrite?", Path.GetFileName( fileName ) );
                return;
            }

            if( !Paths.TestFile( "DumpStats file", fileName, false, FileAccess.Write ) ) {
                player.Message( "Cannot create specified file. See log for details." );
                return;
            }

            PlayerInfo[] infos;
            using( FileStream fs = File.Create( fileName ) ) {
                using( StreamWriter writer = new StreamWriter( fs ) ) {
                    infos = PlayerDB.PlayerInfoList;
                    if( infos.Length == 0 ) {
                        writer.WriteLine( "(TOTAL) (0 players)" );
                        writer.WriteLine();
                    } else {
                        DumpPlayerGroupStats( writer, infos, "(TOTAL)" );
                    }

                    List<PlayerInfo> rankPlayers = new List<PlayerInfo>();
                    foreach( Rank rank in RankManager.Ranks ) {
                        // ReSharper disable LoopCanBeConvertedToQuery
                        for( int i = 0; i < infos.Length; i++ ) {
                            // ReSharper restore LoopCanBeConvertedToQuery
                            if( infos[i].Rank == rank ) rankPlayers.Add( infos[i] );
                        }
                        if( rankPlayers.Count == 0 ) {
                            writer.WriteLine( "{0}: 0 players, 0 banned, 0 inactive", rank.Name );
                            writer.WriteLine();
                        } else {
                            DumpPlayerGroupStats( writer, rankPlayers, rank.Name );
                        }
                        rankPlayers.Clear();
                    }
                }
            }

            player.Message( "Stats saved to \"{0}\"", fileName );
        }

        static void DumpPlayerGroupStats( TextWriter writer, IList<PlayerInfo> infos, string groupName ) {
            RankStats stat = new RankStats();
            foreach( Rank rank2 in RankManager.Ranks ) {
                stat.PreviousRank.Add( rank2, 0 );
            }

            int totalCount = infos.Count;
            int bannedCount = infos.Count( info => info.IsBanned );
            int inactiveCount = infos.Count( info => info.TimeSinceLastSeen.TotalDays >= 30 );
            infos = infos.Where( info => (info.TimeSinceLastSeen.TotalDays < 30 && !info.IsBanned) ).ToList();

            if( infos.Count == 0 ) {
                writer.WriteLine( "{0}: {1} players, {2} banned, {3} inactive",
                                  groupName, totalCount, bannedCount, inactiveCount );
                writer.WriteLine();
                return;
            }

            for( int i = 0; i < infos.Count; i++ ) {
                stat.TimeSinceFirstLogin += infos[i].TimeSinceFirstLogin;
                stat.TimeSinceLastLogin += infos[i].TimeSinceLastLogin;
                stat.TotalTime += infos[i].TotalTime;
                stat.BlocksBuilt += infos[i].BlocksBuilt;
                stat.BlocksDeleted += infos[i].BlocksDeleted;
                stat.BlocksDrawn += infos[i].BlocksDrawn;
                stat.TimesVisited += infos[i].TimesVisited;
                stat.MessagesWritten += infos[i].MessagesWritten;
                stat.TimesKicked += infos[i].TimesKicked;
                stat.TimesKickedOthers += infos[i].TimesKickedOthers;
                stat.TimesBannedOthers += infos[i].TimesBannedOthers;
                if( infos[i].PreviousRank != null ) stat.PreviousRank[infos[i].PreviousRank]++;
            }

            stat.BlockRatio = stat.BlocksBuilt / (double)Math.Max( stat.BlocksDeleted, 1 );
            stat.BlocksChanged = stat.BlocksDeleted + stat.BlocksBuilt;


            stat.TimeSinceFirstLoginMedian = DateTime.UtcNow.Subtract( infos.OrderByDescending( info => info.FirstLoginDate )
                                                                            .ElementAt( infos.Count / 2 ).FirstLoginDate );
            stat.TimeSinceLastLoginMedian = DateTime.UtcNow.Subtract( infos.OrderByDescending( info => info.LastLoginDate )
                                                                           .ElementAt( infos.Count / 2 ).LastLoginDate );
            stat.TotalTimeMedian = infos.OrderByDescending( info => info.TotalTime ).ElementAt( infos.Count / 2 ).TotalTime;
            stat.BlocksBuiltMedian = infos.OrderByDescending( info => info.BlocksBuilt ).ElementAt( infos.Count / 2 ).BlocksBuilt;
            stat.BlocksDeletedMedian = infos.OrderByDescending( info => info.BlocksDeleted ).ElementAt( infos.Count / 2 ).BlocksDeleted;
            stat.BlocksDrawnMedian = infos.OrderByDescending( info => info.BlocksDrawn ).ElementAt( infos.Count / 2 ).BlocksDrawn;
            PlayerInfo medianBlocksChangedPlayerInfo = infos.OrderByDescending( info => (info.BlocksDeleted + info.BlocksBuilt) ).ElementAt( infos.Count / 2 );
            stat.BlocksChangedMedian = medianBlocksChangedPlayerInfo.BlocksDeleted + medianBlocksChangedPlayerInfo.BlocksBuilt;
            PlayerInfo medianBlockRatioPlayerInfo = infos.OrderByDescending( info => (info.BlocksBuilt / (double)Math.Max( info.BlocksDeleted, 1 )) )
                                                    .ElementAt( infos.Count / 2 );
            stat.BlockRatioMedian = medianBlockRatioPlayerInfo.BlocksBuilt / (double)Math.Max( medianBlockRatioPlayerInfo.BlocksDeleted, 1 );
            stat.TimesVisitedMedian = infos.OrderByDescending( info => info.TimesVisited ).ElementAt( infos.Count / 2 ).TimesVisited;
            stat.MessagesWrittenMedian = infos.OrderByDescending( info => info.MessagesWritten ).ElementAt( infos.Count / 2 ).MessagesWritten;
            stat.TimesKickedMedian = infos.OrderByDescending( info => info.TimesKicked ).ElementAt( infos.Count / 2 ).TimesKicked;
            stat.TimesKickedOthersMedian = infos.OrderByDescending( info => info.TimesKickedOthers ).ElementAt( infos.Count / 2 ).TimesKickedOthers;
            stat.TimesBannedOthersMedian = infos.OrderByDescending( info => info.TimesBannedOthers ).ElementAt( infos.Count / 2 ).TimesBannedOthers;


            stat.TopTimeSinceFirstLogin = infos.OrderBy( info => info.FirstLoginDate ).ToArray();
            stat.TopTimeSinceLastLogin = infos.OrderBy( info => info.LastLoginDate ).ToArray();
            stat.TopTotalTime = infos.OrderByDescending( info => info.TotalTime ).ToArray();
            stat.TopBlocksBuilt = infos.OrderByDescending( info => info.BlocksBuilt ).ToArray();
            stat.TopBlocksDeleted = infos.OrderByDescending( info => info.BlocksDeleted ).ToArray();
            stat.TopBlocksDrawn = infos.OrderByDescending( info => info.BlocksDrawn ).ToArray();
            stat.TopBlocksChanged = infos.OrderByDescending( info => (info.BlocksDeleted + info.BlocksBuilt) ).ToArray();
            stat.TopBlockRatio = infos.OrderByDescending( info => (info.BlocksBuilt / (double)Math.Max( info.BlocksDeleted, 1 )) ).ToArray();
            stat.TopTimesVisited = infos.OrderByDescending( info => info.TimesVisited ).ToArray();
            stat.TopMessagesWritten = infos.OrderByDescending( info => info.MessagesWritten ).ToArray();
            stat.TopTimesKicked = infos.OrderByDescending( info => info.TimesKicked ).ToArray();
            stat.TopTimesKickedOthers = infos.OrderByDescending( info => info.TimesKickedOthers ).ToArray();
            stat.TopTimesBannedOthers = infos.OrderByDescending( info => info.TimesBannedOthers ).ToArray();


            writer.WriteLine( "{0}: {1} players, {2} banned, {3} inactive",
                              groupName, totalCount, bannedCount, inactiveCount );
            writer.WriteLine( "    TimeSinceFirstLogin: {0} mean,  {1} median,  {2} total",
                              TimeSpan.FromTicks( stat.TimeSinceFirstLogin.Ticks / infos.Count ).ToCompactString(),
                              stat.TimeSinceFirstLoginMedian.ToCompactString(),
                              stat.TimeSinceFirstLogin.ToCompactString() );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimeSinceFirstLogin.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimeSinceFirstLogin.ToCompactString(), info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimeSinceFirstLogin.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimeSinceFirstLogin.ToCompactString(), info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimeSinceFirstLogin ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimeSinceFirstLogin.ToCompactString(), info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TimeSinceLastLogin: {0} mean,  {1} median,  {2} total",
                              TimeSpan.FromTicks( stat.TimeSinceLastLogin.Ticks / infos.Count ).ToCompactString(),
                              stat.TimeSinceLastLoginMedian.ToCompactString(),
                              stat.TimeSinceLastLogin.ToCompactString() );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimeSinceLastLogin.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimeSinceLastLogin.ToCompactString(), info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimeSinceLastLogin.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimeSinceLastLogin.ToCompactString(), info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimeSinceLastLogin ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimeSinceLastLogin.ToCompactString(), info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TotalTime: {0} mean,  {1} median,  {2} total",
                              TimeSpan.FromTicks( stat.TotalTime.Ticks / infos.Count ).ToCompactString(),
                              stat.TotalTimeMedian.ToCompactString(),
                              stat.TotalTime.ToCompactString() );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTotalTime.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TotalTime.ToCompactString(), info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTotalTime.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TotalTime.ToCompactString(), info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTotalTime ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TotalTime.ToCompactString(), info.Name );
                }
            }
            writer.WriteLine();



            writer.WriteLine( "    BlocksBuilt: {0} mean,  {1} median,  {2} total",
                              stat.BlocksBuilt / infos.Count,
                              stat.BlocksBuiltMedian,
                              stat.BlocksBuilt );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopBlocksBuilt.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksBuilt, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopBlocksBuilt.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksBuilt, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopBlocksBuilt ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksBuilt, info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    BlocksDeleted: {0} mean,  {1} median,  {2} total",
                              stat.BlocksDeleted / infos.Count,
                              stat.BlocksDeletedMedian,
                              stat.BlocksDeleted );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopBlocksDeleted.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksDeleted, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopBlocksDeleted.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksDeleted, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopBlocksDeleted ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksDeleted, info.Name );
                }
            }
            writer.WriteLine();



            writer.WriteLine( "    BlocksChanged: {0} mean,  {1} median,  {2} total",
                              stat.BlocksChanged / infos.Count,
                              stat.BlocksChangedMedian,
                              stat.BlocksChanged );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopBlocksChanged.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", (info.BlocksDeleted + info.BlocksBuilt), info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopBlocksChanged.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", (info.BlocksDeleted + info.BlocksBuilt), info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopBlocksChanged ) {
                    writer.WriteLine( "        {0,20}  {1}", (info.BlocksDeleted + info.BlocksBuilt), info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    BlocksDrawn: {0} mean,  {1} median,  {2} total",
                              stat.BlocksDrawn / infos.Count,
                              stat.BlocksDrawnMedian,
                              stat.BlocksDrawn );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopBlocksDrawn.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksDrawn, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopBlocksDrawn.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksDrawn, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopBlocksDrawn ) {
                    writer.WriteLine( "        {0,20}  {1}", info.BlocksDrawn, info.Name );
                }
            }


            writer.WriteLine( "    BlockRatio: {0:0.000} mean,  {1:0.000} median",
                              stat.BlockRatio,
                              stat.BlockRatioMedian );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopBlockRatio.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20:0.000}  {1}", (info.BlocksBuilt / (double)Math.Max( info.BlocksDeleted, 1 )), info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopBlockRatio.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20:0.000}  {1}", (info.BlocksBuilt / (double)Math.Max( info.BlocksDeleted, 1 )), info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopBlockRatio ) {
                    writer.WriteLine( "        {0,20:0.000}  {1}", (info.BlocksBuilt / (double)Math.Max( info.BlocksDeleted, 1 )), info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TimesVisited: {0} mean,  {1} median,  {2} total",
                              stat.TimesVisited / infos.Count,
                              stat.TimesVisitedMedian,
                              stat.TimesVisited );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimesVisited.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesVisited, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimesVisited.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesVisited, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimesVisited ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesVisited, info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    MessagesWritten: {0} mean,  {1} median,  {2} total",
                              stat.MessagesWritten / infos.Count,
                              stat.MessagesWrittenMedian,
                              stat.MessagesWritten );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopMessagesWritten.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.MessagesWritten, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopMessagesWritten.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.MessagesWritten, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopMessagesWritten ) {
                    writer.WriteLine( "        {0,20}  {1}", info.MessagesWritten, info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TimesKicked: {0:0.0} mean,  {1} median,  {2} total",
                              stat.TimesKicked / (double)infos.Count,
                              stat.TimesKickedMedian,
                              stat.TimesKicked );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimesKicked.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesKicked, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimesKicked.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesKicked, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimesKicked ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesKicked, info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TimesKickedOthers: {0:0.0} mean,  {1} median,  {2} total",
                              stat.TimesKickedOthers / (double)infos.Count,
                              stat.TimesKickedOthersMedian,
                              stat.TimesKickedOthers );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimesKickedOthers.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesKickedOthers, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimesKickedOthers.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesKickedOthers, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimesKickedOthers ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesKickedOthers, info.Name );
                }
            }
            writer.WriteLine();


            writer.WriteLine( "    TimesBannedOthers: {0:0.0} mean,  {1} median,  {2} total",
                              stat.TimesBannedOthers / (double)infos.Count,
                              stat.TimesBannedOthersMedian,
                              stat.TimesBannedOthers );
            if( infos.Count() > TopPlayersToList * 2 + 1 ) {
                foreach( PlayerInfo info in stat.TopTimesBannedOthers.Take( TopPlayersToList ) ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesBannedOthers, info.Name );
                }
                writer.WriteLine( "                           ...." );
                foreach( PlayerInfo info in stat.TopTimesBannedOthers.Reverse().Take( TopPlayersToList ).Reverse() ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesBannedOthers, info.Name );
                }
            } else {
                foreach( PlayerInfo info in stat.TopTimesBannedOthers ) {
                    writer.WriteLine( "        {0,20}  {1}", info.TimesBannedOthers, info.Name );
                }
            }
            writer.WriteLine();
        }


        sealed class RankStats {
            public TimeSpan TimeSinceFirstLogin;
            public TimeSpan TimeSinceLastLogin;
            public TimeSpan TotalTime;
            public long BlocksBuilt;
            public long BlocksDeleted;
            public long BlocksChanged;
            public long BlocksDrawn;
            public double BlockRatio;
            public long TimesVisited;
            public long MessagesWritten;
            public long TimesKicked;
            public long TimesKickedOthers;
            public long TimesBannedOthers;
            public readonly Dictionary<Rank, int> PreviousRank = new Dictionary<Rank, int>();

            public TimeSpan TimeSinceFirstLoginMedian;
            public TimeSpan TimeSinceLastLoginMedian;
            public TimeSpan TotalTimeMedian;
            public int BlocksBuiltMedian;
            public int BlocksDeletedMedian;
            public int BlocksChangedMedian;
            public long BlocksDrawnMedian;
            public double BlockRatioMedian;
            public int TimesVisitedMedian;
            public int MessagesWrittenMedian;
            public int TimesKickedMedian;
            public int TimesKickedOthersMedian;
            public int TimesBannedOthersMedian;

            public PlayerInfo[] TopTimeSinceFirstLogin;
            public PlayerInfo[] TopTimeSinceLastLogin;
            public PlayerInfo[] TopTotalTime;
            public PlayerInfo[] TopBlocksBuilt;
            public PlayerInfo[] TopBlocksDeleted;
            public PlayerInfo[] TopBlocksChanged;
            public PlayerInfo[] TopBlocksDrawn;
            public PlayerInfo[] TopBlockRatio;
            public PlayerInfo[] TopTimesVisited;
            public PlayerInfo[] TopMessagesWritten;
            public PlayerInfo[] TopTimesKicked;
            public PlayerInfo[] TopTimesKickedOthers;
            public PlayerInfo[] TopTimesBannedOthers;
        }

        #endregion


        #region AutoRank

        static readonly CommandDescriptor CdAutoRankAll = new CommandDescriptor {
            Name = "AutoRankAll",
            Category = CommandCategory.Maintenance | CommandCategory.Moderation,
            IsConsoleSafe = true,
            IsHidden = true,
            Permissions = new[] { Permission.EditPlayerDB, Permission.Promote, Permission.Demote },
            Help = "If AutoRank is disabled, it can still be called manually using this command.",
            Usage = "/AutoRankAll [FromRank]",
            Handler = AutoRankAllHandler
        };

        static void AutoRankAllHandler( Player player, Command cmd ) {
            string rankName = cmd.Next();
            Rank rank = null;
            if( rankName != null ) {
                rank = RankManager.FindRank( rankName );
                if( rank == null ) {
                    player.MessageNoRank( rankName );
                    return;
                }
            }

            PlayerInfo[] list;
            if( rank == null ) {
                list = PlayerDB.PlayerInfoList;
            } else {
                list = PlayerDB.PlayerInfoList.Where( p => p.Rank == rank ).ToArray();
            }
            DoAutoRankAll( player, list, false, "~AutoRankAll" );
        }

        internal static void DoAutoRankAll( [NotNull] Player player, [NotNull] PlayerInfo[] list, bool silent, string message ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( list == null ) throw new ArgumentNullException( "list" );

            if( !AutoRankManager.HasCriteria ) {
                player.Message( "AutoRankAll: No criteria found." );
                return;
            }

            player.Message( "AutoRankAll: Evaluating {0} players...", list.Length );

            Stopwatch sw = Stopwatch.StartNew();
            int promoted = 0, demoted = 0;
            for( int i = 0; i < list.Length; i++ ) {
                Rank newRank = AutoRankManager.Check( list[i] );
                if( newRank != null ) {
                    if( newRank > list[i].Rank ) {
                        promoted++;
                    } else if( newRank < list[i].Rank ) {
                        demoted++;
                    }
                    try {
                        list[i].ChangeRank( player, newRank, message, !silent, true, true );
                    } catch (PlayerOpException ex){
                        player.Message( ex.MessageColored );
                    }
                }
            }
            sw.Stop();
            player.Message( "AutoRankAll: Worked for {0}ms, {1} players promoted, {2} demoted.", sw.ElapsedMilliseconds, promoted, demoted );
        }

        #endregion


        #region MassRank

        static readonly CommandDescriptor CdMassRank = new CommandDescriptor {
            Name = "MassRank",
            Category = CommandCategory.Maintenance | CommandCategory.Moderation,
            IsHidden = true,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.EditPlayerDB, Permission.Promote, Permission.Demote },
            Help = "",
            Usage = "/MassRank FromRank ToRank Reason",
            Handler = MassRankHandler
        };

        static void MassRankHandler( Player player, Command cmd ) {
            string fromRankName = cmd.Next();
            string toRankName = cmd.Next();
            string reason = cmd.NextAll();
            if( fromRankName == null || toRankName == null ) {
                CdMassRank.PrintUsage( player );
                return;
            }

            Rank fromRank = RankManager.FindRank( fromRankName );
            if( fromRank == null ) {
                player.MessageNoRank( fromRankName );
                return;
            }

            Rank toRank = RankManager.FindRank( toRankName );
            if( toRank == null ) {
                player.MessageNoRank( toRankName );
                return;
            }

            if( fromRank == toRank ) {
                player.Message( "Ranks must be different" );
                return;
            }

            int playerCount = fromRank.PlayerCount;
            string verb = (fromRank > toRank ? "demot" : "promot");

            if( !cmd.IsConfirmed ) {
                player.Confirm( cmd, "{0}e {1} players?", verb.UppercaseFirst(), playerCount );
                return;
            }

            player.Message( "MassRank: {0}ing {1} players...",
                            verb, playerCount );

            int affected = PlayerDB.MassRankChange( player, fromRank, toRank, reason );
            player.Message( "MassRank: done, {0} records affected.", affected );
        }

        #endregion


        #region SetInfo

        static readonly CommandDescriptor CdSetInfo = new CommandDescriptor {
            Name = "SetInfo",
            Category = CommandCategory.Maintenance | CommandCategory.Moderation,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.EditPlayerDB },
            Help = "Allows direct editing of players' database records. List of editable properties: " +
                   "BanReason, DisplayedName, KickReason, PreviousRank, RankChangeType, " +
                   "RankReason, TimesKicked, TotalTime, UnbanReason. For detailed help see &H/Help SetInfo <Property>",
            HelpSections = new Dictionary<string, string>{
                { "banreason",      "&H/SetInfo <PlayerName> BanReason <Reason>\n&S" +
                                    "Changes ban reason for the given player. Original ban reason is preserved in the logs." },
                { "displayedname",  "&H/SetInfo <RealPlayerName> DisplayedName <DisplayedName>\n&S" +
                                    "Sets or resets the way player's name is displayed in chat. "+
                                    "Any printable symbols or color codes may be used in the displayed name. "+
                                    "Note that player's real name is still used in logs and on the in-game player list. "+
                                    "To remove a custom name, type \"&H/SetInfo <RealName> DisplayedName&S\" (omit the name)." },
                { "kickreason",     "&H/SetInfo <PlayerName> KickReason <Reason>\n&S" +
                                    "Changes reason of most-recent kick for the given player. " +
                                    "Original kick reason is preserved in the logs." },
                { "previousrank",   "&H/SetInfo <PlayerName> PreviousRank <RankName>\n&S" +
                                    "Changes previous rank held by the player. " +
                                    "To reset previous rank to \"none\" (will show as \"default\" in &H/Info&S), " +
                                    "type \"&H/SetInfo <Name> PreviousRank&S\" (omit the rank name)." },
                { "rankchangetype", "&H/SetInfo <PlayerName> RankChangeType <Type>\n&S" +
                                    "Sets the type of rank change. <Type> can be: Promoted, Demoted, AutoPromoted, AutoDemoted." },
                { "rankreason",     "&H/SetInfo <PlayerName> RankReason <Reason>\n&S" +
                                    "Changes promotion/demotion reason for the given player. "+
                                    "Original promotion/demotion reason is preserved in the logs." },
                { "timeskicked",    "&H/SetInfo <PlayerName> TimesKicked <#>\n&S" +
                                    "Changes the number of times that a player has been kicked. "+
                                    "Acceptible value range: 0-9999" },
                { "totaltime",      "&H/SetInfo <PlayerName> TotalTime <Time>\n&S" +
                                    "Changes the amount of game time that the player has on record. " +
                                    "Accepts values in the common compact time-span format." },
                { "unbanreason",    "&H/SetInfo <PlayerName> UnbanReason <Reason>\n&S" +
                                    "Changes unban reason for the given player. " +
                                    "Original unban reason is preserved in the logs." }
            },
            Usage = "/SetInfo <PlayerName> <Property> <Value>",
            Handler = SetInfoHandler
        };

        static void SetInfoHandler( Player player, Command cmd ) {
            string targetName = cmd.Next();
            string propertyName = cmd.Next();
            string valName = cmd.NextAll();

            if( targetName == null || propertyName == null ) {
                CdSetInfo.PrintUsage( player );
                return;
            }

            PlayerInfo info = PlayerDB.FindPlayerInfoOrPrintMatches( player, targetName );
            if( info == null ) return;

            switch( propertyName.ToLower() ) {
                case "timeskicked":
                    int oldTimesKicked = info.TimesKicked;
                    if( ValidateInt( valName, 0, 9999 ) ) {
                        info.TimesKicked = Int32.Parse( valName );
                        player.Message( "SetInfo: TimesKicked for {0}&S changed from {1} to {2}",
                                        info.ClassyName,
                                        oldTimesKicked,
                                        info.TimesKicked );
                        break;
                    } else {
                        player.Message( "SetInfo: TimesKicked value acceptible (Acceptible value range: 0-9999)" );
                        return;
                    }

                case "previousrank":
                    Rank newPreviousRank;
                    if( valName.Length > 0 ) {
                        newPreviousRank = RankManager.FindRank( valName );
                        if( newPreviousRank == null ) {
                            player.MessageNoRank( valName );
                            return;
                        }
                    } else {
                        newPreviousRank = null;
                    }

                    Rank oldPreviousRank = info.PreviousRank;

                    if( newPreviousRank == oldPreviousRank ) {
                        if( newPreviousRank == null ) {
                            player.Message( "SetInfo: PreviousRank for {0}&S is not set.",
                                            info.ClassyName );
                        } else {
                            player.Message( "SetInfo: PreviousRank for {0}&S is already set to {1}",
                                            info.ClassyName,
                                            newPreviousRank.ClassyName );
                        }
                        return;
                    }
                    info.PreviousRank = newPreviousRank;

                    if( oldPreviousRank == null ) {
                        player.Message( "SetInfo: PreviousRank for {0}&S set to {1}&",
                                        info.ClassyName,
                                        newPreviousRank.ClassyName );
                    } else if( newPreviousRank == null ) {
                        player.Message( "SetInfo: PreviousRank for {0}&S was reset (was {1}&S)",
                                        info.ClassyName,
                                        oldPreviousRank.ClassyName );
                    } else {
                        player.Message( "SetInfo: PreviousRank for {0}&S changed from {1}&S to {2}",
                                        info.ClassyName,
                                        oldPreviousRank.ClassyName,
                                        newPreviousRank.ClassyName );
                    }
                    break;

                case "totaltime":
                    TimeSpan newTotalTime;
                    TimeSpan oldTotalTime = info.TotalTime;
                    if( valName.TryParseMiniTimespan( out newTotalTime ) ) {
                        if( newTotalTime > DateTimeUtil.MaxTimeSpan ) {
                            player.MessageMaxTimeSpan();
                            return;
                        }
                        info.TotalTime = newTotalTime;
                        player.Message( "SetInfo: TotalTime for {0}&S changed from {1} ({2}) to {3} ({4})",
                                        info.ClassyName,
                                        oldTotalTime.ToMiniString(),
                                        oldTotalTime.ToCompactString(),
                                        info.TotalTime.ToMiniString(),
                                        info.TotalTime.ToCompactString() );
                        break;
                    } else {
                        player.Message( "SetInfo: Could not parse value given for TotalTime." );
                        return;
                    }

                case "rankchangetype":
                    RankChangeType oldType = info.RankChangeType;
                    try {
                        info.RankChangeType = (RankChangeType)Enum.Parse( typeof( RankChangeType ), valName, true );
                    } catch( ArgumentException ) {
                        player.Message( "SetInfo: Could not parse RankChangeType. Allowed values: {0}",
                                        String.Join( ", ", Enum.GetNames( typeof( RankChangeType ) ) ) );
                        return;
                    }
                    player.Message( "SetInfo: RankChangeType for {0}&S changed from {1} to {2}",
                                    info.ClassyName,
                                    oldType,
                                    info.RankChangeType );
                    break;

                case "banreason":
                    if( valName.Length == 0 ) valName = null;
                    if( SetPlayerInfoField( player, "BanReason", info, info.BanReason, valName ) ) {
                        info.BanReason = valName;
                        break;
                    } else {
                        return;
                    }

                case "unbanreason":
                    if( valName.Length == 0 ) valName = null;
                    if( SetPlayerInfoField( player, "UnbanReason", info, info.UnbanReason, valName ) ) {
                        info.UnbanReason = valName;
                        break;
                    } else {
                        return;
                    }

                case "rankreason":
                    if( valName.Length == 0 ) valName = null;
                    if( SetPlayerInfoField( player, "RankReason", info, info.RankChangeReason, valName ) ) {
                        info.RankChangeReason = valName;
                        break;
                    } else {
                        return;
                    }

                case "kickreason":
                    if( valName.Length == 0 ) valName = null;
                    if( SetPlayerInfoField( player, "KickReason", info, info.LastKickReason, valName ) ) {
                        info.LastKickReason = valName;
                        break;
                    } else {
                        return;
                    }

                case "displayedname":
                    string oldDisplayedName = info.DisplayedName;
                    if( valName.Length == 0 ) valName = null;
                    if( valName == info.DisplayedName ) {
                        if( valName == null ) {
                            player.Message( "SetInfo: DisplayedName for {0} is not set.",
                                            info.Name );
                        } else {
                            player.Message( "SetInfo: DisplayedName for {0} is already set to \"{1}&S\"",
                                            info.Name,
                                            valName );
                        }
                        return;
                    }
                    info.DisplayedName = valName;

                    if( oldDisplayedName == null ) {
                        player.Message( "SetInfo: DisplayedName for {0} set to \"{1}&S\"",
                                        info.Name,
                                        valName );
                    } else if( valName == null ) {
                        player.Message( "SetInfo: DisplayedName for {0} was reset (was \"{1}&S\")",
                                        info.Name,
                                        oldDisplayedName );
                    } else {
                        player.Message( "SetInfo: DisplayedName for {0} changed from \"{1}&S\" to \"{2}&S\"",
                                        info.Name,
                                        oldDisplayedName,
                                        valName );
                    }
                    break;

                default:
                    player.Message( "Only the following properties are editable: " +
                                    "TimesKicked, PreviousRank, TotalTime, RankChangeType, " +
                                    "BanReason, UnbanReason, RankReason, KickReason, DisplayedName" );
                    return;
            }
            info.LastModified = DateTime.UtcNow;
        }

        static bool SetPlayerInfoField( [NotNull] Player player, [NotNull] string fieldName, [NotNull] IClassy info,
                                        [CanBeNull] string oldValue, [CanBeNull] string newValue ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( fieldName == null ) throw new ArgumentNullException( "fieldName" );
            if( info == null ) throw new ArgumentNullException( "info" );
            if( newValue == oldValue ) {
                if( newValue == null ) {
                    player.Message( "SetInfo: {0} for {1}&S is not set.",
                                    fieldName, info.ClassyName );
                } else {
                    player.Message( "SetInfo: {0} for {1}&S is already set to \"{2}&S\"",
                                    fieldName, info.ClassyName, oldValue );
                }
                return false;
            }

            if( oldValue == null  ) {
                player.Message( "SetInfo: {0} for {1}&S set to \"{2}&S\"",
                                fieldName, info.ClassyName, newValue );
            } else if( newValue == null ) {
                player.Message( "SetInfo: {0} for {1}&S was reset (was \"{2}&S\")",
                                fieldName, info.ClassyName, oldValue );
            } else {
                player.Message( "SetInfo: {0} for {1}&S changed from \"{2}&S\" to \"{3}&S\"",
                                fieldName, info.ClassyName,
                                oldValue, newValue );
            }
            return true;
        }

        static bool ValidateInt( string stringVal, int min, int max ) {
            int val;
            if( Int32.TryParse( stringVal, out val ) ) {
                return (val >= min && val <= max);
            } else {
                return false;
            }
        }

        #endregion


        #region Reload

        static readonly CommandDescriptor CdReload = new CommandDescriptor {
            Name = "Reload",
            Aliases = new[] { "configreload", "reloadconfig", "autorankreload", "reloadautorank" },
            Category = CommandCategory.Maintenance,
            Permissions = new[] { Permission.ReloadConfig },
            IsConsoleSafe = true,
            Usage = "/Reload config/autorank/salt",
            Help = "Reloads a given configuration file or setting. "+
                   "Config note: changes to ranks and IRC settings still require a full restart. "+
                   "Salt note: Until server synchronizes with Minecraft.net, " +
                   "connecting players may have trouble verifying names.",
            Handler = ReloadHandler
        };

        static void ReloadHandler( Player player, Command cmd ) {
            string whatToReload = cmd.Next();
            if( whatToReload == null ) {
                CdReload.PrintUsage( player );
                return;
            }

            whatToReload = whatToReload.ToLower();

            using( LogRecorder rec = new LogRecorder() ) {
                bool success;

                switch( whatToReload ) {
                    case "config":
                        success = Config.Load( true, true );
                        break;

                    case "autorank":
                        success = AutoRankManager.Init();
                        break;

                    case "salt":
                        Heartbeat.Salt = Server.GetRandomString( 32 );
                        player.Message( "&WNote: Until server synchronizes with Minecraft.net, " +
                                        "connecting players may have trouble verifying names." );
                        success = true;
                        break;

                    default:
                        CdReload.PrintUsage( player );
                        return;
                }

                if( rec.HasMessages ) {
                    foreach( string msg in rec.MessageList ) {
                        player.Message( msg );
                    }
                }

                if( success ) {
                    player.Message( "Reload: reloaded {0}.", whatToReload );
                } else {
                    player.Message( "&WReload: Error(s) occured while reloading {0}.", whatToReload );
                }
            }
        }

        #endregion


        #region Shutdown, Restart

        static readonly CommandDescriptor CdShutdown = new CommandDescriptor {
            Name = "Shutdown",
            Category = CommandCategory.Maintenance,
            Permissions = new[] { Permission.ShutdownServer },
            IsConsoleSafe = true,
            Help = "Shuts down the server remotely after a given delay. " +
                   "A shutdown reason or message can be specified to be shown to players. " +
                   "Type &H/Shutdown abort&S to cancel.",
            Usage = "/Shutdown Delay [Reason]&S or &H/Shutdown abort",
            Handler = ShutdownHandler
        };

        static readonly TimeSpan DefaultShutdownTime = TimeSpan.FromSeconds( 5 );

        static void ShutdownHandler( Player player, Command cmd ) {
            string delayString = cmd.Next();
            TimeSpan delayTime = DefaultShutdownTime;
            string reason = "";

            if( delayString != null ) {
                if( delayString.Equals( "abort", StringComparison.OrdinalIgnoreCase ) ) {
                    if( Server.CancelShutdown() ) {
                        Logger.Log( LogType.UserActivity,
                                    "Shutdown aborted by {0}.", player.Name );
                        Server.Message( "&WShutdown aborted by {0}", player.ClassyName );
                    } else {
                        player.MessageNow( "Cannot abort shutdown - too late." );
                    }
                    return;
                } else if( !delayString.TryParseMiniTimespan( out delayTime ) ) {
                    CdShutdown.PrintUsage( player );
                    return;
                }
                if( delayTime > DateTimeUtil.MaxTimeSpan ) {
                    player.MessageMaxTimeSpan();
                    return;
                }
                reason = cmd.NextAll();
            }

            if( delayTime.TotalMilliseconds > Int32.MaxValue - 1 ) {
                player.Message( "WShutdown: Delay is too long, maximum is {0}",
                                TimeSpan.FromMilliseconds( Int32.MaxValue - 1 ).ToMiniString() );
                return;
            }

            Server.Message( "&WServer shutting down in {0}", delayTime.ToMiniString() );

            if( String.IsNullOrEmpty( reason ) ) {
                Logger.Log( LogType.UserActivity,
                            "{0} scheduled a shutdown ({1} delay).",
                            player.Name, delayTime.ToCompactString() );
                ShutdownParams sp = new ShutdownParams( ShutdownReason.ShuttingDown, delayTime, true, false );
                Server.Shutdown( sp, false );
            } else {
                Server.Message( "&SShutdown reason: {0}", reason );
                Logger.Log( LogType.UserActivity,
                            "{0} scheduled a shutdown ({1} delay). Reason: {2}",
                            player.Name, delayTime.ToCompactString(), reason );
                ShutdownParams sp = new ShutdownParams( ShutdownReason.ShuttingDown, delayTime, true, false, reason, player );
                Server.Shutdown( sp, false );
            }
        }



        static readonly CommandDescriptor CdRestart = new CommandDescriptor {
            Name = "Restart",
            Category = CommandCategory.Maintenance,
            Permissions = new[] { Permission.ShutdownServer },
            IsConsoleSafe = true,
            Help = "Restarts the server remotely after a given delay. " +
                   "A restart reason or message can be specified to be shown to players. " +
                   "Type &H/Restart abort&S to cancel.",
            Usage = "/Restart Delay [Reason]&S or &H/Restart abort",
            Handler = RestartHandler
        };

        static void RestartHandler( Player player, Command cmd ) {
            string delayString = cmd.Next();
            TimeSpan delayTime = DefaultShutdownTime;
            string reason = "";

            if( delayString != null ) {
                if( delayString.Equals( "abort", StringComparison.OrdinalIgnoreCase ) ) {
                    if( Server.CancelShutdown() ) {
                        Logger.Log( LogType.UserActivity,
                                    "Restart aborted by {0}.", player.Name );
                        Server.Message( "&WRestart aborted by {0}", player.ClassyName );
                    } else {
                        player.MessageNow( "Cannot abort restart - too late." );
                    }
                    return;
                } else if( !delayString.TryParseMiniTimespan( out delayTime ) ) {
                    CdShutdown.PrintUsage( player );
                    return;
                }
                if( delayTime > DateTimeUtil.MaxTimeSpan ) {
                    player.MessageMaxTimeSpan();
                    return;
                }
                reason = cmd.NextAll();
            }

            if( delayTime.TotalMilliseconds > Int32.MaxValue - 1 ) {
                player.Message( "Restart: Delay is too long, maximum is {0}",
                                TimeSpan.FromMilliseconds( Int32.MaxValue - 1 ).ToMiniString() );
                return;
            }

            Server.Message( "&WServer restarting in {0}", delayTime.ToMiniString() );

            if( String.IsNullOrEmpty( reason ) ) {
                Logger.Log( LogType.UserActivity,
                            "{0} scheduled a restart ({1} delay).",
                            player.Name, delayTime.ToCompactString() );
                ShutdownParams sp = new ShutdownParams( ShutdownReason.Restarting, delayTime, true, true );
                Server.Shutdown( sp, false );
            } else {
                Server.Message( "&WRestart reason: {0}", reason );
                Logger.Log( LogType.UserActivity,
                            "{0} scheduled a restart ({1} delay). Reason: {2}",
                            player.Name, delayTime.ToCompactString(), reason );
                ShutdownParams sp = new ShutdownParams( ShutdownReason.Restarting, delayTime, true, true, reason, player );
                Server.Shutdown( sp, false );
            }
        }

        #endregion


        #region PruneDB

        static readonly CommandDescriptor CdPruneDB = new CommandDescriptor {
            Name = "PruneDB",
            Category = CommandCategory.Maintenance,
            IsConsoleSafe = true,
            IsHidden = true,
            Permissions = new[] { Permission.EditPlayerDB },
            Help = "Removes inactive players from the player database. Use with caution.",
            Handler = PruneDBHandler
        };

        static void PruneDBHandler( Player player, Command cmd ) {
            if( !cmd.IsConfirmed ) {
                player.MessageNow( "PruneDB: Finding inactive players..." );
                int inactivePlayers = PlayerDB.CountInactivePlayers();
                if( inactivePlayers == 0 ) {
                    player.Message( "PruneDB: No inactive players found." );
                } else {
                    player.Confirm( cmd, "PruneDB: Erase {0} records of inactive players?",
                                    inactivePlayers );
                }
            } else {
                Scheduler.NewBackgroundTask( PruneDBTask, player ).RunOnce();
            }
        }


        static void PruneDBTask( SchedulerTask task ) {
            int removedCount = PlayerDB.RemoveInactivePlayers();
            Player player = (Player)task.UserState;
            player.Message( "PruneDB: Removed {0} inactive players!", removedCount );
        }

        #endregion


        #region Importing

        static readonly CommandDescriptor CdImport = new CommandDescriptor {
            Name = "Import",
            Aliases = new[] { "importbans", "importranks" },
            Category = CommandCategory.Maintenance,
            IsConsoleSafe = true,
            Permissions = new[] { Permission.Import },
            Usage = "/Import bans Software File&S or &H/Import ranks Software File Rank",
            Help = "Imports data from formats used by other servers. " +
                   "Currently only MCSharp/MCZall/MCLawl/MCForge files are supported.",
            Handler = ImportHandler
        };

        static void ImportHandler( Player player, Command cmd ) {
            string action = cmd.Next();
            if( action == null ) {
                CdImport.PrintUsage( player );
                return;
            }

            switch( action.ToLower() ) {
                case "bans":
                    if( !player.Can( Permission.Ban ) ) {
                        player.MessageNoAccess( Permission.Ban );
                        return;
                    }
                    ImportBans( player, cmd );
                    break;

                case "ranks":
                    if( !player.Can( Permission.Promote ) ) {
                        player.MessageNoAccess( Permission.Promote );
                        return;
                    }
                    ImportRanks( player, cmd );
                    break;

                default:
                    CdImport.PrintUsage( player );
                    break;
            }
        }


        static void ImportBans( Player player, Command cmd ) {
            string serverName = cmd.Next();
            string file = cmd.Next();

            // Make sure all parameters are specified
            if( serverName == null || file == null ) {
                CdImport.PrintUsage( player );
                return;
            }

            // Check if file exists
            if( !File.Exists( file ) ) {
                player.Message( "File not found: {0}", file );
                return;
            }

            string[] names;

            switch( serverName.ToLower() ) {
                case "mcsharp":
                case "mczall":
                case "mclawl":
                case "mcforge":
                    try {
                        names = File.ReadAllLines( file );
                    } catch( Exception ex ) {
                        Logger.Log( LogType.Error,
                                    "Could not open \"{0}\" to import bans: {1}",
                                    file, ex );
                        return;
                    }
                    break;
                default:
                    player.Message( "800Craft does not support importing from {0}", serverName );
                    return;
            }

            if( !cmd.IsConfirmed ) {
                player.Confirm( cmd, "Import {0} bans.", names.Length );
                return;
            }

            string reason = "(import from " + serverName + ")";
            foreach( string name in names ) {
                try {
                    IPAddress ip;
                    if( Server.IsIP( name ) && IPAddress.TryParse( name, out ip ) ) {
                        ip.BanIP( player, reason, true, true );
                    } else if( Player.IsValidName( name ) ) {
                        PlayerInfo info = PlayerDB.FindPlayerInfoExact( name ) ??
                                          PlayerDB.AddFakeEntry( name, RankChangeType.Default );
                        info.Ban( player, reason, true, true );

                    } else {
                        player.Message( "Could not parse \"{0}\" as either name or IP. Skipping.", name );
                    }
                } catch( PlayerOpException ex ) {
                    Logger.Log( LogType.Warning, "ImportBans: " + ex.Message );
                    player.Message( ex.MessageColored );
                }
            }

            PlayerDB.Save();
            IPBanList.Save();
        }


        static void ImportRanks( Player player, Command cmd ) {
            string serverName = cmd.Next();
            string fileName = cmd.Next();
            string rankName = cmd.Next();
            bool silent = (cmd.Next() != null);


            // Make sure all parameters are specified
            if( serverName == null || fileName == null || rankName == null ) {
                CdImport.PrintUsage( player );
                return;
            }

            // Check if file exists
            if( !File.Exists( fileName ) ) {
                player.Message( "File not found: {0}", fileName );
                return;
            }

            Rank targetRank = RankManager.FindRank( rankName );
            if( targetRank == null ) {
                player.MessageNoRank( rankName );
                return;
            }

            string[] names;

            switch( serverName.ToLower() ) {
                case "mcsharp":
                case "mczall":
                case "mclawl":
                case "mcforge":
                    try {
                        names = File.ReadAllLines( fileName );
                    } catch( Exception ex ) {
                        Logger.Log( LogType.Error,
                                    "Could not open \"{0}\" to import ranks: {1}",
                                    fileName, ex );
                        return;
                    }
                    break;
                default:
                    player.Message( "800Craft does not support importing from {0}", serverName );
                    return;
            }

            if( !cmd.IsConfirmed ) {
                player.Confirm( cmd, "Import {0} player ranks?", names.Length );
                return;
            }

            string reason = "(Import from " + serverName + ")";
            foreach( string name in names ) {
                try {
                    PlayerInfo info = PlayerDB.FindPlayerInfoExact( name ) ??
                                      PlayerDB.AddFakeEntry( name, RankChangeType.Promoted );
                    try {
                        info.ChangeRank( player, targetRank, reason, !silent, true, false );
                    } catch( PlayerOpException ex ) {
                        player.Message( ex.MessageColored );
                    }
                } catch( PlayerOpException ex ) {
                    Logger.Log( LogType.Warning, "ImportRanks: " + ex.Message );
                    player.Message( ex.MessageColored );
                }
            }

            PlayerDB.Save();
        }

        #endregion


        static readonly CommandDescriptor CdInfoSwap = new CommandDescriptor {
            Name = "InfoSwap",
            Category = CommandCategory.Maintenance,
            IsConsoleSafe = true,
            IsHidden = true,
            Permissions = new[] { Permission.EditPlayerDB },
            Usage = "/InfoSwap Player1 Player2",
            Help = "Swaps records between two players. EXPERIMENTAL, use at your own risk.",
            Handler = DoPlayerDB
        };

        static void DoPlayerDB( Player player, Command cmd ) {
            string p1Name = cmd.Next();
            string p2Name = cmd.Next();
            if( p1Name == null || p2Name == null ) {
                CdInfoSwap.PrintUsage( player );
                return;
            }

            PlayerInfo p1 = PlayerDB.FindPlayerInfoOrPrintMatches( player, p1Name );
            if( p1 == null ) return;
            PlayerInfo p2 = PlayerDB.FindPlayerInfoOrPrintMatches( player, p2Name );
            if( p2 == null ) return;

            if( p1 == p2 ) {
                player.Message( "InfoSwap: Please specify 2 different players." );
                return;
            }

            if( p1.IsOnline || p2.IsOnline ) {
                player.Message( "InfoSwap: Both players must be offline to swap info." );
                return;
            }

            if( !cmd.IsConfirmed ) {
                player.Confirm( cmd, "InfoSwap: Swap stats of players {0}&S and {1}&S?", p1.ClassyName, p2.ClassyName );
                return;
            } else {
                PlayerDB.SwapPlayerInfo( p1, p2 );
                player.Message( "InfoSwap: Stats of {0}&S and {1}&S have been swapped.",
                                p1.ClassyName, p2.ClassyName );
            }
        }
    }
}