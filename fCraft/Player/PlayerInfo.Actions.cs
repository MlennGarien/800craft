// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Linq;
using System.Net;
using System.Threading;
using fCraft.Drawing;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    public sealed partial class PlayerInfo {
        readonly object actionLock = new object();

        #region Ban / Unban

        /// <summary> Bans given player. Kicks if online. Throws PlayerOpException on problems. </summary>
        /// <param name="player"> Player who is banning. </param>
        /// <param name="reason"> Reason for ban. May be empty, if permitted by server configuration. </param>
        /// <param name="announce"> Whether ban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether BanChanging and BanChanged events should be raised. </param>
        /// <exception cref="fCraft.PlayerOpException" />
        public void Ban( [NotNull] Player player, [CanBeNull] string reason, bool announce, bool raiseEvents ) {
            BanPlayerInfoInternal( player, reason, false, announce, raiseEvents );
        }


        /// <summary> Unbans a player. Throws PlayerOpException on problems. </summary>
        /// <param name="player"> Player who is unbanning. </param>
        /// <param name="reason"> Reason for unban. May be empty, if permitted by server configuration. </param>
        /// <param name="announce"> Whether unban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether BanChanging and BanChanged events should be raised. </param>
        /// <exception cref="fCraft.PlayerOpException" />
        public void Unban( [NotNull] Player player, [CanBeNull] string reason, bool announce, bool raiseEvents ) {
            BanPlayerInfoInternal( player, reason, true, announce, raiseEvents );
        }


        void BanPlayerInfoInternal( [NotNull] Player player, [CanBeNull] string reason,
                                    bool unban, bool announce, bool raiseEvents ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( reason != null && reason.Trim().Length == 0 ) reason = null;

            lock( actionLock ) {
                // Check if player can ban/unban in general
                if( !player.Can( Permission.Ban ) ) {
                    PlayerOpException.ThrowPermissionMissing( player, this,
                                                              unban ? "unban" : "ban", Permission.Ban );
                }

                // Check if player is trying to ban/unban self
                if( player.Info == this ) {
                    PlayerOpException.ThrowCannotTargetSelf( player, this, unban ? "unban" : "ban" );
                }

                // See if target is already banned/unbanned
                if( unban && BanStatus != BanStatus.Banned ) {
                    PlayerOpException.ThrowPlayerNotBanned( player, this, "banned" );
                } else if( !unban && BanStatus == BanStatus.Banned ) {
                    PlayerOpException.ThrowPlayerAlreadyBanned( player, this, "banned" );
                }

                // Check if player has sufficient rank permissions
                if( !unban && !player.Can( Permission.Ban, Rank ) ) {
                    PlayerOpException.ThrowPermissionLimit( player, this, "ban", Permission.Ban );
                }

                PlayerOpException.CheckBanReason( reason, player, this, unban );

                // Raise PlayerInfo.BanChanging event
                PlayerInfoBanChangingEventArgs e = new PlayerInfoBanChangingEventArgs( this, player, unban, reason, announce );
                if( raiseEvents ) {
                    RaiseBanChangingEvent( e );
                    if( e.Cancel ) PlayerOpException.ThrowCancelled( player, this );
                    reason = e.Reason;
                }

                // Actually ban
                bool result;
                if( unban ) {
                    result = ProcessUnban( player.Name, reason );
                } else {
                    result = ProcessBan( player, player.Name, reason );
                }

                // Check what happened
                if( result ) {
                    if( raiseEvents ) {
                        RaiseBanChangedEvent( e );
                    }
                    Player target = PlayerObject;
                    string verb = (unban ? "unbanned" : "banned");

                    Logger.Log( LogType.UserActivity,
                                "{0} {1} {2}. Reason: {3}",
                                player.Name, verb, Name, reason ?? "" );

                    if( target != null ) {
                        // Log and announce ban/unban
                        if( announce ) {
                            Server.Message( target, "{0}&W was {1} by {2}",
                                            target.ClassyName, verb, player.ClassyName );
                        }

                        // Kick the target
                        if( !unban ) {
                            string kickReason;
                            if( reason != null ) {
                                kickReason = String.Format( "Banned by {0}: {1}", player.Name, reason );
                            } else {
                                kickReason = String.Format( "Banned by {0}", player.Name );
                            }
                            // TODO: check side effects of not using DoKick
                            target.Kick( kickReason, LeaveReason.Ban );
                        }
                    } else {
                        if( announce ) {
                            Server.Message( "{0}&W (offline) was {1} by {2}",
                                            ClassyName, verb, player.ClassyName );
                        }
                    }

                    // Announce ban/unban reason
                    if( announce && ConfigKey.AnnounceKickAndBanReasons.Enabled() && reason != null ) {
                        if( unban ) {
                            Server.Message( "&WUnban reason: {0}", reason );
                        } else {
                            Server.Message( "&WBan reason: {0}", reason );
                        }
                    }

                } else {
                    // Player is already banned/unbanned
                    if( unban ) {
                        PlayerOpException.ThrowPlayerNotBanned( player, this, "banned" );
                    } else {
                        PlayerOpException.ThrowPlayerAlreadyBanned( player, this, "banned" );
                    }
                }
            }
        }


        /// <summary> Bans given player and their IP address.
        /// All players from IP are kicked. Throws PlayerOpException on problems. </summary>
        /// <param name="player"> Player who is banning. </param>
        /// <param name="reason"> Reason for ban. May be empty, if permitted by server configuration. </param>
        /// <param name="announce"> Whether ban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether AddingIPBan, AddedIPBan,
        /// BanChanging, and BanChanged events should be raised. </param>
        /// <exception cref="fCraft.PlayerOpException" />
        public void BanIP( [NotNull] Player player, [CanBeNull] string reason, bool announce, bool raiseEvents ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( reason != null && reason.Trim().Length == 0 ) reason = null;
            lock( actionLock ) {
                if( !player.Can( Permission.Ban, Permission.BanIP ) ) {
                    PlayerOpException.ThrowPermissionMissing( player, this, "IP-ban", Permission.Ban, Permission.BanIP );
                }

                IPAddress address = LastIP;

                // Check if player is trying to ban self
                if( player.Info == this || address.Equals( player.IP ) && !player.IsSuper ) {
                    PlayerOpException.ThrowCannotTargetSelf( player, this, "IP-ban" );
                }

                // Check if a non-bannable address was given (0.0.0.0 or 255.255.255.255)
                if( address.Equals( IPAddress.None ) || address.Equals( IPAddress.Any ) ) {
                    PlayerOpException.ThrowInvalidIP( player, this, address );
                }

                // Check if any high-ranked players use this address
                PlayerInfo unbannable = PlayerDB.FindPlayers( address )
                                                .FirstOrDefault( info => !player.Can( Permission.Ban, info.Rank ) );
                if( unbannable != null ) {
                    PlayerOpException.ThrowPermissionLimitIP( player, unbannable, address );
                }

                // Check existing ban statuses
                bool needNameBan = !IsBanned;
                bool needIPBan = !IPBanList.Contains( address );
                if( !needIPBan && !needNameBan ) {
                    string msg, colorMsg;
                    if( player.Can( Permission.ViewPlayerIPs ) ) {
                        msg = String.Format( "Given player ({0}) and their IP address ({1}) are both already banned.",
                                             Name, address );
                        colorMsg = String.Format( "&SGiven player ({0}&S) and their IP address ({1}) are both already banned.",
                                                  ClassyName, address );
                    } else {
                        msg = String.Format( "Given player ({0}) and their IP address are both already banned.",
                                             Name );
                        colorMsg = String.Format( "&SGiven player ({0}&S) and their IP address are both already banned.",
                                                  ClassyName );
                    }
                    throw new PlayerOpException( player, this, PlayerOpExceptionCode.NoActionNeeded, msg, colorMsg );
                }

                // Check if target is IPBan-exempt
                bool targetIsExempt = (BanStatus == BanStatus.IPBanExempt);
                if( !needIPBan && targetIsExempt ) {
                    string msg = String.Format( "Given player ({0}) is exempt from IP bans. Remove the exemption and retry.",
                                                Name );
                    string colorMsg = String.Format( "&SGiven player ({0}&S) is exempt from IP bans. Remove the exemption and retry.",
                                                     ClassyName );
                    throw new PlayerOpException( player, this, PlayerOpExceptionCode.TargetIsExempt, msg, colorMsg );
                }

                // Ban the name
                if( needNameBan ) {
                    Ban( player, reason, announce, raiseEvents );
                }

                PlayerOpException.CheckBanReason( reason, player, this, false );

                // Ban the IP
                if( needIPBan ) {
                    IPBanInfo banInfo = new IPBanInfo( address, Name, player.Name, reason );
                    if( IPBanList.Add( banInfo, raiseEvents ) ) {
                        Logger.Log( LogType.UserActivity,
                                    "{0} banned {1} (BanIP {2}). Reason: {3}",
                                    player.Name, address, Name, reason ?? "" );

                        // Announce ban on the server
                        if( announce ) {
                            var can = Server.Players.Can( Permission.ViewPlayerIPs );
                            can.Message( "&WPlayer {0}&W was IP-banned ({1}) by {2}",
                                         ClassyName, address, player.ClassyName );
                            var cant = Server.Players.Cant( Permission.ViewPlayerIPs );
                            cant.Message( "&WPlayer {0}&W was IP-banned by {1}",
                                          ClassyName, player.ClassyName );
                            if( ConfigKey.AnnounceKickAndBanReasons.Enabled() && reason != null ) {
                                Server.Message( "&WBanIP reason: {0}", reason );
                            }
                        }
                    } else {
                        // IP is already banned
                        string msg, colorMsg;
                        if( player.Can( Permission.ViewPlayerIPs ) ) {
                            msg = String.Format( "IP of player {0} ({1}) is already banned.",
                                                 Name, address );
                            colorMsg = String.Format( "&SIP of player {0}&S ({1}) is already banned.",
                                                      Name, address );
                        } else {
                            msg = String.Format( "IP of player {0} is already banned.",
                                                 Name );
                            colorMsg = String.Format( "&SIP of player {0}&S is already banned.",
                                                      ClassyName );
                        }
                        throw new PlayerOpException( player, null, PlayerOpExceptionCode.NoActionNeeded, msg, colorMsg );
                    }
                }
            }
        }


        /// <summary> Unbans given player and their IP address. Throws PlayerOpException on problems. </summary>
        /// <param name="player"> Player who is unbanning. </param>
        /// <param name="reason"> Reason for unban. May be empty, if permitted by server configuration. </param>
        /// <param name="announce"> Whether unban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether RemovingIPBan, RemovedIPBan,
        /// BanChanging, and BanChanged events should be raised. </param>
        /// <exception cref="fCraft.PlayerOpException" />
        public void UnbanIP( [NotNull] Player player, [CanBeNull] string reason, bool announce, bool raiseEvents ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( reason != null && reason.Trim().Length == 0 ) reason = null;
            lock( actionLock ) {
                if( !player.Can( Permission.Ban, Permission.BanIP ) ) {
                    PlayerOpException.ThrowPermissionMissing( player, this, "IP-unban", Permission.Ban, Permission.BanIP );
                }

                IPAddress address = LastIP;

                // Check if player is trying to unban self
                if( player.Info == this || address.Equals( player.IP ) && !player.IsSuper ) {
                    PlayerOpException.ThrowCannotTargetSelf( player, this, "IP-unban" );
                }

                // Check if a non-bannable address was given (0.0.0.0 or 255.255.255.255)
                if( address.Equals( IPAddress.None ) || address.Equals( IPAddress.Any ) ) {
                    PlayerOpException.ThrowInvalidIP( player, this, address );
                }

                // Check existing unban statuses
                bool needNameUnban = IsBanned;
                bool needIPUnban = (IPBanList.Get( address ) != null);
                if( !needIPUnban && !needNameUnban ) {
                    PlayerOpException.ThrowPlayerAndIPNotBanned( player, this, address );
                }

                PlayerOpException.CheckBanReason( reason, player, this, true );

                // Unban the name
                if( needNameUnban ) {
                    Unban( player, reason, announce, raiseEvents );
                }

                // Unban the IP
                if( needIPUnban ) {
                    if( IPBanList.Remove( address, raiseEvents ) ) {
                        Logger.Log( LogType.UserActivity,
                                    "{0} unbanned {1} (UnbanIP {2}). Reason: {3}",
                                    player.Name, address, Name, reason ?? "" );

                        // Announce unban on the server
                        if( announce ) {
                            var can = Server.Players.Can( Permission.ViewPlayerIPs );
                            can.Message( "&WPlayer {0}&W was IP-unbanned ({1}) by {2}",
                                         ClassyName, address, player.ClassyName );
                            var cant = Server.Players.Cant( Permission.ViewPlayerIPs );
                            cant.Message( "&WPlayer {0}&W was IP-unbanned by {1}",
                                          ClassyName, player.ClassyName );
                            if( ConfigKey.AnnounceKickAndBanReasons.Enabled() && reason != null ) {
                                Server.Message( "&WUnbanIP reason: {0}", reason );
                            }
                        }
                    } else {
                        PlayerOpException.ThrowPlayerAndIPNotBanned( player, this, address );
                    }
                }
            }
        }


        /// <summary> Bans given player, their IP, and all other accounts on IP.
        /// All players from IP are kicked. Throws PlayerOpException on problems. </summary>
        /// <param name="player"> Player who is banning. </param>
        /// <param name="reason"> Reason for ban. May be empty, if permitted by server configuration. </param>
        /// <param name="announce"> Whether ban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether AddingIPBan, AddedIPBan,
        /// BanChanging, and BanChanged events should be raised. </param>
        /// <exception cref="fCraft.PlayerOpException" />
        public void BanAll( [NotNull] Player player, [CanBeNull] string reason, bool announce, bool raiseEvents ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( reason != null && reason.Trim().Length == 0 ) reason = null;
            lock( actionLock ) {
                if( !player.Can( Permission.Ban, Permission.BanIP, Permission.BanAll ) ) {
                    PlayerOpException.ThrowPermissionMissing( player, this, "ban-all",
                                                         Permission.Ban, Permission.BanIP, Permission.BanAll );
                }

                IPAddress address = LastIP;

                // Check if player is trying to ban self
                if( player.Info == this || address.Equals( player.IP ) && !player.IsSuper ) {
                    PlayerOpException.ThrowCannotTargetSelf( player, this, "ban-all" );
                }

                // Check if a non-bannable address was given (0.0.0.0 or 255.255.255.255)
                if( address.Equals( IPAddress.None ) || address.Equals( IPAddress.Any ) ) {
                    PlayerOpException.ThrowInvalidIP( player, this, address );
                }

                // Check if any high-ranked players use this address
                PlayerInfo[] allPlayersOnIP = PlayerDB.FindPlayers( address );
                PlayerInfo infoWhomPlayerCantBan = allPlayersOnIP.FirstOrDefault( info => !player.Can( Permission.Ban, info.Rank ) );
                if( infoWhomPlayerCantBan != null ) {
                    PlayerOpException.ThrowPermissionLimitIP( player, infoWhomPlayerCantBan, address );
                }

                PlayerOpException.CheckBanReason( reason, player, this, false );
                bool somethingGotBanned = false;

                // Ban the IP
                if( !IPBanList.Contains( address ) ) {
                    IPBanInfo banInfo = new IPBanInfo( address, Name, player.Name, reason );
                    if( IPBanList.Add( banInfo, raiseEvents ) ) {
                        Logger.Log( LogType.UserActivity,
                                    "{0} banned {1} (BanAll {2}). Reason: {3}",
                                    player.Name, address, Name, reason ?? "" );

                        // Announce ban on the server
                        if( announce ) {
                            var can = Server.Players.Can( Permission.ViewPlayerIPs );
                            can.Message( "&WPlayer {0}&W was IP-banned ({1}) by {2}",
                                         ClassyName, address, player.ClassyName );
                            var cant = Server.Players.Cant( Permission.ViewPlayerIPs );
                            cant.Message( "&WPlayer {0}&W was IP-banned by {1}",
                                          ClassyName, player.ClassyName );
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
                        RaiseBanChangingEvent( e );
                        if( e.Cancel ) continue;
                        reason = e.Reason;
                    }

                    // Do the ban
                    if( targetAlt.ProcessBan( player, player.Name, reason ) ) {
                        if( raiseEvents ) {
                            RaiseBanChangedEvent( e );
                        }

                        // Log and announce ban
                        Logger.Log( LogType.UserActivity,
                                    "{0} banned {1} (BanAll {2}). Reason: {3}",
                                    player.Name, targetAlt.Name, Name, reason ?? "" );
                        if( announce ) {
                            if( targetAlt == this ) {
                                Server.Message( "&WPlayer {0}&W was banned by {1}&W (BanAll)",
                                                targetAlt.ClassyName, player.ClassyName );
                            } else {
                                Server.Message( "&WPlayer {0}&W was banned by {1}&W by association with {2}",
                                                targetAlt.ClassyName, player.ClassyName, ClassyName );
                            }
                        }
                        somethingGotBanned = true;
                    }
                }

                // If no one ended up getting banned, quit here
                if( !somethingGotBanned ) {
                    PlayerOpException.ThrowNoOneToBan( player, this, address );
                }

                // Announce BanAll reason towards the end of all bans
                if( announce && ConfigKey.AnnounceKickAndBanReasons.Enabled() && reason != null ) {
                    Server.Message( "&WBanAll reason: {0}", reason );
                }

                // Kick all players from IP
                Player[] targetsOnline = Server.Players.FromIP( address ).ToArray();
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
        }


        /// <summary> Unbans given player, their IP address, and all other accounts on IP.
        /// Throws PlayerOpException on problems. </summary>
        /// <param name="player"> Player who is unbanning. </param>
        /// <param name="reason"> Reason for unban. May be empty, if permitted by server configuration. </param>
        /// <param name="announce"> Whether unban should be publicly announced on the server. </param>
        /// <param name="raiseEvents"> Whether RemovingIPBan, RemovedIPBan,
        /// BanChanging, and BanChanged events should be raised. </param>
        /// <exception cref="fCraft.PlayerOpException" />
        public void UnbanAll( [NotNull] Player player, [CanBeNull] string reason, bool announce, bool raiseEvents ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( reason != null && reason.Trim().Length == 0 ) reason = null;
            lock( actionLock ) {
                if( !player.Can( Permission.Ban, Permission.BanIP, Permission.BanAll ) ) {
                    PlayerOpException.ThrowPermissionMissing( player, this, "unban-all",
                                                         Permission.Ban, Permission.BanIP, Permission.BanAll );
                }

                IPAddress address = LastIP;

                // Check if player is trying to unban self
                if( player.Info == this || address.Equals( player.IP ) && !player.IsSuper ) {
                    PlayerOpException.ThrowCannotTargetSelf( player, this, "unban-all" );
                }

                // Check if a non-bannable address was given (0.0.0.0 or 255.255.255.255)
                if( address.Equals( IPAddress.None ) || address.Equals( IPAddress.Any ) ) {
                    PlayerOpException.ThrowInvalidIP( player, this, address );
                }

                PlayerOpException.CheckBanReason( reason, player, this, true );
                bool somethingGotUnbanned = false;

                // Unban the IP
                if( IPBanList.Contains( address ) ) {
                    if( IPBanList.Remove( address, raiseEvents ) ) {
                        Logger.Log( LogType.UserActivity,
                                    "{0} unbanned {1} (UnbanAll {2}). Reason: {3}",
                                    player.Name, address, Name, reason ?? "" );

                        // Announce unban on the server
                        if( announce ) {
                            var can = Server.Players.Can( Permission.ViewPlayerIPs );
                            can.Message( "&WPlayer {0}&W was IP-unbanned ({1}) by {2}",
                                         ClassyName, address, player.ClassyName );
                            var cant = Server.Players.Cant( Permission.ViewPlayerIPs );
                            cant.Message( "&WPlayer {0}&W was IP-unbanned by {1}",
                                          ClassyName, player.ClassyName );
                        }

                        somethingGotUnbanned = true;
                    }
                }

                // Unban individual players
                PlayerInfo[] allPlayersOnIP = PlayerDB.FindPlayers( address );
                foreach( PlayerInfo targetAlt in allPlayersOnIP ) {
                    if( targetAlt.BanStatus != BanStatus.Banned ) continue;

                    // Raise PlayerInfo.BanChanging event
                    PlayerInfoBanChangingEventArgs e = new PlayerInfoBanChangingEventArgs( targetAlt, player, true, reason, announce );
                    if( raiseEvents ) {
                        RaiseBanChangingEvent( e );
                        if( e.Cancel ) continue;
                        reason = e.Reason;
                    }

                    // Do the ban
                    if( targetAlt.ProcessUnban( player.Name, reason ) ) {
                        if( raiseEvents ) {
                            RaiseBanChangedEvent( e );
                        }

                        // Log and announce ban
                        Logger.Log( LogType.UserActivity,
                                    "{0} unbanned {1} (UnbanAll {2}). Reason: {3}",
                                    player.Name, targetAlt.Name, Name, reason ?? "" );
                        if( announce ) {
                            if( targetAlt == this ) {
                                Server.Message( "&WPlayer {0}&W was unbanned by {1}&W (UnbanAll)",
                                                targetAlt.ClassyName, player.ClassyName );
                            } else {
                                Server.Message( "&WPlayer {0}&W was unbanned by {1}&W by association with {2}",
                                                targetAlt.ClassyName, player.ClassyName, ClassyName );
                            }
                        }
                        somethingGotUnbanned = true;
                    }
                }

                // If no one ended up getting unbanned, quit here
                if( !somethingGotUnbanned ) {
                    PlayerOpException.ThrowNoOneToUnban( player, this, address );
                }

                // Announce UnbanAll reason towards the end of all unbans
                if( announce && ConfigKey.AnnounceKickAndBanReasons.Enabled() && reason != null ) {
                    Server.Message( "&WUnbanAll reason: {0}", reason );
                }
            }
        }


        internal bool ProcessBan( [NotNull] Player bannedBy, [NotNull] string bannedByName, [CanBeNull] string banReason ) {
            if( bannedBy == null ) throw new ArgumentNullException( "bannedBy" );
            if( bannedByName == null ) throw new ArgumentNullException( "bannedByName" );
            lock( actionLock ) {
                if( IsBanned ) {
                    return false;
                }
                BanStatus = BanStatus.Banned;
                BannedBy = bannedByName;
                BanDate = DateTime.UtcNow;
                BanReason = banReason;
                Interlocked.Increment( ref bannedBy.Info.TimesBannedOthers );
                MutedUntil = DateTime.MinValue;
                MutedBy = null;
                if( IsFrozen ) {
                    try {
                        Unfreeze( bannedBy, false, true );
                    } catch( PlayerOpException ex ) {
                        Logger.Log( LogType.Warning,
                                    "PlayerInfo.ProcessBan: {0}", ex.Message );
                    }
                }
                IsHidden = false;
                LastModified = DateTime.UtcNow;
                return true;
            }
        }


        internal bool ProcessUnban( [NotNull] string unbannedByName, [CanBeNull] string unbanReason ) {
            if( unbannedByName == null ) throw new ArgumentNullException( "unbannedByName" );
            lock( actionLock ) {
                if( IsBanned ) {
                    BanStatus = BanStatus.NotBanned;
                    UnbannedBy = unbannedByName;
                    UnbanDate = DateTime.UtcNow;
                    UnbanReason = unbanReason;
                    LastModified = DateTime.UtcNow;
                    return true;
                } else {
                    return false;
                }
            }
        }

        #endregion


        #region ChangeRank

        /// <summary> Changes rank of the player (promotes or demotes). Throws PlayerOpException on problems. </summary>
        /// <param name="player"> Player who originated the promotion/demotion action. </param>
        /// <param name="newRank"> New rank. </param>
        /// <param name="reason"> Reason for promotion/demotion. </param>
        /// <param name="announce"> Whether rank change should be publicly announced or not. </param>
        /// <param name="raiseEvents"> Whether PlayerInfo.RankChanging and PlayerInfo.RankChanged events should be raised. </param>
        /// <param name="auto"> Whether rank change should be marked as "automatic" or manual. </param>
        /// <exception cref="fCraft.PlayerOpException" />
        public void ChangeRank( [NotNull] Player player, [NotNull] Rank newRank, [CanBeNull] string reason,
                                bool announce, bool raiseEvents, bool auto ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( newRank == null ) throw new ArgumentNullException( "newRank" );

            if( reason != null && reason.Trim().Length == 0 ) reason = null;

            bool promoting = (newRank > Rank);
            string verb = (promoting ? "promote" : "demote");
            string verbed = (promoting ? "promoted" : "demoted");

            // Check if player is trying to promote/demote self
            if( player.Info == this ) {
                PlayerOpException.ThrowCannotTargetSelf( player, this, verb );
            }

            // Check if target already has the desired rank
            if( newRank == Rank ) {
                string msg = String.Format( "Player {0} is already ranked {1}", Name, Rank.Name );
                string colorMsg = String.Format( "&SPlayer {0}&S is already ranked {1}", ClassyName, Rank.ClassyName );
                throw new PlayerOpException( player, this, PlayerOpExceptionCode.NoActionNeeded, msg, colorMsg );
            }

            // Check if player has permissions in general
            if( promoting && !player.Can( Permission.Promote ) ) {
                PlayerOpException.ThrowPermissionMissing( player, this, verb, Permission.Promote );
            } else if( !promoting && !player.Can( Permission.Demote ) ) {
                PlayerOpException.ThrowPermissionMissing( player, this, verb, Permission.Demote );
            }

            // Check if player's specific permission limits are enough
            if( promoting && !player.Can( Permission.Promote, newRank ) ) {
                string msg = String.Format( "Cannot promote {0} to {1}: you may only promote players up to rank {2}.",
                                            Name, newRank.Name,
                                            player.Info.Rank.GetLimit( Permission.Promote ).Name );
                string colorMsg = String.Format( "&SCannot promote {0}&S to {1}&S: you may only promote players up to rank {2}&S.",
                                                 ClassyName, newRank.ClassyName,
                                                 player.Info.Rank.GetLimit( Permission.Promote ).ClassyName );
                throw new PlayerOpException( player, this, PlayerOpExceptionCode.PermissionLimitTooLow,
                                             msg, colorMsg );
            } else if( !promoting && !player.Can( Permission.Demote, Rank ) ) {
                string msg = String.Format( "Cannot demote {0} (ranked {1}): you may only demote players ranked {2} or below.",
                                            Name, Rank.Name,
                                            player.Info.Rank.GetLimit( Permission.Demote ).Name );
                string colorMsg = String.Format( "&SCannot demote {0}&S (ranked {1}&S): you may only demote players ranked {2}&S or below.",
                                                 ClassyName, Rank.ClassyName,
                                                 player.Info.Rank.GetLimit( Permission.Demote ).ClassyName );
                throw new PlayerOpException( player, this, PlayerOpExceptionCode.PermissionLimitTooLow,
                                             msg, colorMsg );
            }

            // Check if promotion/demotion reason is required/missing
            PlayerOpException.CheckRankChangeReason( reason, player, this, promoting );

            RankChangeType changeType;
            if( newRank >= Rank ) {
                changeType = (auto ? RankChangeType.AutoPromoted : RankChangeType.Promoted);
            } else {
                changeType = (auto ? RankChangeType.AutoDemoted : RankChangeType.Demoted);
            }

            // Raise PlayerInfo.RankChanging event
            if( raiseEvents && RaiseRankChangingEvent( this, player, newRank, reason, changeType, announce ) ) {
                PlayerOpException.ThrowCancelled( player, this );
            }

            // Log the rank change
            Logger.Log( LogType.UserActivity,
                        "{0} {1} {2} from {3} to {4}. Reason: {5}",
                        player.Name, verbed, Name, Rank.Name, newRank.Name, reason ?? "" );
            //add promocount
            if (promoting) player.Info.PromoCount++;

            // Actually change rank
            Rank oldRank = Rank;
            ProcessRankChange( newRank, player.Name, reason, changeType );

            // Make necessary adjustments related to rank change
            Player target = PlayerObject;
            if( target == null ) {
                if( raiseEvents ) RaiseRankChangedEvent( this, player, oldRank, reason, changeType, announce );
                if( IsHidden && !Rank.Can( Permission.Hide ) ) {
                    IsHidden = false;
                }
            } else {
                Server.RaisePlayerListChangedEvent();
                if( raiseEvents ) RaiseRankChangedEvent( this, player, oldRank, reason, changeType, announce );

                // reset binds (water, lava, admincrete)
                target.ResetAllBinds();

                // reset admincrete deletion permission
                target.Send( PacketWriter.MakeSetPermission( target ) );

                // cancel selection in progress
                if( target.IsMakingSelection ) {
                    target.Message( "Selection cancelled." );
                    target.SelectionCancel();
                }

                // reset brush to normal, if not allowed to draw advanced
                if( !target.Can( Permission.DrawAdvanced ) ) {
                    target.Brush = NormalBrushFactory.Instance;
                }

                // unhide, if needed
                if( IsHidden && !target.Can( Permission.Hide ) ) {
                    IsHidden = false;
                    player.Message( "You are no longer hidden." );
                }

                // check if target is still allowed to spectate
                Player spectatedPlayer = target.SpectatedPlayer;
                if( spectatedPlayer != null && !target.Can( Permission.Spectate, spectatedPlayer.Info.Rank ) ) {
                    target.StopSpectating();
                }

                // check if others are still allowed to spectate target
                foreach( Player spectator in Server.Players.Where( p => p.SpectatedPlayer == target ) ) {
                    if( !spectator.Can( Permission.Spectate, newRank ) ) {
                        spectator.StopSpectating();
                    }
                }

                // ensure copy slot consistency
                target.InitCopySlots();

                // inform the player of the rank change
                target.Message( "You were {0} to {1}&S by {2}",
                                verbed,
                                newRank.ClassyName,
                                player.ClassyName );
                if( reason != null ) {
                    target.Message( "{0} reason: {1}",
                                    promoting ? "Promotion" : "Demotion",
                                    reason );
                }
            }

            // Announce the rank change
            if( announce ) {
                if( ConfigKey.AnnounceRankChanges.Enabled() ) {
                    Server.Message( target,
                                    "{0}&S {1} {2}&S from {3}&S to {4}",
                                    player.ClassyName,
                                    verbed,
                                    ClassyName,
                                    oldRank.ClassyName,
                                    newRank.ClassyName );
                    if( ConfigKey.AnnounceRankChangeReasons.Enabled() && reason != null ) {
                        Server.Message( target,
                                        "&S{0} reason: {1}",
                                        promoting ? "Promotion" : "Demotion",
                                        reason );
                    }
                } else {
                    player.Message( "You {0} {1}&S from {2}&S to {3}",
                                    verbed,
                                    ClassyName,
                                    oldRank.ClassyName,
                                    newRank.ClassyName );
                    if( target != null && reason != null ) {
                        target.Message( "&S{0} reason: {1}",
                                        promoting ? "Promotion" : "Demotion",
                                        reason );
                    }
                }
            }
        }

        #endregion


        #region Freeze / Unfreeze

        /// <summary> Freezes this player (prevents from moving, building, and from using most commands).
        /// Throws PlayerOpException on problems. </summary>
        /// <param name="player"> Player who is doing the freezing. </param>
        /// <param name="announce"> Whether to announce freezing publicly on the server. </param>
        /// <param name="raiseEvents"> Whether to raise PlayerInfo.FreezeChanging and PlayerInfo.FreezeChanged events. </param>
        public void Freeze( [NotNull] Player player, bool announce, bool raiseEvents ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            // Check if player is trying to freeze self
            if( player.Info == this ) {
                PlayerOpException.ThrowCannotTargetSelf( player, this, "freeze" );
            }

            lock( actionLock ) {
                // Check if player can freeze in general
                if( !player.Can( Permission.Freeze ) ) {
                    PlayerOpException.ThrowPermissionMissing( player, this, "freeze", Permission.Freeze );
                }

                // Check if player has sufficient rank permissions
                if( !player.Can( Permission.Freeze, Rank ) ) {
                    PlayerOpException.ThrowPermissionLimit( player, this, "freeze", Permission.Freeze );
                }

                // Check if target is already frozen
                if( IsFrozen ) {
                    string msg = String.Format( "Player {0} is already frozen (by {1}).", Name, FrozenBy );
                    string colorMsg = String.Format( "&SPlayer {0}&S is already frozen (by {1}&S).", ClassyName, FrozenByClassy );
                    throw new PlayerOpException( player, this, PlayerOpExceptionCode.NoActionNeeded, msg, colorMsg );
                }

                // Raise PlayerInfo.FreezeChanging event
                if( raiseEvents && RaiseFreezeChangingEvent( this, player, false, announce ) ) {
                    PlayerOpException.ThrowCancelled( player, this );
                }

                // Actually freeze
                IsFrozen = true;
                IsHidden = false;
                FrozenOn = DateTime.UtcNow;
                FrozenBy = player.Name;
                LastModified = DateTime.UtcNow;

                // Apply side effects
                Player target = PlayerObject;
                if( target != null ) target.IsDeaf = false;

                // Log and announce
                Logger.Log( LogType.UserActivity,
                            "{0} froze {1}",
                            player.Name, Name );
                if( announce ) {
                    if( target != null ) {
                        target.Message( "&WYou were frozen by {0}", player.ClassyName );
                    }
                    Server.Message( target, "&SPlayer {0}&S was frozen by {1}",
                                            ClassyName, player.ClassyName );
                }

                // Raise PlayerInfo.FreezeChanged event
                if( raiseEvents ) RaiseFreezeChangedEvent( this, player, false, announce );
            }
        }


        /// <summary> Unfreezes this player. Throws PlayerOpException on problems. </summary>
        /// <param name="player"> Player who is doing the unfreezing. </param>
        /// <param name="announce"> Whether to announce freezing publicly on the server. </param>
        /// <param name="raiseEvents"> Whether to raise PlayerInfo.FreezeChanging and PlayerInfo.FreezeChanged events. </param>
        public void Unfreeze( [NotNull] Player player, bool announce, bool raiseEvents ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            // Check if player is trying to freeze self
            if( player.Info == this ) {
                PlayerOpException.ThrowCannotTargetSelf( player, this, "unfreeze" );
            }

            lock( actionLock ) {
                // Check if player can freeze in general
                if( !player.Can( Permission.Freeze ) ) {
                    PlayerOpException.ThrowPermissionMissing( player, this, "unfreeze", Permission.Freeze );
                }

                // Check if target is already frozen
                if( !IsFrozen ) {
                    string msg = String.Format( "Player {0} is not currently frozen.", Name );
                    string colorMsg = String.Format( "&SPlayer {0}&S is not currently frozen.", ClassyName );
                    throw new PlayerOpException( player, this, PlayerOpExceptionCode.NoActionNeeded, msg, colorMsg );
                }

                // Check if player has sufficient rank permissions
                if( !player.Can( Permission.Freeze, Rank ) ) {
                    PlayerOpException.ThrowPermissionLimit( player, this, "unfreeze", Permission.Freeze );
                }

                // Raise PlayerInfo.FreezeChanging event
                if( raiseEvents && RaiseFreezeChangingEvent( this, player, true, announce ) ) {
                    PlayerOpException.ThrowCancelled( player, this );
                }

                // Actually unfreeze
                Unfreeze();

                // Log and announce unfreeze
                Logger.Log( LogType.UserActivity,
                            "{0} unfroze {1}",
                            player.Name, Name );
                if( announce ) {
                    Player target = PlayerObject;
                    if( target != null ) {
                        target.Message( "&WYou were unfrozen by {0}", player.ClassyName );
                    }
                    Server.Message( target, "&SPlayer {0}&S was unfrozen by {1}",
                                            ClassyName, player.ClassyName );
                }

                // Raise PlayerInfo.FreezeChanged event
                if( raiseEvents ) RaiseFreezeChangedEvent( this, player, true, announce );
            }
        }


        internal void Unfreeze() {
            lock( actionLock ) {
                IsFrozen = false;
                LastModified = DateTime.UtcNow;
            }
        }

        #endregion


        #region Mute / Unmute

        /// <summary> Mutes this player (prevents from writing chat messages). </summary>
        /// <param name="player"> Player who is doing the muting. </param>
        /// <param name="duration"> Duration of the mute. If a player is already muted for same or greater length of time,
        /// PlayerOpException is thrown with NoActionNeeded code. If a player is already muted for a shorter length of time,
        /// the mute duration is extended. </param>
        /// <param name="announce"> Whether to announce muting publicly on the sever. </param>
        /// <param name="raiseEvents"> Whether to raise PlayerInfo.MuteChanging and MuteChanged events. </param>
        public void Mute( [NotNull] Player player, TimeSpan duration, bool announce, bool raiseEvents ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( duration <= TimeSpan.Zero ) {
                throw new ArgumentException( "Mute duration may not be zero or negative.", "duration" );
            }

            // Check if player is trying to mute self
            if( player.Info == this ) {
                PlayerOpException.ThrowCannotTargetSelf( player, this, "mute" );
            }

            lock( actionLock ) {
                // Check if player can mute in general
                if( !player.Can( Permission.Mute ) ) {
                    PlayerOpException.ThrowPermissionMissing( player, this, "mute", Permission.Mute );
                }

                // Check if player has sufficient rank permissions
                if( !player.Can( Permission.Mute, Rank ) ) {
                    PlayerOpException.ThrowPermissionLimit( player, this, "mute", Permission.Mute );
                }

                // Check if target is already muted for longer
                DateTime newMutedUntil = DateTime.UtcNow.Add( duration );
                if( newMutedUntil > MutedUntil ) {

                    // raise PlayerInfo.MuteChanging event
                    if( raiseEvents ) {
                        if( RaiseMuteChangingEvent( this, player, duration, false, announce ) ) {
                            PlayerOpException.ThrowCancelled( player, this );
                        }
                    }

                    // actually mute
                    MutedUntil = newMutedUntil;
                    MutedBy = player.Name;
                    LastModified = DateTime.UtcNow;

                    // raise PlayerInfo.MuteChanged event
                    if( raiseEvents ) {
                        RaiseMuteChangedEvent( this, player, duration, false, announce );
                    }

                    // log and announce mute publicly
                    Logger.Log( LogType.UserActivity,
                                "Player {0} was muted by {1} for {2}",
                                Name, player.Name, duration );
                    if( announce ) {
                        Player target = PlayerObject;
                        if( target != null ) {
                            target.Message( "You were muted by {0}&S for {1}",
                                            player.ClassyName, duration.ToMiniString() );
                        }
                        Server.Message( target,
                                        "&SPlayer {0}&S was muted by {1}&S for {2}",
                                        ClassyName, player.ClassyName, duration.ToMiniString() );
                    }

                } else {
                    // no action needed - already muted for same or longer duration
                    string msg = String.Format( "Player {0} was already muted by {1} ({2} left)",
                                                ClassyName, MutedBy,
                                                TimeMutedLeft.ToMiniString() );
                    string colorMsg = String.Format( "&SPlayer {0}&S was already muted by {1}&S ({2} left)",
                                                     ClassyName, MutedByClassy,
                                                     TimeMutedLeft.ToMiniString() );
                    throw new PlayerOpException( player, this, PlayerOpExceptionCode.NoActionNeeded, msg, colorMsg );
                }
            }
        }


        /// <summary> Unmutes this player (allows them to write chat again). </summary>
        /// <param name="player"> Player who is doing the unmuting. </param>
        /// <param name="announce"> Whether to announce unmuting publicly on the sever. </param>
        /// <param name="raiseEvents"> Whether to raise PlayerInfo.MuteChanging and MuteChanged events. </param>
        public void Unmute( [NotNull] Player player, bool announce, bool raiseEvents ) {
            if( player == null ) throw new ArgumentNullException( "player" );

            // Check if player is trying to unmute self
            if( player.Info == this ) {
                PlayerOpException.ThrowCannotTargetSelf( player, this, "unmute" );
            }

            lock( actionLock ) {
                TimeSpan timeLeft = TimeMutedLeft;
                // Check if player can unmute in general
                if( !player.Can( Permission.Mute ) ) {
                    PlayerOpException.ThrowPermissionMissing( player, this, "unmute", Permission.Mute );
                }
                // Check if player has sufficient rank permissions
                if (!player.Can(Permission.Mute, Rank))
                {
                    PlayerOpException.ThrowPermissionLimit(player, this, "mute", Permission.Mute);
                }

                if( timeLeft <= TimeSpan.Zero ) {
                    string msg = String.Format( "Player {0} is not currently muted.", Name );
                    string msgColor = String.Format( "&SPlayer {0}&S is not currently muted.", ClassyName );
                    throw new PlayerOpException( player, this, PlayerOpExceptionCode.NoActionNeeded, msg, msgColor );
                }

                // raise PlayerInfo.MuteChanging event
                if( raiseEvents ) {
                    if( RaiseMuteChangingEvent( this, player, timeLeft, true, announce ) ) {
                        PlayerOpException.ThrowCancelled( player, this );
                    }
                }

                Unmute();

                // raise PlayerInfo.MuteChanged event
                if( raiseEvents ) {
                    RaiseMuteChangedEvent( this, player, timeLeft, true, announce );
                }

                // log and announce mute publicly
                Logger.Log( LogType.UserActivity,
                            "Player {0} was unmuted by {1} ({2} was left on the mute)",
                            Name, player.Name, timeLeft );
                if( announce ) {
                    Player target = PlayerObject;
                    if( target != null ) {
                        target.Message( "You were unmuted by {0}", player.ClassyName );
                    }
                    Server.Message( target,
                                    "&SPlayer {0}&S was unmuted by {1}",
                                    ClassyName, player.ClassyName );
                }
            }
        }


        internal void Unmute() {
            lock( actionLock ) {
                MutedUntil = DateTime.MinValue;
                MutedBy = null;
                LastModified = DateTime.UtcNow;
            }
        }

        #endregion
    }
}