// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft {

    /// <summary> Controller for setting and checking per-rank permissions and per-player exceptions.
    /// Used by World.AccessSecurity, World.BuildSecurity, and Zone. </summary>
    public sealed class SecurityController : ICloneable, INotifiesOnChange {

        readonly Dictionary<string, PlayerInfo> includedPlayers = new Dictionary<string, PlayerInfo>();
        readonly Dictionary<string, PlayerInfo> excludedPlayers = new Dictionary<string, PlayerInfo>();

        public PlayerExceptions ExceptionList { get; private set; }
        readonly object locker = new object();
        
        [CanBeNull]
        Rank minRank;

        /// <summary> Lowest allowed player rank. </summary>
        [NotNull]
        public Rank MinRank {
            get {
                return minRank ?? RankManager.LowestRank;
            }
            set {
                if( value == null ) throw new ArgumentNullException( "value" );
                if( minRank != value ) {
                    minRank = value;
                    RaiseChangedEvent();
                }
            }
        }

        public void ResetMinRank() {
            if( minRank != null ) {
                minRank = null;
                RaiseChangedEvent();
            }
        }


        /// <summary> True if a rank restriction is in effect.
        /// This property is used to distinguish cases of no MinRank set
        /// vs. cases of MinRank set to LowestRank. </summary>
        public bool HasRankRestriction {
            get { return (minRank != null); }
        }


        /// <summary> True if this controller has any restrictions
        /// (per-rank or per-player). </summary>
        public bool HasRestrictions {
            get {
                return MinRank > RankManager.LowestRank ||
                       ExceptionList.Excluded.Length > 0;
            }
        }


        /// <summary> Creates a new controller with no restrictions. </summary>
        public SecurityController() {
            UpdatePlayerListCache();
        }


        void UpdatePlayerListCache() {
            lock( locker ) {
                ExceptionList = new PlayerExceptions( includedPlayers.Values.ToArray(),
                                                      excludedPlayers.Values.ToArray() );
            }
        }


        /// <summary> Either specially includes a player (if their state
        /// was previously neutral), or removes a specific exclusion. </summary>
        /// <param name="info"> Player's info. </param>
        /// <returns> Previous exception state of the player. </returns>
        public PermissionOverride Include( [NotNull] PlayerInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            lock( locker ) {
                if( includedPlayers.ContainsValue( info ) ) {
                    return PermissionOverride.Allow;
                } else if( excludedPlayers.ContainsValue( info ) ) {
                    excludedPlayers.Remove( info.Name.ToLower() );
                    UpdatePlayerListCache();
                    RaiseChangedEvent();
                    return PermissionOverride.Deny;
                } else {
                    includedPlayers.Add( info.Name.ToLower(), info );
                    UpdatePlayerListCache();
                    RaiseChangedEvent();
                    return PermissionOverride.None;
                }
            }
        }


        /// <summary> Either specially excludes a player (if their state
        /// was previously neutral), or removes a specific inclusion. </summary>
        /// <param name="info"> Player's info. </param>
        /// <returns> Previous exception state of the player. </returns>
        public PermissionOverride Exclude( [NotNull] PlayerInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            lock( locker ) {
                if( excludedPlayers.ContainsValue( info ) ) {
                    return PermissionOverride.Deny;
                } else if( includedPlayers.ContainsValue( info ) ) {
                    includedPlayers.Remove( info.Name.ToLower() );
                    UpdatePlayerListCache();
                    RaiseChangedEvent();
                    return PermissionOverride.Allow;
                } else {
                    excludedPlayers.Add( info.Name.ToLower(), info );
                    UpdatePlayerListCache();
                    RaiseChangedEvent();
                    return PermissionOverride.None;
                }
            }
        }


        /// <summary> Checks whether a player is allowed by this controller. </summary>
        /// <param name="info"> Player to check. </param>
        /// <returns> True if player is allowed. </returns>
        public bool Check( [NotNull] PlayerInfo info ) {
            // ReSharper disable LoopCanBeConvertedToQuery
            if( info == null ) throw new ArgumentNullException( "info" );
            PlayerExceptions listCache = ExceptionList;
            for( int i = 0; i < listCache.Excluded.Length; i++ ) {
                if( listCache.Excluded[i] == info ) {
                    return false;
                }
            }

            if( info.Rank >= MinRank /*&& player.info.rank <= maxRank*/ ) return true; // TODO: implement maxrank

            for( int i = 0; i < listCache.Included.Length; i++ ) {
                if( listCache.Included[i] == info ) {
                    return true;
                }
            }

            return false;
            // ReSharper restore LoopCanBeConvertedToQuery
        }


        /// <summary> Checks player's permission status with this controller, in detail. </summary>
        /// <param name="info"> Player to check. </param>
        /// <returns> Security check result. </returns>
        public SecurityCheckResult CheckDetailed( [NotNull] PlayerInfo info ) {
            // ReSharper disable LoopCanBeConvertedToQuery
            if( info == null ) throw new ArgumentNullException( "info" );
            PlayerExceptions listCache = ExceptionList;
            for( int i = 0; i < listCache.Excluded.Length; i++ ) {
                if( listCache.Excluded[i] == info ) {
                    return SecurityCheckResult.BlackListed;
                }
            }

            if( info.Rank >= MinRank /*&& player.info.rank <= maxRank*/ ) // TODO: implement maxrank
                return SecurityCheckResult.Allowed;

            for( int i = 0; i < listCache.Included.Length; i++ ) {
                if( listCache.Included[i] == info ) {
                    return SecurityCheckResult.WhiteListed;
                }
            }

            return SecurityCheckResult.RankTooLow;
            // ReSharper restore LoopCanBeConvertedToQuery
        }


        /// <summary> Creates a description string of the controller. </summary>
        /// <param name="target"> Name of the object that owns this controller. </param>
        /// <param name="noun"> The type of target (e.g. "world" or "zone"). </param>
        /// <param name="verb"> The action, in past tense, that this
        /// controller manages (e.g. "accessed" or "modified"). </param>
        public string GetDescription( [NotNull] IClassy target, [NotNull] string noun, [NotNull] string verb ) {
            if( target == null ) throw new ArgumentNullException( "target" );
            if( noun == null ) throw new ArgumentNullException( "noun" );
            if( verb == null ) throw new ArgumentNullException( "verb" );
            PlayerExceptions list = ExceptionList;

            StringBuilder message = new StringBuilder( noun );
            message[0] = Char.ToUpper( message[0] ); // capitalize first letter.

            if( HasRankRestriction ) {
                message.AppendFormat( " {0}&S can only be {1} by {2}+&S",
                                      target.ClassyName,
                                      verb,
                                      MinRank.ClassyName );
            } else {
                message.AppendFormat( " {0}&S can be {1} by anyone",
                                      target.ClassyName,
                                      verb );
            }

            if( list.Included.Length > 0 ) {
                message.AppendFormat( " and {0}&S", list.Included.JoinToClassyString() );
            }

            if( list.Excluded.Length > 0 ) {
                message.AppendFormat( ", except {0}&S", list.Excluded.JoinToClassyString() );
            }

            message.Append( '.' );
            return message.ToString();
        }


        #region XML Serialization

        public const string XmlRootElementName = "PermissionController";

        readonly XElement[] rawExceptions;

        public SecurityController( [NotNull] XContainer el, bool parseExceptions ) {
            if( el == null ) throw new ArgumentNullException( "el" );
            if( el.Element( "minRank" ) != null ) {
                // ReSharper disable PossibleNullReferenceException
                minRank = Rank.Parse( el.Element( "minRank" ).Value );
                // ReSharper restore PossibleNullReferenceException
            } else {
                minRank = null;
            }

            if( parseExceptions ) {
                //maxRank = Rank.Parse( root.Element( "maxRank" ).Value );
                foreach( XElement player in el.Elements( "included" ) ) {
                    if( !Player.IsValidName( player.Value ) ) continue;
                    PlayerInfo info = PlayerDB.FindPlayerInfoExact( player.Value );
                    if( info != null ) Include( info );
                }

                foreach( XElement player in el.Elements( "excluded" ) ) {
                    if( !Player.IsValidName( player.Value ) ) continue;
                    PlayerInfo info = PlayerDB.FindPlayerInfoExact( player.Value );
                    if( info != null ) Exclude( info );
                }
            } else {
                rawExceptions = el.Elements( "included" ).Union( el.Elements( "excluded" ) ).ToArray();
            }
            UpdatePlayerListCache();
        }


        public XElement Serialize() {
            return Serialize( XmlRootElementName );
        }


        public XElement Serialize( [NotNull] string tagName ) {
            if( tagName == null ) throw new ArgumentNullException( "tagName" );

            XElement root = new XElement( tagName );
            if( HasRankRestriction ) {
                root.Add( new XElement( "minRank", MinRank.FullName ) );
            }
            //root.Add( new XElement( "maxRank", maxRank ) );

            lock( locker ) {
                foreach( string playerName in includedPlayers.Keys ) {
                    root.Add( new XElement( "included", playerName ) );
                }
                foreach( string playerName in excludedPlayers.Keys ) {
                    root.Add( new XElement( "excluded", playerName ) );
                }
                if( rawExceptions != null ) {
                    foreach( XElement exception in rawExceptions ) {
                        root.Add( exception );
                    }
                }
            }
            return root;
        }

        #endregion


        #region Resetting

        /// <summary> Clears the list of specifically included players. </summary>
        public void ResetIncludedList() {
            lock( locker ) {
                includedPlayers.Clear();
                UpdatePlayerListCache();
            }
        }


        /// <summary> Clears the list of specifically excluded players. </summary>
        public void ResetExcludedList() {
            lock( locker ) {
                excludedPlayers.Clear();
                UpdatePlayerListCache();
            }
        }


        /// <summary> Resets all permissions: minimum rank,
        /// excluded player list, and included player list. </summary>
        public void Reset() {
            ResetMinRank();
            ResetIncludedList();
            ResetExcludedList();
        }

        #endregion


        #region Cloning

        /// <summary> Creates a copy of an existing controller. </summary>
        public SecurityController( [NotNull] SecurityController other ) {
            if( other == null ) throw new ArgumentNullException( "other" );
            minRank = other.minRank;
            rawExceptions = other.rawExceptions;
            lock( other.locker ) {
                includedPlayers = new Dictionary<string, PlayerInfo>( other.includedPlayers );
                excludedPlayers = new Dictionary<string, PlayerInfo>( other.excludedPlayers );
            }
            UpdatePlayerListCache();
        }


        /// <summary> Creates a copy of an existing controller. </summary>
        public object Clone() {
            return new SecurityController( this );
        }

        #endregion

        public event EventHandler Changed;

        void RaiseChangedEvent() {
            var h = Changed;
            if( h != null ) h( null, EventArgs.Empty );
        }
    }


    /// <summary> List of included and excluded players. </summary>
    public struct PlayerExceptions {
        public PlayerExceptions( [NotNull] PlayerInfo[] included, [NotNull] PlayerInfo[] excluded ) {
            if( included == null ) throw new ArgumentNullException( "included" );
            if( excluded == null ) throw new ArgumentNullException( "excluded" );
            Included = included;
            Excluded = excluded;
        }

        // keeping both lists on one object allows lock-free synchronization
        public readonly PlayerInfo[] Included;

        public readonly PlayerInfo[] Excluded;
    }


    #region Enums

    /// <summary> Indicates what kind of per-entity override/exception is defined in a security controller. </summary>
    public enum PermissionOverride {
        /// <summary> No permission exception. </summary>
        None,

        /// <summary> Entity is explicitly allowed / whitelisted. </summary>
        Allow,

        /// <summary> Entity is explicitly denied / blacklisted. </summary>
        Deny
    }


    /// <summary> Possible results of a SecurityController permission check. </summary>
    public enum SecurityCheckResult {
        /// <summary> Allowed, no permission involved. </summary>
        Allowed,

        /// <summary> Denied, rank too low. </summary>
        RankTooLow,

        /// <summary> Denied, rank too high (not yet implemented). </summary>
        RankTooHigh,

        /// <summary> Allowed, this entity was explicitly allowed / whitelisted. </summary>
        WhiteListed,

        /// <summary> Denied, this entity was explicitly denied / blacklisted. </summary>
        BlackListed
    }

    #endregion
}