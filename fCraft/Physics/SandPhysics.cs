using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;
using System.Threading;

namespace fCraft.Physics
{
    class SandPhysics
    {
        public static Thread sandThread;

        public static void SandSearch(SchedulerTask task)
        {
            if (sandThread != null)
            {
                if (sandThread.ThreadState != ThreadState.Stopped) //stops multiple threads from opening
                {
                    return;
                }
            }
            sandThread = new Thread(new ThreadStart(delegate
            {
                sandThread.Priority = ThreadPriority.BelowNormal;
                sandThread.IsBackground = true;
                foreach (World world in WorldManager.Worlds.Where(w => w.IsLoaded && w.Map != null && w.sandPhysics))
                {
                    Map map = world.Map;
                    for (int x = world.Map.Bounds.XMin; x <= world.Map.Bounds.XMax; x++)
                    {
                        for (int y = world.Map.Bounds.YMin; y <= world.Map.Bounds.YMax; y++)
                        {
                            for (int z = world.Map.Bounds.ZMin; z <= world.Map.Bounds.ZMax; z++)
                            {
                                if (world.Map == null && !world.IsLoaded)
                                {
                                    break;
                                }

                                if (world.sandPhysics)
                                {
                                    SandCheck(x, y, z, map);
                                }
                            }
                        }
                    }
                }
            })); sandThread.Start();
        }

        public static void SandCheck(int x, int y, int z, Map map)
        {
            if (map.GetBlock(x, y, z) == Block.Sand || map.GetBlock(x, y, z) == Block.Gravel)
            {
                if (!Physics.BlockThrough(map.GetBlock(x, y, z - 1)))
                {
                    return;
                }
                if (z - 1 == z) return;
                Thread.Sleep(10);
                Block oldBlock = map.GetBlock(x, y, z);
                map.QueueUpdate(new BlockUpdate(null, (short)x, (short)y, (short)z, Block.Air));
                map.QueueUpdate(new BlockUpdate(null, (short)x, (short)y, (short)(z - 1), oldBlock));
            }
        }
    }
}
