// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    public class EllipsoidDrawOperation : DrawOperation {
        Vector3F radius, center;

        public override string Name {
            get { return "Ellipsoid"; }
        }

        public EllipsoidDrawOperation( Player player )
            : base( player ) {
        }


        public override bool Prepare( Vector3I[] marks ) {
            if( !base.Prepare( marks ) ) return false;

            double rx = Bounds.Width / 2d;
            double ry = Bounds.Length / 2d;
            double rz = Bounds.Height / 2d;

            radius.X = (float)(1 / (rx * rx));
            radius.Y = (float)(1 / (ry * ry));
            radius.Z = (float)(1 / (rz * rz));

            // find center points
            center.X = (float)((Bounds.XMin + Bounds.XMax) / 2d);
            center.Y = (float)((Bounds.YMin + Bounds.YMax) / 2d);
            center.Z = (float)((Bounds.ZMin + Bounds.ZMax) / 2d);

            BlocksTotalEstimate = (int)Math.Ceiling( 4 / 3d * Math.PI * rx * ry * rz );

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