// Copyright 2009, 2010, 2011, 2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    public class DiabloDrawOperation : DrawOperation {
        Vector3F radius, center;

        public override string Name {
            get { return "Diablo"; }
        }

        public DiabloDrawOperation( Player player )
            : base( player ) {
        }
        //uninstall VS 2010
        public override bool Prepare( Vector3I[] marks ) {
            if( !base.Prepare( marks ) ) return false;
            float vx = Bounds.XMax - Bounds.XMin;
            float vy = Bounds.YMax - Bounds.YMin;
            float vz = Bounds.ZMax - Bounds.ZMin;

            double v = Math.Sqrt(vx * vx + vy * vy + vz * vz);
            double ax = 57.2957795 * Math.Acos(vz / v);
            radius.X = -vy * vz;
            radius.Y = vx * vz;
            if (vz == 0)
                vz = 1f;

            // find center points
            center.X = (float)((Bounds.XMin + Bounds.XMax) / 2d);
            center.Y = (float)((Bounds.YMin + Bounds.YMax) / 2d);
            center.Z = (float)((Bounds.ZMin + Bounds.ZMax) / 2d);

            BlocksTotalEstimate = (int)v;

            coordEnumerator = BlockEnumerator().GetEnumerator();
            return true;
        }


        IEnumerator<Vector3I> coordEnumerator;
        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            while( coordEnumerator.MoveNext() ) {
                Coords = coordEnumerator.Current;
                if( DrawOneBlock() ) {
                    blocksDone++;
                    if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                }
                if( TimeToEndBatch ) return blocksDone;
            }
            IsDone = true;
            return blocksDone;
        }


        IEnumerable<Vector3I> BlockEnumerator() {
            for( int x = Bounds.XMin; x <= Bounds.XMax; x++ ) {
                for( int y = Bounds.YMin; y <= Bounds.YMax; y++ ) {
                    for( int z = Bounds.ZMin; z <= Bounds.ZMax; z++ ) {
                        double dx = (x - center.X);
                        double dy = (y - center.Y);
                        double dz = (z - center.Z);

                        // test if it's inside ellipse
                        if( (dx * dx) * radius.X + (dy * dy) * radius.Y + (dz * dz) * radius.Z <= 1 ) {
                            yield return new Vector3I( x, y, z );
                        }
                    }
                }
            }
        }
    }
}