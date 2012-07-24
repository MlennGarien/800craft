// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Static class that allows registration and parsing of all text commands. </summary>
    public static class CommandManager {
        static readonly SortedList<string, string> Aliases = new SortedList<string, string>();
        static readonly SortedList<string, CommandDescriptor> Commands = new SortedList<string, CommandDescriptor>();

        public static readonly string[] ReservedCommandNames = new[] { "ok", "nvm", "dev" };

        // Sets up all the command hooks
        public static void Init() {
            DevCommands.Init();
            ModerationCommands.Init();
            BuildingCommands.Init();
            InfoCommands.Init();
            WorldCommands.Init();
            ZoneCommands.Init();
            MaintenanceCommands.Init();
            ChatCommands.Init();
            FunCommands.Init();
            MathCommands.Init();
            Logger.Log( LogType.Debug,
                        "CommandManager: {0} commands registered ({1} hidden, {2} aliases)",
                        Commands.Count,
                        GetCommands( true ).Length,
                        Aliases.Count );
        }


        /// <summary> Gets a list of all commands (includding hidden ones). </summary>
        public static CommandDescriptor[] GetCommands() {
            return Commands.Values.ToArray();
        }


        /// <summary> Gets a list of ONLY hidden or non-hidden commands, not both. </summary>
        public static CommandDescriptor[] GetCommands( bool hidden ) {
            return Commands.Values
                           .Where( cmd => ( cmd.IsHidden == hidden ) )
                           .ToArray();
        }


        /// <summary> Gets a list of commands available to a specified rank. </summary>
        public static CommandDescriptor[] GetCommands( [NotNull] Rank rank, bool includeHidden ) {
            if( rank == null ) throw new ArgumentNullException( "rank" );
            return Commands.Values
                           .Where( cmd => ( !cmd.IsHidden || includeHidden ) &&
                                          cmd.CanBeCalledBy( rank ) )
                           .ToArray();
        }


        /// <summary> Gets a list of commands in a specified category.
        /// Note that commands may belong to more than one category. </summary>
        public static CommandDescriptor[] GetCommands( CommandCategory category, bool includeHidden ) {
            return Commands.Values
                           .Where( cmd => ( includeHidden || !cmd.IsHidden ) &&
                                          ( cmd.Category & category ) == category )
                           .ToArray();
        }


        /// <summary> Registers a custom command with fCraft.
        /// CommandRegistrationException may be thrown if the given descriptor does not meet all the requirements. </summary>
        public static void RegisterCustomCommand( [NotNull] CommandDescriptor descriptor ) {
            if( descriptor == null ) throw new ArgumentNullException( "descriptor" );
            descriptor.IsCustom = true;
            RegisterCommand( descriptor );
        }


        internal static void RegisterCommand( [NotNull] CommandDescriptor descriptor ) {
            if( descriptor == null ) throw new ArgumentNullException( "descriptor" );

#if DEBUG
            if( descriptor.Category == CommandCategory.None && !descriptor.IsCustom ) {
                throw new CommandRegistrationException( "Standard commands must have a category set." );
            }
#endif

            if( !IsValidCommandName( descriptor.Name ) ) {
                throw new CommandRegistrationException( "All commands need a name, between 1 and 16 alphanumeric characters long." );
            }

            string normalizedName = descriptor.Name.ToLower();

            if( Commands.ContainsKey( normalizedName ) ) {
                throw new CommandRegistrationException( "A command with the name \"{0}\" is already registered.", descriptor.Name );
            }

            if( ReservedCommandNames.Contains( normalizedName ) ) {
                throw new CommandRegistrationException( "The command name is reserved." );
            }

            if( descriptor.Handler == null ) {
                throw new CommandRegistrationException( "All command descriptors are required to provide a handler callback." );
            }

            if( descriptor.Aliases != null ) {
                if( descriptor.Aliases.Any( alias => Commands.ContainsKey( alias ) ) ) {
                    throw new CommandRegistrationException( "One of the aliases for \"{0}\" is using the name of an already-defined command." );
                }
            }

            if( !Char.IsUpper( descriptor.Name[0] ) ) {
                descriptor.Name = descriptor.Name.UppercaseFirst();
            }

            if( descriptor.Usage == null ) {
                descriptor.Usage = "/" + descriptor.Name;
            }

            if( RaiseCommandRegisteringEvent( descriptor ) ) return;

            if( Aliases.ContainsKey( normalizedName ) ) {
                Logger.Log( LogType.Warning,
                            "CommandManager.RegisterCommand: \"{0}\" was defined as an alias for \"{1}\", " +
                            "but has now been replaced by a different command of the same name.",
                            descriptor.Name, Aliases[descriptor.Name] );
                Aliases.Remove( normalizedName );
            }

            if( descriptor.Aliases != null ) {
                foreach( string alias in descriptor.Aliases ) {
                    string normalizedAlias = alias.ToLower();
                    if( ReservedCommandNames.Contains( normalizedAlias ) ) {
                        Logger.Log( LogType.Warning,
                                    "CommandManager.RegisterCommand: Alias \"{0}\" for \"{1}\" ignored (reserved name).",
                                    alias, descriptor.Name );
                    } else if( Aliases.ContainsKey( normalizedAlias ) ) {
                        Logger.Log( LogType.Warning,
                                    "CommandManager.RegisterCommand: \"{0}\" was defined as an alias for \"{1}\", " +
                                    "but has been overridden to resolve to \"{2}\" instead.",
                                    alias, Aliases[normalizedAlias], descriptor.Name );
                    } else {
                        Aliases.Add( normalizedAlias, normalizedName );
                    }
                }
            }

            Commands.Add( normalizedName, descriptor );

            RaiseCommandRegisteredEvent( descriptor );
        }


        /// <summary> Finds an instance of CommandDescriptor for a given command.
        /// Case-insensitive, but no autocompletion. </summary>
        /// <param name="commandName"> Command to find. </param>
        /// <param name="alsoCheckAliases"> Whether to check command aliases. </param>
        /// <returns> CommandDesriptor object if found, null if not found. </returns>
        [CanBeNull]
        public static CommandDescriptor GetDescriptor( [NotNull] string commandName, bool alsoCheckAliases ) {
            if( commandName == null ) throw new ArgumentNullException( "commandName" );
            commandName = commandName.ToLower();
            if( Commands.ContainsKey( commandName ) ) {
                return Commands[commandName];
            } else if( alsoCheckAliases && Aliases.ContainsKey( commandName ) ) {
                return Commands[Aliases[commandName]];
            } else {
                return null;
            }
        }


        /// <summary> Parses and calls a specified command. </summary>
        /// <param name="player"> Player who issued the command. </param>
        /// <param name="cmd"> Command to be parsed and executed. </param>
        /// <param name="fromConsole"> Whether this command is being called from a non-player (e.g. Console). </param>
        /// <returns> True if the command was called, false if something prevented it from being called. </returns>
        public static bool ParseCommand( [NotNull] Player player, [NotNull] Command cmd, bool fromConsole ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            CommandDescriptor descriptor = GetDescriptor( cmd.Name, true );

            if( descriptor == null ) {
                player.Message( "Unknown command \"{0}\". See &H/Commands", cmd.Name );
                return false;
            }

            if( !descriptor.IsConsoleSafe && fromConsole ) {
                player.Message( "You cannot use this command from console." );
            } else {
                if( descriptor.Permissions != null ) {
                    if( !descriptor.CanBeCalledBy( player.Info.Rank ) ) {
                        player.MessageNoAccess( descriptor );
                    } else if( !descriptor.Call( player, cmd, true ) ) {
                        player.Message( "Command was cancelled." );
                    } else {
                        return true;
                    }
                } else {
                    if( descriptor.Call( player, cmd, true ) ) {
                        return true;
                    } else {
                        player.Message( "Command was cancelled." );
                    }
                }
            }
            return false;
        }


        /// <summary> Checks whether a command name is acceptible.
        /// Constraints are similar to Player.IsValidName, except for minimum length. </summary>
        /// <param name="name"> Command name to check. </param>
        /// <returns> True if the name is valid. </returns>
        public static bool IsValidCommandName( [NotNull] string name ) {
            if( name == null ) throw new ArgumentNullException( "name" );
            if( name.Length == 0 || name.Length > 16 ) return false;
            // ReSharper disable LoopCanBeConvertedToQuery
            for( int i = 0; i < name.Length; i++ ) {
                char ch = name[i];
                if( ( ch < '0' && ch != '.' ) || ( ch > '9' && ch < 'A' ) || ( ch > 'Z' && ch < '_' ) ||
                    ( ch > '_' && ch < 'a' ) || ch > 'z' ) {
                    return false;
                }
            }
            // ReSharper restore LoopCanBeConvertedToQuery
            return true;
        }


        #region Events

        /// <summary> Occurs when a command is being registered (cancellable). </summary>
        public static event EventHandler<CommandRegistringEventArgs> CommandRegistering;

        /// <summary> Occurs when a command has been registered. </summary>
        public static event EventHandler<CommandRegisteredEventArgs> CommandRegistered;

        /// <summary> Occurs when a command is being called by a player or the console (cancellable). </summary>
        public static event EventHandler<CommandCallingEventArgs> CommandCalling;

        /// <summary> Occurs when the command has been called by a player or the console. </summary>
        public static event EventHandler<CommandCalledEventArgs> CommandCalled;


        static bool RaiseCommandRegisteringEvent( CommandDescriptor descriptor ) {
            var h = CommandRegistering;
            if( h == null ) return false;
            var e = new CommandRegistringEventArgs( descriptor );
            h( null, e );
            return e.Cancel;
        }


        static void RaiseCommandRegisteredEvent( CommandDescriptor descriptor ) {
            var h = CommandRegistered;
            if( h != null ) h( null, new CommandRegisteredEventArgs( descriptor ) );
        }


        internal static bool RaiseCommandCallingEvent( Command cmd, CommandDescriptor descriptor, Player player ) {
            var h = CommandCalling;
            if( h == null ) return false;
            var e = new CommandCallingEventArgs( cmd, descriptor, player );
            h( null, e );
            return e.Cancel;
        }


        internal static void RaiseCommandCalledEvent( Command cmd, CommandDescriptor descriptor, Player player ) {
            var h = CommandCalled;
            if( h != null ) CommandCalled( null, new CommandCalledEventArgs( cmd, descriptor, player ) );
        }

        #endregion
    }

    public sealed class CommandRegistrationException : Exception {
        public CommandRegistrationException( string message ) : base( message ) { }
        [StringFormatMethod( "message" )]
        public CommandRegistrationException( string message, params object[] args ) :
            base( String.Format( message, args ) ) { }
    }
}


