// Copyright 2009-2013 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;

namespace fCraft.Drawing {

    public sealed class PlaneWireframeDrawOperation : DrawOperation {

        public override int ExpectedMarks {
            get { return 4; }
        }

        public override string Name {
            get { return "PlaneW"; }
        }

        public PlaneWireframeDrawOperation( Player player )
            : base( player ) {
        }

        public override bool Prepare( Vector3I[] marks ) {
            Vector3I minVector = new Vector3I( Math.Min( Math.Min( marks[0].X, marks[1].X ), Math.Min( marks[2].X, marks[3].X ) ),
                                               Math.Min( Math.Min( marks[0].Y, marks[1].Y ), Math.Min( marks[2].Y, marks[3].Y ) ),
                                               Math.Min( Math.Min( marks[0].Z, marks[1].Z ), Math.Min( marks[2].Z, marks[3].Z ) ) );
            Vector3I maxVector = new Vector3I( Math.Max( Math.Max( marks[0].X, marks[1].X ), Math.Max( marks[2].X, marks[3].X ) ),
                                               Math.Max( Math.Max( marks[0].Y, marks[1].Y ), Math.Max( marks[2].Y, marks[3].Y ) ),
                                               Math.Max( Math.Max( marks[0].Z, marks[1].Z ), Math.Max( marks[2].Z, marks[3].Z ) ) );
            Bounds = new BoundingBox( minVector, maxVector );

            if ( !base.Prepare( marks ) )
                return false;

            BlocksTotalEstimate = Math.Max( Bounds.Width, Math.Max( Bounds.Height, Bounds.Length ) );

            coordEnumerator1 = LineEnumerator( Marks[0], Marks[1] ).GetEnumerator();
            coordEnumerator2 = LineEnumerator( Marks[1], Marks[2] ).GetEnumerator();
            coordEnumerator3 = LineEnumerator( Marks[2], Marks[3] ).GetEnumerator();
            coordEnumerator4 = LineEnumerator( Marks[3], Marks[0] ).GetEnumerator();
            return true;
        }

        private IEnumerator<Vector3I> coordEnumerator1, coordEnumerator2, coordEnumerator3, coordEnumerator4;

        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            while ( coordEnumerator1.MoveNext() ) {
                Coords = coordEnumerator1.Current;
                if ( DrawOneBlockIfNotDuplicate() ) {
                    blocksDone++;
                    if ( blocksDone >= maxBlocksToDraw )
                        return blocksDone;
                }
                if ( TimeToEndBatch )
                    return blocksDone;
            }
            while ( coordEnumerator2.MoveNext() ) {
                Coords = coordEnumerator2.Current;
                if ( DrawOneBlockIfNotDuplicate() ) {
                    blocksDone++;
                    if ( blocksDone >= maxBlocksToDraw )
                        return blocksDone;
                }
                if ( TimeToEndBatch )
                    return blocksDone;
            }
            while ( coordEnumerator3.MoveNext() ) {
                Coords = coordEnumerator3.Current;
                if ( DrawOneBlockIfNotDuplicate() ) {
                    blocksDone++;
                    if ( blocksDone >= maxBlocksToDraw )
                        return blocksDone;
                }
                if ( TimeToEndBatch )
                    return blocksDone;
            }

            while ( coordEnumerator4.MoveNext() ) {
                Coords = coordEnumerator4.Current;
                if ( DrawOneBlockIfNotDuplicate() ) {
                    blocksDone++;
                    if ( blocksDone >= maxBlocksToDraw )
                        return blocksDone;
                }
                if ( TimeToEndBatch )
                    return blocksDone;
            }
            IsDone = true;
            return blocksDone;
        }

        private readonly HashSet<int> modifiedBlocks = new HashSet<int>();

        private bool DrawOneBlockIfNotDuplicate() {
            int index = Map.Index( Coords );
            if ( modifiedBlocks.Contains( index ) ) {
                return false;
            } else {
                modifiedBlocks.Add( index );
                return DrawOneBlock();
            }
        }
    }
}