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
        public static Thread sandQueueThread;

        public static void processSand(SchedulerTask task)
        {
            sandQueueThread = new Thread(new ThreadStart(delegate
            {
                sandQueueThread.Priority = ThreadPriority.BelowNormal;
                sandQueueThread.IsBackground = true;
                foreach (World world in WorldManager.Worlds)
                {
                    if (world.Map != null && world.IsLoaded && world.sandQueue.Count > 0)
                    {
                        if (world.sandPhysics)
                        {
                            foreach (Vector3I block in world.sandQueue.Values)
                            {
                                if (world.Map.GetBlock(block) == Block.Sand)
                                {
                                    if (Physics.MoveSand(block, world))
                                    {
                                        Map map = world.Map;
                                        map.QueueUpdate(new BlockUpdate(null,
                                                            block,
                                                            Block.Air));
                                        map.QueueUpdate(new BlockUpdate(null,
                                                            (short)block.X,
                                                            (short)block.Y,
                                                            (short)(block.Z - 1),
                                                            Block.Sand));
                                    }
                                    else
                                    {
                                        Vector3I removed;
                                        world.sandQueue.TryRemove(block.ToString(), out removed);
                                    }
                                }
                                else
                                {
                                    Vector3I removed;
                                    world.sandQueue.TryRemove(block.ToString(), out removed);
                                }
                            }
                        }
                    }
                }
            })); sandQueueThread.Start();
        }


        public static void checkSand(SchedulerTask task)
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
                sandThread.Priority = ThreadPriority.Lowest;
                sandThread.IsBackground = true;
                var worlds = WorldManager.Worlds.Where(w => w.IsLoaded && w.Map != null && w.sandPhysics);
                foreach (World world in worlds)
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
                                    Vector3I block = new Vector3I(x, y, z);
                                    if (Physics.MoveSand(block, world) && !world.sandQueue.Values.Contains(block))
                                    {
                                        world.sandQueue.TryAdd(block.ToString(), block);
                                    }
                                }
                            }
                        }
                    }
                }
            })); sandThread.Start();
        }
    }
}
