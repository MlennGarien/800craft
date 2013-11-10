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

namespace fCraft {

    internal class MessageBlockHandler {
        private static MessageBlockHandler instance;

        private MessageBlockHandler() {
            // Empty, singleton
        }

        public static MessageBlockHandler GetInstance() {
            if ( instance == null ) {
                instance = new MessageBlockHandler();
                Player.Moved += new EventHandler<Events.PlayerMovedEventArgs>( Player_Moved );
                Player.PlacingBlock += new EventHandler<Events.PlayerPlacingBlockEventArgs>( Player_PlacingBlock );
            }
            return instance;
        }

        private static void Player_PlacingBlock( object sender, Events.PlayerPlacingBlockEventArgs e ) {
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
                                        if ( M == "" )
                                            return;
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

        private static void Player_Moved( object sender, Events.PlayerMovedEventArgs e ) {
            try {
                if ( ( e.OldPosition.X != e.NewPosition.X ) || ( e.OldPosition.Y != e.NewPosition.Y ) || ( e.OldPosition.Z != ( e.NewPosition.Z ) ) ) {
                    lock ( e.Player.MessageBlockLock ) {
                        if ( e.Player.WorldMap == null )
                            return;
                        if ( e.Player.WorldMap.MessageBlocks != null ) {
                            lock ( e.Player.WorldMap.MessageBlocks ) {
                                foreach ( MessageBlock mb in e.Player.WorldMap.MessageBlocks ) {
                                    if ( e.Player.WorldMap == null )
                                        return;
                                    if ( mb.IsInRange( e.Player ) ) {
                                        string M = mb.GetMessage();
                                        if ( M == "" )
                                            return;
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
            } catch ( Exception ex ) { Logger.Log( LogType.Error, "MessageBlock_Moving: " + ex ); }
        }

        public static MessageBlock GetMessageBlock( World world, Vector3I block ) {
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

        public static void CreateMessageBlock( MessageBlock MessageBlock, World source ) {
            World world = WorldManager.FindWorldExact( MessageBlock.World );

            if ( source.Map.MessageBlocks == null ) {
                source.Map.MessageBlocks = new ArrayList();
            }

            lock ( source.Map.MessageBlocks.SyncRoot ) {
                source.Map.MessageBlocks.Add( MessageBlock );
            }
        }

        public static bool IsInRangeOfSpawnpoint( World world, Vector3I block ) {
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