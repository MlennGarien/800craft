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

namespace fCraft.Drawing {

    public sealed class WallsDrawOperation : DrawOperation {
        private bool fillInner;

        public override string Name {
            get { return "Walls"; }
        }

        public override string Description {
            get { return Name; }
        }

        public WallsDrawOperation( Player player )
            : base( player ) {
        }

        public override bool Prepare( Vector3I[] marks ) {
            if ( !base.Prepare( marks ) )
                return false;

            fillInner = Brush.HasAlternateBlock && Bounds.Width > 2 && Bounds.Length > 2 && Bounds.Height > 2;

            BlocksTotalEstimate = Bounds.Volume;
            if ( !fillInner ) {
                BlocksTotalEstimate -= Math.Max( 0, Bounds.Width - 2 ) * Math.Max( 0, Bounds.Length - 2 ) * Math.Max( 0, Bounds.Height - 2 );
            }

            coordEnumerator = BlockEnumerator().GetEnumerator();
            return true;
        }

        private IEnumerator<Vector3I> coordEnumerator;

        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            while ( coordEnumerator.MoveNext() ) {
                Coords = coordEnumerator.Current;
                if ( DrawOneBlock() ) {
                    blocksDone++;
                    if ( blocksDone >= maxBlocksToDraw )
                        return blocksDone;
                }
            }
            IsDone = true;
            return blocksDone;
        }

        //all works. Maybe look at Block estimation.
        private IEnumerable<Vector3I> BlockEnumerator() {
            for ( int x = Bounds.XMin; x <= Bounds.XMax; x++ ) {
                for ( int z = Bounds.ZMin - 1; z < Bounds.ZMax; z++ ) {
                    yield return new Vector3I( x, Bounds.YMin, z + 1 );
                    if ( Bounds.YMin != Bounds.YMax ) {
                        yield return new Vector3I( x, Bounds.YMax, z + 1 );
                    }
                }
                for ( int y = Bounds.YMin; y < Bounds.YMax; y++ ) {
                    for ( int z = Bounds.ZMin - 1; z < Bounds.ZMax; z++ ) {
                        yield return new Vector3I( Bounds.XMin, y, z + 1 );
                        if ( Bounds.XMin != Bounds.XMax ) {
                            yield return new Vector3I( Bounds.XMax, y, z + 1 );
                        }
                    }
                }
            }

            if ( fillInner ) {
                UseAlternateBlock = true;
                for ( int x = Bounds.XMin + 1; x < Bounds.XMax; x++ ) {
                    for ( int y = Bounds.YMin + 1; y < Bounds.YMax; y++ ) {
                        for ( int z = Bounds.ZMin; z < Bounds.ZMax + 1; z++ ) {
                            yield return new Vector3I( x, y, z );
                        }
                    }
                }
            }
        }
    }
}