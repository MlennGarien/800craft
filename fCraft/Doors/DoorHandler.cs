//Copyright (C) <2012>  <Jon Baker, Glenn Mariën and Lao Tszy>

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.

//Copyright (C) <2012> Glenn Mariën (http://project-vanilla.com)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace fCraft.Doors {
    class DoorHandler {

        private static DoorHandler instance;

        private DoorHandler () {
            // Empty, singleton
        }

        public static DoorHandler GetInstance () {
            if ( instance == null ) {
                instance = new DoorHandler();
                Player.Clicked += new EventHandler<Events.PlayerClickedEventArgs>( PlayerClickedDoor );
            }

            return instance;
        }

        public static void PlayerClickedDoor ( object sender, Events.PlayerClickedEventArgs e ) {
            if ( e.Player.World.Map.Doors == null ) return;
            if ( e.Player.IsMakingSelection ) return;
            Player.RaisePlayerPlacedBlockEvent( e.Player, e.Player.WorldMap, e.Coords, e.Block, e.Block, BlockChangeContext.Manual );
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


        public static Door GetDoor ( Vector3I Coords, Player player ) {
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

        public Door GetDoor ( World world, Vector3I block ) {
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

        public static void CreateDoor ( Door Door, World source ) {
            World world = WorldManager.FindWorldExact( Door.World );

            if ( source.Map.Doors == null ) {
                source.Map.Doors = new ArrayList();
            }
            lock ( source.Map.Doors.SyncRoot ) {
                source.Map.Doors.Add( Door );
            }
        }
        static List<Door> openDoors = new List<Door>();
        static readonly object openDoorsLock = new object();
        static readonly TimeSpan DoorCloseTimer = TimeSpan.FromMilliseconds( 1500 );

        struct DoorInfo {
            public readonly Door Door;
            public readonly Block[] Buffer;
            public readonly Map WorldMap;
            public DoorInfo ( Door door, Block[] buffer, Map worldMap ) {
                Door = door;
                Buffer = buffer;
                WorldMap = worldMap;
            }
        }

        static void doorTimer_Elapsed ( SchedulerTask task ) {
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

        static void openDoor ( Door door, Player player ) {
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
