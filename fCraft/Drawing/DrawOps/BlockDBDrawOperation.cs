// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft.Drawing {
    /// <summary> Draw operation that applies changes from a given BlockDBEntry array. </summary>
    public sealed class BlockDBDrawOperation : DrawOpWithBrush {
        BlockDBEntry[] changes;
        int entryIndex;
        Block block;
        readonly string commandName;


        public override string Name {
            get { return commandName; }
        }


        public override string Description {
            get {
                if ( String.IsNullOrEmpty( paramDescription ) ) {
                    return Name;
                } else {
                    return String.Format( "{0}({1})", Name, paramDescription );
                }
            }
        }
        readonly string paramDescription;


        public override int ExpectedMarks {
            get { return expectedMarks; }
        }
        readonly int expectedMarks;


        public BlockDBDrawOperation ( Player player, string commandName, string paramDescription, int expectedMarks )
            : base( player ) {
            if ( commandName == null ) throw new ArgumentNullException( "commandName" );
            this.paramDescription = paramDescription;
            this.commandName = commandName;
            this.expectedMarks = expectedMarks;
        }


        public bool Prepare ( Vector3I[] marks, BlockDBEntry[] changesToApply ) {
            if ( changesToApply == null ) throw new ArgumentNullException( "changesToApply" );
            changes = changesToApply;
            return Prepare( marks );
        }


        public override bool Prepare ( Vector3I[] marks ) {
            if ( changes == null ) {
                throw new InvalidOperationException( "Call the other overload to set entriesToUndo" );
            }
            Brush = this;
            if ( !base.Prepare( marks ) ) return false;
            BlocksTotalEstimate = changes.Length;
            if ( marks.Length != 2 ) {
                Bounds = FindBounds();
            }
            return true;
        }


        BoundingBox FindBounds () {
            if ( changes.Length == 0 ) return BoundingBox.Empty;
            Vector3I min = new Vector3I( int.MaxValue, int.MaxValue, int.MaxValue );
            Vector3I max = new Vector3I( int.MinValue, int.MinValue, int.MinValue );
            for ( int i = 0; i < changes.Length; i++ ) {
                if ( changes[i].X < min.X ) min.X = changes[i].X;
                if ( changes[i].Y < min.Y ) min.Y = changes[i].Y;
                if ( changes[i].Z < min.Z ) min.Z = changes[i].Z;
                if ( changes[i].X > max.X ) max.X = changes[i].X;
                if ( changes[i].Y > max.Y ) max.Y = changes[i].Y;
                if ( changes[i].Z > max.Z ) max.Z = changes[i].Z;
            }
            return new BoundingBox( min, max );
        }


        public override int DrawBatch ( int maxBlocksToDraw ) {
            int blocksDone = 0;
            for ( ; entryIndex < changes.Length; entryIndex++ ) {
                BlockDBEntry entry = changes[entryIndex];
                Coords = new Vector3I( entry.X, entry.Y, entry.Z );
                block = entry.OldBlock;
                if ( entry.PlayerID == Player.Info.ID ) {
                    Context = BlockChangeContext.UndoneSelf | BlockChangeContext.Drawn;
                } else {
                    Context = BlockChangeContext.UndoneOther | BlockChangeContext.Drawn;
                }
                if ( DrawOneBlock() ) {
                    blocksDone++;
                    if ( blocksDone >= maxBlocksToDraw || TimeToEndBatch ) {
                        entryIndex++;
                        return blocksDone;
                    }
                }
            }
            IsDone = true;
            return blocksDone;
        }


        protected override Block NextBlock () {
            return block;
        }


        public override bool ReadParams ( Command cmd ) {
            return true;
        }
    }
}