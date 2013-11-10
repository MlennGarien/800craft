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

//Copyright (C) <2011 - 2013> Jon Baker (http://au70.net)

using System;
using System.Collections.Concurrent;
using System.Threading;
using fCraft.Events;

namespace fCraft {

    public class GunGlassTimer {
        private readonly Timer _timer;
        private bool _started;
        private readonly Player _player;
        private const int Tick = 125;
        private readonly object _objectLock = new object();

        public GunGlassTimer( Player player ) {
            _player = player;
            _started = false;
            _timer = new Timer( Callback, null, Timeout.Infinite, Timeout.Infinite );
        }

        public void Start() {
            lock ( _objectLock ) {
                if ( !_started ) {
                    _started = true;
                    _timer.Change( 0, Timeout.Infinite );
                }
            }
        }

        public void Stop() {
            lock ( _objectLock ) {
                _started = false;
                _timer.Change( Timeout.Infinite, Timeout.Infinite );
            }
        }

        private void Callback( object state ) {
            try {
                if ( _player.IsOnline && _player != null ) {
                    if ( _player.GunMode ) {
                        GunClass.GunMove( _player );
                    } else {
                        Stop();
                    }
                } else {
                    Stop();
                }
            } catch ( Exception e ) {
                Logger.Log( LogType.Error, "GunGlassTimer: " + e );
            }

            lock ( _objectLock ) {
                if ( _started )
                    _timer.Change( Tick, Timeout.Infinite );
            }
        }
    }

    internal class GunClass {

        public static void Init() {
            Player.Clicking += ClickedGlass;//
            Player.JoinedWorld += ChangedWorld;//
            Player.Moving += MovePortal;//
            Player.Disconnected += PlayerDisconnected;//
            Player.PlacingBlock += PlayerPlaced;
            CommandManager.RegisterCommand( CdGun );
        }

        private static readonly CommandDescriptor CdGun = new CommandDescriptor {
            Name = "Gun",
            Category = CommandCategory.Moderation,
            IsConsoleSafe = false,
            Permissions = new[] { Permission.Gun },
            Usage = "/Gun",
            Help = "Fire At Will! TNT blocks explode TNT with physics on, Blue blocks make a Blue Portal, Orange blocks make an Orange Portal.",
            Handler = GunHandler
        };

        public static void GunHandler( Player player, Command cmd ) {
            if ( player.GunMode ) {
                player.GunMode = false;
                try {
                    foreach ( Vector3I block in player.GunCache.Values ) {
                        player.Send( PacketWriter.MakeSetBlock( block.X, block.Y, block.Z, player.WorldMap.GetBlock( block ) ) );
                        Vector3I removed;
                        player.GunCache.TryRemove( block.ToString(), out removed );
                    }
                    if ( player.bluePortal.Count > 0 ) {
                        int i = 0;
                        foreach ( Vector3I block in player.bluePortal ) {
                            if ( player.WorldMap != null && player.World.IsLoaded ) {
                                player.WorldMap.QueueUpdate( new BlockUpdate( null, block, player.blueOld[i] ) );
                                i++;
                            }
                        }
                        player.blueOld.Clear();
                        player.bluePortal.Clear();
                    }
                    if ( player.orangePortal.Count > 0 ) {
                        int i = 0;
                        foreach ( Vector3I block in player.orangePortal ) {
                            if ( player.WorldMap != null && player.World.IsLoaded ) {
                                player.WorldMap.QueueUpdate( new BlockUpdate( null, block, player.orangeOld[i] ) );
                                i++;
                            }
                        }
                        player.orangeOld.Clear();
                        player.orangePortal.Clear();
                    }
                    player.Message( "&SGunMode deactivated" );
                } catch ( Exception ex ) {
                    Logger.Log( LogType.SeriousError, "" + ex );
                }
            } else {
                if ( player.World != null && !player.World.gunPhysics ) {
                    player.Message( "&WGun physics are disabled on this world" );
                    return;
                }
                player.GunMode = true;
                var timer = new GunGlassTimer( player );
                timer.Start();
                player.Message( "&SGunMode activated. Fire at will!" );
            }
        }

