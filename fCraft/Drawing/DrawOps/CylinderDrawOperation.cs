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

//Copyright (C) <2012> Jon Baker (http://au70.net)
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
            if( !base.Prepare( marks ) ) return false;

            double r = Math.Sqrt((marks[0].X - marks[1].X) * (marks[0].X - marks[1].X) +
                                 (marks[0].Y - marks[1].Y) * (marks[0].Y - marks[1].Y) +
                                 (marks[0].Z - marks[1].Z) * (marks[0].Z - marks[1].Z));

            double rx = Bounds.Width / 2d;
            double ry = Math.Sqrt(r * r - rx * rx);
            double rz = Bounds.Height / 2d;

            radius.X = (float)(1 / (rx * rx));
            radius.Y = (float)(1 / (ry * ry));
            radius.Z = (float)(1 / (rz * rz));

            center.X = (Bounds.XMin + Bounds.XMax) / 2f;
            center.Y = (Bounds.YMin + Bounds.YMax) / 2f;
            center.Z = (Bounds.ZMin + Bounds.ZMax) / 2f;

            fillInner = Brush.HasAlternateBlock &&
                        Bounds.Width > 2 &&
                        Bounds.Length > 2 &&
                        Bounds.Height > 2;

            Coords = Bounds.MinVertex;

            if( fillInner ) {
                BlocksTotalEstimate = (int)(4 / 3d * Math.PI * rx * ry * rz);
            } else {
                BlocksTotalEstimate = (int)(4 / 3d * Math.PI * ((rx + .5) * (ry + .5) * (rz + .5) -
                                                                (rx - .5) * (ry - .5) * (rz - .5)) * 0.85);
            }
            return true;
        }

        State state;
        Vector3F radius, center, delta;
        bool fillInner;
        int firstZ;

        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            for( ; Coords.X <= Bounds.XMax; Coords.X++ ) {
                for( ; Coords.Y <= Bounds.YMax; Coords.Y++ ) {
                    for( ; Coords.Z <= Bounds.ZMax; Coords.Z++ ) {
                        switch( state ) {
                            case State.BeforeBlock:
                                state = State.BeforeBlock;
                                delta.X = (Coords.X - center.X);
                                delta.Y = (Coords.Y - center.Y);
                                delta.Z = (Coords.Z - center.Z);
                                if( delta.X2 * radius.X + delta.Y2 * radius.Y + delta.Z2 * radius.Z > 1 ) continue;
                                goto case State.OuterBlock1;


                            case State.OuterBlock1:
                                state = State.OuterBlock1;
                                firstZ = Coords.Z;
                                if( DrawOneBlock() ) {
                                    blocksDone++;
                                }
                                goto case State.OuterBlock2;


                            case State.OuterBlock2:
                                state = State.OuterBlock2;
                                if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                                int secondZ = (int)(center.Z - delta.Z);
                                if( secondZ != firstZ ) {
                                    int oldZ = Coords.Z;
                                    Coords.Z = secondZ;
                                    if( DrawOneBlock() ) {
                                        blocksDone++;
                                    }
                                    Coords.Z = oldZ;
                                }
                                goto case State.AfterOuterBlock;


                            case State.AfterOuterBlock:
                                state = State.AfterOuterBlock;
                                if( blocksDone >= maxBlocksToDraw || TimeToEndBatch ) return blocksDone;
                                delta.Z = (++Coords.Z - center.Z);
                                if( Coords.Z <= (int)center.Z &&
                                    ((delta.X + 1) * (delta.X + 1) * radius.X + delta.Y2 * radius.Y + delta.Z2 * radius.Z > 1 ||
                                    (delta.X - 1) * (delta.X - 1) * radius.X + delta.Y2 * radius.Y + delta.Z2 * radius.Z > 1 ||
                                    delta.X2 * radius.X + (delta.Y + 1) * (delta.Y + 1) * radius.Y + delta.Z2 * radius.Z > 1 ||
                                    delta.X2 * radius.X + (delta.Y - 1) * (delta.Y - 1) * radius.Y + delta.Z2 * radius.Z > 1) ) {
                                    goto case State.OuterBlock1;
                                }

                                if( !fillInner ) {
                                    state = State.BeforeBlock;
                                    break;
                                }

                                UseAlternateBlock = true;
                                goto case State.InnerBlock;


                            case State.InnerBlock:
                                state = State.InnerBlock;
                                if( Coords.Z > (int)(center.Z - delta.Z) ) {
                                    UseAlternateBlock = false;
                                    state = State.BeforeBlock;
                                    break;
                                }
                                if( DrawOneBlock() ) {
                                    blocksDone++;
                                    Coords.Z++;
                                    if( blocksDone >= maxBlocksToDraw || TimeToEndBatch ) {
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


        enum State {
            BeforeBlock = 0,
            OuterBlock1 = 1,
            OuterBlock2 = 2,
            AfterOuterBlock = 3,
            InnerBlock = 4
        }
    }
}