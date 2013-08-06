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
using System.Collections;
using System.Collections.Generic;

namespace fCraft.Doors {

    internal class DoorHandler {
        private static DoorHandler instance;

        private DoorHandler() {
            // Empty, singleton
        }

        public static DoorHandler GetInstance() {
            if ( instance == null ) {
                instance = new DoorHandler();
                Player.Clicked += new EventHandler<Events.PlayerClickedEventArgs>( PlayerClickedDoor );
                Player.PlacingBlock += new EventHandler<Events.PlayerPlacingBlockEventArgs>( PlayerPlacedDoor );
            }

            return instance;
        }

        //cuboid over-fix?
        public static void PlayerPlacedDoor( object sender, Events.PlayerPlacingBlockEventArgs e ) {
            if ( e.Map.Doors == null )
                return;
            if ( e.Map.World != null ) {
                if ( e.Map != null ) {
                    if ( e.Map.Doors.Count > 0 ) {
                        lock ( e.Map.Doors.SyncRoot ) {
                            foreach ( Door door in e.Map.Doors ) {
                                if ( e.Map == null )
                                    break;
                                if ( door.IsInRange( e.Coords ) ) {
                                    e.Result = CanPlaceResult.Revert;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void PlayerClickedDoor( object sender, Events.PlayerClickedEventArgs e ) {
            if ( e.Action == ClickAction.Delete ) {
                if ( e.Player.World.Map.Doors == null )
                    return;
                if ( e.Player.IsMakingSelection )
                    return;
                lock ( openDoorsLock ) {
                    foreach ( Door door in e.Player.World.Map.Doors ) {
                        if ( door.IsInRange( e.Coords ) ) {
                            if ( !openDoors.Contains( door ) ) {
                                openDoor( door, e.Player );
                                openDoors.Add( door );
                            }
                        }
                    }
                }
            }
        }

        public static Door GetDoor( Vector3I Coords, Player player ) {
            Door Door = null;
            try {
                if ( player.World.Map.Doors != null && player.World.Map.Doors.Count > 0 ) {
                    lock ( player.World.Map.Doors.SyncRoot ) {
                        foreach ( Door possibleDoor in player.World.Map.Doors ) {
                            if ( possibleDoor.IsInRange( Coords ) ) {
                                return possibleDoor;
                            }
                        }
                    }
                }
            } catch ( Exception ex ) {
                Logger.Log( LogType.Error, "DoorHandler.GetDoor: " + ex );
            }

            return Door;
        }

        public Vector3I[] GetAffectedBlocks( Door door ) {
            Vector3I[] temp = new Vector3I[] { };
            List<Vector3I> temp2 = new List<Vector3I>();
            for ( int x = door.Range.Xmin; x < door.Range.Xmax; x++ )
                for ( int y = door.Range.Ymin; y < door.Range.Ymax; y++ )
                    for ( int z = door.Range.Zmin; z < door.Range.Zmax; z++ ) {
                        temp2.Add( new Vector3I( x, y, z ) );
                    }
            temp = temp2.ToArray();
            return temp;
        }

        public static int GetPlayerOwnedDoorsNumber( World world, Player player ) {
            int Number = 0;
            foreach ( Door door in world.Map.Doors ) {
                if ( door.Creator == player.Name ) {
                    Number++;
                }
            }
            return Number;
        }

        public Door GetDoor( World world, Vector3I block ) {
            Door Door = null;
            try {
                if ( world.Map.Doors != null && world.Map.Doors.Count > 0 ) {
                    lock ( world.Map.Doors.SyncRoot ) {
                        foreach ( Door possibleDoor in world.Map.Doors ) {
                            if ( possibleDoor.IsInRange( block ) ) {
                                return possibleDoor;
                            }
                        }
                    }
                }
            } catch ( Exception ex ) {
                Logger.Log( LogType.Error, "DoorHandler.GetDoor: " + ex );
            }

            return Door;
        }

        public static void CreateDoor( Door Door, World source ) {
            World world = WorldManager.FindWorldExact( Door.World );

            if ( source.Map.Doors == null ) {
                source.Map.Doors = new ArrayList();
            }
            lock ( source.Map.Doors.SyncRoot ) {
                source.Map.Doors.Add( Door );
            }
        }

        private static List<Door> openDoors = new List<Door>();
        private static readonly object openDoorsLock = new object();
        private static readonly TimeSpan DoorCloseTimer = TimeSpan.FromMilliseconds( 1500 );

        private struct DoorInfo {
            public readonly Door Door;
            public readonly Block[] Buffer;
            public readonly Map WorldMap;

            public DoorInfo( Door door, Block[] buffer, Map worldMap ) {
                Door = door;
                Buffer = buffer;
                WorldMap = worldMap;
            }
        }

        private static void doorTimer_Elapsed( SchedulerTask task ) {
            DoorInfo info = ( DoorInfo )task.UserState;
            int counter = 0;
            for ( int x = info.Door.Range.Xmin; x <= info.Door.Range.Xmax; x++ ) {
                for ( int y = info.Door.Range.Ymin; y <= info.Door.Range.Ymax; y++ ) {
                    for ( int z = info.Door.Range.Zmin; z <= info.Door.Range.Zmax; z++ ) {
                        info.WorldMap.QueueUpdate( new BlockUpdate( null, new Vector3I( x, y, z ), info.Buffer[counter] ) );
                        counter++;
                    }
                }
            }

            lock ( openDoorsLock ) { openDoors.Remove( info.Door ); }
        }

        private static void openDoor( Door door, Player player ) {
            int sx = door.Range.Xmin;
            int ex = door.Range.Xmax;
            int sy = door.Range.Ymin;
            int ey = door.Range.Ymax;
            int sz = door.Range.Zmin;
            int ez = door.Range.Zmax;

            Block[] buffer = new Block[( ex - sx + 1 ) * ( ey - sy + 1 ) * ( ez - sz + 1 )];

            int counter = 0;
            for ( int x = sx; x <= ex; x++ ) {
                for ( int y = sy; y <= ey; y++ ) {
                    for ( int z = sz; z <= ez; z++ ) {
                        buffer[counter] = player.WorldMap.GetBlock( x, y, z );
                        player.WorldMap.QueueUpdate( new BlockUpdate( null, new Vector3I( x, y, z ), Block.Air ) );
                        counter++;
                    }
                }
            }

            DoorInfo info = new DoorInfo( door, buffer, player.WorldMap );
            //reclose door
            Scheduler.NewTask( doorTimer_Elapsed ).RunOnce( info, DoorCloseTimer );
        }
    }
}