        public static void GunMove( Player player ) {
            World world = player.World;
            if ( null == world )
                return;
            try {
                lock ( world.SyncRoot ) {
                    if ( null == world.Map )
                        return;
                    if ( player.IsOnline ) {
                        Position p = player.Position;
                        double ksi = 2.0 * Math.PI * ( -player.Position.L ) / 256.0;
                        double phi = 2.0 * Math.PI * ( player.Position.R - 64 ) / 256.0;
                        double sphi = Math.Sin( phi );
                        double cphi = Math.Cos( phi );
                        double sksi = Math.Sin( ksi );
                        double cksi = Math.Cos( ksi );

                        if ( player.IsOnline ) {
                            if ( player.GunCache.Values.Count > 0 ) {
                                foreach ( Vector3I block in player.GunCache.Values ) {
                                    if ( player.IsOnline ) {
                                        player.Send( PacketWriter.MakeSetBlock( block.X, block.Y, block.Z, world.Map.GetBlock( block ) ) );
                                        Vector3I removed;
                                        player.GunCache.TryRemove( block.ToString(), out removed );
                                    }
                                }
                            }
                        }

                        for ( int y = -1; y < 2; ++y ) {
                            for ( int z = -1; z < 2; ++z ) {
                                if ( player.IsOnline ) {
                                    //4 is the distance betwen the player and the glass wall
                                    var glassBlockPos = new Vector3I( ( int )( cphi * cksi * 4 - sphi * ( 0.5 + y ) - cphi * sksi * ( 0.5 + z ) ),
                                          ( int )( sphi * cksi * 4 + cphi * ( 0.5 + y ) - sphi * sksi * ( 0.5 + z ) ),
                                          ( int )( sksi * 4 + cksi * ( 0.5 + z ) ) );
                                    glassBlockPos += p.ToBlockCoords();
                                    if ( world.Map.GetBlock( glassBlockPos ) == Block.Air ) {
                                        player.Send( PacketWriter.MakeSetBlock( glassBlockPos, Block.Glass ) );
                                        player.GunCache.TryAdd( glassBlockPos.ToString(), glassBlockPos );
                                    }
                                }
                            }
                        }
                    }
                }
            } catch ( Exception ex ) {
                Logger.Log( LogType.SeriousError, "GunGlass: " + ex );
            }
        }

        public static void PlayerPlaced( object sender, PlayerPlacingBlockEventArgs e ) {
            try {
                if (e.Player.World != null)
                    foreach ( Player p in e.Player.World.Players ) {
                        if ( e.OldBlock == Block.Water || e.OldBlock == Block.Lava ) {
                            if ( p.orangePortal.Contains( e.Coords ) || p.bluePortal.Contains( e.Coords ) ) {
                                e.Result = CanPlaceResult.Revert;
                            }
                        }
                    }
            } catch ( Exception ex ) {
                Logger.Log( LogType.SeriousError, "PlacingInPortal: " + ex );
            }
        }

        private static readonly TntBulletBehavior _tntBulletBehavior = new TntBulletBehavior();
        private static readonly BulletBehavior _bulletBehavior = new BulletBehavior();

        public static void ClickedGlass( object sender, PlayerClickingEventArgs e ) {
            if ( e.Player.GunMode && !e.Player.Info.IsHidden && !e.Player.Info.IsFrozen ) {
                World world = e.Player.World;
                if ( e.Player.GunCache.Values.Contains( e.Coords ) ) {
                    if ( world != null && world.gunPhysics ) {
                        e.Player.Send( PacketWriter.MakeSetBlock( e.Coords.X, e.Coords.Y, e.Coords.Z, Block.Glass ) );
                        if ( e.Block == Block.TNT && world.tntPhysics ) {
                            if ( e.Player.CanFireTNT() ) {
                                double ksi = 2.0 * Math.PI * ( -e.Player.Position.L ) / 256.0;
                                double r = Math.Cos( ksi );
                                double phi = 2.0 * Math.PI * ( e.Player.Position.R - 64 ) / 256.0;
                                var dir = new Vector3F( ( float )( r * Math.Cos( phi ) ), ( float )( r * Math.Sin( phi ) ), ( float )( Math.Sin( ksi ) ) );
                                world.AddPhysicsTask( new Particle( world, e.Coords, dir, e.Player, Block.TNT, _tntBulletBehavior ), 0 );
                            }
                        } else {
                            Block block = e.Block;
                            if ( block == Block.Blue )
                                block = Block.Water;
                            if ( block == Block.Orange )
                                block = Block.Lava;
                            double ksi = 2.0 * Math.PI * ( -e.Player.Position.L ) / 256.0;
                            double r = Math.Cos( ksi );
                            double phi = 2.0 * Math.PI * ( e.Player.Position.R - 64 ) / 256.0;
                            var dir = new Vector3F( ( float )( r * Math.Cos( phi ) ), ( float )( r * Math.Sin( phi ) ), ( float )( Math.Sin( ksi ) ) );
                            world.AddPhysicsTask( new Particle( world, e.Coords, dir, e.Player, block, _bulletBehavior ), 0 );
                        }
                    }
                }
            }
        }

