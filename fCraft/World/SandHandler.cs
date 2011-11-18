using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
    public static class SandHandler
    {

        public static World world;

        public static void Init(World world_)
        {
            world = world_;
            Sand.Init(world_);
        }

        public static void Trigger(Player player, Map map, int x, int y, int z, Block type)
        {
            world.Map.SetBlock(x, y, z, type);
            Sand.SandTrigger(player, x, y, z, type);
        }
    }
}
