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

//Copyright (C) <2011 - 2013> Glenn Mariën (http://project-vanilla.com) and Jon Baker (http://au70.net)

using System;
using System.Collections.Concurrent;

namespace fCraft.Utils {

    internal class FlyHandler {
        private static FlyHandler instance;

        private FlyHandler() {
            // Empty, singleton
        }

        public static FlyHandler GetInstance() {
            if ( instance == null ) {
                instance = new FlyHandler();
                Player.PlacingBlock += new EventHandler<Events.PlayerPlacingBlockEventArgs>( Player_Clicked );
            }

            return instance;
        }

        private static void Player_Clicked( object sender, Events.PlayerPlacingBlockEventArgs e ) //placing air
        {
            if ( e.Player.IsFlying ) {
                if ( e.Context == BlockChangeContext.Manual )//ignore all other things {
                    if ( e.Player.FlyCache.Values.Contains( e.Coords ) ) {
                        e.Result = CanPlaceResult.Revert; //nothing saves to blockcount or blockdb
                    }
            }
        }

        public void StartFlying( Player player ) {
            player.IsFlying = true;
            player.FlyCache = new ConcurrentDictionary<string, Vector3I>();
        }

        public void StopFlying( Player player ) {
            try {
                player.IsFlying = false;

                foreach ( Vector3I block in player.FlyCache.Values ) {
                    player.Send( PacketWriter.MakeSetBlock( block, Block.Air ) );
                }

                player.FlyCache = null;
            } catch ( Exception ex ) {
                Logger.Log( LogType.Error, "FlyHandler.StopFlying: " + ex );
            }
        }

        public static bool CanRemoveBlock( Player player, Vector3I block, Vector3I newPos ) {
            int x = block.X - newPos.X;
            int y = block.Y - newPos.Y;
            int z = block.Z - newPos.Z;

            if ( !( x >= -1 && x <= 1 ) || !( y >= -1 && y <= 1 ) || !( z >= -3 && z <= 4 ) ) {
                return true;
            }
            if ( !( x >= -1 && x <= 1 ) || !( y >= -1 && y <= 1 ) || !( z >= -3 && z <= 4 ) ) {
                return true;
            }
            return false;
        }
    }
}