namespace fCraft.Events {
    public class CommandRegisteredEventArgs : EventArgs {
        internal CommandRegisteredEventArgs( CommandDescriptor commandDescriptor ) {
            CommandDescriptor = commandDescriptor;
        }

        public CommandDescriptor CommandDescriptor { get; private set; }
    }


    public sealed class CommandRegistringEventArgs : CommandRegisteredEventArgs, ICancellableEvent {
        internal CommandRegistringEventArgs( CommandDescriptor commandDescriptor )
            : base( commandDescriptor ) {
        }

        public bool Cancel { get; set; }
    }


    public class CommandCalledEventArgs : EventArgs {
        internal CommandCalledEventArgs( Command command, CommandDescriptor commandDescriptor, Player player ) {
            Command = command;
            CommandDescriptor = commandDescriptor;
            Player = player;
        }

        public Command Command { get; private set; }
        public CommandDescriptor CommandDescriptor { get; private set; }
        public Player Player { get; private set; }
    }


    public sealed class CommandCallingEventArgs : CommandCalledEventArgs, ICancellableEvent {
        internal CommandCallingEventArgs( Command command, CommandDescriptor commandDescriptor, Player player ) :
            base( command, commandDescriptor, player ) {
        }

        public bool Cancel { get; set; }
    }
}