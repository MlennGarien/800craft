// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {
    public sealed class TriangleWireframeDrawOperation : DrawOperation {

        public override int ExpectedMarks {
            get { return 3; }
        }

        public override string Name {
            get { return "TriangleW"; }
        }

        public TriangleWireframeDrawOperation( Player player )
            : base( player ) {
        }


        public override bool Prepare( Vector3I[] marks ) {
            Vector3I minVector = new Vector3I( Math.Min( marks[0].X, Math.Min( marks[1].X, marks[2].X ) ),
                                               Math.Min( marks[0].Y, Math.Min( marks[1].Y, marks[2].Y ) ),
                                               Math.Min( marks[0].Z, Math.Min( marks[1].Z, marks[2].Z ) ) );
            Vector3I maxVector = new Vector3I( Math.Max( marks[0].X, Math.Max( marks[1].X, marks[2].X ) ),
                                               Math.Max( marks[0].Y, Math.Max( marks[1].Y, marks[2].Y ) ),
                                               Math.Max( marks[0].Z, Math.Max( marks[1].Z, marks[2].Z ) ) );
            Bounds = new BoundingBox( minVector, maxVector );

            if( !base.Prepare( marks ) ) return false;

            BlocksTotalEstimate = Math.Max( Bounds.Width, Math.Max( Bounds.Height, Bounds.Length ) );

            coordEnumerator1 = LineEnumerator( Marks[0], Marks[1] ).GetEnumerator();
            coordEnumerator2 = LineEnumerator( Marks[1], Marks[2] ).GetEnumerator();
            coordEnumerator3 = LineEnumerator( Marks[2], Marks[0] ).GetEnumerator();
            return true;
        }


        IEnumerator<Vector3I> coordEnumerator1, coordEnumerator2, coordEnumerator3;
        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            while( coordEnumerator1.MoveNext() ) {
                Coords = coordEnumerator1.Current;
                if( DrawOneBlockIfNotDuplicate() ) {
                    blocksDone++;
                    if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                }
                if( TimeToEndBatch ) return blocksDone;
            }
            while( coordEnumerator2.MoveNext() ) {
                Coords = coordEnumerator2.Current;
                if( DrawOneBlockIfNotDuplicate() ) {
                    blocksDone++;
                    if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                }
                if( TimeToEndBatch ) return blocksDone;
            }
            while( coordEnumerator3.MoveNext() ) {
                Coords = coordEnumerator3.Current;
                if( DrawOneBlockIfNotDuplicate() ) {
                    blocksDone++;
                    if( blocksDone >= maxBlocksToDraw ) return blocksDone;
                }
                if( TimeToEndBatch ) return blocksDone;
            }
            IsDone = true;
            return blocksDone;
        }

        

        readonly HashSet<int> modifiedBlocks = new HashSet<int>();

        bool DrawOneBlockIfNotDuplicate() {
            int index = Map.Index( Coords );
            if( modifiedBlocks.Contains( index ) ) {
                return false;
            } else {
                modifiedBlocks.Add( index );
                return DrawOneBlock();
            }
        }
    }
}