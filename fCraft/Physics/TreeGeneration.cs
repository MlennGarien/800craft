using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;
using fCraft;

namespace fCraft.Physics
{
    public static class TreeGeneration
    {
        public static Random Rand = new Random();

        public static void MakeNormalFoliage(Player player, Vector3I Pos, int Height)
        {
            int topy = Pos.Z + Height - 1;
            int start = topy - 2;
            int end = topy + 2;

            for (int y = start; y < end; y++)
            {
                int rad;
                if (y > start + 1)
                    rad = 1;
                else
                    rad = 2;
                for (int xoff = -rad; xoff < rad + 1; xoff++)
                {
                    for (int zoff = -rad; zoff < rad + 1; zoff++)
                    {
                        if (Rand.NextDouble() > .618 &&
                            Math.Abs(xoff) == Math.Abs(zoff) &&
                            Math.Abs(xoff) == rad)
                        {
                            continue;
                        }
                        player.WorldMap.QueueUpdate(new
                            BlockUpdate(null, (short)(Pos.X + xoff), (short)(Pos.Y + zoff), (short)y, Block.Leaves));
                    }
                }
            }
        }


        public static void MakePalmFoliage(Player player, Vector3I Pos, int Height)
        {
            int z = Pos.Z + Height;
            for (int xoff = -2; xoff < 3; xoff++)
            {
                for (int yoff = -2; yoff < 3; yoff++)
                {
                    if (Math.Abs(xoff) == Math.Abs(yoff))
                        player.WorldMap.QueueUpdate(new BlockUpdate(null, (short)(Pos.Z + xoff), (short)(Pos.Y + yoff), (short)z, Block.Leaves));
                }
            }
        }
    }
}

