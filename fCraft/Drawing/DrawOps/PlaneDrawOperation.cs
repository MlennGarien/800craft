// fCraft is Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
// TriangleDrawOperation contributed by Conrad "Redshift" Morgan
using System;

namespace fCraft.Drawing {

    public sealed class PlaneDrawOperation : DrawOperation {

        public override string Name {
            get { return "Plane"; }
        }

        public override int ExpectedMarks {
            get { return 3; }
        }

        public PlaneDrawOperation( Player player )
            : base( player ) {
        }

        // Plane vertices.
        private Vector3I a, b, c, d;

        // Edge planes perpendicular to surface, pointing outwards.
        private Vector3F s1, s2, s3, s4;

        private Vector3I normal;
        private Vector3F normalF;

        public override bool Prepare( Vector3I[] marks ) {
            a = marks[0];
            c = marks[1];
            b = marks[2];

            d = new Vector3I( a.X + c.X - b.X, a.Y + c.Y - b.Y, a.Z + c.Z - b.Z );

            Bounds = new BoundingBox(
                Math.Min( Math.Min( a.X, b.X ), Math.Min( c.X, d.X ) ),
                Math.Min( Math.Min( a.Y, b.Y ), Math.Min( c.Y, d.Y ) ),
                Math.Min( Math.Min( a.Z, b.Z ), Math.Min( c.Z, d.Z ) ),
                Math.Max( Math.Max( a.X, b.X ), Math.Max( c.X, d.X ) ),
                Math.Max( Math.Max( a.Y, b.Y ), Math.Max( c.Y, d.Y ) ),
                Math.Max( Math.Max( a.Z, b.Z ), Math.Max( c.Z, d.Z ) )
            );

            Coords = Bounds.MinVertex;

            if ( !base.Prepare( marks ) )
                return false;

            normal = ( b - a ).Cross( c - a );
            normalF = normal.Normalize();
            BlocksTotalEstimate = GetBlockTotalEstimate();

            s1 = normal.Cross( a - b ).Normalize();
            s2 = normal.Cross( b - c ).Normalize();
            s3 = normal.Cross( c - d ).Normalize();
            s4 = normal.Cross( d - a ).Normalize();

            return true;
        }

        private int GetBlockTotalEstimate() {
            Vector3I nabs = normal.Abs();
            return Math.Max( Math.Max( nabs.X, nabs.Y ), nabs.Z ) / 2;
        }

        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            for ( ; Coords.X <= Bounds.XMax; Coords.X++ ) {
                for ( ; Coords.Y <= Bounds.YMax; Coords.Y++ ) {
                    for ( ; Coords.Z <= Bounds.ZMax; Coords.Z++ ) {
                        if ( IsPlaneBlock() && DrawOneBlock() ) {
                            blocksDone++;
                            if ( blocksDone >= maxBlocksToDraw )
                                return blocksDone;
                        }
                        if ( TimeToEndBatch )
                            return blocksDone;
                    }
                    Coords.Z = Bounds.ZMin;
                }
                Coords.Y = Bounds.YMin;
            }

            IsDone = true;
            return blocksDone;
        }

        private const float Extra = 0.5f;

        private bool IsPlaneBlock() {
            // Early out.
            if ( Math.Abs( normalF.Dot( Coords - a ) ) > 1 )
                return false;

            // Check if within plane region.
            if ( ( Coords - a ).Dot( s1 ) > Extra ||
                ( Coords - b ).Dot( s2 ) > Extra ||
                ( Coords - c ).Dot( s3 ) > Extra ||
                ( Coords - d ).Dot( s4 ) > Extra )
                return false;

            // Check if minimal plane block.
            return TestAxis( 1, 0, 0 ) ||
                   TestAxis( 0, 1, 0 ) ||
                   TestAxis( 0, 0, 1 );
        }

        // Checks distance to plane along axis.
        private bool TestAxis( int x, int y, int z ) {
            Vector3I v = new Vector3I( x, y, z );
            int numerator = normal.Dot( a - Coords );
            int denominator = normal.Dot( v );
            if ( denominator == 0 )
                return numerator == 0;
            double distance = ( double )numerator / denominator;
            return distance > -0.5 && distance <= 0.5;
        }
    }
}