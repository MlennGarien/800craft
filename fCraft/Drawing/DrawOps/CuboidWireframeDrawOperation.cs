// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    public sealed class CuboidWireframeDrawOperation : DrawOperation {
        public override string Name {
            get { return "CuboidW"; }
        }

        public CuboidWireframeDrawOperation( Player player )
            : base( player ) {
        }


        public override bool Prepare( Vector3I[] marks ) {
            if( !base.Prepare( marks ) ) return false;

            int hollowVolume = Math.Max( 0, Bounds.Width - 2 ) * Math.Max( 0, Bounds.Length - 2 ) * Math.Max( 0, Bounds.Height - 2 );
            int sideVolumeX = Math.Max( 0, Bounds.Width - 2 ) * Math.Max( 0, Bounds.Length - 2 ) * (Bounds.ZMax != Bounds.ZMin ? 2 : 1);
            int sideVolumeY = Math.Max( 0, Bounds.Length - 2 ) * Math.Max( 0, Bounds.Height - 2 ) * (Bounds.XMax != Bounds.XMin ? 2 : 1);
            int sideVolumeZ = Math.Max( 0, Bounds.Height - 2 ) * Math.Max( 0, Bounds.Width - 2 ) * (Bounds.YMax != Bounds.YMin ? 2 : 1);

            BlocksTotalEstimate = Bounds.Volume - hollowVolume - sideVolumeX - sideVolumeY - sideVolumeZ;

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
            // Draw cuboid vertices
            yield return new Vector3I( Bounds.XMin, Bounds.YMin, Bounds.ZMin );

            if( Bounds.XMin != Bounds.XMax ) yield return new Vector3I( Bounds.XMax, Bounds.YMin, Bounds.ZMin );
            if( Bounds.YMin != Bounds.YMax ) yield return new Vector3I( Bounds.XMin, Bounds.YMax, Bounds.ZMin );
            if( Bounds.ZMin != Bounds.ZMax ) yield return new Vector3I( Bounds.XMin, Bounds.YMin, Bounds.ZMax );

            if( Bounds.XMin != Bounds.XMax && Bounds.YMin != Bounds.YMax )
                yield return new Vector3I( Bounds.XMax, Bounds.YMax, Bounds.ZMin );
            if( Bounds.YMin != Bounds.YMax && Bounds.ZMin != Bounds.ZMax )
                yield return new Vector3I( Bounds.XMin, Bounds.YMax, Bounds.ZMax );
            if( Bounds.ZMin != Bounds.ZMax && Bounds.XMin != Bounds.XMax )
                yield return new Vector3I( Bounds.XMax, Bounds.YMin, Bounds.ZMax );

            if( Bounds.XMin != Bounds.XMax && Bounds.YMin != Bounds.YMax && Bounds.ZMin != Bounds.ZMax )
                yield return new Vector3I( Bounds.XMax, Bounds.YMax, Bounds.ZMax );

            // Draw edges along the X axis
            if( Bounds.Width > 2 ) {
                for( int x = Bounds.XMin + 1; x < Bounds.XMax; x++ ) {
                    yield return new Vector3I( x, Bounds.YMin, Bounds.ZMin );
                    if( Bounds.ZMin != Bounds.ZMax ) yield return new Vector3I( x, Bounds.YMin, Bounds.ZMax );
                    if( Bounds.YMin != Bounds.YMax ) {
                        yield return new Vector3I( x, Bounds.YMax, Bounds.ZMin );
                        if( Bounds.ZMin != Bounds.ZMax ) yield return new Vector3I( x, Bounds.YMax, Bounds.ZMax );
                    }
                }
            }

            // Draw edges along the Y axis
            if( Bounds.Length > 2 ) {
                for( int y = Bounds.YMin + 1; y < Bounds.YMax; y++ ) {
                    yield return new Vector3I( Bounds.XMin, y, Bounds.ZMin );
                    if( Bounds.ZMin != Bounds.ZMax ) yield return new Vector3I( Bounds.XMin, y, Bounds.ZMax );
                    if( Bounds.XMin != Bounds.XMax ) {
                        yield return new Vector3I( Bounds.XMax, y, Bounds.ZMin );
                        if( Bounds.ZMin != Bounds.ZMax ) yield return new Vector3I( Bounds.XMax, y, Bounds.ZMax );
                    }
                }
            }

            // Draw edges along the Z axis
            if( Bounds.Height > 2 ) {
                for( int z = Bounds.ZMin + 1; z < Bounds.ZMax; z++ ) {
                    yield return new Vector3I( Bounds.XMin, Bounds.YMin, z );
                    if( Bounds.YMin != Bounds.YMax ) yield return new Vector3I( Bounds.XMin, Bounds.YMax, z );
                    if( Bounds.XMin != Bounds.XMax ) {
                        yield return new Vector3I( Bounds.XMax, Bounds.YMax, z );
                        if( Bounds.YMin != Bounds.YMax ) yield return new Vector3I( Bounds.XMax, Bounds.YMin, z );
                    }
                }
            }
        }
    }
}