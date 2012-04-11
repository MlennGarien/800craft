// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace fCraft {

    /// <summary> Delegate for command handlers/callbacks. </summary>
    /// <param name="source"> Player who called the command. </param>
    /// <param name="cmd"> Command arguments. </param>
    public delegate void CommandHandler( Player source, Command cmd );


    /// <summary> Describes a chat command. Defines properties, permission requirements, and usage information.
    /// Specifies a handler method. </summary>
    public sealed class CommandDescriptor : IClassy {

        /// <summary> List of aliases. May be null or empty. Default: null </summary>
        [CanBeNull]
        public string[] Aliases { get; set; }

        /// <summary> Command category. Must be set before registering. </summary>
        public CommandCategory Category { get; set; }

        /// <summary> Whether the command may be used from console. Default: false </summary>
        public bool IsConsoleSafe { get; set; }

        /// <summary> Callback function to execute when command is called. Must be set before registering. </summary>
        public CommandHandler Handler { get; set; }

        /// <summary> Full text of the help message. Default: null </summary>
        public string Help { get; set; }

        /// <summary> Whether the command is hidden from command list (/cmds). Default: false </summary>
        public bool IsHidden { get; set; }

        /// <summary> Whether the command is not part of fCraft core (set automatically). </summary>
        public bool IsCustom { get; internal set; }

        /// <summary> Whether the command should be repeated by the "/" shortcut. Default: false </summary>
        public bool NotRepeatable { get; set; }

        /// <summary> Whether the command should be usable by frozen players. Default: false </summary>
        public bool UsableByFrozenPlayers { get; set; }

        /// <summary> Whether calls to this command should not be logged. </summary>
        public bool DisableLogging { get; set; }

        /// <summary> Primary command name. Must be set before registering. </summary>
        public string Name { get; set; }

        /// <summary> List of permissions required to call the command. May be empty or null. Default: null </summary>
        public Permission[] Permissions { get; set; }

        /// <summary> Whether any permission from the list is enough.
        /// If this is false, ALL permissions are required. </summary>
        public bool AnyPermission { get; set; }

        /// <summary> Brief demonstration of command's usage syntax. Defaults to "/Name". </summary>
        public string Usage { get; set; }

        /// <summary> Help sub-sections. </summary>
        public Dictionary<string, string> HelpSections { get; set; }

        /// <summary> Whether this command involves a selection that can be repeated with /static. Default: false </summary>
        public bool RepeatableSelection { get; set; }


        /// <summary> Checks whether this command may be called by players of a given rank. </summary>
        public bool CanBeCalledBy( [NotNull] Rank rank ) {
            if( rank == null ) throw new ArgumentNullException( "rank" );
            return Permissions == null ||
                   Permissions.All( rank.Can ) ||
                   AnyPermission && Permissions.Any( rank.Can );
        }


        public Rank MinRank {
            get {
                if( AnyPermission ) {
                    return RankManager.GetMinRankWithAnyPermission( Permissions );
                } else {
                    return RankManager.GetMinRankWithAllPermissions( Permissions );
                }
            }
        }


        /// <summary> Checks whether players of the given rank should see this command in /cmds list.
        /// Takes permissions and the hidden flag into account. </summary>
        public bool IsVisibleTo( [NotNull] Rank rank ) {
            if( rank == null ) throw new ArgumentNullException( "rank" );
            return !IsHidden && CanBeCalledBy( rank );
        }


        /// <summary> Prints command usage syntax to the given player. </summary>
        public void PrintUsage( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( Usage != null ) {
                player.Message( "Usage: &H{0}", Usage );
            } else {
                player.Message( "Usage: &H/{0}", Name );
            }
        }


        /// <summary> Calls this command. </summary>
        /// <param name="player"> Player who called the command. </param>
        /// <param name="cmd"> Command arguments. </param>
        /// <param name="raiseEvent"> Whether CommandCalling and CommandCalled events should be raised. </param>
        /// <returns> True if the command was called succesfully.
        /// False if the call was cancelled by the CommandCalling event. </returns>
        public bool Call( [NotNull] Player player, [NotNull] Command cmd, bool raiseEvent ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( raiseEvent && CommandManager.RaiseCommandCallingEvent( cmd, this, player ) ) return false;
            Handler( player, cmd );
            if( raiseEvent ) CommandManager.RaiseCommandCalledEvent( cmd, this, player );
            return true;
        }


        public override string ToString() {
            return String.Format( "CommandDescriptor({0})", Name );
        }

        public string ClassyName {
            get {
                if( ConfigKey.RankColorsInChat.Enabled() ) {
                    Rank minRank;
                    if( Permissions != null && Permissions.Length > 0 ) {
                        minRank = MinRank;
                    } else {
                        minRank = RankManager.LowestRank;
                    }
                    if( minRank == null ) {
                        return Name;
                    } else if( ConfigKey.RankPrefixesInChat.Enabled() ) {
                        return minRank.Color + minRank.Prefix + Name;
                    } else {
                        return minRank.Color + Name;
                    }
                } else {
                    return Name;
                }
            }
        }
    }
}