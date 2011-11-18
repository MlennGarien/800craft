using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
    public class Sand
    {
        static World world;
        static HashSet<Block> sandFall = new HashSet<Block>() { Block.Air, Block.Water, Block.Lava, Block.StillWater, Block.StillLava };
        //^things sand can fall into
        public static void Init(World world_)
        {
            world = world_;
        }

        public static void SandTrigger(Player player, int x, int y, int z, Block type) //trigger
        {
            if (type == Block.Sand || type == Block.Gravel) //supported blocks
            {
                World world = player.World;
                int dropHeight = Drop(x, y, z);
                if (dropHeight != z)
                {
                    Send(x, y, z, dropHeight, type, player);
                    DropSpread(x, y, dropHeight);
                }
            }
            StartSpread(x, y, z);
        }

        static void Propagate(int sx, int sy, int sh, int dx, int dy, int dh)
        {
            int x = dx + sx;
            int y = dy + sy;
            int z = dh + sh;
            Block type = (Block)world.Map.GetBlock(x, y, z);
            if (type == Block.Sand || type == Block.Gravel) //if sand or gravel, go to Drop()
            {
                int dropHeight = Drop(x, y, z);
                if (dropHeight != z)
                {
                    Send(x, y, z, dropHeight, type);
                    DropSpread(x, y, dropHeight);
                    ChangeSpread(x, y, z, dx, dy, dh);
                }
            }
        }
        static void StartSpread(int x, int y, int z)
        {
            DropSpread(x, y, z);
            Propagate(x, y, z, 0, 0, 1);
        }
        static void DropSpread(int x, int y, int z)
        {
            Propagate(x, y, z, 1, 0, 0);
            Propagate(x, y, z, -1, 0, 0);
            Propagate(x, y, z, 0, 1, 0);
            Propagate(x, y, z, 0, -1, 0);
            Propagate(x, y, z, 0, 0, -1);
        }
        static void ChangeSpread(int x, int y, int z, int dx, int dy, int dh)
        {
            if (dx != -1)
                Propagate(x, y, z, 1, 0, 0);
            if (dx != 1)
                Propagate(x, y, z, -1, 0, 0);
            if (dy != -1)
                Propagate(x, y, z, 0, 1, 0);
            if (dy != 1)
                Propagate(x, y, z, 0, -1, 0);
            if (dh != 1)
                Propagate(x, y, z, 0, 0, -1);
            Propagate(x, y, z, 0, 0, 1);
        }
        static void Send(int x, int y, int z, int fh, Block type) //for use with Propagate
        {
            world.Map.QueueUpdate(new BlockUpdate(Player.Console, (short)x, (short)y, (short)z, 0));
            world.Map.QueueUpdate(new BlockUpdate(Player.Console, (short)x, (short)y, (short)fh, (Block)(byte)type));
            world.Map.SetBlock(x, y, z, Block.Air);
            world.Map.SetBlock(x, y, fh, type);
        }
        static void Send(int x, int y, int z, int fh, Block type, Player player) //for use with sandtrigger
        {
            player.SendNow(PacketWriter.MakeSetBlock(x, y, z, 0));
            player.SendNow(PacketWriter.MakeSetBlock(x, y, fh, (Block)(byte)type));

            world.Map.QueueUpdate(new BlockUpdate(player, (short)x, (short)y, (short)z, 0));
            world.Map.QueueUpdate(new BlockUpdate(player, (short)x, (short)y, (short)fh, (Block)(byte)type));
            world.Map.SetBlock(x, y, z, Block.Air);
            world.Map.SetBlock(x, y, fh, type);
        }
        static int Drop(int x, int y, int z)
        {
            if (z == 0)
                return 0;
            while (sandFall.Contains((Block)world.Map.GetBlock(x, y, z - 1)) && --z > 0) ; //crank dat soulja boy
            return z;
        }
    }
}