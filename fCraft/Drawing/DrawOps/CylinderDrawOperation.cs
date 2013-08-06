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

namespace fCraft.Drawing {

    public class CylinderDrawOperation : DrawOperation {

        public override string Name {
            get { return "Cylinder"; }
        }

        public CylinderDrawOperation( Player player )
            : base( player ) {
        }

        public override bool Prepare( Vector3I[] marks ) {
            if ( !base.Prepare( marks ) )
                return false;

            double r = Math.Sqrt( ( marks[0].X - marks[1].X ) * ( marks[0].X - marks[1].X ) +
                                 ( marks[0].Y - marks[1].Y ) * ( marks[0].Y - marks[1].Y ) +
                                 ( marks[0].Z - marks[1].Z ) * ( marks[0].Z - marks[1].Z ) );

            double rx = Bounds.Width / 2d;
            double ry = Math.Sqrt( r * r - rx * rx );
            double rz = Bounds.Height / 2d;

            radius.X = ( float )( 1 / ( rx * rx ) );
            radius.Y = ( float )( 1 / ( ry * ry ) );
            radius.Z = ( float )( 1 / ( rz * rz ) );

            center.X = ( Bounds.XMin + Bounds.XMax ) / 2f;
            center.Y = ( Bounds.YMin + Bounds.YMax ) / 2f;
            center.Z = ( Bounds.ZMin + Bounds.ZMax ) / 2f;

            fillInner = Brush.HasAlternateBlock &&
                        Bounds.Width > 2 &&
                        Bounds.Length > 2 &&
                        Bounds.Height > 2;

            Coords = Bounds.MinVertex;

            if ( fillInner ) {
                BlocksTotalEstimate = ( int )( 4 / 3d * Math.PI * rx * ry * rz );
            } else {
                BlocksTotalEstimate = ( int )( 4 / 3d * Math.PI * ( ( rx + .5 ) * ( ry + .5 ) * ( rz + .5 ) -
                                                                ( rx - .5 ) * ( ry - .5 ) * ( rz - .5 ) ) * 0.85 );
            }
            return true;
        }

        private State state;
        private Vector3F radius, center, delta;
        private bool fillInner;
        private int firstZ;

        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            for ( ; Coords.X <= Bounds.XMax; Coords.X++ ) {
                for ( ; Coords.Y <= Bounds.YMax; Coords.Y++ ) {
                    for ( ; Coords.Z <= Bounds.ZMax; Coords.Z++ ) {
                        switch ( state ) {
                            case State.BeforeBlock:
                                state = State.BeforeBlock;
                                delta.X = ( Coords.X - center.X );
                                delta.Y = ( Coords.Y - center.Y );
                                delta.Z = ( Coords.Z - center.Z );
                                if ( delta.X2 * radius.X + delta.Y2 * radius.Y + delta.Z2 * radius.Z > 1 )
                                    continue;
                                goto case State.OuterBlock1;

                            case State.OuterBlock1:
                                state = State.OuterBlock1;
                                firstZ = Coords.Z;
                                if ( DrawOneBlock() ) {
                                    blocksDone++;
                                }
                                goto case State.OuterBlock2;

                            case State.OuterBlock2:
                                state = State.OuterBlock2;
                                if ( blocksDone >= maxBlocksToDraw )
                                    return blocksDone;
                                int secondZ = ( int )( center.Z - delta.Z );
                                if ( secondZ != firstZ ) {
                                    int oldZ = Coords.Z;
                                    Coords.Z = secondZ;
                                    if ( DrawOneBlock() ) {
                                        blocksDone++;
                                    }
                                    Coords.Z = oldZ;
                                }
                                goto case State.AfterOuterBlock;

                            case State.AfterOuterBlock:
                                state = State.AfterOuterBlock;
                                if ( blocksDone >= maxBlocksToDraw || TimeToEndBatch )
                                    return blocksDone;
                                delta.Z = ( ++Coords.Z - center.Z );
                                if ( Coords.Z <= ( int )center.Z &&
                                    ( ( delta.X + 1 ) * ( delta.X + 1 ) * radius.X + delta.Y2 * radius.Y + delta.Z2 * radius.Z > 1 ||
                                    ( delta.X - 1 ) * ( delta.X - 1 ) * radius.X + delta.Y2 * radius.Y + delta.Z2 * radius.Z > 1 ||
                                    delta.X2 * radius.X + ( delta.Y + 1 ) * ( delta.Y + 1 ) * radius.Y + delta.Z2 * radius.Z > 1 ||
                                    delta.X2 * radius.X + ( delta.Y - 1 ) * ( delta.Y - 1 ) * radius.Y + delta.Z2 * radius.Z > 1 ) ) {
                                    goto case State.OuterBlock1;
                                }

                                if ( !fillInner ) {
                                    state = State.BeforeBlock;
                                    break;
                                }

                                UseAlternateBlock = true;
                                goto case State.InnerBlock;

                            case State.InnerBlock:
                                state = State.InnerBlock;
                                if ( Coords.Z > ( int )( center.Z - delta.Z ) ) {
                                    UseAlternateBlock = false;
                                    state = State.BeforeBlock;
                                    break;
                                }
                                if ( DrawOneBlock() ) {
                                    blocksDone++;
                                    Coords.Z++;
                                    if ( blocksDone >= maxBlocksToDraw || TimeToEndBatch ) {
                                        return blocksDone;
                                    }
                                } else {
                                    Coords.Z++;
                                }
                                goto case State.InnerBlock;
                        }
                        break;
                    }
                    Coords.Z = Bounds.ZMin;
                }
                Coords.Y = Bounds.YMin;
            }
            IsDone = true;
            return blocksDone;
        }

        private enum State {
            BeforeBlock = 0,
            OuterBlock1 = 1,
            OuterBlock2 = 2,
            AfterOuterBlock = 3,
            InnerBlock = 4
        }
    }
}