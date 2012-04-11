// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    public static class IPBanList {
        static readonly SortedDictionary<string, IPBanInfo> Bans = new SortedDictionary<string, IPBanInfo>();
        static readonly object BanListLock = new object();


        #region Loading/Saving

        const string Header = "IP,bannedBy,banDate,banReason,playerName,attempts,lastAttemptName,lastAttemptDate";
        const int FormatVersion = 2;
        public static bool IsLoaded { get; private set; }

        internal static void Load() {
            if( File.Exists( Paths.IPBanListFileName ) ) {
                using( StreamReader reader = File.OpenText( Paths.IPBanListFileName ) ) {

                    string headerText = reader.ReadLine();
                    if( headerText == null ) {
                        Logger.Log( LogType.Warning, "IPBanList.Load: IP ban file is empty." );
                        return;
                    }

                    int version = ParseHeader( headerText );
                    if( version > FormatVersion ) {
                        Logger.Log( LogType.Warning,
                                    "IPBanList.Load: Attempting to load unsupported IPBanList format ({0}). Errors may occur.",
                                    version );
                    } else if( version < FormatVersion ) {
                        Logger.Log( LogType.Warning,
                                    "IPBanList.Load: Converting IPBanList to a newer format (version {0} to {1}).",
                                    version, FormatVersion );
                    }

                    while( !reader.EndOfStream ) {
                        string line = reader.ReadLine();
                        if( line == null ) break;
                        string[] fields = line.Split( ',' );
                        if( fields.Length == IPBanInfo.FieldCount ) {
                            try {
                                IPBanInfo ban;
                                switch( version ) {
                                    case 0:
                                        ban = IPBanInfo.LoadFormat0( fields, true );
                                        break;
                                    case 1:
                                        ban = IPBanInfo.LoadFormat1( fields );
                                        break;
                                    case 2:
                                        ban = IPBanInfo.LoadFormat2( fields );
                                        break;
                                    default:
                                        return;
                                }

                                if( ban.Address.Equals( IPAddress.Any ) || ban.Address.Equals( IPAddress.None ) ) {
                                    Logger.Log( LogType.Warning,
                                                "IPBanList.Load: Invalid IP address skipped." );
                                } else {
                                    Bans.Add( ban.Address.ToString(), ban );
                                }
                            } catch( IOException ex ) {
                                Logger.Log( LogType.Error,
                                            "IPBanList.Load: Error while trying to read from file: {0}", ex.Message );
                            } catch( Exception ex ) {
                                Logger.Log( LogType.Error,
                                            "IPBanList.Load: Could not parse a record: {0}", ex.Message );
                            }
                        } else {
                            Logger.Log( LogType.Error,
                                        "IPBanList.Load: Corrupt record skipped ({0} fields instead of {1}): {2}",
                                        fields.Length, IPBanInfo.FieldCount, String.Join( ",", fields ) );
                        }
                    }
                }

                Logger.Log( LogType.Debug,
                            "IPBanList.Load: Done loading IP ban list ({0} records).", Bans.Count );
            } else {
                Logger.Log( LogType.Warning,
                            "IPBanList.Load: No IP ban file found." );
            }
            IsLoaded = true;
        }


        static int ParseHeader( [NotNull] string header ) {
            if( header == null ) throw new ArgumentNullException( "header" );
            if( header.IndexOf( ' ' ) > 0 ) {
                string firstPart = header.Substring( 0, header.IndexOf( ' ' ) );
                int version;
                if( Int32.TryParse( firstPart, out version ) ) {
                    return version;
                } else {
                    return 0;
                }
            } else {
                return 0;
            }
        }


        internal static void Save() {
            if( !IsLoaded ) return;
            Logger.Log( LogType.Debug,
                        "IPBanList.Save: Saving IP ban list ({0} records).", Bans.Count );
            const string tempFile = Paths.IPBanListFileName + ".temp";

            lock( BanListLock ) {
                using( StreamWriter writer = File.CreateText( tempFile ) ) {
                    writer.WriteLine( "{0} {1}", FormatVersion, Header );
                    foreach( IPBanInfo entry in Bans.Values ) {
                        writer.WriteLine( entry.Serialize() );
                    }
                }
            }
            try {
                Paths.MoveOrReplace( tempFile, Paths.IPBanListFileName );
            } catch( Exception ex ) {
                Logger.Log( LogType.Error,
                            "IPBanList.Save: An error occured while trying to save ban list file: {0}", ex );
            }
        }

        #endregion


        /// <summary> Adds a new IP Ban. </summary>
        /// <param name="ban"> Ban information </param>
        /// <param name="raiseEvent"> Whether AddingIPBan and AddedIPBan events should be raised. </param>
        /// <returns> True if ban was added, false if it was already on the list </returns>
        public static bool Add( [NotNull] IPBanInfo ban, bool raiseEvent ) {
            if( ban == null ) throw new ArgumentNullException( "ban" );
            lock( BanListLock ) {
                if( Bans.ContainsKey( ban.Address.ToString() ) ) return false;
                if( raiseEvent ) {
                    if( RaiseAddingIPBanEvent( ban ) ) return false;
                    Bans.Add( ban.Address.ToString(), ban );
                    RaiseAddedIPBanEvent( ban );
                } else {
                    Bans.Add( ban.Address.ToString(), ban );
                }
                Save();
                return true;
            }
        }


        /// <summary> Retrieves ban information for a given IP address. </summary>
        /// <param name="address"> IP address to check. </param>
        /// <returns> IPBanInfo object if found, otherwise null. </returns>
        [CanBeNull]
        public static IPBanInfo Get( [NotNull] IPAddress address ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            lock( BanListLock ) {
                IPBanInfo info;
                if( Bans.TryGetValue( address.ToString(), out info ) ) {
                    return info;
                } else {
                    return null;
                }
            }
        }


        /// <summary> Checks whether the given address is banned. </summary>
        /// <param name="address"> Address to look for. </param>
        public static bool Contains( [NotNull] IPAddress address ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            lock( BanListLock ) {
                return Bans.ContainsKey( address.ToString() );
            }
        }


        /// <summary> Removes a given IP address from the ban list (if present). </summary>
        /// <param name="address"> Address to unban. </param>
        /// <param name="raiseEvents"> Whether to raise RemovingIPBan and RemovedIPBan events. </param>
        /// <returns> True if IP was unbanned.
        /// False if it was not banned in the first place, or if it was cancelled by an event. </returns>
        public static bool Remove( [NotNull] IPAddress address, bool raiseEvents ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            lock( BanListLock ) {
                if( !Bans.ContainsKey( address.ToString() ) ) {
                    return false;
                }
                IPBanInfo info = Bans[address.ToString()];
                if( raiseEvents ) {
                    if( RaiseRemovingIPBanEvent( info ) ) return false;
                }
                if( Bans.Remove( address.ToString() ) ) {
                    if( raiseEvents ) RaiseRemovedIPBanEvent( info );
                    Save();
                    return true;
                } else {
                    return false;
                }
            }
        }


        public static int Count {
            get {
                return Bans.Count;
            }
        }


        /// <summary> Bans given IP address. All players from IP are kicked. If an associated PlayerInfo is known,
        /// use a different overload of this method instead. Throws PlayerOpException on problems. </summary>
        /// <param name="targetAddress"> IP address that is being banned. </param>
        /// <param name="player"> Player who is banning. </param>
        /// <param name="reason"> Reason for ban. May be empty, if permitted by server configuration. </param>
        /// <param name="announce"> Whether ban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether AddingIPBan and AddedIPBan events should be raised. </param>
        public static void BanIP( [NotNull] this IPAddress targetAddress, [NotNull] Player player, [CanBeNull] string reason,
                                  bool announce, bool raiseEvents ) {
            if( targetAddress == null ) throw new ArgumentNullException( "targetAddress" );
            if( player == null ) throw new ArgumentNullException( "player" );
            if( reason != null && reason.Trim().Length == 0 ) reason = null;

            // Check if player can ban IPs in general
            if( !player.Can( Permission.Ban, Permission.BanIP ) ) {
                PlayerOpException.ThrowPermissionMissing( player, null, "IP-ban", Permission.Ban, Permission.BanIP );
            }

            // Check if a non-bannable address was given (0.0.0.0 or 255.255.255.255)
            if( targetAddress.Equals( IPAddress.None ) || targetAddress.Equals( IPAddress.Any ) ) {
                PlayerOpException.ThrowInvalidIP( player, null, targetAddress );
            }

            // Check if player is trying to ban self
            if( targetAddress.Equals( player.IP ) && !player.IsSuper ) {
                PlayerOpException.ThrowCannotTargetSelf( player, null, "IP-ban" );
            }

            // Check if target is already banned
            IPBanInfo existingBan = Get( targetAddress );
            if( existingBan != null ) {
                string msg;
                if( player.Can( Permission.ViewPlayerIPs ) ) {
                    msg = String.Format( "IP address {0} is already banned.", targetAddress );
                } else {
                    msg = String.Format( "Given IP address is already banned." );
                }
                string colorMsg = "&S" + msg;
                throw new PlayerOpException( player, null, PlayerOpExceptionCode.NoActionNeeded, msg, colorMsg );
            }

            // Check if any high-ranked players use this address
            PlayerInfo infoWhomPlayerCantBan = PlayerDB.FindPlayers( targetAddress )
                                                       .FirstOrDefault( info => !player.Can( Permission.Ban, info.Rank ) );
            if( infoWhomPlayerCantBan != null ) {
                PlayerOpException.ThrowPermissionLimitIP( player, infoWhomPlayerCantBan, targetAddress );
            }

            PlayerOpException.CheckBanReason( reason, player, null, false );

            // Actually ban
            IPBanInfo banInfo = new IPBanInfo( targetAddress, null, player.Name, reason );
            bool result = Add( banInfo, raiseEvents );

            if( result ) {
                Logger.Log( LogType.UserActivity,
                            "{0} banned {1} (BanIP {1}). Reason: {2}",
                            player.Name, targetAddress, reason ?? "" );
                if( announce ) {
                    // Announce ban on the server
                    var can = Server.Players.Can( Permission.ViewPlayerIPs );
                    can.Message( "&W{0} was banned by {1}", targetAddress, player.ClassyName );
                    var cant = Server.Players.Cant( Permission.ViewPlayerIPs );
                    cant.Message( "&WAn IP was banned by {0}", player.ClassyName );
                    if( ConfigKey.AnnounceKickAndBanReasons.Enabled() && reason != null ) {
                        Server.Message( "&WBanIP reason: {0}", reason );
                    }
                }

                // Kick all players connected from address
                string kickReason;
                if( reason != null ) {
                    kickReason = String.Format( "IP-Banned by {0}: {1}", player.Name, reason );
                } else {
                    kickReason = String.Format( "IP-Banned by {0}", player.Name );
                }
                foreach( Player other in Server.Players.FromIP( targetAddress ) ) {
                    if( other.Info.BanStatus != BanStatus.IPBanExempt ) {
                        other.Kick( kickReason, LeaveReason.BanIP ); // TODO: check side effects of not using DoKick
                    }
                }

            } else {
                // address is already banned
                string msg;
                if( player.Can( Permission.ViewPlayerIPs ) ) {
                    msg = String.Format( "{0} is already banned.", targetAddress );
                } else {
                    msg = "Given IP address is already banned.";
                }
                string colorMsg = "&S" + msg;
                throw new PlayerOpException( player, null, PlayerOpExceptionCode.NoActionNeeded, msg, colorMsg );
            }
        }


        /// <summary> Unbans an IP address. If an associated PlayerInfo is known,
        /// use a different overload of this method instead. Throws PlayerOpException on problems. </summary>
        /// <param name="targetAddress"> IP address that is being unbanned. </param>
        /// <param name="player"> Player who is unbanning. </param>
        /// <param name="reason"> Reason for unban. May be empty, if permitted by server configuration. </param>
        /// <param name="announce"> Whether unban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether RemovingIPBan and RemovedIPBan events should be raised. </param>
        public static void UnbanIP( [NotNull] this IPAddress targetAddress, [NotNull] Player player, [CanBeNull] string reason,
                                    bool announce, bool raiseEvents ) {
            if( targetAddress == null ) throw new ArgumentNullException( "targetAddress" );
            if( player == null ) throw new ArgumentNullException( "player" );
            if( reason != null && reason.Trim().Length == 0 ) reason = null;

            // Check if player can unban IPs in general
            if( !player.Can( Permission.Ban, Permission.BanIP ) ) {
                PlayerOpException.ThrowPermissionMissing( player, null, "IP-unban", Permission.Ban, Permission.BanIP );
            }

            // Check if a non-bannable address was given (0.0.0.0 or 255.255.255.255)
            if( targetAddress.Equals( IPAddress.None ) || targetAddress.Equals( IPAddress.Any ) ) {
                PlayerOpException.ThrowInvalidIP( player, null, targetAddress );
            }

            // Check if player is trying to unban self
            if( targetAddress.Equals( player.IP ) && !player.IsSuper ) {
                PlayerOpException.ThrowCannotTargetSelf( player, null, "IP-unban" );
            }

            PlayerOpException.CheckBanReason( reason, player, null, true );

            // Actually unban
            bool result = Remove( targetAddress, raiseEvents );

            if( result ) {
                Logger.Log( LogType.UserActivity,
                            "{0} unbanned {1} (UnbanIP {1}). Reason: {2}",
                            player.Name, targetAddress, reason ?? "" );
                if( announce ) {
                    var can = Server.Players.Can( Permission.ViewPlayerIPs );
                    can.Message( "&W{0} was unbanned by {1}", targetAddress, player.ClassyName );
                    var cant = Server.Players.Cant( Permission.ViewPlayerIPs );
                    cant.Message( "&WAn IP was unbanned by {0}", player.ClassyName );
                    if( ConfigKey.AnnounceKickAndBanReasons.Enabled() && reason != null ) {
                        Server.Message( "&WUnbanIP reason: {0}", reason );
                    }
                }
            } else {
                string msg;
                if( player.Can( Permission.ViewPlayerIPs ) ) {
                    msg = String.Format( "IP address {0} is not currently banned.", targetAddress );
                } else {
                    msg = String.Format( "Given IP address is not currently banned." );
                }
                string colorMsg = "&S" + msg;
                throw new PlayerOpException( player, null, PlayerOpExceptionCode.NoActionNeeded, msg, colorMsg );
            }
        }


        /// <summary> Bans given IP address and all accounts on that IP. All players from IP are kicked.
        /// Throws PlayerOpException on problems. </summary>
        /// <param name="targetAddress"> IP address that is being banned. </param>
        /// <param name="player"> Player who is banning. </param>
        /// <param name="reason"> Reason for ban. May be empty, if permitted by server configuration. </param>
        /// <param name="announce"> Whether ban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether AddingIPBan, AddedIPBan, BanChanging, and BanChanged events should be raised. </param>
        public static void BanAll( [NotNull] this IPAddress targetAddress, [NotNull] Player player, [CanBeNull] string reason,
                                   bool announce, bool raiseEvents ) {
            if( targetAddress == null ) throw new ArgumentNullException( "targetAddress" );
            if( player == null ) throw new ArgumentNullException( "player" );
            if( reason != null && reason.Trim().Length == 0 ) reason = null;

            if( !player.Can( Permission.Ban, Permission.BanIP, Permission.BanAll ) ) {
                PlayerOpException.ThrowPermissionMissing( player, null, "ban-all",
                                                     Permission.Ban, Permission.BanIP, Permission.BanAll );
            }

            // Check if player is trying to ban self
            if( targetAddress.Equals( player.IP ) && !player.IsSuper ) {
                PlayerOpException.ThrowCannotTargetSelf( player, null, "ban-all" );
            }

            // Check if a non-bannable address was given (0.0.0.0 or 255.255.255.255)
            if( targetAddress.Equals( IPAddress.None ) || targetAddress.Equals( IPAddress.Any ) ) {
                PlayerOpException.ThrowInvalidIP( player, null, targetAddress );
            }

            // Check if any high-ranked players use this address
            PlayerInfo[] allPlayersOnIP = PlayerDB.FindPlayers( targetAddress );
            PlayerInfo infoWhomPlayerCantBan = allPlayersOnIP.FirstOrDefault( info => !player.Can( Permission.Ban, info.Rank ) );
            if( infoWhomPlayerCantBan != null ) {
                PlayerOpException.ThrowPermissionLimitIP( player, infoWhomPlayerCantBan, targetAddress );
            }

            PlayerOpException.CheckBanReason( reason, player, null, false );
            bool somethingGotBanned = false;

            // Ban the IP
            if( !Contains( targetAddress ) ) {
                IPBanInfo banInfo = new IPBanInfo( targetAddress, null, player.Name, reason );
                if( Add( banInfo, raiseEvents ) ) {
                    Logger.Log( LogType.UserActivity,
                                "{0} banned {1} (BanAll {1}). Reason: {2}",
                                player.Name, targetAddress, reason ?? "" );

                    // Announce ban on the server
                    if( announce ) {
                        var can = Server.Players.Can( Permission.ViewPlayerIPs );
                        can.Message( "&W{0} was banned by {1}", targetAddress, player.ClassyName );
                        var cant = Server.Players.Cant( Permission.ViewPlayerIPs );
                        cant.Message( "&WAn IP was banned by {0}", player.ClassyName );
                    }
                    somethingGotBanned = true;
                }
            }

            // Ban individual players
            foreach( PlayerInfo targetAlt in allPlayersOnIP ) {
                if( targetAlt.BanStatus != BanStatus.NotBanned ) continue;

                // Raise PlayerInfo.BanChanging event
                PlayerInfoBanChangingEventArgs e = new PlayerInfoBanChangingEventArgs( targetAlt, player, false, reason, announce );
                if( raiseEvents ) {
                    PlayerInfo.RaiseBanChangingEvent( e );
                    if( e.Cancel ) continue;
                    reason = e.Reason;
                }

                // Do the ban
                if( targetAlt.ProcessBan( player, player.Name, reason ) ) {
                    if( raiseEvents ) {
                        PlayerInfo.RaiseBanChangedEvent( e );
                    }

                    // Log and announce ban
                    Logger.Log( LogType.UserActivity,
                                "{0} banned {1} (BanAll {2}). Reason: {3}",
                                player.Name, targetAlt.Name, targetAddress, reason ?? "" );
                    if( announce ) {
                        Server.Message( "&WPlayer {0}&W was banned by {1}&W (BanAll)",
                                        targetAlt.ClassyName, player.ClassyName );
                    }
                    somethingGotBanned = true;
                }
            }

            // If no one ended up getting banned, quit here
            if( !somethingGotBanned ) {
                PlayerOpException.ThrowNoOneToBan( player, null, targetAddress );
            }

            // Announce BanAll reason towards the end of all bans
            if( announce && ConfigKey.AnnounceKickAndBanReasons.Enabled() && reason != null ) {
                Server.Message( "&WBanAll reason: {0}", reason );
            }

            // Kick all players from IP
            Player[] targetsOnline = Server.Players.FromIP( targetAddress ).ToArray();
            if( targetsOnline.Length > 0 ) {
                string kickReason;
                if( reason != null ) {
                    kickReason = String.Format( "Banned by {0}: {1}", player.Name, reason );
                } else {
                    kickReason = String.Format( "Banned by {0}", player.Name );
                }
                for( int i = 0; i < targetsOnline.Length; i++ ) {
                    targetsOnline[i].Kick( kickReason, LeaveReason.BanAll );
                }
            }
        }


        /// <summary> Unbans given IP address and all accounts on that IP. Throws PlayerOpException on problems. </summary>
        /// <param name="targetAddress"> IP address that is being unbanned. </param>
        /// <param name="player"> Player who is unbanning. </param>
        /// <param name="reason"> Reason for unban. May be null or empty, if permitted by server configuration. </param>
        /// <param name="announce"> Whether unban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether RemovingIPBan, RemovedIPBan, BanChanging, and BanChanged events should be raised. </param>
        public static void UnbanAll( [NotNull] this IPAddress targetAddress, [NotNull] Player player, [CanBeNull] string reason,
                                     bool announce, bool raiseEvents ) {
            if( targetAddress == null ) throw new ArgumentNullException( "targetAddress" );
            if( player == null ) throw new ArgumentNullException( "player" );
            if( reason != null && reason.Trim().Length == 0 ) reason = null;

            if( !player.Can( Permission.Ban, Permission.BanIP, Permission.BanAll ) ) {
                PlayerOpException.ThrowPermissionMissing( player, null, "unban-all",
                                                     Permission.Ban, Permission.BanIP, Permission.BanAll );
            }

            // Check if player is trying to unban self
            if( targetAddress.Equals( player.IP ) && !player.IsSuper ) {
                PlayerOpException.ThrowCannotTargetSelf( player, null, "unban-all" );
            }

            // Check if a non-bannable address was given (0.0.0.0 or 255.255.255.255)
            if( targetAddress.Equals( IPAddress.None ) || targetAddress.Equals( IPAddress.Any ) ) {
                PlayerOpException.ThrowInvalidIP( player, null, targetAddress );
            }

            PlayerOpException.CheckBanReason( reason, player, null, true );
            bool somethingGotUnbanned = false;

            // Unban the IP
            if( Contains( targetAddress ) ) {
                if( Remove( targetAddress, raiseEvents ) ) {
                    Logger.Log( LogType.UserActivity,
                                "{0} unbanned {1} (UnbanAll {1}). Reason: {2}",
                                player.Name, targetAddress, reason ?? "" );

                    // Announce unban on the server
                    if( announce ) {
                        var can = Server.Players.Can( Permission.ViewPlayerIPs );
                        can.Message( "&W{0} was unbanned by {1}", targetAddress, player.ClassyName );
                        var cant = Server.Players.Cant( Permission.ViewPlayerIPs );
                        cant.Message( "&WAn IP was unbanned by {0}", player.ClassyName );
                    }

                    somethingGotUnbanned = true;
                }
            }

            // Unban individual players
            PlayerInfo[] allPlayersOnIP = PlayerDB.FindPlayers( targetAddress );
            foreach( PlayerInfo targetAlt in allPlayersOnIP ) {
                if( targetAlt.BanStatus != BanStatus.Banned ) continue;

                // Raise PlayerInfo.BanChanging event
                PlayerInfoBanChangingEventArgs e = new PlayerInfoBanChangingEventArgs( targetAlt, player, true, reason, announce );
                if( raiseEvents ) {
                    PlayerInfo.RaiseBanChangingEvent( e );
                    if( e.Cancel ) continue;
                    reason = e.Reason;
                }

                // Do the ban
                if( targetAlt.ProcessUnban( player.Name, reason ) ) {
                    if( raiseEvents ) {
                        PlayerInfo.RaiseBanChangedEvent( e );
                    }

                    // Log and announce ban
                    Logger.Log( LogType.UserActivity,
                                "{0} unbanned {1} (UnbanAll {2}). Reason: {3}",
                                player.Name, targetAlt.Name, targetAddress, reason ?? "" );
                    if( announce ) {
                        Server.Message( "&WPlayer {0}&W was unbanned by {1}&W (UnbanAll)",
                                        targetAlt.ClassyName, player.ClassyName );
                    }
                    somethingGotUnbanned = true;
                }
            }

            // If no one ended up getting unbanned, quit here
            if( !somethingGotUnbanned ) {
                PlayerOpException.ThrowNoOneToUnban( player, null, targetAddress );
            }

            // Announce UnbanAll reason towards the end of all unbans
            if( announce && ConfigKey.AnnounceKickAndBanReasons.Enabled() && reason != null ) {
                Server.Message( "&WUnbanAll reason: {0}", reason );
            }
        }


        #region Events

        /// <summary> Occurs when a new IP ban is about to be added (cancellable). </summary>
        public static event EventHandler<IPBanCancellableEventArgs> AddingIPBan;


        /// <summary> Occurs when a new IP ban has been added. </summary>
        public static event EventHandler<IPBanEventArgs> AddedIPBan;


        /// <summary> Occurs when an existing IP ban is about to be removed (cancellable). </summary>
        public static event EventHandler<IPBanCancellableEventArgs> RemovingIPBan;


        /// <summary> Occurs after an existing IP ban has been removed. </summary>
        public static event EventHandler<IPBanEventArgs> RemovedIPBan;


        static bool RaiseAddingIPBanEvent( [NotNull] IPBanInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            var h = AddingIPBan;
            if( h == null ) return false;
            var e = new IPBanCancellableEventArgs( info );
            h( null, e );
            return e.Cancel;
        }

        static void RaiseAddedIPBanEvent( [NotNull] IPBanInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            var h = AddedIPBan;
            if( h != null ) h( null, new IPBanEventArgs( info ) );
        }

        static bool RaiseRemovingIPBanEvent( [NotNull] IPBanInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            var h = RemovingIPBan;
            if( h == null ) return false;
            var e = new IPBanCancellableEventArgs( info );
            h( null, e );
            return e.Cancel;
        }

        static void RaiseRemovedIPBanEvent( [NotNull] IPBanInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            var h = RemovedIPBan;
            if( h != null ) h( null, new IPBanEventArgs( info ) );
        }

        #endregion
    }
}


namespace fCraft.Events {

    public class IPBanEventArgs : EventArgs {
        internal IPBanEventArgs( [NotNull] IPBanInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            BanInfo = info;
        }

        [NotNull]
        public IPBanInfo BanInfo { get; private set; }
    }


    public sealed class IPBanCancellableEventArgs : IPBanEventArgs, ICancellableEvent {
        internal IPBanCancellableEventArgs( IPBanInfo info ) :
            base( info ) {
        }

        public bool Cancel { get; set; }
    }

}