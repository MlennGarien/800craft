// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using System.IO;

namespace fCraft {
    /// <summary> Object representing persistent state ("record") of a player, online or offline.
    /// There is exactly one PlayerInfo object for each known Minecraft account. All data is stored in the PlayerDB. </summary>
    public sealed partial class PlayerInfo : IClassy {
        public const int MinFieldCount = 24;

        /// <summary> Player's Minecraft account name. </summary>
        [NotNull]
        public string Name { get; internal set; }

        /// <summary> If set, replaces Name when printing name in chat. </summary>
        [CanBeNull]
        public string DisplayedName;

        /// <summary>Player's title.</summary>
        [CanBeNull]
        public string TitleName;

        /// <summary> Player's unique numeric ID. Issued on first join. </summary>
        public int ID;

        /// <summary> First time the player ever logged in, UTC.
        /// May be DateTime.MinValue if player has never been online. </summary>
        public DateTime FirstLoginDate;

        /// <summary> Most recent time the player logged in, UTC.
        /// May be DateTime.MinValue if player has never been online. </summary>
        public DateTime LastLoginDate;

        /// <summary> Last time the player has been seen online (last logout), UTC.
        /// May be DateTime.MinValue if player has never been online. </summary>
        public DateTime LastSeen;

        /// <summary> Reason for leaving the server last time. </summary>
        public LeaveReason LeaveReason;

        public int PromoCount;
        //dummy
        
        public int DummyID;
        public string DummyName;
        public Position DummyPos;
        public bool IsFollowing = false;
        public string followingID;
        public int followingCount = 0;
        public bool Static = true;
        public string LeaveMsg = "left the server";

        //Games
        public bool ArrivedLate = false;
        public bool InGame = false;

        //slap, kill
        public DateTime LastUsedSlap;
        public DateTime LastUsedKill;
        public bool KillWait = false;

        //bromode
        public string oldname;
        public bool changedName = false;

        public bool HasVoted = false;


        #region Rank

        /// <summary> Player's current rank. </summary>
        [NotNull]
        public Rank Rank { get; internal set; }

        /// <summary> Player's previous rank.
        /// May be null if player has never been promoted/demoted before. </summary>
        [CanBeNull]
        public Rank PreviousRank;

        /// <summary> Date of the most recent promotion/demotion, UTC.
        /// May be DateTime.MinValue if player has never been promoted/demoted before. </summary>
        public DateTime RankChangeDate;

        /// <summary> Name of the entity that most recently promoted/demoted this player. May be empty. </summary>
        [CanBeNull]
        public string RankChangedBy;

        [NotNull]
        public string RankChangedByClassy {
            get {
                return PlayerDB.FindExactClassyName( RankChangedBy );
            }
        }

        /// <summary> Reason given for the most recent promotion/demotion. May be empty. </summary>
        [CanBeNull]
        public string RankChangeReason;

        /// <summary> Type of the most recent promotion/demotion. </summary>
        public RankChangeType RankChangeType;

        #endregion


        #region Bans

        /// <summary> Player's current BanStatus: Banned, NotBanned, or Exempt. </summary>
        public BanStatus BanStatus;

        /// <summary> Returns whether player is name-banned or not. </summary>
        public bool IsBanned {
            get { return BanStatus == BanStatus.Banned; }
        }

        /// <summary> Date of most recent ban, UTC. May be DateTime.MinValue if player was never banned. </summary>
        public DateTime BanDate;

        /// <summary> Name of the entity responsible for most recent ban. May be empty. </summary>
        [CanBeNull]
        public string BannedBy;

        [NotNull]
        public string BannedByClassy {
            get {
                return PlayerDB.FindExactClassyName( BannedBy );
            }
        }

        public bool IsWarned;

        public string WarnedBy = "";
        public DateTime WarnedOn;

        public bool IsTempbanned
        {
            get
            {
                return DateTime.UtcNow < MutedUntil;
            }
        }

