// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>

namespace fCraft {
    /// <summary> Structure representing a pending update to the map's block array.
    /// Contains information about the block coordinates, type, and change's origin. </summary>
    public struct BlockUpdate {
        public readonly Player Origin; // Used for stat tracking. Can be null (to avoid crediting any stats at all).
        public readonly short X, Y, Z;
        public readonly Block BlockType;

        public BlockUpdate( Player origin, short x, short y, short z, Block blockType ) {
            Origin = origin;
            X = x;
            Y = y;
            Z = z;
            BlockType = blockType;
        }

        public BlockUpdate( Player origin, Vector3I coord, Block blockType ) {
            Origin = origin;
            X = (short)coord.X;
            Y = (short)coord.Y;
            Z = (short)coord.Z;
            BlockType = blockType;
        }
    }
}