        public static void MovePortal( object sender, PlayerMovingEventArgs e ) {
            try {
                if ( e.Player.LastUsedPortal != null && ( DateTime.UtcNow - e.Player.LastUsedPortal ).TotalSeconds < 4 ) {
                    return;
                }
                var newPos = new Vector3I( e.NewPosition.X / 32, e.NewPosition.Y / 32, ( e.NewPosition.Z / 32 ) );
                if (e.Player.World != null)
                    foreach ( Player p in e.Player.World.Players ) {
                        foreach ( Vector3I block in p.bluePortal ) {
                            if ( newPos == block ) {
                                if ( p.World != null && p.World.Map.GetBlock( block ) == Block.Water ) {
                                    if ( p.orangePortal.Count > 0 ) {
                                        e.Player.TeleportTo( new Position {
                                            X = ( short )( ( ( p.orangePortal[0].X ) + 0.5 ) * 32 ),
                                            Y = ( short )( ( ( p.orangePortal[0].Y ) + 0.5 ) * 32 ),
                                            Z = ( short )( ( ( p.orangePortal[0].Z ) + 1.59375 ) * 32 ),
                                            R = ( byte )( p.blueOut - 128 ),
                                            L = e.Player.Position.L
                                        } );
                                    }
                                    e.Player.LastUsedPortal = DateTime.UtcNow;
                                }
                            }
                        }

                        foreach ( Vector3I block in p.orangePortal ) {
                            if ( newPos == block ) {
                                if ( p.World != null && p.World.Map.GetBlock( block ) == Block.Lava ) {
                                    if ( p.bluePortal.Count > 0 ) {
                                        e.Player.TeleportTo( new Position {
                                            X = ( short )( ( ( p.bluePortal[0].X + 0.5 ) ) * 32 ),
                                            Y = ( short )( ( ( p.bluePortal[0].Y + 0.5 ) ) * 32 ),
                                            Z = ( short )( ( ( p.bluePortal[0].Z ) + 1.59375 ) * 32 ), //fixed point 1.59375 lol.
                                            R = ( byte )( p.orangeOut - 128 ),
                                            L = e.Player.Position.L
                                        } );
                                    }
                                    e.Player.LastUsedPortal = DateTime.UtcNow;
                                }
                            }
                        }
                    }
            } catch ( Exception ex ) {
                Logger.Log( LogType.SeriousError, "MovePortal: " + ex );
            }
        }

