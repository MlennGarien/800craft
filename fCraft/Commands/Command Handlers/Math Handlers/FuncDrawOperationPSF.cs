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

//Copyright (C) <2011 - 2013> Lao Tszy (lao_tszy@yahoo.co.uk)

using System;

namespace fCraft {

    public class FuncDrawOperationPoints : FuncDrawOperation {

        public FuncDrawOperationPoints( Player player, Command cmd )
            : base( player, cmd ) {
        }

        protected override void DrawFasePrepare( int min1, int max1, int min2, int max2 ) {
        }

        protected override void DrawFase1( int fval, ref int arg1, ref int arg2, ref int val, int min1, int max1, int min2, int max2,
                                          int minV, int maxV, int maxBlocksToDraw ) {
            if ( fval <= maxV && fval >= minV ) {
                val = fval;
                if ( DrawOneBlock() )
                    ++Count;
            }
        }

        protected override void DrawFase2( ref int arg1, ref int arg2, ref int val, int min1, int max1, int min2, int max2,
                                          int minV, int maxV, int maxBlocksToDraw ) {
        }

        public override string Name {
            get {
                return base.Name + "Points";
            }
        }
    }

    public class FuncDrawOperationFill : FuncDrawOperation {

        public FuncDrawOperationFill( Player player, Command cmd )
            : base( player, cmd ) {
        }

        protected override void DrawFasePrepare( int min1, int max1, int min2, int max2 ) {
        }

        protected override void DrawFase1( int fval, ref int arg1, ref int arg2, ref int val, int min1, int max1, int min2, int max2,
                                          int minV, int maxV, int maxBlocksToDraw ) {
            for ( val = minV; val <= fval && val <= maxV; ++val ) {
                if ( DrawOneBlock() ) {
                    ++Count;
                    //if (TimeToEndBatch)
                    //    return;
                }
            }
        }

        protected override void DrawFase2( ref int arg1, ref int arg2, ref int val, int min1, int max1, int min2, int max2,
                                          int minV, int maxV, int maxBlocksToDraw ) {
        }

        public override string Name {
            get {
                return base.Name + "Fill";
            }
        }
    }

    public class FuncDrawOperationSurface : FuncDrawOperation {
        private int[][] _surface;

        public FuncDrawOperationSurface( Player player, Command cmd )
            : base( player, cmd ) {
        }

        protected override void DrawFasePrepare( int min1, int max1, int min2, int max2 ) {
            _surface = new int[max1 - min1 + 1][];
            for ( int i = 0; i < _surface.Length; ++i ) {
                _surface[i] = new int[max2 - min2 + 1];
                for ( int j = 1; j < _surface[i].Length; ++j )
                    _surface[i][j] = int.MaxValue;
            }
        }

        protected override void DrawFase1( int fval, ref int arg1, ref int arg2, ref int val, int min1, int max1, int min2, int max2,
                                          int minV, int maxV, int maxBlocksToDraw ) {
            _surface[arg1 - min1][arg2 - min2] = fval;
        }

        protected override void DrawFase2( ref int arg1, ref int arg2, ref int val, int min1, int max1, int min2, int max2,
                                          int minV, int maxV, int maxBlocksToDraw ) {
            for ( arg1 = min1; arg1 <= max1; ++arg1 ) {
                for ( arg2 = min2; arg2 <= max2; ++arg2 ) {
                    int a1 = arg1 - min1, a2 = arg2 - min2;
                    if ( _surface[a1][a2] == int.MaxValue )
                        continue;
                    //find min value around
                    int minVal = _surface[a1][a2];
                    if ( a1 - 1 >= 0 )
                        minVal = Math.Min( minVal, _surface[a1 - 1][a2] + 1 );
                    if ( a1 + 1 < _surface.Length )
                        minVal = Math.Min( minVal, _surface[a1 + 1][a2] + 1 );
                    if ( a2 - 1 >= 0 )
                        minVal = Math.Min( minVal, _surface[a1][a2 - 1] + 1 );
                    if ( a2 + 1 < _surface[a1].Length )
                        minVal = Math.Min( minVal, _surface[a1][a2 + 1] + 1 );
                    minVal = Math.Max( minVal, minV );

                    for ( val = minVal; val <= _surface[a1][a2] && val <= maxV; ++val )
                        if ( DrawOneBlock() ) {
                            //if (TimeToEndBatch)
                            //    return;
                        }
                }
            }
        }

        public override string Name {
            get {
                return base.Name + "Surface";
            }
        }
    }
}