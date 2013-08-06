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
using fCraft.Events;

namespace fCraft {

    public class Football {
        private World _world;
        private Vector3I _startPos;

        public Football( Player player, World world, Vector3I FootballPos ) {
            _world = world;
            Player.Clicked += ClickedFootball;
        }

        public void ResetFootball() {
            if ( _startPos == null ) {
                _startPos.X = _world.Map.Bounds.XMax - _world.Map.Bounds.XMin;
                _startPos.Y = _world.Map.Bounds.YMax - _world.Map.Bounds.YMin;
                for ( int z = _world.Map.Bounds.ZMax; z > 0; z-- ) {
                    if ( _world.Map.GetBlock( _startPos.X, _startPos.Y, z ) != Block.Air ) {
                        _startPos.Z = z + 1;
                        break;
                    }
                }
            }
            _world.Map.QueueUpdate( new BlockUpdate( null, _startPos, Block.White ) );
        }

        private FootballBehavior _footballBehavior = new FootballBehavior();

        public void ClickedFootball( object sender, PlayerClickedEventArgs e ) {
            //replace e.coords with player.Pos.toblock() (moving event)
            if ( e.Coords == _world.footballPos ) {
                double ksi = 2.0 * Math.PI * ( -e.Player.Position.L ) / 256.0;
                double r = Math.Cos( ksi );
                double phi = 2.0 * Math.PI * ( e.Player.Position.R - 64 ) / 256.0;
                Vector3F dir = new Vector3F( ( float )( r * Math.Cos( phi ) ), ( float )( r * Math.Sin( phi ) ), ( float )( Math.Sin( ksi ) ) );
                _world.AddPhysicsTask( new Particle( _world, e.Coords, dir, e.Player, Block.White, _footballBehavior ), 0 );
            }
        }
    }
}