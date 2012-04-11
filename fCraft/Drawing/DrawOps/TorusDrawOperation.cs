// fCraft is Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
// original TorusDrawOperation written and contributed by M1_Abrams
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    public sealed class TorusDrawOperation : DrawOperation {
        const float Bias = 0.5f;

        Vector3I center;

        int tubeR;
        double bigR;
        public override string Name {
            get { return "Torus"; }
        }

        public TorusDrawOperation( Player player )
            : base( player ) {
        }


        public override bool Prepare( Vector3I[] marks ) {
            if( !base.Prepare( marks ) ) return false;

            // center of the torus
            center = marks[0];

            Vector3I radiusVector = (marks[1] - center).Abs();

            // tube radius is figured out from Z component of the mark vector
            tubeR = radiusVector.Z;

            // torus radius is figured out from length of vector's X-Y components
            bigR = Math.Sqrt( radiusVector.X * radiusVector.X +
                              radiusVector.Y * radiusVector.Y + .5 );

            // tube + torus radius, rounded up. This will be the maximum extend of the torus.
            int combinedRadius = (int)Math.Ceiling( bigR + tubeR );

            // vector from center of torus to the furthest-away point of the bounding box
            Vector3I combinedRadiusVector = new Vector3I( combinedRadius, combinedRadius, tubeR + 1 );

            // adjusted bounding box
            Bounds = new BoundingBox( center - combinedRadiusVector, center + combinedRadiusVector );

            BlocksTotalEstimate = (int)(2 * Math.PI * Math.PI * bigR * (tubeR * tubeR + Bias));

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

                        // test if it's inside the torus
                        double r1 = bigR - Math.Sqrt( dx * dx + dy * dy );
                        if( r1 * r1 + dz * dz <= tubeR * tubeR + Bias ) {
                            yield return new Vector3I( x, y, z );
                        }
                    }
                }
            }
        }
    }
}