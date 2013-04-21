/*        ----
        Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com>
        All rights reserved.

        Redistribution and use in source and binary forms, with or without
        modification, are permitted provided that the following conditions are met:
         * Redistributions of source code must retain the above copyright
              notice, this list of conditions and the following disclaimer.
            * Redistributions in binary form must reproduce the above copyright
             notice, this list of conditions and the following disclaimer in the
             documentation and/or other materials provided with the distribution.
            * Neither the name of 800Craft or the names of its
             contributors may be used to endorse or promote products derived from this
             software without specific prior written permission.

        THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
        ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
        WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
        DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
        DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
        (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
        LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
        ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
        (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
        SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
        ----*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RandomMaze;
using System.Threading;

namespace fCraft {
    internal static class FunCommands {
        internal static void Init () {
            CommandManager.RegisterCommand( CdRandomMaze );
            CommandManager.RegisterCommand( CdMazeCuboid );
            CommandManager.RegisterCommand( CdFirework );
            CommandManager.RegisterCommand( CdLife );
            CommandManager.RegisterCommand( CdPossess );
            CommandManager.RegisterCommand( CdUnpossess );
        }

        #region Possess / UnPossess

        static readonly CommandDescriptor CdPossess = new CommandDescriptor {
            Name = "Possess",
            Category = CommandCategory.Fun,
            Permissions = new[] { Permission.Possess },
            Usage = "/Possess PlayerName",
            Handler = PossessHandler
        };

        static void PossessHandler ( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if ( targetName == null ) {
                CdPossess.PrintUsage( player );
                return;
            }
            Player target = Server.FindPlayerOrPrintMatches( player, targetName, false, true );
            if ( target == null ) return;
            if ( target.Immortal ) {
                player.Message( "You cannot possess {0}&S, they are immortal", target.ClassyName );
                return;
            }
            if ( target == player ) {
                player.Message( "You cannot possess yourself." );
                return;
            }

            if ( !player.Can( Permission.Possess, target.Info.Rank ) ) {
                player.Message( "You may only possess players ranked {0}&S or lower.",
                player.Info.Rank.GetLimit( Permission.Possess ).ClassyName );
                player.Message( "{0}&S is ranked {1}",
                                target.ClassyName, target.Info.Rank.ClassyName );
                return;
            }

            if ( !player.Possess( target ) ) {
                player.Message( "Already possessing {0}", target.ClassyName );
            }
        }


        static readonly CommandDescriptor CdUnpossess = new CommandDescriptor {
            Name = "unpossess",
            Category = CommandCategory.Fun,
            Permissions = new[] { Permission.Possess },
            NotRepeatable = true,
            Usage = "/Unpossess target",
            Handler = UnpossessHandler
        };

        static void UnpossessHandler ( Player player, Command cmd ) {
            string targetName = cmd.Next();
            if ( targetName == null ) {
                CdUnpossess.PrintUsage( player );
                return;
            }
            Player target = Server.FindPlayerOrPrintMatches( player, targetName, true, true );
            if ( target == null ) return;

            if ( !player.StopPossessing( target ) ) {
                player.Message( "You are not currently possessing anyone." );
            }
        }

        #endregion

        static readonly CommandDescriptor CdLife = new CommandDescriptor {
            Name = "Life",
            Category = CommandCategory.Fun,
            Permissions = new[] { Permission.DrawAdvanced },
            IsConsoleSafe = false,
            NotRepeatable = true,
            Usage = "/Life <command> [params]",
            Help = "&HGoogle \"Conwey's Game of Life\"\n'&H/Life help'&S for more usage info\n(c) 2012 LaoTszy",
            UsableByFrozenPlayers = false,
            Handler = LifeHandlerFunc,
        };

        static readonly CommandDescriptor CdFirework = new CommandDescriptor {
            Name = "Firework",
            Category = CommandCategory.Fun,
            Permissions = new[] { Permission.Fireworks },
            IsConsoleSafe = false,
            NotRepeatable = false,
            Usage = "/Firework",
            Help = "&HToggles Firework Mode on/off for yourself. " +
            "All Gold blocks will be replaced with fireworks if " +
            "firework physics are enabled for the current world.",
            UsableByFrozenPlayers = false,
            Handler = FireworkHandler
        };

        static void FireworkHandler ( Player player, Command cmd ) {
            if ( player.fireworkMode ) {
                player.fireworkMode = false;
                player.Message( "Firework Mode has been turned off." );
                return;
            } else {
                player.fireworkMode = true;
                player.Message( "Firework Mode has been turned on. " +
                    "All Gold blocks are now being replaced with Fireworks." );
            }
        }


        static readonly CommandDescriptor CdRandomMaze = new CommandDescriptor {
            Name = "RandomMaze",
            Aliases = new string[] { "3dmaze" },
            Category = CommandCategory.Fun,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help =
                "Choose the size (width, length and height) and it will draw a random maze at the chosen point. " +
                "Optional parameters tell if the lifts are to be drawn and if hint blocks (log) are to be added. \n(C) 2012 Lao Tszy",
            Usage = "/randommaze <width> <length> <height> [nolifts] [hints]",
            Handler = MazeHandler
        };
        static readonly CommandDescriptor CdMazeCuboid = new CommandDescriptor {
            Name = "MazeCuboid",
            Aliases = new string[] { "Mc", "Mz", "Maze" },
            Category = CommandCategory.Fun,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help =
                "Draws a cuboid with the current brush and with a random maze inside.(C) 2012 Lao Tszy",
            Usage = "/MazeCuboid [block type]",
            Handler = MazeCuboidHandler,
        };

        private static void MazeHandler ( Player p, Command cmd ) {
            try {
                RandomMazeDrawOperation op = new RandomMazeDrawOperation( p, cmd );
                BuildingCommands.DrawOperationBegin( p, cmd, op );
            } catch ( Exception e ) {
                Logger.Log( LogType.Error, "Error: " + e.Message );
            }
        }
        private static void MazeCuboidHandler ( Player p, Command cmd ) {
            try {
                MazeCuboidDrawOperation op = new MazeCuboidDrawOperation( p );
                BuildingCommands.DrawOperationBegin( p, cmd, op );
            } catch ( Exception e ) {
                Logger.Log( LogType.Error, "Error: " + e.Message );
            }
        }
        private static void LifeHandlerFunc ( Player p, Command cmd ) {
            try {
                if ( !cmd.HasNext ) {
                    p.Message( "&H/Life <command> <params>. Commands are Help, Create, Delete, Start, Stop, Set, List, Print" );
                    p.Message( "Type /Life help <command> for more information" );
                    return;
                }
                LifeHandler.ProcessCommand( p, cmd );
            } catch ( Exception e ) {
                p.Message( "Error: " + e.Message );
            }
        }


    }
}
