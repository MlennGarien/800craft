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

//Copyright (C) 2012 Lao Tszy (lao_tszy@yahoo.co.uk)

using fCraft;
using fCraft.Drawing;

namespace RandomMaze {

    internal class MazeCuboidDrawOperation : DrawOperation {
        private Maze _maze;
        private int _count = 0;

        public override string Name {
            get { return "MazeCuboid"; }
        }

        public MazeCuboidDrawOperation( Player player )
            : base( player ) {
        }

        public override bool Prepare( Vector3I[] marks ) {
            if ( !base.Prepare( marks ) )
                return false;
            if ( Bounds.Width < 3 || Bounds.Length < 3 ) {
                Player.Message( "Too small area marked (at least 3x3 blocks by X and Y)" );
                return false;
            }
            if ( Bounds.Width % 2 != 1 || Bounds.Length % 2 != 1 ) {
                Player.Message( "Warning: bounding box X and Y dimensions must be uneven, current bounding box will be cropped!" );
            }
            BlocksTotalEstimate = Bounds.Volume;

            _maze = new Maze( ( Bounds.Width - 1 ) / 2, ( Bounds.Length - 1 ) / 2, 1 );

            return true;
        }

        public override int DrawBatch( int maxBlocksToDraw ) {
            for ( int j = 0; j < _maze.YSize; ++j ) {
                for ( int i = 0; i < _maze.XSize; ++i ) {
                    DrawAtXY( i * 2, j * 2 );
                    if ( _maze.GetCell( i, j, 0 ).Wall( Direction.All[3] ) )
                        DrawAtXY( i * 2 + 1, j * 2 );
                    if ( _maze.GetCell( i, j, 0 ).Wall( Direction.All[2] ) )
                        DrawAtXY( i * 2, j * 2 + 1 );
                }
                DrawAtXY( _maze.XSize * 2, j * 2 );
                if ( _maze.GetCell( _maze.XSize - 1, j, 0 ).Wall( Direction.All[0] ) )
                    DrawAtXY( _maze.XSize * 2, j * 2 + 1 );
            }
            for ( int i = 0; i < _maze.XSize; ++i ) {
                DrawAtXY( i * 2, _maze.YSize * 2 );
                if ( _maze.GetCell( i, _maze.YSize - 1, 0 ).Wall( Direction.All[1] ) )
                    DrawAtXY( i * 2 + 1, _maze.YSize * 2 );
            }
            DrawAtXY( _maze.XSize * 2, _maze.YSize * 2 );

            IsDone = true;
            return _count;
        }

        private void DrawAtXY( int x, int y ) {
            Coords.X = x + Bounds.XMin;
            Coords.Y = y + Bounds.YMin;
            for ( Coords.Z = Bounds.ZMin; Coords.Z <= Bounds.ZMax; ++Coords.Z )
                if ( DrawOneBlock() )
                    ++_count;
        }
    }
}