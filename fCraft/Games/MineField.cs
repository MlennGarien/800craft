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

//Copyright (C) <2011 - 2013> Jon Baker(http://au70.net)
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using fCraft.MapConversion;
using fCraft.Events;

namespace fCraft {
    class MineField {
        private static World _world;
        private const int _ground = 1;
        private static Map _map;
        public static List<Player> Failed;
        public static ConcurrentDictionary<string, Vector3I> Mines;
        private static Random _rand;
        private static bool _stopped;
        private static MineField instance;

        private MineField () {
            // Empty, singleton
        }

        public static MineField GetInstance () {
            if ( instance == null ) {
                instance = new MineField();
                Failed = new List<Player>();
                Mines = new ConcurrentDictionary<string, Vector3I>();
                Player.Moving += new EventHandler<PlayerMovingEventArgs>( PlayerMoving );
                Player.PlacingBlock += new EventHandler<PlayerPlacingBlockEventArgs>( PlayerPlacing );
                _rand = new Random();
                _stopped = false;
            }
            return instance;
        }
        public static void Start ( Player player ) {
            Map map = MapGenerator.GenerateEmpty( 64, 128, 16 );
            map.Save( "maps/minefield.fcm" );
            if ( _world != null ) {
                WorldManager.RemoveWorld( _world );
            }
            WorldManager.AddWorld( Player.Console, "Minefield", map, true );
            _map = map;
            _world = WorldManager.FindWorldExact( "Minefield" );
            SetUpRed();
            SetUpMiddleWater();
            SetUpGreen();
            SetUpMines();
            _map.Spawn = new Position( _map.Width / 2, 5, _ground + 3 ).ToVector3I().ToPlayerCoords();
            _world.LoadMap();
            _world.gameMode = GameMode.MineField;
            _world.EnableTNTPhysics( Player.Console, false );
            Server.Message( "{0}&S started a game of MineField on world Minefield!", player.ClassyName );
            WorldManager.SaveWorldList();
            Server.RequestGC();
        }

        public static void Stop ( Player player, bool Won ) {
            if ( Failed != null && Mines != null ) {
                Failed.Clear();

                foreach ( Vector3I m in Mines.Values ) {
                    Vector3I removed;
                    Mines.TryRemove( m.ToString(), out removed );
                }
            }
            World world = WorldManager.FindWorldOrPrintMatches( player, "Minefield" );
            WorldManager.RemoveWorld( world );
            WorldManager.SaveWorldList();
            Server.RequestGC();
            instance = null;
            if ( Won ) {
                Server.Players.Message( "{0}&S Won the game of MineField!", player.ClassyName );
            } else {
                Server.Players.Message( "{0}&S aborted the game of MineField", player.ClassyName );
            }
        }

        private static void SetUpRed () {
            for ( int x = 0; x <= _map.Width; x++ ) {
                for ( int y = 0; y <= 10; y++ ) {
                    _map.SetBlock( x, y, _ground, Block.Red );
                    _map.SetBlock( x, y, _ground - 1, Block.Black );
                }
            }
        }

        private static void SetUpMiddleWater () {
            for ( int x = _map.Width; x >= 0; x-- ) {
                for ( int y = _map.Length - 50; y >= _map.Length - 56; y-- ) {
                    _map.SetBlock( x, y, _ground, Block.Water );
                    _map.SetBlock( x, y, _ground - 1, Block.Water );
                }
            }
        }

        private static void SetUpGreen () {
            for ( int x = _map.Width; x >= 0; x-- ) {
                for ( int y = _map.Length; y >= _map.Length - 10; y-- ) {
                    _map.SetBlock( x, y, _ground, Block.Green );
                    _map.SetBlock( x, y, _ground - 1, Block.Black );
                }
            }
        }

        private static void SetUpMines () {
            for ( short i = 0; i <= _map.Width; ++i ) {
                for ( short j = 0; j <= _map.Length; ++j ) {
                    if ( _map.GetBlock( i, j, _ground ) != Block.Red &&
                        _map.GetBlock( i, j, _ground ) != Block.Green &&
                        _map.GetBlock( i, j, _ground ) != Block.Water ) {
                        _map.SetBlock( i, j, _ground, Block.Dirt );
                        _map.SetBlock( i, j, _ground - 1, Block.Dirt );
                        if ( _rand.Next( 1, 100 ) > 96 ) {
                            Vector3I vec = new Vector3I( i, j, _ground );
                            Mines.TryAdd( vec.ToString(), vec );
                            //_map.SetBlock(vec, Block.Red);//
                        }
                    }
                }
            }
        }

        public static bool PlayerBlowUpCheck ( Player player ) {
            if ( !Failed.Contains( player ) ) {
                Failed.Add( player );
                return true;
            }
            return false;
        }

        private static void PlayerPlacing ( object sender, PlayerPlacingBlockEventArgs e ) {
            World world = e.Player.World;
            if ( world.gameMode == GameMode.MineField ) {
                e.Result = CanPlaceResult.Revert;
            }
        }

        private static void PlayerMoving ( object sender, PlayerMovingEventArgs e ) {
            if ( _world != null && e.Player.World == _world ) {
                if ( _world.gameMode == GameMode.MineField && !Failed.Contains( e.Player ) ) {
                    if ( e.NewPosition != null ) {
                        Vector3I oldPos = new Vector3I( e.OldPosition.X / 32, e.OldPosition.Y / 32, e.OldPosition.Z / 32 );
                        Vector3I newPos = new Vector3I( e.NewPosition.X / 32, e.NewPosition.Y / 32, e.NewPosition.Z / 32 );

                        if ( oldPos.X != newPos.X || oldPos.Y != newPos.Y || oldPos.Z != newPos.Z ) {
                            if ( !_map.InBounds( newPos ) ) {
                                e.Player.TeleportTo( _map.Spawn );
                                newPos = ( Vector3I )_map.Spawn;
                            }
                            // Check if the player jumped, flew, whatevers
                            if ( newPos.Z > _ground + 2 ) {
                                e.Player.TeleportTo( e.OldPosition );
                                newPos = oldPos;
                            }
                            foreach ( Vector3I pos in Mines.Values ) {
                                if ( newPos == new Vector3I( pos.X, pos.Y, pos.Z + 2 ) ||
                                    newPos == new Vector3I( pos.X, pos.Y, pos.Z + 1 ) ||
                                    newPos == new Vector3I( pos.X, pos.Y, pos.Z ) ) {
                                    _world.Map.QueueUpdate( new BlockUpdate( null, pos, Block.TNT ) );
                                    _world.AddPhysicsTask( new TNTTask( _world, pos, null, true, false ), 0 );
                                    Vector3I removed;
                                    Mines.TryRemove( pos.ToString(), out removed );
                                }
                            }
                            if ( _map.GetBlock( newPos.X, newPos.Y, newPos.Z - 2 ) == Block.Green
                                && !_stopped ) {
                                _stopped = true;
                                Stop( e.Player, true );
                            }
                        }
                    }
                }
            }
        }
    }
}
