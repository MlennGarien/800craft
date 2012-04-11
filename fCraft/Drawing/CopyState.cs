// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using JetBrains.Annotations;

namespace fCraft.Drawing {
    public sealed class CopyState : ICloneable {
        public CopyState( Vector3I mark1, Vector3I mark2 ) {
            BoundingBox box = new BoundingBox( mark1, mark2 );
            Orientation = new Vector3I( mark1.X <= mark2.X ? 1 : -1,
                                        mark1.Y <= mark2.Y ? 1 : -1,
                                        mark1.Z <= mark2.Z ? 1 : -1 );
            Buffer = new Block[box.Width, box.Length, box.Height];
        }

        public CopyState( [NotNull] CopyState original ) {
            if( original == null ) throw new ArgumentNullException();
            Buffer = (Block[, ,])original.Buffer.Clone();
            Orientation = original.Orientation;
            Slot = original.Slot;
            OriginWorld = original.OriginWorld;
            CopyTime = original.CopyTime;
        }

        public CopyState( [NotNull] CopyState original, [NotNull] Block[, ,] buffer ) {
            if( original == null ) throw new ArgumentNullException();
            Buffer = buffer;
            Orientation = original.Orientation;
            Slot = original.Slot;
            OriginWorld = original.OriginWorld;
            CopyTime = original.CopyTime;
        }

        public Block[, ,] Buffer { get; set; }
        public Vector3I Dimensions {
            get {
                return new Vector3I( Buffer.GetLength( 0 ), Buffer.GetLength( 1 ), Buffer.GetLength( 2 ) );
            }
        }
        public Vector3I Orientation { get; set; }
        public int Slot { get; set; }

        // using "string" instead of "World" here
        // to avoid keeping World on the GC after it has been removed.
        public string OriginWorld { get; set; }
        public DateTime CopyTime { get; set; }


        public object Clone() {
            return new CopyState( this );
        }
    }
}