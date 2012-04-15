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
            try
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
                    foreach (World world in WorldManager.Worlds)
                    {
                        if (world.IsLoaded && world != null)
                        {
                            if (world.sandPhysics)
                            {
                                Map map = world.Map;
                                for (int x = world.Map.Bounds.XMin; x <= world.Map.Bounds.XMax; x++)
                                {
                                    if (world != null && world.IsLoaded)
                                    {
                                        for (int y = world.Map.Bounds.YMin; y <= world.Map.Bounds.YMax; y++)
                                        {
                                            if (world != null && world.IsLoaded)
                                            {
                                                for (int z = world.Map.Bounds.ZMin; z <= world.Map.Bounds.ZMax; z++)
                                                {
                                                    if (world != null && world.IsLoaded)
                                                    {
                                                        if (world.sandPhysics)
                                                        {
                                                            SandCheck(x, y, z, world);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                })); sandThread.Start();
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.SeriousError, "" + ex);
            }
        }

        public static void SandCheck(int x, int y, int z, World world)
        {
            try
            {
                if (world.Map != null && world.IsLoaded)
                {
                    Map map = world.Map;
                    if (map != null)
                    {
                        if (map.GetBlock(x, y, z) == Block.Sand || map.GetBlock(x, y, z) == Block.Gravel)
                        {
                            if (!Physics.BlockThrough(map.GetBlock(x, y, z - 1)))
                            {
                                return;
                            }
                            if (z - 1 == z) return;
                            Thread.Sleep(10);
                            if (world.Map != null && world.IsLoaded)
                            {
                                Block oldBlock = map.GetBlock(x, y, z);

                                map.QueueUpdate(new BlockUpdate(null, (short)x, (short)y, (short)z, Block.Air));
                                map.QueueUpdate(new BlockUpdate(null, (short)x, (short)y, (short)(z - 1), oldBlock));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.SeriousError, "" + ex);
            }
        }
    }
}
