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

namespace fCraft {
    class MessageBlockHandler {

        private static MessageBlockHandler instance;

        private MessageBlockHandler () {
            // Empty, singleton
        }

        public static MessageBlockHandler GetInstance () {
            if ( instance == null ) {
                instance = new MessageBlockHandler();
                Player.Moved += new EventHandler<Events.PlayerMovedEventArgs>( Player_Moved );
                Player.PlacingBlock += new EventHandler<Events.PlayerPlacingBlockEventArgs>( Player_PlacingBlock );
            }
            return instance;
        }

        static void Player_PlacingBlock ( object sender, Events.PlayerPlacingBlockEventArgs e ) {
            Map map = e.Map;
            if ( e.Map.MessageBlocks != null ) {
                if ( e.Map.MessageBlocks.Count > 0 ) {
                    lock ( e.Map.MessageBlocks ) {
                        foreach ( MessageBlock mb in e.Map.MessageBlocks ) {
                            if ( e.Coords == mb.AffectedBlock ) {
                                e.Result = CanPlaceResult.Revert;
                                if ( e.Context == BlockChangeContext.Manual ) {
                                    if ( mb.IsInRange( e.Coords ) ) {
                                        string M = mb.GetMessage();
                                        if ( M == "" ) return;
                                        if ( e.Player.LastUsedMessageBlock == null ) {
                                            e.Player.LastUsedMessageBlock = DateTime.UtcNow;
                                            e.Player.Message( M );
                                            return;
                                        }
                                        if ( ( DateTime.UtcNow - e.Player.LastUsedMessageBlock ).TotalSeconds > 4 ) {
                                            e.Player.Message( M );
                                            e.Player.LastUsedMessageBlock = DateTime.UtcNow;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        static void Player_Moved ( object sender, Events.PlayerMovedEventArgs e ) {
            try {
                if ( ( e.OldPosition.X != e.NewPosition.X ) || ( e.OldPosition.Y != e.NewPosition.Y ) || ( e.OldPosition.Z != ( e.NewPosition.Z ) ) ) {
                    lock ( e.Player.MessageBlockLock ) {
                        if ( e.Player.WorldMap == null ) return;
                        if ( e.Player.WorldMap.MessageBlocks != null ) {
                            lock ( e.Player.WorldMap.MessageBlocks ) {
                                foreach ( MessageBlock mb in e.Player.WorldMap.MessageBlocks ) {
                                    if ( e.Player.WorldMap == null ) return;
                                    if ( mb.IsInRange( e.Player ) ) {
                                        string M = mb.GetMessage();
                                        if ( M == "" ) return;
                                        if ( e.Player.LastUsedMessageBlock == null ) {
                                            e.Player.LastUsedMessageBlock = DateTime.UtcNow;
                                            e.Player.Message( M );
                                            return;
                                        }
                                        if ( ( DateTime.UtcNow - e.Player.LastUsedMessageBlock ).TotalSeconds > 4 ) {
                                            e.Player.Message( M );
                                            e.Player.LastUsedMessageBlock = DateTime.UtcNow;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            } catch ( Exception ex ) { Logger.Log( LogType.Error, "MessageBlock_Moving: " + ex ); };
        }

        public MessageBlock GetMessageBlock ( World world, Vector3I block ) {
            MessageBlock MessageBlock = null;
            try {
                if ( world.Map.MessageBlocks != null && world.Map.MessageBlocks.Count > 0 ) {
                    lock ( world.Map.MessageBlocks.SyncRoot ) {
                        foreach ( MessageBlock possibleMessageBlock in world.Map.MessageBlocks ) {
                            if ( possibleMessageBlock.IsInRange( block ) ) {
                                return possibleMessageBlock;
                            }
                        }
                    }
                }
            } catch ( Exception ex ) {
                Logger.Log( LogType.Error, "MessageBlockHandler.GetMessageBlock: " + ex );
            }

            return MessageBlock;
        }

        public static void CreateMessageBlock ( MessageBlock MessageBlock, World source ) {
            World world = WorldManager.FindWorldExact( MessageBlock.World );

            if ( source.Map.MessageBlocks == null ) {
                source.Map.MessageBlocks = new ArrayList();
            }

            lock ( source.Map.MessageBlocks.SyncRoot ) {
                source.Map.MessageBlocks.Add( MessageBlock );
            }
        }

        public static bool IsInRangeOfSpawnpoint ( World world, Vector3I block ) {
            try {
                int Xdistance = ( world.Map.Spawn.X / 32 ) - block.X;
                int Ydistance = ( world.Map.Spawn.Y / 32 ) - block.Y;
                int Zdistance = ( world.Map.Spawn.Z / 32 ) - block.Z;

                if ( Xdistance <= 10 && Xdistance >= -10 ) {
                    if ( Ydistance <= 10 && Ydistance >= -10 ) {
                        if ( Zdistance <= 10 && Zdistance >= -10 ) {
                            return true;
                        }
                    }
                }
            } catch ( Exception ex ) {
                Logger.Log( LogType.Error, "MessageBlockHandler.IsInRangeOfSpawnpoint: " + ex );
            }

            return false;
        }
    }
}