        public static void ChangedWorld( object sender, PlayerJoinedWorldEventArgs e ) {
            try {
                if ( e.OldWorld != null ) {
                    if ( e.OldWorld.Name == e.NewWorld.Name ) {
                        e.Player.orangeOld.Clear();
                        e.Player.orangePortal.Clear();
                        e.Player.blueOld.Clear();
                        e.Player.bluePortal.Clear();
                    }
                    if ( e.OldWorld.IsLoaded ) {
                        Map map = e.OldWorld.Map;
                        if ( e.Player.orangePortal.Count > 0 ) {
                            int i = 0;
                            foreach ( Vector3I block in e.Player.orangePortal )
                            {
                                if (map != null) map.QueueUpdate( new BlockUpdate( null, block, e.Player.orangeOld[i] ) );
                                i++;
                            }
                            e.Player.orangeOld.Clear();
                            e.Player.orangePortal.Clear();
                        }

                        if ( e.Player.bluePortal.Count > 0 ) {
                            int i = 0;
                            foreach ( var block in e.Player.bluePortal )
                            {
                                if (map != null) map.QueueUpdate( new BlockUpdate( null, block, e.Player.blueOld[i] ) );
                                i++;
                            }
                            e.Player.blueOld.Clear();
                            e.Player.bluePortal.Clear();
                        }
                    } else {
                        if ( e.Player.bluePortal.Count > 0 ) {
                            if (e.OldWorld.Map != null)
                            {
                                e.OldWorld.Map.Blocks[e.OldWorld.Map.Index( e.Player.bluePortal[0] )] = ( byte )e.Player.blueOld[0];
                                e.OldWorld.Map.Blocks[e.OldWorld.Map.Index( e.Player.bluePortal[1] )] = ( byte )e.Player.blueOld[1];
                            }
                            e.Player.blueOld.Clear();
                            e.Player.bluePortal.Clear();
                        }
                        if (e.Player.orangePortal.Count <= 0) return;
                        if (e.OldWorld.Map != null)
                        {
                            e.OldWorld.Map.Blocks[e.OldWorld.Map.Index( e.Player.orangePortal[0] )] = ( byte )e.Player.orangeOld[0];
                            e.OldWorld.Map.Blocks[e.OldWorld.Map.Index( e.Player.orangePortal[1] )] = ( byte )e.Player.orangeOld[1];
                        }
                        e.Player.orangeOld.Clear();
                        e.Player.orangePortal.Clear();
                    }
                }
            } catch ( Exception ex ) {
                Logger.Log( LogType.SeriousError, "GunPortalChangeWorld: " + ex );
            }
        }

        public static void PlayerDisconnected( object sender, PlayerDisconnectedEventArgs e ) {
            try
            {
                if (e.Player.World == null) return;
                if ( e.Player.World.IsLoaded ) {
                    var map = e.Player.World.Map;
                    if ( e.Player.orangePortal.Count > 0 ) {
                        int i = 0;
                        foreach ( Vector3I block in e.Player.orangePortal )
                        {
                            if (map != null) map.QueueUpdate( new BlockUpdate( null, block, e.Player.orangeOld[i] ) );
                            i++;
                        }
                        e.Player.orangeOld.Clear();
                        e.Player.orangePortal.Clear();
                    }

                    if ( e.Player.bluePortal.Count > 0 ) {
                        var i = 0;
                        foreach ( Vector3I block in e.Player.bluePortal )
                        {
                            if (map != null) map.QueueUpdate( new BlockUpdate( null, block, e.Player.blueOld[i] ) );
                            i++;
                        }
                        e.Player.blueOld.Clear();
                        e.Player.bluePortal.Clear();
                    }
                } else {
                    if ( e.Player.bluePortal.Count > 0 ) {
                        if (e.Player.World.Map != null)
                        {
                            e.Player.World.Map.Blocks[e.Player.World.Map.Index( e.Player.bluePortal[0] )] = ( byte )e.Player.blueOld[0];
                            e.Player.World.Map.Blocks[e.Player.World.Map.Index( e.Player.bluePortal[1] )] = ( byte )e.Player.blueOld[1];
                        }
                    }
                    if (e.Player.orangePortal.Count <= 0) return;
                    e.Player.WorldMap.Blocks[e.Player.WorldMap.Index( e.Player.orangePortal[0] )] = ( byte )e.Player.orangeOld[0];
                    e.Player.WorldMap.Blocks[e.Player.WorldMap.Index( e.Player.orangePortal[1] )] = ( byte )e.Player.orangeOld[1];
                }
            }
            catch ( Exception ex ) {
                Logger.Log( LogType.SeriousError, "GunPortalDisconnected: " + ex );
            }
        }

        public static void Removal( ConcurrentDictionary<String, Vector3I> bullets, Map map ) {
            foreach ( Vector3I bp in bullets.Values ) {
                map.QueueUpdate( new BlockUpdate( null,
                    ( short )bp.X,
                    ( short )bp.Y,
                    ( short )bp.Z,
                    Block.Air ) );
                Vector3I removed;
                bullets.TryRemove( bp.ToString(), out removed );
            }
        }

        public static bool CanPlacePortal( short x, short y, short z, Map map ) {
            var count = 0;
            var Z = z;
            for (; Z < z + 2; Z++ ) {
                var check = map.GetBlock( x, y, Z );
                if ( check != Block.Air && check != Block.Water && check != Block.Lava ) {
                    count++;
                }
            }
            return count == 2;
        }
    }
}