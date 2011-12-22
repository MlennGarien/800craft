// Copyright 2009, 2010, 2011 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Checks for updates, and keeps track of current version/revision. </summary>
    public static class Updater {

        public static readonly ReleaseInfo CurrentRelease = new ReleaseInfo(
            103,
            191, //number of commits
            new DateTime( 2011, 12, 22, 21, 0, 0, DateTimeKind.Utc ),
            "", "",
            ReleaseFlags.Bugfix
#if DEBUG
            | ReleaseFlags.Dev
#endif
 );

        public static string UserAgent {
            get { return "800Craft " + CurrentRelease.VersionString; }
        }

        public const string LatestStable = "0.103_r191";

        public static string UpdateUrl { get; set; }

        static Updater() {
            UpdateCheckTimeout = 4000;
            UpdateUrl = "http://800craft.project-vanilla.com/UpdateCheck.php?r={0}";
        }


        public static int UpdateCheckTimeout { get; set; }

        public static UpdaterResult CheckForUpdates() {
            UpdaterMode mode = ConfigKey.UpdaterMode.GetEnum<UpdaterMode>();
            if( mode == UpdaterMode.Disabled ) return UpdaterResult.NoUpdate;

            string url = String.Format( UpdateUrl, CurrentRelease.Revision );
            if( RaiseCheckingForUpdatesEvent( ref url ) ) return UpdaterResult.NoUpdate;

            Logger.Log( LogType.SystemActivity, "Checking for 800Craft updates..." );
            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create( url );

                request.Method = "GET";
                request.UserAgent = "800Craft";
                request.Timeout = UpdateCheckTimeout;
                request.ReadWriteTimeout = UpdateCheckTimeout;
                request.CachePolicy = new HttpRequestCachePolicy( HttpRequestCacheLevel.BypassCache );
                request.UserAgent = UserAgent;

                using( WebResponse response = request.GetResponse() ) {
                    // ReSharper disable AssignNullToNotNullAttribute
                    // ReSharper disable PossibleNullReferenceException
                    using( XmlTextReader reader = new XmlTextReader( response.GetResponseStream() ) ) {
                        // ReSharper restore AssignNullToNotNullAttribute
                        XDocument doc = XDocument.Load( reader );
                        XElement root = doc.Root;
                        if( root.Attribute( "result" ).Value == "update" ) {
                            string downloadUrl = root.Attribute( "url" ).Value;
                            var releases = new List<ReleaseInfo>();
                            // ReSharper disable LoopCanBeConvertedToQuery
                            foreach( XElement el in root.Elements( "Release" ) ) {
                                releases.Add(
                                    new ReleaseInfo(
                                        Int32.Parse( el.Attribute( "v" ).Value ),
                                        Int32.Parse( el.Attribute( "r" ).Value ),
                                        Int64.Parse( el.Attribute( "date" ).Value ).ToDateTime(),
                                        el.Element( "Summary" ).Value,
                                        el.Element( "ChangeLog" ).Value,
                                        ReleaseInfo.StringToReleaseFlags( el.Attribute( "flags" ).Value )
                                    )
                                );
                            }
                            // ReSharper restore LoopCanBeConvertedToQuery
                            // ReSharper restore PossibleNullReferenceException
                            UpdaterResult result = new UpdaterResult( (releases.Count > 0), new Uri( downloadUrl ),
                                                                      releases.ToArray() );
                            RaiseCheckedForUpdatesEvent( UpdateUrl, result );
                            return result;
                        } else {
                            return UpdaterResult.NoUpdate;
                        }
                    }
                }
            } catch( Exception ex ) {
                Logger.Log( LogType.Error,
                            "An error occured while trying to check for updates: {0}: {1}",
                            ex.GetType(), ex.Message );
                return UpdaterResult.NoUpdate;
            }
        }


        public static bool RunAtShutdown { get; set; }


        #region Events

        /// <summary> Occurs when fCraft is about to check for updates (cancellable).
        /// The update Url may be overridden. </summary>
        public static event EventHandler<CheckingForUpdatesEventArgs> CheckingForUpdates;


        /// <summary> Occurs when fCraft has just checked for updates. </summary>
        public static event EventHandler<CheckedForUpdatesEventArgs> CheckedForUpdates;


        static bool RaiseCheckingForUpdatesEvent( ref string updateUrl ) {
            var h = CheckingForUpdates;
            if( h == null ) return false;
            var e = new CheckingForUpdatesEventArgs( updateUrl );
            h( null, e );
            updateUrl = e.Url;
            return e.Cancel;
        }


        static void RaiseCheckedForUpdatesEvent( string url, UpdaterResult result ) {
            var h = CheckedForUpdates;
            if( h != null ) h( null, new CheckedForUpdatesEventArgs( url, result ) );
        }

        #endregion
    }


    public sealed class UpdaterResult {
        public static UpdaterResult NoUpdate {
            get {
                return new UpdaterResult( false, null, new ReleaseInfo[0] );
            }
        }
        internal UpdaterResult( bool updateAvailable, Uri downloadUri, IEnumerable<ReleaseInfo> releases ) {
            UpdateAvailable = updateAvailable;
            DownloadUri = downloadUri;
            History = releases.OrderByDescending( r => r.Revision ).ToArray();
            LatestRelease = releases.FirstOrDefault();
        }
        public bool UpdateAvailable { get; private set; }
        public Uri DownloadUri { get; private set; }
        public ReleaseInfo[] History { get; private set; }
        public ReleaseInfo LatestRelease { get; private set; }
    }


    public sealed class ReleaseInfo {
        internal ReleaseInfo( int version, int revision, DateTime releaseDate,
                              string summary, string changeLog, ReleaseFlags releaseType ) {
            Version = version;
            Revision = revision;
            Date = releaseDate;
            Summary = summary;
            ChangeLog = changeLog.Split( new[] { '\n' } );
            Flags = releaseType;
        }

        public ReleaseFlags Flags { get; private set; }

        public string FlagsString { get { return ReleaseFlagsToString( Flags ); } }

        public string[] FlagsList { get { return ReleaseFlagsToStringArray( Flags ); } }

        public int Version { get; private set; }

        public int Revision { get; private set; }

        public DateTime Date { get; private set; }

        public TimeSpan Age {
            get {
                return DateTime.UtcNow.Subtract( Date );
            }
        }

        public string VersionString {
            get {
                string formatString = "{0:0.000}_r{1}";
                if( IsFlagged( ReleaseFlags.Dev ) ) {
                    formatString += "_dev";
                }
                if( IsFlagged( ReleaseFlags.Unstable ) ) {
                    formatString += "_u";
                }
                return String.Format( CultureInfo.InvariantCulture, formatString,
                                      Decimal.Divide( Version, 1000 ),
                                      Revision );
            }
        }

        public string Summary { get; private set; }

        public string[] ChangeLog { get; private set; }

        public static ReleaseFlags StringToReleaseFlags( [NotNull] string str ) {
            if( str == null ) throw new ArgumentNullException( "str" );
            ReleaseFlags flags = ReleaseFlags.None;
            for( int i = 0; i < str.Length; i++ ) {
                switch( Char.ToUpper( str[i] ) ) {
                    case 'A':
                        flags |= ReleaseFlags.APIChange;
                        break;
                    case 'B':
                        flags |= ReleaseFlags.Bugfix;
                        break;
                    case 'C':
                        flags |= ReleaseFlags.ConfigFormatChange;
                        break;
                    case 'D':
                        flags |= ReleaseFlags.Dev;
                        break;
                    case 'F':
                        flags |= ReleaseFlags.Feature;
                        break;
                    case 'M':
                        flags |= ReleaseFlags.MapFormatChange;
                        break;
                    case 'P':
                        flags |= ReleaseFlags.PlayerDBFormatChange;
                        break;
                    case 'S':
                        flags |= ReleaseFlags.Security;
                        break;
                    case 'U':
                        flags |= ReleaseFlags.Unstable;
                        break;
                    case 'O':
                        flags |= ReleaseFlags.Optimized;
                        break;
                }
            }
            return flags;
        }

        public static string ReleaseFlagsToString( ReleaseFlags flags ) {
            StringBuilder sb = new StringBuilder();
            if( (flags & ReleaseFlags.APIChange) == ReleaseFlags.APIChange ) sb.Append( 'A' );
            if( (flags & ReleaseFlags.Bugfix) == ReleaseFlags.Bugfix ) sb.Append( 'B' );
            if( (flags & ReleaseFlags.ConfigFormatChange) == ReleaseFlags.ConfigFormatChange ) sb.Append( 'C' );
            if( (flags & ReleaseFlags.Dev) == ReleaseFlags.Dev ) sb.Append( 'D' );
            if( (flags & ReleaseFlags.Feature) == ReleaseFlags.Feature ) sb.Append( 'F' );
            if( (flags & ReleaseFlags.MapFormatChange) == ReleaseFlags.MapFormatChange ) sb.Append( 'M' );
            if( (flags & ReleaseFlags.PlayerDBFormatChange) == ReleaseFlags.PlayerDBFormatChange ) sb.Append( 'P' );
            if( (flags & ReleaseFlags.Security) == ReleaseFlags.Security ) sb.Append( 'S' );
            if( (flags & ReleaseFlags.Unstable) == ReleaseFlags.Unstable ) sb.Append( 'U' );
            if( (flags & ReleaseFlags.Optimized) == ReleaseFlags.Optimized ) sb.Append( 'O' );
            return sb.ToString();
        }

        public static string[] ReleaseFlagsToStringArray( ReleaseFlags flags ) {
            List<string> list = new List<string>();
            if( (flags & ReleaseFlags.APIChange) == ReleaseFlags.APIChange ) list.Add( "API Changes" );
            if( (flags & ReleaseFlags.Bugfix) == ReleaseFlags.Bugfix ) list.Add( "Fixes" );
            if( (flags & ReleaseFlags.ConfigFormatChange) == ReleaseFlags.ConfigFormatChange ) list.Add( "Config Changes" );
            if( (flags & ReleaseFlags.Dev) == ReleaseFlags.Dev ) list.Add( "Developer" );
            if( (flags & ReleaseFlags.Feature) == ReleaseFlags.Feature ) list.Add( "New Features" );
            if( (flags & ReleaseFlags.MapFormatChange) == ReleaseFlags.MapFormatChange ) list.Add( "Map Format Changes" );
            if( (flags & ReleaseFlags.PlayerDBFormatChange) == ReleaseFlags.PlayerDBFormatChange ) list.Add( "PlayerDB Changes" );
            if( (flags & ReleaseFlags.Security) == ReleaseFlags.Security ) list.Add( "Security Patch" );
            if( (flags & ReleaseFlags.Unstable) == ReleaseFlags.Unstable ) list.Add( "Unstable" );
            if( (flags & ReleaseFlags.Optimized) == ReleaseFlags.Optimized ) list.Add( "Optimized" );
            return list.ToArray();
        }

        public bool IsFlagged( ReleaseFlags flag ) {
            return (Flags & flag) == flag;
        }
    }


    #region Enums

    /// <summary> Updater behavior. </summary>
    public enum UpdaterMode {
        /// <summary> Does not check for updates. </summary>
        Disabled,

        /// <summary> Checks for updates and notifies of availability (in console/log). </summary>
        Notify,

        /// <summary> Checks for updates, downloads them if available, and prompts to install.
        /// Behavior is frontend-specific: in ServerGUI, a dialog is shown with the list of changes and
        /// options to update immediately or next time. In ServerCLI, asks to type in 'y' to confirm updating
        /// or press any other key to skip. '''Note: Requires user interaction
        /// (if you restart the server remotely while unattended, it may get stuck on this dialog).''' </summary>
        Prompt,

        /// <summary> Checks for updates, automatically downloads and installs the updates, and restarts the server. </summary>
        Auto,
    }


    /// <summary> A list of release flags/attributes.
    /// Use binary flag logic (value & flag == flag) or Release.IsFlagged() to test for flags. </summary>
    [Flags]
    public enum ReleaseFlags {
        None = 0,

        /// <summary> The API was notably changed in this release. </summary>
        APIChange = 1,

        /// <summary> Bugs were fixed in this release. </summary>
        Bugfix = 2,

        /// <summary> Config.xml format was changed (and version was incremented) in this release. </summary>
        ConfigFormatChange = 4,

        /// <summary> This is a developer-only release, not to be used on live servers.
        /// Untested/undertested releases are often marked as such. </summary>
        Dev = 8,

        /// <summary> A notable new feature was added in this release. </summary>
        Feature = 16,

        /// <summary> The map format was changed in this release (rare). </summary>
        MapFormatChange = 32,

        /// <summary> The PlayerDB format was changed in this release. </summary>
        PlayerDBFormatChange = 64,

        /// <summary> A security issue was addressed in this release. </summary>
        Security = 128,

        /// <summary> There are known or likely stability issues in this release. </summary>
        Unstable = 256,

        /// <summary> This release contains notable optimizations. </summary>
        Optimized = 512
    }

    #endregion
}


namespace fCraft.Events {
    public sealed class CheckingForUpdatesEventArgs : EventArgs, ICancellableEvent {
        internal CheckingForUpdatesEventArgs( string url ) {
            Url = url;
        }

        public string Url { get; set; }
        public bool Cancel { get; set; }
    }


    public sealed class CheckedForUpdatesEventArgs : EventArgs {
        internal CheckedForUpdatesEventArgs( string url, UpdaterResult result ) {
            Url = url;
            Result = result;
        }

        public string Url { get; private set; }
        public UpdaterResult Result { get; private set; }
    }
}