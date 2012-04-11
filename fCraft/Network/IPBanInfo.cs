// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> IP ban record. </summary>
    public sealed class IPBanInfo {
        public const int FieldCount = 8;

        /// <summary> Banned IP address. </summary>
        [NotNull]
        public IPAddress Address { get; private set; }

        /// <summary> Name of the player or entity who banned this player. </summary>
        [NotNull]
        public string BannedBy { get; private set; }

        /// <summary> Date/time (UTC) when the ban was issued. </summary>
        public DateTime BanDate;

        /// <summary> Reason/memo for the ban. May be null. </summary>
        [CanBeNull]
        public string BanReason { get; private set; }

        /// <summary> Name of the player associted with this IP (if given at the time of banning). May be null. </summary>
        [CanBeNull]
        public string PlayerName { get; private set; }

        /// <summary> Login attempts from this IP. </summary>
        public int Attempts;

        /// <summary> Name of the player who attempted to log in from this banned IP most recently. </summary>
        public string LastAttemptName { get; private set; }

        /// <summary> Date/time (UTC) of the most recent login attempt. </summary>
        public DateTime LastAttemptDate;


        IPBanInfo() { }


        internal IPBanInfo( [NotNull] IPAddress address, [CanBeNull] string playerName,
                          [NotNull] string bannedBy, [CanBeNull] string banReason ) {
            if( address == null ) throw new ArgumentNullException( "address" );
            if( bannedBy == null ) throw new ArgumentNullException( "bannedBy" );
            Address = address;
            BannedBy = bannedBy;
            BanDate = DateTime.UtcNow;
            BanReason = banReason;
            PlayerName = playerName;
            LastAttemptName = playerName;
            LastAttemptDate = DateTime.MinValue;
        }


        internal static IPBanInfo LoadFormat2( [NotNull] string[] fields ) {
            if( fields == null ) throw new ArgumentNullException( "fields" );
            if( fields.Length != 8 ) throw new ArgumentException( "Unexpected field count", "fields" );
            IPBanInfo info = new IPBanInfo {
                                               Address = IPAddress.Parse( fields[0] ),
                                               BannedBy = PlayerInfo.Unescape( fields[1] )
                                           };

            fields[2].ToDateTime( ref info.BanDate );
            if( fields[3].Length > 0 ) {
                info.BanReason = PlayerInfo.Unescape( fields[3] );
            }
            if( fields[4].Length > 0 ) {
                info.PlayerName = PlayerInfo.Unescape( fields[4] );
            }

            Int32.TryParse( fields[5], out info.Attempts );
            info.LastAttemptName = PlayerInfo.Unescape( fields[6] );
            fields[7].ToDateTime( ref info.LastAttemptDate );

            return info;
        }


        internal static IPBanInfo LoadFormat1( [NotNull] string[] fields ) {
            if( fields == null ) throw new ArgumentNullException( "fields" );
            if( fields.Length != 8 ) throw new ArgumentException( "Unexpected field count", "fields" );
            IPBanInfo info = new IPBanInfo {
                                               Address = IPAddress.Parse( fields[0] ),
                                               BannedBy = PlayerInfo.Unescape( fields[1] )
                                           };

            fields[2].ToDateTimeLegacy( ref info.BanDate );
            if( fields[3].Length > 0 ) {
                info.BanReason = PlayerInfo.Unescape( fields[3] );
            }
            if( fields[4].Length > 0 ) {
                info.PlayerName = PlayerInfo.Unescape( fields[4] );
            }

            Int32.TryParse( fields[5], out info.Attempts );
            info.LastAttemptName = PlayerInfo.Unescape( fields[6] );
            fields[7].ToDateTimeLegacy( ref info.LastAttemptDate );

            return info;
        }


        internal static IPBanInfo LoadFormat0( [NotNull] string[] fields, bool convertDatesToUtc ) {
            if( fields == null ) throw new ArgumentNullException( "fields" );
            if( fields.Length != 8 ) throw new ArgumentException( "Unexpected field count", "fields" );
            IPBanInfo info = new IPBanInfo {
                                               Address = IPAddress.Parse( fields[0] ),
                                               BannedBy = PlayerInfo.UnescapeOldFormat( fields[1] )
                                           };

            DateTimeUtil.TryParseLocalDate( fields[2], out info.BanDate );
            info.BanReason = PlayerInfo.UnescapeOldFormat( fields[3] );
            if( fields[4].Length > 1 ) {
                info.PlayerName = PlayerInfo.UnescapeOldFormat( fields[4] );
            }

            info.Attempts = Int32.Parse( fields[5] );
            info.LastAttemptName = PlayerInfo.UnescapeOldFormat( fields[6] );
            DateTimeUtil.TryParseLocalDate( fields[7], out info.LastAttemptDate );

            if( convertDatesToUtc ) {
                if( info.BanDate != DateTime.MinValue ) info.BanDate = info.BanDate.ToUniversalTime();
                if( info.LastAttemptDate != DateTime.MinValue ) info.LastAttemptDate = info.LastAttemptDate.ToUniversalTime();
            }

            return info;
        }


        internal string Serialize() {
            string[] fields = new string[FieldCount];

            fields[0] = Address.ToString();
            fields[1] = PlayerInfo.Escape( BannedBy );
            fields[2] = BanDate.ToUnixTimeString();
            fields[3] = PlayerInfo.Escape( BanReason );
            fields[4] = PlayerInfo.Escape( PlayerName );
            fields[5] = Attempts.ToString();
            fields[6] = PlayerInfo.Escape( LastAttemptName );
            fields[7] = LastAttemptDate.ToUnixTimeString();

            return String.Join( ",", fields );
        }


        internal void ProcessAttempt( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            Attempts++;
            LastAttemptDate = DateTime.UtcNow;
            LastAttemptName = player.Name;
        }


        #region Shortcuts

        [NotNull]
        public string BannedByClassy {
            get { return PlayerDB.FindExactClassyName( BannedBy ); }
        }

        [NotNull]
        public string PlayerNameClassy {
            get { return PlayerDB.FindExactClassyName( PlayerName ); }
        }

        [NotNull]
        public string LastAttemptNameClassy {
            get { return PlayerDB.FindExactClassyName( LastAttemptName ); }
        }

        public TimeSpan TimeSinceBan {
            get { return DateTime.UtcNow.Subtract( BanDate ); }
        }

        public TimeSpan TimeSinceLastAttempt {
            get { return DateTime.UtcNow.Subtract( LastAttemptDate ); }
        }

        #endregion
    }
}