        public bool UnWarn()
        {
            if (IsWarned)
            {
                IsWarned = false;
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool Warn(string by)
        {
            if (by == null) throw new ArgumentNullException("by");
            if (!IsWarned)
            {
                IsWarned = true;
                WarnedOn = DateTime.UtcNow;
                WarnedBy = by;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Tempban(string by, TimeSpan timespan)
        {
            if (by == null) throw new ArgumentNullException("by");
            if (timespan <= TimeSpan.Zero)
            {
                throw new ArgumentException("Tempban duration must be longer than 0", "timespan");
            }
            DateTime newBannedUntil = DateTime.UtcNow.Add(timespan);

            if (newBannedUntil > BannedUntil)
            {
                BannedUntil = newBannedUntil;
                BannedBy = by;
                LastModified = DateTime.UtcNow;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary> Reason given for the most recent ban. May be empty. </summary>
        [CanBeNull]
        public string BanReason;

        /// <summary> Date of most recent unban, UTC. May be DateTime.MinValue if player was never unbanned. </summary>
        public DateTime UnbanDate;

        /// <summary> Name of the entity responsible for most recent unban. May be empty. </summary>
        [CanBeNull]
        public string UnbannedBy;

        [NotNull]
        public string UnbannedByClassy {
            get {
                return PlayerDB.FindExactClassyName( UnbannedBy );
            }
        }

        /// <summary> Reason given for the most recent unban. May be empty. </summary>
        [CanBeNull]
        public string UnbanReason;

        /// <summary> Date of most recent failed attempt to log in, UTC. </summary>
        public DateTime LastFailedLoginDate;

        /// <summary> IP from which player most recently tried (and failed) to log in, UTC. </summary>
        [NotNull]
        public IPAddress LastFailedLoginIP = IPAddress.None;

        #endregion


        #region Stats

        /// <summary> Total amount of time the player spent on this server. </summary>
        public TimeSpan TotalTime;

        /// <summary> Total number of blocks manually built or painted by the player. </summary>
        public int BlocksBuilt;

        /// <summary> Total number of blocks manually deleted by the player. </summary>
        public int BlocksDeleted;

        /// <summary> Total number of blocks modified using draw and copy/paste commands. </summary>
        public long BlocksDrawn;

        /// <summary> Number of sessions/logins. </summary>
        public int TimesVisited;

        /// <summary> Total number of messages written. </summary>
        public int MessagesWritten;

        /// <summary> Number of kicks issues by this player. </summary>
        public int TimesKickedOthers;

        /// <summary> Number of bans issued by this player. </summary>
        public int TimesBannedOthers;

        #endregion


        #region Kicks

        /// <summary> Number of times that this player has been manually kicked. </summary>
        public int TimesKicked;

        /// <summary> Date of the most recent kick.
        /// May be DateTime.MinValue if the player has never been kicked. </summary>
        public DateTime LastKickDate;

        /// <summary> Name of the entity that most recently kicked this player. May be empty. </summary>
        [CanBeNull]
        public string LastKickBy;

        [NotNull]
        public string LastKickByClassy {
            get {
                return PlayerDB.FindExactClassyName( LastKickBy );
            }
        }

        /// <summary> Reason given for the most recent kick. May be empty. </summary>
        [CanBeNull]
        public string LastKickReason;

        #endregion


        #region Freeze And Mute

        /// <summary> Whether this player is currently frozen. </summary>
        public bool IsFrozen;

        /// <summary> Date of the most recent freezing.
        /// May be DateTime.MinValue of the player has never been frozen. </summary>
        public DateTime FrozenOn;

        /// <summary> Name of the entity that most recently froze this player. May be empty. </summary>
        [CanBeNull]
        public string FrozenBy;

        [NotNull]
        public string FrozenByClassy {
            get {
                return PlayerDB.FindExactClassyName( FrozenBy );
            }
        }

        /// <summary> Whether this player is currently muted. </summary>
        public bool IsMuted {
            get {
                return DateTime.UtcNow < MutedUntil;
            }
        }

        /// <summary> Date until which the player is muted. If the date is in the past, player is NOT muted. </summary>
        public DateTime MutedUntil;

        /// <summary> Name of the entity that most recently muted this player. May be empty. </summary>
        [CanBeNull]
        public string MutedBy;

        [NotNull]
        public string MutedByClassy {
            get {
                return PlayerDB.FindExactClassyName( MutedBy );
            }
        }

        #endregion


        /// <summary> Whether the player is currently online.
        /// Another way to check online status is to check if PlayerObject is null. </summary>
        public bool IsOnline { get; private set; }

        /// <summary> If player is online, Player object associated with the session.
        /// If player is offline, null. </summary>
        [CanBeNull]
        public Player PlayerObject { get; private set; }

        /// <summary> Whether the player is currently hidden.
        /// Use Player.CanSee() method to check visibility to specific observers. </summary>
        public bool IsHidden;

        /// <summary> For offline players, last IP used to succesfully log in.
        /// For online players, current IP. </summary>
        [NotNull]
        public IPAddress LastIP;


        #region Constructors and Serialization

        internal PlayerInfo( int id ) {
            ID = id;
        }

        PlayerInfo() {
            // reset everything to defaults
            LastIP = IPAddress.None;
            RankChangeDate = DateTime.MinValue;
            BanDate = DateTime.MinValue;
            UnbanDate = DateTime.MinValue;
            LastFailedLoginDate = DateTime.MinValue;
            FirstLoginDate = DateTime.MinValue;
            LastLoginDate = DateTime.MinValue;
            TotalTime = TimeSpan.Zero;
            RankChangeType = RankChangeType.Default;
            LastKickDate = DateTime.MinValue;
            LastSeen = DateTime.MinValue;
            BannedUntil = DateTime.MinValue;
            FrozenOn = DateTime.MinValue;
            MutedUntil = DateTime.MinValue;
            BandwidthUseMode = BandwidthUseMode.Default;
            LastModified = DateTime.UtcNow;
        }

        // fabricate info for an unrecognized player
        public PlayerInfo( [NotNull] string name, [NotNull] Rank rank,
                           bool setLoginDate, RankChangeType rankChangeType )
            : this() {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( rank == null ) throw new ArgumentNullException( "rank" );
            Name = name;
            Rank = rank;
            if( setLoginDate ) {
                FirstLoginDate = DateTime.UtcNow;
                LastLoginDate = FirstLoginDate;
                LastSeen = FirstLoginDate;
                TimesVisited = 1;
            }
            RankChangeType = rankChangeType;
        }


        // generate blank info for a new player
        public PlayerInfo( [NotNull] string name, [NotNull] IPAddress lastIP, [NotNull] Rank startingRank )
            : this() {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( lastIP == null ) throw new ArgumentNullException( "lastIP" );
            if( startingRank == null ) throw new ArgumentNullException( "startingRank" );
            FirstLoginDate = DateTime.UtcNow;
            LastSeen = DateTime.UtcNow;
            LastLoginDate = DateTime.UtcNow;
            Rank = startingRank;
            Name = name;
            ID = PlayerDB.GetNextID();
            LastIP = lastIP;
        }

        #endregion


        #region Loading

        internal static PlayerInfo LoadFormat2( string[] fields ) {
            PlayerInfo info = new PlayerInfo { Name = fields[0] };

            if( fields[1].Length == 0 || !IPAddress.TryParse( fields[1], out info.LastIP ) ) {
                info.LastIP = IPAddress.None;
            }

            info.Rank = Rank.Parse( fields[2] ) ?? RankManager.DefaultRank;
            fields[3].ToDateTime( ref info.RankChangeDate );
            if( fields[4].Length > 0 ) info.RankChangedBy = fields[4];

            switch( fields[5] ) {
                case "b":
                    info.BanStatus = BanStatus.Banned;
                    break;
                case "x":
                    info.BanStatus = BanStatus.IPBanExempt;
                    break;
                default:
                    info.BanStatus = BanStatus.NotBanned;
                    break;
            }

            // ban information
            if( fields[6].ToDateTime( ref info.BanDate ) ) {
                if( fields[7].Length > 0 ) info.BannedBy = Unescape( fields[7] );
                if( fields[10].Length > 0 ) info.BanReason = Unescape( fields[10] );
            }

            // unban information
            if( fields[8].ToDateTime( ref info.UnbanDate ) ) {
                if( fields[9].Length > 0 ) info.UnbannedBy = Unescape( fields[9] );
                if( fields[11].Length > 0 ) info.UnbanReason = Unescape( fields[11] );
            }

            // failed logins
            fields[12].ToDateTime( ref info.LastFailedLoginDate );

            if( fields[13].Length > 1 || !IPAddress.TryParse( fields[13], out info.LastFailedLoginIP ) ) { // LEGACY
                info.LastFailedLoginIP = IPAddress.None;
            }
            // skip 14

            fields[15].ToDateTime( ref info.FirstLoginDate );

            // login/logout times
            fields[16].ToDateTime( ref info.LastLoginDate );
            fields[17].ToTimeSpan( out info.TotalTime );

            // stats
            if( fields[18].Length > 0 ) Int32.TryParse( fields[18], out info.BlocksBuilt );
            if( fields[19].Length > 0 ) Int32.TryParse( fields[19], out info.BlocksDeleted );
            Int32.TryParse( fields[20], out info.TimesVisited );
            if( fields[20].Length > 0 ) Int32.TryParse( fields[21], out info.MessagesWritten );
            // field 23 are no longer in use
            Int32.TryParse(fields[22], out info.PromoCount); //promocount
            if (fields[23].Length > 0) info.TitleName = fields[23].ToString(); //titlename

            if( fields[24].Length > 0 ) info.PreviousRank = Rank.Parse( fields[24] );
            if( fields[25].Length > 0 ) info.RankChangeReason = Unescape( fields[25] );
            Int32.TryParse( fields[26], out info.TimesKicked );
            Int32.TryParse( fields[27], out info.TimesKickedOthers );
            Int32.TryParse( fields[28], out info.TimesBannedOthers );

            info.ID = Int32.Parse( fields[29] );
            if( info.ID < 256 )
                info.ID = PlayerDB.GetNextID();

            byte rankChangeTypeCode;
            if( Byte.TryParse( fields[30], out rankChangeTypeCode ) ) {
                info.RankChangeType = (RankChangeType)rankChangeTypeCode;
                if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                    info.GuessRankChangeType();
                }
            } else {
                info.GuessRankChangeType();
            }

            fields[31].ToDateTime( ref info.LastKickDate );
            if( !fields[32].ToDateTime( ref info.LastSeen ) || info.LastSeen < info.LastLoginDate ) {
                info.LastSeen = info.LastLoginDate;
            }
            Int64.TryParse( fields[33], out info.BlocksDrawn );
            if( fields[34].Length > 0 ) info.LastKickBy = Unescape( fields[34] );
            if( fields[35].Length > 0 ) info.LastKickReason = Unescape( fields[35] );

            fields[36].ToDateTime( ref info.BannedUntil );
            info.IsFrozen = (fields[37] == "f");
            if( fields[38].Length > 0 ) info.FrozenBy = Unescape( fields[38] );
            fields[39].ToDateTime( ref info.FrozenOn );
            fields[40].ToDateTime( ref info.MutedUntil );
            if( fields[41].Length > 0 ) info.MutedBy = Unescape( fields[41] );
            info.Password = Unescape( fields[42] );
            // fields[43] is "online", and is ignored

            byte bandwidthUseModeCode;
            if( Byte.TryParse( fields[44], out bandwidthUseModeCode ) ) {
                info.BandwidthUseMode = (BandwidthUseMode)bandwidthUseModeCode;
                if( !Enum.IsDefined( typeof( BandwidthUseMode ), bandwidthUseModeCode ) ) {
                    info.BandwidthUseMode = BandwidthUseMode.Default;
                }
            } else {
                info.BandwidthUseMode = BandwidthUseMode.Default;
            }

            if( fields.Length > 45 ) {
                if( fields[45].Length == 0 ) {
                    info.IsHidden = false;
                } else {
                    info.IsHidden = info.Rank.Can( Permission.Hide );
                }
            }
            if( fields.Length > 46 ) {
                fields[46].ToDateTime( ref info.LastModified );
            }
            if( fields.Length > 47 && fields[47].Length > 0 ) {
                info.DisplayedName = Unescape( fields[47] );
            }

            if( info.LastSeen < info.FirstLoginDate ) {
                info.LastSeen = info.FirstLoginDate;
            }
            if( info.LastLoginDate < info.FirstLoginDate ) {
                info.LastLoginDate = info.FirstLoginDate;
            }

            return info;
        }


        internal static PlayerInfo LoadFormat1( string[] fields ) {
            PlayerInfo info = new PlayerInfo { Name = fields[0] };

            if( fields[1].Length == 0 || !IPAddress.TryParse( fields[1], out info.LastIP ) ) {
                info.LastIP = IPAddress.None;
            }

            info.Rank = Rank.Parse( fields[2] ) ?? RankManager.DefaultRank;
            fields[3].ToDateTimeLegacy( ref info.RankChangeDate );
            if( fields[4].Length > 0 ) info.RankChangedBy = fields[4];

            switch( fields[5] ) {
                case "b":
                    info.BanStatus = BanStatus.Banned;
                    break;
                case "x":
                    info.BanStatus = BanStatus.IPBanExempt;
                    break;
                default:
                    info.BanStatus = BanStatus.NotBanned;
                    break;
            }

            // ban information
            if( fields[6].ToDateTimeLegacy( ref info.BanDate ) ) {
                if( fields[7].Length > 0 ) info.BannedBy = Unescape( fields[7] );
                if( fields[10].Length > 0 ) info.BanReason = Unescape( fields[10] );
            }

            // unban information
            if( fields[8].ToDateTimeLegacy( ref info.UnbanDate ) ) {
                if( fields[9].Length > 0 ) info.UnbannedBy = Unescape( fields[9] );
                if( fields[11].Length > 0 ) info.UnbanReason = Unescape( fields[11] );
            }

            // failed logins
            fields[12].ToDateTimeLegacy( ref info.LastFailedLoginDate );

            if( fields[13].Length > 1 || !IPAddress.TryParse( fields[13], out info.LastFailedLoginIP ) ) { // LEGACY
                info.LastFailedLoginIP = IPAddress.None;
            }
            // skip 14
            fields[15].ToDateTimeLegacy( ref info.FirstLoginDate );

            // login/logout times
            fields[16].ToDateTimeLegacy( ref info.LastLoginDate );
            fields[17].ToTimeSpanLegacy( ref info.TotalTime );

            // stats
            if( fields[18].Length > 0 ) Int32.TryParse( fields[18], out info.BlocksBuilt );
            if( fields[19].Length > 0 ) Int32.TryParse( fields[19], out info.BlocksDeleted );
            Int32.TryParse( fields[20], out info.TimesVisited );
            if( fields[20].Length > 0 ) Int32.TryParse( fields[21], out info.MessagesWritten );
            // fields 22-23 are no longer in use

            if( fields[24].Length > 0 ) info.PreviousRank = Rank.Parse( fields[24] );
            if( fields[25].Length > 0 ) info.RankChangeReason = Unescape( fields[25] );
            Int32.TryParse( fields[26], out info.TimesKicked );
            Int32.TryParse( fields[27], out info.TimesKickedOthers );
            Int32.TryParse( fields[28], out info.TimesBannedOthers );

            info.ID = Int32.Parse( fields[29] );
            if( info.ID < 256 )
                info.ID = PlayerDB.GetNextID();

            byte rankChangeTypeCode;
            if( Byte.TryParse( fields[30], out rankChangeTypeCode ) ) {
                info.RankChangeType = (RankChangeType)rankChangeTypeCode;
                if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                    info.GuessRankChangeType();
                }
            } else {
                info.GuessRankChangeType();
            }

            fields[31].ToDateTimeLegacy( ref info.LastKickDate );
            if( !fields[32].ToDateTimeLegacy( ref info.LastSeen ) || info.LastSeen < info.LastLoginDate ) {
                info.LastSeen = info.LastLoginDate;
            }
            Int64.TryParse( fields[33], out info.BlocksDrawn );

            if( fields[34].Length > 0 ) info.LastKickBy = Unescape( fields[34] );
            if( fields[34].Length > 0 ) info.LastKickReason = Unescape( fields[35] );

            fields[36].ToDateTimeLegacy( ref info.BannedUntil );
            info.IsFrozen = (fields[37] == "f");
            if( fields[38].Length > 0 ) info.FrozenBy = Unescape( fields[38] );
            fields[39].ToDateTimeLegacy( ref info.FrozenOn );
            fields[40].ToDateTimeLegacy( ref info.MutedUntil );
            if( fields[41].Length > 0 ) info.MutedBy = Unescape( fields[41] );
            info.Password = Unescape( fields[42] );
            // fields[43] is "online", and is ignored

            byte bandwidthUseModeCode;
            if( Byte.TryParse( fields[44], out bandwidthUseModeCode ) ) {
                info.BandwidthUseMode = (BandwidthUseMode)bandwidthUseModeCode;
                if( !Enum.IsDefined( typeof( BandwidthUseMode ), bandwidthUseModeCode ) ) {
                    info.BandwidthUseMode = BandwidthUseMode.Default;
                }
            } else {
                info.BandwidthUseMode = BandwidthUseMode.Default;
            }

            if( fields.Length > 45 ) {
                if( fields[45].Length == 0 ) {
                    info.IsHidden = false;
                } else {
                    info.IsHidden = info.Rank.Can( Permission.Hide );
                }
            }

            if( info.LastSeen < info.FirstLoginDate ) {
                info.LastSeen = info.FirstLoginDate;
            }
            if( info.LastLoginDate < info.FirstLoginDate ) {
                info.LastLoginDate = info.FirstLoginDate;
            }

            return info;
        }


        internal static PlayerInfo LoadFormat0( string[] fields, bool convertDatesToUtc ) {
            PlayerInfo info = new PlayerInfo { Name = fields[0] };

            if( fields[1].Length == 0 || !IPAddress.TryParse( fields[1], out info.LastIP ) ) {
                info.LastIP = IPAddress.None;
            }

            info.Rank = Rank.Parse( fields[2] ) ?? RankManager.DefaultRank;
            DateTimeUtil.TryParseLocalDate( fields[3], out info.RankChangeDate );
            if( fields[4].Length > 0 ) {
                info.RankChangedBy = fields[4];
                if( info.RankChangedBy == "-" ) info.RankChangedBy = null;
            }

            switch( fields[5] ) {
                case "b":
                    info.BanStatus = BanStatus.Banned;
                    break;
                case "x":
                    info.BanStatus = BanStatus.IPBanExempt;
                    break;
                default:
                    info.BanStatus = BanStatus.NotBanned;
                    break;
            }

            // ban information
            if( DateTimeUtil.TryParseLocalDate( fields[6], out info.BanDate ) ) {
                if( fields[7].Length > 0 ) info.BannedBy = fields[7];
                if( fields[10].Length > 0 ) {
                    info.BanReason = UnescapeOldFormat( fields[10] );
                    if( info.BanReason == "-" ) info.BanReason = null;
                }
            }

            // unban information
            if( DateTimeUtil.TryParseLocalDate( fields[8], out info.UnbanDate ) ) {
                if( fields[9].Length > 0 ) info.UnbannedBy = fields[9];
                if( fields[11].Length > 0 ) {
                    info.UnbanReason = UnescapeOldFormat( fields[11] );
                    if( info.UnbanReason == "-" ) info.UnbanReason = null;
                }
            }

            // failed logins
            if( fields[12].Length > 1 ) {
                DateTimeUtil.TryParseLocalDate( fields[12], out info.LastFailedLoginDate );
            }
            if( fields[13].Length > 1 || !IPAddress.TryParse( fields[13], out info.LastFailedLoginIP ) ) { // LEGACY
                info.LastFailedLoginIP = IPAddress.None;
            }
            // skip 14

            // login/logout times
            DateTimeUtil.TryParseLocalDate( fields[15], out info.FirstLoginDate );
            DateTimeUtil.TryParseLocalDate( fields[16], out info.LastLoginDate );
            TimeSpan.TryParse( fields[17], out info.TotalTime );

            // stats
            if( fields[18].Length > 0 ) Int32.TryParse( fields[18], out info.BlocksBuilt );
            if( fields[19].Length > 0 ) Int32.TryParse( fields[19], out info.BlocksDeleted );
            Int32.TryParse( fields[20], out info.TimesVisited );
            if( fields[20].Length > 0 ) Int32.TryParse( fields[21], out info.MessagesWritten );
            // fields 22-23 are no longer in use

            if( fields.Length > MinFieldCount ) {
                if( fields[24].Length > 0 ) info.PreviousRank = Rank.Parse( fields[24] );
                if( fields[25].Length > 0 ) info.RankChangeReason = UnescapeOldFormat( fields[25] );
                Int32.TryParse( fields[26], out info.TimesKicked );
                Int32.TryParse( fields[27], out info.TimesKickedOthers );
                Int32.TryParse( fields[28], out info.TimesBannedOthers );
                if( fields.Length > 29 ) {
                    info.ID = Int32.Parse( fields[29] );
                    if( info.ID < 256 )
                        info.ID = PlayerDB.GetNextID();
                    byte rankChangeTypeCode;
                    if( Byte.TryParse( fields[30], out rankChangeTypeCode ) ) {
                        info.RankChangeType = (RankChangeType)rankChangeTypeCode;
                        if( !Enum.IsDefined( typeof( RankChangeType ), rankChangeTypeCode ) ) {
                            info.GuessRankChangeType();
                        }
                    } else {
                        info.GuessRankChangeType();
                    }
                    DateTimeUtil.TryParseLocalDate( fields[31], out info.LastKickDate );
                    if( !DateTimeUtil.TryParseLocalDate( fields[32], out info.LastSeen ) || info.LastSeen < info.LastLoginDate ) {
                        info.LastSeen = info.LastLoginDate;
                    }
                    Int64.TryParse( fields[33], out info.BlocksDrawn );

                    if( fields[34].Length > 0 ) info.LastKickBy = UnescapeOldFormat( fields[34] );
                    if( fields[35].Length > 0 ) info.LastKickReason = UnescapeOldFormat( fields[35] );

                } else {
                    info.ID = PlayerDB.GetNextID();
                    info.GuessRankChangeType();
                    info.LastSeen = info.LastLoginDate;
                }

                if( fields.Length > 36 ) {
                    DateTimeUtil.TryParseLocalDate( fields[36], out info.BannedUntil );
                    info.IsFrozen = (fields[37] == "f");
                    if( fields[38].Length > 0 ) info.FrozenBy = UnescapeOldFormat( fields[38] );
                    DateTimeUtil.TryParseLocalDate( fields[39], out info.FrozenOn );
                    DateTimeUtil.TryParseLocalDate( fields[40], out info.MutedUntil );
                    if( fields[41].Length > 0 ) info.MutedBy = UnescapeOldFormat( fields[41] );
                    info.Password = UnescapeOldFormat( fields[42] );
                    // fields[43] is "online", and is ignored
                }

                if( fields.Length > 44 ) {
                    if( fields[44].Length != 0 ) {
                        info.BandwidthUseMode = (BandwidthUseMode)Int32.Parse( fields[44] );
                    }
                }
            }

            if( info.LastSeen < info.FirstLoginDate ) {
                info.LastSeen = info.FirstLoginDate;
            }
            if( info.LastLoginDate < info.FirstLoginDate ) {
                info.LastLoginDate = info.FirstLoginDate;
            }

            if( convertDatesToUtc ) {
                if( info.RankChangeDate != DateTime.MinValue ) info.RankChangeDate = info.RankChangeDate.ToUniversalTime();
                if( info.BanDate != DateTime.MinValue ) info.BanDate = info.BanDate.ToUniversalTime();
                if( info.UnbanDate != DateTime.MinValue ) info.UnbanDate = info.UnbanDate.ToUniversalTime();
                if( info.LastFailedLoginDate != DateTime.MinValue ) info.LastFailedLoginDate = info.LastFailedLoginDate.ToUniversalTime();
                if( info.FirstLoginDate != DateTime.MinValue ) info.FirstLoginDate = info.FirstLoginDate.ToUniversalTime();
                if( info.LastLoginDate != DateTime.MinValue ) info.LastLoginDate = info.LastLoginDate.ToUniversalTime();
                if( info.LastKickDate != DateTime.MinValue ) info.LastKickDate = info.LastKickDate.ToUniversalTime();
                if( info.LastSeen != DateTime.MinValue ) info.LastSeen = info.LastSeen.ToUniversalTime();
                if( info.BannedUntil != DateTime.MinValue ) info.BannedUntil = info.BannedUntil.ToUniversalTime();
                if( info.FrozenOn != DateTime.MinValue ) info.FrozenOn = info.FrozenOn.ToUniversalTime();
                if( info.MutedUntil != DateTime.MinValue ) info.MutedUntil = info.MutedUntil.ToUniversalTime();
            }

            return info;
        }


        void GuessRankChangeType() {
            if( PreviousRank != null ) {
                if( RankChangeReason == "~AutoRank" || RankChangeReason == "~AutoRankAll" || RankChangeReason == "~MassRank" ) {
                    if( PreviousRank > Rank ) {
                        RankChangeType = RankChangeType.AutoDemoted;
                    } else if( PreviousRank < Rank ) {
                        RankChangeType = RankChangeType.AutoPromoted;
                    }
                } else {
                    if( PreviousRank > Rank ) {
                        RankChangeType = RankChangeType.Demoted;
                    } else if( PreviousRank < Rank ) {
                        RankChangeType = RankChangeType.Promoted;
                    }
                }
            } else {
                RankChangeType = RankChangeType.Default;
            }
        }


        internal static PlayerInfo LoadBinaryFormat0( [NotNull] BinaryReader reader ) {
            if( reader == null ) throw new ArgumentNullException( "reader" );
            // ReSharper disable UseObjectOrCollectionInitializer
            PlayerInfo info = new PlayerInfo();
            // ReSharper restore UseObjectOrCollectionInitializer

            // General
            info.Name = reader.ReadString();
            info.DisplayedName = ReadString( reader );
            info.ID = Read7BitEncodedInt( reader );
            info.LastSeen = DateTimeUtil.ToDateTime( reader.ReadUInt32() );

            // Rank
            int rankIndex = Read7BitEncodedInt( reader );
            info.Rank = PlayerDB.GetRankByIndex(rankIndex);
            {
                bool hasPrevRank = reader.ReadBoolean();
                if( hasPrevRank ) {
                    int prevRankIndex = Read7BitEncodedInt( reader );
                    info.Rank = PlayerDB.GetRankByIndex( prevRankIndex );
                }
            }
            info.RankChangeType = (RankChangeType)reader.ReadByte();
            if( info.RankChangeType != RankChangeType.Default ) {
                info.RankChangeDate = ReadDate( reader );
                info.RankChangedBy = ReadString( reader );
                info.RankChangeReason = ReadString( reader );
            }

            // Bans
            info.BanStatus = (BanStatus)reader.ReadByte();
            info.BanDate = ReadDate( reader );
            info.BannedBy = ReadString( reader );
            info.BanReason = ReadString( reader );
            if( info.BanStatus == BanStatus.Banned ) {
                info.BannedUntil = ReadDate( reader );
                info.LastFailedLoginDate = ReadDate( reader );
                info.LastFailedLoginIP = new IPAddress( reader.ReadBytes( 4 ) );
            } else {
                info.UnbanDate = ReadDate( reader );
                info.UnbannedBy = ReadString( reader );
                info.UnbanReason = ReadString( reader );
            }

            // Stats
            info.FirstLoginDate = DateTimeUtil.ToDateTime( reader.ReadUInt32() );
            info.LastLoginDate = DateTimeUtil.ToDateTime( reader.ReadUInt32() );
            info.TotalTime = new TimeSpan( reader.ReadUInt32() * TimeSpan.TicksPerSecond );
            info.BlocksBuilt = Read7BitEncodedInt( reader );
            info.BlocksDeleted = Read7BitEncodedInt( reader );
            if( reader.ReadBoolean() ) {
                info.BlocksDrawn = reader.ReadInt64();
            }
            info.TimesVisited = Read7BitEncodedInt( reader );
            info.MessagesWritten = Read7BitEncodedInt( reader );
            info.TimesKickedOthers = Read7BitEncodedInt( reader );
            info.TimesBannedOthers = Read7BitEncodedInt( reader );

            // Kicks
            info.TimesKicked = Read7BitEncodedInt( reader );
            if( info.TimesKicked > 0 ) {
                info.LastKickDate = ReadDate( reader );
                info.LastKickBy = ReadString( reader );
                info.LastKickReason = ReadString( reader );
            }

            // Freeze/Mute
            info.IsFrozen = reader.ReadBoolean();
            if( info.IsFrozen ) {
                info.FrozenOn = ReadDate( reader );
                info.FrozenBy = ReadString( reader );
            }
            info.MutedUntil = ReadDate( reader );
            if( info.MutedUntil != DateTime.MinValue ) {
                info.MutedBy = ReadString( reader );
            }

            // Misc
            info.Password = ReadString( reader );
            info.LastModified = DateTimeUtil.ToDateTime( reader.ReadUInt32() );
            info.IsOnline = reader.ReadBoolean();
            info.IsHidden = reader.ReadBoolean();
            info.LastIP = new IPAddress( reader.ReadBytes( 4 ) );
            info.LeaveReason = (LeaveReason)reader.ReadByte();
            info.BandwidthUseMode = (BandwidthUseMode)reader.ReadByte();

            return info;
        }


        static DateTime ReadDate( [NotNull] BinaryReader reader ) {
            if( reader.ReadBoolean() ) {
                return DateTimeUtil.ToDateTime( reader.ReadUInt32() );
            } else {
                return DateTime.MinValue;
            }
        }


        static string ReadString( [NotNull] BinaryReader reader ) {
            if( reader.ReadBoolean() ) {
                return reader.ReadString();
            } else {
                return null;
            }
        }


        static int Read7BitEncodedInt( [NotNull] BinaryReader reader ) {
            byte num3;
            int num = 0;
            int num2 = 0;
            do {
                if( num2 == 0x23 ) {
                    throw new FormatException( "Invalid 7bit encoded integer." );
                }
                num3 = reader.ReadByte();
                num |= (num3 & 0x7f) << num2;
                num2 += 7;
            }
            while( (num3 & 0x80) != 0 );
            return num;
        }

        #endregion


        #region Saving

        internal void Serialize( StringBuilder sb ) {
            sb.Append( Name ).Append( ',' ); // 0
            if( !LastIP.Equals( IPAddress.None ) ) sb.Append( LastIP ); // 1
            sb.Append( ',' );

            sb.Append( Rank.FullName ).Append( ',' ); // 2
            RankChangeDate.ToUnixTimeString( sb ).Append( ',' ); // 3

            sb.AppendEscaped( RankChangedBy ).Append( ',' ); // 4

            switch( BanStatus ) {
                case BanStatus.Banned:
                    sb.Append( 'b' );
                    break;
                case BanStatus.IPBanExempt:
                    sb.Append( 'x' );
                    break;
            }
            sb.Append( ',' ); // 5

            BanDate.ToUnixTimeString( sb ).Append( ',' ); // 6
            sb.AppendEscaped( BannedBy ).Append( ',' ); // 7
            UnbanDate.ToUnixTimeString( sb ).Append( ',' ); // 8
            sb.AppendEscaped( UnbannedBy ).Append( ',' ); // 9
            sb.AppendEscaped( BanReason ).Append( ',' ); // 10
            sb.AppendEscaped( UnbanReason ).Append( ',' ); // 11

            LastFailedLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 12

            if( !LastFailedLoginIP.Equals( IPAddress.None ) ) sb.Append( LastFailedLoginIP.ToString() ); // 13
            sb.Append( ',', 2 ); // skip 14

            FirstLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 15
            LastLoginDate.ToUnixTimeString( sb ).Append( ',' ); // 16

            Player pObject = PlayerObject;
            if( pObject != null ) {
                (TotalTime.Add( TimeSinceLastLogin )).ToTickString( sb ).Append( ',' ); // 17
            } else {
                TotalTime.ToTickString( sb ).Append( ',' ); // 17
            }

            if( BlocksBuilt > 0 ) sb.Digits( BlocksBuilt ); // 18
            sb.Append( ',' );

            if( BlocksDeleted > 0 ) sb.Digits( BlocksDeleted ); // 19
            sb.Append( ',' );

            sb.Append( TimesVisited ).Append( ',' ); // 20


            if( MessagesWritten > 0 ) sb.Digits( MessagesWritten ); // 21
            sb.Append( ',' );

            if (PromoCount > 0) sb.Digits(PromoCount); //22
            sb.Append(','); //23 is now titlename

            sb.Append(TitleName); //23
            sb.Append(',');
            

            if( PreviousRank != null ) sb.Append( PreviousRank.FullName ); // 24
            sb.Append( ',' );

            sb.AppendEscaped( RankChangeReason ).Append( ',' ); // 25


            if( TimesKicked > 0 ) sb.Digits( TimesKicked ); // 26
            sb.Append( ',' );

            if( TimesKickedOthers > 0 ) sb.Digits( TimesKickedOthers ); // 27
            sb.Append( ',' );

            if( TimesBannedOthers > 0 ) sb.Digits( TimesBannedOthers ); // 28
            sb.Append( ',' );


            sb.Digits( ID ).Append( ',' ); // 29

            sb.Digits( (int)RankChangeType ).Append( ',' ); // 30


            LastKickDate.ToUnixTimeString( sb ).Append( ',' ); // 31

            if( IsOnline ) DateTime.UtcNow.ToUnixTimeString( sb ); // 32
            else LastSeen.ToUnixTimeString( sb );
            sb.Append( ',' );

            if( BlocksDrawn > 0 ) sb.Append( BlocksDrawn ); // 33
            sb.Append( ',' );

            sb.AppendEscaped( LastKickBy ).Append( ',' ); // 34
            sb.AppendEscaped( LastKickReason ).Append( ',' ); // 35

            BannedUntil.ToUnixTimeString( sb ); // 36

            if( IsFrozen ) {
                sb.Append( ',' ).Append( 'f' ).Append( ',' ); // 37
                sb.AppendEscaped( FrozenBy ).Append( ',' ); // 38
                FrozenOn.ToUnixTimeString( sb ).Append( ',' ); // 39
            } else {
                sb.Append( ',', 4 ); // 37-39
            }

            if( MutedUntil > DateTime.UtcNow ) {
                MutedUntil.ToUnixTimeString( sb ).Append( ',' ); // 40
                sb.AppendEscaped( MutedBy ).Append( ',' ); // 41
            } else {
                sb.Append( ',', 2 ); // 40-41
            }

            sb.AppendEscaped( Password ).Append( ',' ); // 42

            if( IsOnline ) sb.Append( 'o' ); // 43
            sb.Append( ',' );

            if( BandwidthUseMode != BandwidthUseMode.Default ) sb.Append( (int)BandwidthUseMode ); // 44
            sb.Append( ',' );

            if( IsHidden ) sb.Append( 'h' ); // 45

            sb.Append( ',' );
            LastModified.ToUnixTimeString( sb ); // 46

            sb.Append( ',' );
            sb.AppendEscaped( DisplayedName ); // 47
        }


        internal void Serialize( [NotNull] BinaryWriter writer ) {
            if( writer == null ) throw new ArgumentNullException( "writer" );
            // General
            writer.Write( Name ); // 0
            WriteString( writer, DisplayedName ); // 1
            Write7BitEncodedInt( writer, ID ); // 2
            if( IsOnline ) {
                writer.Write( (uint)DateTime.UtcNow.ToUnixTime() ); // 5
            } else {
                writer.Write( (uint)LastSeen.ToUnixTime() ); // 5
            }

            // Rank
            Write7BitEncodedInt( writer, Rank.Index ); // 7
            {
                bool hasPrevRank = (PreviousRank != null);
                writer.Write( hasPrevRank );
                if( hasPrevRank ) {
                    Write7BitEncodedInt( writer, PreviousRank.Index ); // 8
                }
            }
            writer.Write( (byte)RankChangeType ); // 12
            if( RankChangeType != RankChangeType.Default ) {
                WriteDate( writer, RankChangeDate ); // 9
                WriteString( writer, RankChangedBy ); // 10
                WriteString( writer, RankChangeReason ); // 11
            }

            // Bans
            writer.Write( (byte)BanStatus ); // 13
            WriteDate( writer, BanDate ); // 14
            WriteString( writer, BannedBy ); // 15
            WriteString( writer, BanReason ); // 16
            if( BanStatus == BanStatus.Banned ) {
                WriteDate( writer, BannedUntil ); // 14
                WriteDate( writer, LastFailedLoginDate ); // 20
                writer.Write( LastFailedLoginIP.GetAddressBytes() ); // 21
            } else {
                WriteDate( writer, UnbanDate ); // 17
                WriteString( writer, UnbannedBy ); // 18
                WriteString( writer, UnbanReason ); // 18
            }

            // Stats
            writer.Write( (uint)FirstLoginDate.ToUnixTime() ); // 3
            writer.Write( (uint)LastLoginDate.ToUnixTime() ); // 4
            if( IsOnline ) {
                writer.Write( (uint)TotalTime.Add( TimeSinceLastLogin ).ToSeconds() ); // 22
            } else {
                writer.Write( (uint)TotalTime.ToSeconds() ); // 22
            }
            Write7BitEncodedInt( writer, BlocksBuilt ); // 23
            Write7BitEncodedInt( writer, BlocksDeleted ); // 24
            writer.Write( BlocksDrawn > 0 );
            if( BlocksDrawn > 0 ) writer.Write( BlocksDrawn ); // 25
            Write7BitEncodedInt( writer, TimesVisited ); // 26
            Write7BitEncodedInt( writer, MessagesWritten ); // 27
            Write7BitEncodedInt(writer, PromoCount); //22 (?)
            WriteString(writer, TitleName); //23 (?)
            Write7BitEncodedInt( writer, TimesKickedOthers ); // 28
            Write7BitEncodedInt( writer, TimesBannedOthers ); // 29

            // Kicks
            Write7BitEncodedInt( writer, TimesKicked ); // 30
            if( TimesKicked > 0 ) {
                WriteDate( writer, LastKickDate ); // 31
                WriteString( writer, LastKickBy ); // 32
                WriteString( writer, LastKickReason ); // 33
            }

            // Freeze/Mute
            writer.Write( IsFrozen ); // 34
            if( IsFrozen ) {
                WriteDate( writer, FrozenOn ); // 35
                WriteString( writer, FrozenBy ); // 36
            }
            WriteDate( writer, MutedUntil ); // 37
            if( MutedUntil != DateTime.MinValue ) {
                WriteString( writer, MutedBy ); // 38
            }

            // Misc
            WriteString( writer, Password );
            writer.Write( (uint)LastModified.ToUnixTime() );
            writer.Write( IsOnline ); // 39
            writer.Write( IsHidden ); // 40
            writer.Write( LastIP.GetAddressBytes() ); // 41
            writer.Write( (byte)LeaveReason ); // 6
            writer.Write( (byte)BandwidthUseMode ); // 6
        }


        static void WriteDate( [NotNull] BinaryWriter writer, DateTime dateTime ) {
            bool hasDate = (dateTime != DateTime.MinValue);
            writer.Write( hasDate );
            if( hasDate ) {
                writer.Write( (uint)dateTime.ToUnixTime() );
            }
        }


        static void WriteString( [NotNull] BinaryWriter writer, [CanBeNull] string str ) {
            bool hasString = (str != null);
            writer.Write( hasString );
            if( hasString ) {
                writer.Write( str );
            }
        }


        static void Write7BitEncodedInt( [NotNull] BinaryWriter writer, int value ) {
            uint num = (uint)value;
            while( num >= 0x80 ) {
                writer.Write( (byte)(num | 0x80) );
                num = num >> 7;
            }
            writer.Write( (byte)num );
        }

        #endregion


        #region Update Handlers

        public void ProcessMessageWritten() {
            Interlocked.Increment( ref MessagesWritten );
            LastModified = DateTime.UtcNow;
        }


        public void ProcessLogin( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            LastIP = player.IP;
            LastLoginDate = DateTime.UtcNow;
            LastSeen = DateTime.UtcNow;
            Interlocked.Increment( ref TimesVisited );
            IsOnline = true;
            PlayerObject = player;
            LastModified = DateTime.UtcNow;
        }


        public void ProcessFailedLogin( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            LastFailedLoginDate = DateTime.UtcNow;
            LastFailedLoginIP = player.IP;
            LastModified = DateTime.UtcNow;
        }


        public void ProcessLogout( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            TotalTime += player.LastActiveTime.Subtract( player.LoginTime );
            LastSeen = DateTime.UtcNow;
            IsOnline = false;
            PlayerObject = null;
            LeaveReason = player.LeaveReason;
            LastModified = DateTime.UtcNow;
        }


        public void ProcessRankChange( [NotNull] Rank newRank, [NotNull] string changer, [CanBeNull] string reason, RankChangeType type ) {
            if( newRank == null ) throw new ArgumentNullException( "newRank" );
            if( changer == null ) throw new ArgumentNullException( "changer" );
            PreviousRank = Rank;
            Rank = newRank;
            RankChangeDate = DateTime.UtcNow;

            RankChangedBy = changer;
            RankChangeReason = reason;
            RankChangeType = type;
            LastModified = DateTime.UtcNow;
        }


        public void ProcessBlockPlaced( byte type ) {
            if( type == 0 ) { // air
                Interlocked.Increment( ref BlocksDeleted );
            } else {
                Interlocked.Increment( ref BlocksBuilt );
            }
            LastModified = DateTime.UtcNow;
        }


        public void ProcessDrawCommand( int blocksDrawn ) {
            Interlocked.Add( ref BlocksDrawn, blocksDrawn );
            LastModified = DateTime.UtcNow;
        }


        internal void ProcessKick( [NotNull] Player kickedBy, [CanBeNull] string reason ) {
            if( kickedBy == null ) throw new ArgumentNullException( "kickedBy" );
            if( reason != null && reason.Trim().Length == 0 ) reason = null;

            lock( actionLock ) {
                Interlocked.Increment( ref TimesKicked );
                Interlocked.Increment( ref kickedBy.Info.TimesKickedOthers );
                LastKickDate = DateTime.UtcNow;
                LastKickBy = kickedBy.Name;
                LastKickReason = reason;
                if( IsFrozen ) {
                    try {
                        Unfreeze( kickedBy, false, true );
                    } catch( PlayerOpException ex ) {
                        Logger.Log( LogType.Warning,
                                    "PlayerInfo.ProcessKick: {0}", ex.Message );
                    }
                }
                LastModified = DateTime.UtcNow;
            }
        }

        #endregion


        #region Utilities

        public static string Escape( [CanBeNull] string str ) {
            if( String.IsNullOrEmpty( str ) ) {
                return "";
            } else if( str.IndexOf( ',' ) > -1 ) {
                return str.Replace( ',', '\xFF' );
            } else {
                return str;
            }
        }


        public static string UnescapeOldFormat( [NotNull] string str ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            return str.Replace( '\xFF', ',' ).Replace( "\'", "'" ).Replace( @"\\", @"\" );
        }


        public static string Unescape( [NotNull] string str ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            if( str.IndexOf( '\xFF' ) > -1 ) {
                return str.Replace( '\xFF', ',' );
            } else {
                return str;
            }
        }


        // implements IClassy interface
        public string ClassyName {
            get {
                StringBuilder sb = new StringBuilder();
                if ( TitleName != null )
                {
                    sb.Append(TitleName);
                }
                if( ConfigKey.RankColorsInChat.Enabled() ) {
                    sb.Append( Rank.Color );
                }
                if( DisplayedName != null ) {
                    sb.Append( DisplayedName );
                } else {
                    if( ConfigKey.RankPrefixesInChat.Enabled() ) {
                        sb.Append( Rank.Prefix );
                    }
                    sb.Append( Name );
                }
                if (IsBanned)
                {
                    sb.Append(Color.Warning).Append('*');
                }
                if (IsFrozen)
                {
                    sb.Append(Color.Blue).Append('*');
                }

                if (IsMuted)
                {
                    sb.Append(Color.Olive).Append('*');
                }

                if (IsWarned)
                {
                    sb.Append(Color.Black).Append('*');
                }
                return sb.ToString();
            }
        }

        #endregion


        #region TimeSince_____ shortcuts

        public TimeSpan TimeSinceRankChange {
            get { return DateTime.UtcNow.Subtract( RankChangeDate ); }
        }

        public TimeSpan TimeSinceBan {
            get { return DateTime.UtcNow.Subtract( BanDate ); }
        }

        public TimeSpan TimeSinceUnban {
            get { return DateTime.UtcNow.Subtract( UnbanDate ); }
        }

        public TimeSpan TimeSinceFirstLogin {
            get { return DateTime.UtcNow.Subtract( FirstLoginDate ); }
        }

        public TimeSpan TimeSinceLastLogin {
            get { return DateTime.UtcNow.Subtract( LastLoginDate ); }
        }

        public TimeSpan TimeSinceLastKick {
            get { return DateTime.UtcNow.Subtract( LastKickDate ); }
        }

        public TimeSpan TimeSinceLastSeen {
            get { return DateTime.UtcNow.Subtract( LastSeen ); }
        }

        public TimeSpan TimeSinceFrozen {
            get { return DateTime.UtcNow.Subtract( FrozenOn ); }
        }

        public TimeSpan TimeMutedLeft {
            get { return MutedUntil.Subtract( DateTime.UtcNow ); }
        }

        #endregion


        public override string ToString() {
            return String.Format( "PlayerInfo({0},{1})", Name, Rank.Name );
        }

        public bool Can( Permission permission ) {
            return Rank.Can( permission );
        }

        public bool Can( Permission permission, Rank rank ) {
            return Rank.Can( permission, rank );
        }


        #region Unfinished / Not Implemented

        /// <summary> Not implemented (IRC/server password hash). </summary>
        public string Password = ""; // TODO

        public DateTime LastModified; // TODO

        public BandwidthUseMode BandwidthUseMode; // TODO

        /// <summary> Not implemented (for temp bans). </summary>
        public DateTime BannedUntil; // TODO

        #endregion
    }


    public sealed class PlayerInfoComparer : IComparer<PlayerInfo> {
        readonly Player observer;

        public PlayerInfoComparer( Player observer ) {
            this.observer = observer;
        }

        public int Compare( PlayerInfo x, PlayerInfo y ) {
            Player xPlayer = x.PlayerObject;
            Player yPlayer = y.PlayerObject;
            bool xIsOnline = xPlayer != null && observer.CanSee( xPlayer );
            bool yIsOnline = yPlayer != null && observer.CanSee( yPlayer );

            if( !xIsOnline && yIsOnline ) {
                return 1;
            } else if( xIsOnline && !yIsOnline ) {
                return -1;
            }

            if( x.Rank == y.Rank ) {
                return Math.Sign( y.LastSeen.Ticks - x.LastSeen.Ticks );
            } else {
                return x.Rank.Index - y.Rank.Index;
            }
        }
    }
}