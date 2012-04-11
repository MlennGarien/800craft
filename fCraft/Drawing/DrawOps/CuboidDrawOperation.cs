// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>

namespace fCraft.Drawing {
    public sealed class CuboidDrawOperation : DrawOperation {
        public override string Name {
            get { return "Cuboid"; }
        }

        public CuboidDrawOperation( Player player )
            : base( player ) {
        }


        public override bool Prepare( Vector3I[] marks ) {
            if( !base.Prepare( marks ) ) return false;
            BlocksTotalEstimate = Bounds.Volume;
            Coords = Bounds.MinVertex;
            return true;
        }


        public override int DrawBatch( int maxBlocksToDraw ) {
            int blocksDone = 0;
            for( ; Coords.X <= Bounds.XMax; Coords.X++ ) {
                for( ; Coords.Y <= Bounds.YMax; Coords.Y++ ) {
                    for( ; Coords.Z <= Bounds.ZMax; Coords.Z++ ) {
                        if( !DrawOneBlock() ) continue;
                        blocksDone++;
                        if( blocksDone >= maxBlocksToDraw ) {
                            Coords.Z++;
                            return blocksDone;
                        }
                    }
                    Coords.Z = Bounds.ZMin;
                }
                Coords.Y = Bounds.YMin;
                if( TimeToEndBatch ) {
                    Coords.X++;
                    return blocksDone;
                }
            }
            IsDone = true;
            return blocksDone;
        }
    }
}