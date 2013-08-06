using System;

/*using LibNbt;
using LibNbt.Exceptions;
using LibNbt.Queries;
using LibNbt.Tags;*/
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

namespace fCraft {

    internal static class DevCommands {

        public static void Init() {
            /*
             * NOTE: These commands are unfinished, under development and non-supported.
             * If you are using a dev build of 800Craft, please comment these the below out to ensure
             * stability.
             * */

            //CommandManager.RegisterCommand(CdFeed);
            //CommandManager.RegisterCommand(CdBot);
            //CommandManager.RegisterCommand(CdSpell);
            //CommandManager.RegisterCommand(CdGame);
        }

        private static readonly CommandDescriptor CdGame = new CommandDescriptor {
            Name = "Game",
            Category = CommandCategory.World,
            Permissions = new Permission[] { Permission.Games },
            IsConsoleSafe = false,
            Usage = "/Unfinished command.",
            Handler = GameHandler
        };

        private static void GameHandler( Player player, Command cmd ) {
            string GameMode = cmd.Next();
            string Option = cmd.Next();
            World world = player.World;
            /*if (world == WorldManager.MainWorld){
                player.Message("/Game cannot be used on the main world");
                return;
            }*/

            if ( GameMode.ToLower() == "zombie" ) {
                if ( Option.ToLower() == "start" ) {
                    ZombieGame game = new ZombieGame( player.World ); //move to world
                    game.Start();
                    return;
                } else {
                    CdGame.PrintUsage( player );
                    return;
                }
            }
            if ( GameMode.ToLower() == "minefield" ) {
                if ( Option.ToLower() == "start" ) {
                    if ( WorldManager.FindWorldExact( "Minefield" ) != null ) {
                        player.Message( "&WA game of Minefield is currently running and must first be stopped" );
                        return;
                    }
                    MineField.GetInstance();
                    MineField.Start( player );
                    return;
                } else if ( Option.ToLower() == "stop" ) {
                    if ( WorldManager.FindWorldExact( "Minefield" ) == null ) {
                        player.Message( "&WA game of Minefield is currently not running" );
                        return;
                    }
                    MineField.Stop( player, false );
                    return;
                } else {
                    CdGame.PrintUsage( player );
                    return;
                }
            } else {
                CdGame.PrintUsage( player );
                return;
            }
        }

        /*static readonly CommandDescriptor CdBot = new CommandDescriptor {
            Name = "Bot",
            Category = CommandCategory.Fun,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = false,
            NotRepeatable = true,
            Usage = "/Spell",
            Help = "Penis",
            UsableByFrozenPlayers = false,
            Handler = BotHandler,
        };
        internal static void BotHandler ( Player player, Command cmd ) {
            Bot bot = player.Bot;
            string yes = cmd.Next();
            if ( yes.ToLower() == "create" ) {
                string Name = cmd.Next();
                Position Pos = new Position( player.Position.X, player.Position.Y, player.Position.Z, player.Position.R, player.Position.L );
                player.Bot = new Bot( Name, Pos, 1, player.World );
                //player.Bot.SetBot();
            }
        }*/

        private static readonly CommandDescriptor CdSpell = new CommandDescriptor {
            Name = "Spell",
            Category = CommandCategory.Fun,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = false,
            NotRepeatable = true,
            Usage = "/Spell",
            Help = "Penis",
            UsableByFrozenPlayers = false,
            Handler = SpellHandler,
        };

        public static SpellStartBehavior particleBehavior = new SpellStartBehavior();

        internal static void SpellHandler( Player player, Command cmd ) {
            World world = player.World;
            Vector3I pos1 = player.Position.ToBlockCoords();
            Random _r = new Random();
            int n = _r.Next( 8, 12 );
            for ( int i = 0; i < n; ++i ) {
                double phi = -_r.NextDouble() + -player.Position.L * 2 * Math.PI;
                double ksi = -_r.NextDouble() + player.Position.R * Math.PI - Math.PI / 2.0;

                Vector3F direction = ( new Vector3F( ( float )( Math.Cos( phi ) * Math.Cos( ksi ) ), ( float )( Math.Sin( phi ) * Math.Cos( ksi ) ), ( float )Math.Sin( ksi ) ) ).Normalize();
                world.AddPhysicsTask( new Particle( world, ( pos1 + 2 * direction ).Round(), direction, player, Block.Obsidian, particleBehavior ), 0 );
            }
        }
    }
}