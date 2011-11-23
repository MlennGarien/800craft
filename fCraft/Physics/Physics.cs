using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections;

namespace fCraft.Physics
{
    /// <summary>
    //  ░░░▄▄▄▄▀▀▀▀▀▀▀▀▄▄▄▄▄▄
    //░░░░█░░░░▒▒▒▒▒▒▒▒▒▒▒▒░░▀▀▄
    //░░░█░░░▒▒▒▒▒▒░░░░░░░░▒▒▒░░█
    //░░█░░░░░░▄██▀▄▄░░░░░▄▄▄░░░█
    //░▀▒▄▄▄▒░█▀▀▀▀▄▄█░░░██▄▄█░░░█
    //█▒█▒▄░▀▄▄▄▀░░░░░░░░█░░░▒▒▒▒▒█
    //█▒█░█▀▄▄░░░░░█▀░░░░▀▄░░▄▀▀▀▄▒█
    //░█▀▄░█▄░█▀▄▄░▀░▀▀░▄▄▀░░░░█░░█
    //░░█░░▀▄▀█▄▄░█▀▀▀▄▄▄▄▀▀█▀██░█
    //░░░█░░██░░▀█▄▄▄█▄▄█▄████░█
    //░░░░█░░░▀▀▄░█░░░█░███████░█
    //░░░░░▀▄░░░▀▀▄▄▄█▄█▄█▄█▄▀░░█
    //░░░░░░░▀▄▄░▒▒▒▒░░░░░░░░░░█
    //░░░░░░░░░░▀▀▄▄░▒▒▒▒▒▒▒▒▒▒░█
    //░░░░░░░░░░░░░░▀▄▄▄▄▄░░░░░█
    // Trollphysics, incoming? Admit it, you just laughed.
    /// </summary>
    class Physics
    {
        // Threads
        private static Thread checkGrass;
        private static Thread checkGrassQueue;

        // Queues
        private static ArrayList grassQueue = new ArrayList();

        public static void Load()
        {
            SchedulerTask checkGrass = Scheduler.NewBackgroundTask(CheckGrass).RunForever(TimeSpan.FromSeconds(5));
            SchedulerTask checkGrassQueue = Scheduler.NewBackgroundTask(CheckGrassQueue).RunForever(TimeSpan.FromSeconds(1));
        }

        public static void CheckGrass(SchedulerTask task)
        {
            if (checkGrass != null)
            {
                if (checkGrass.ThreadState != ThreadState.Stopped)
                {
                    return;
                }
            }

            checkGrass = new Thread(new ThreadStart(delegate
            {
                foreach (World world in WorldManager.Worlds)
                {
                    if (world.Map != null)
                    {
                        for (int x = world.Map.Bounds.XMin; x < world.Map.Bounds.XMax; x++)
                        {
                            for (int y = world.Map.Bounds.YMin; y < world.Map.Bounds.YMax; y++)
                            {
                                for (int z = world.Map.Bounds.ZMin; z < world.Map.Bounds.ZMax; z++)
                                {
                                    if (world.Map.GetBlock(new Vector3I(x, y, z)) == Block.Dirt)
                                    {
                                        if (CanPutGrassOn(new Vector3I(x, y, z), world))
                                        {
                                            // Okay let's plant some seeds
                                            int randomDelay = new Random().Next(1, 60);
                                            GrassUpdate update = new GrassUpdate(world, new Vector3I(x, y, z), DateTime.Now.AddSeconds(randomDelay));
                                            
                                            lock (grassQueue.SyncRoot)
                                            {
                                                grassQueue.Add(update);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }));
            checkGrass.Start();
        }

        private static void CheckGrassQueue(SchedulerTask task)
        {
            try
            {
                if (checkGrassQueue != null)
                {
                    if (checkGrassQueue.ThreadState != ThreadState.Stopped)
                    {
                        return;
                    }
                }
                checkGrassQueue = new Thread(new ThreadStart(delegate
                {
                    lock (grassQueue.SyncRoot)
                    {
                        for (int i = 0; i < grassQueue.Count; i++)
                        {
                            GrassUpdate update = (GrassUpdate)grassQueue[i];

                            if (DateTime.Now > update.Scheduled)
                            {
                                try
                                {
                                    if (CanPutGrassOn(update.Block, update.World))
                                    {
                                        BlockUpdate grassUpdate = new BlockUpdate(null, update.Block, Block.Grass);
                                        update.World.Map.QueueUpdate(grassUpdate);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log(LogType.Error, "Physics.CheckGrassQueue: " + ex);
                                }
                                finally
                                {
                                    grassQueue.Remove(update);
                                }
                            }
                        }
                    }
                }));
                checkGrassQueue.Start();
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, "Physics.CheckGrassQueue: " + ex);
            }
        }

        public static bool CanPutGrassOn(Vector3I block, World world)
        {
            if (world.Map.InBounds(block.X, block.Y, block.Z + 1))
            {
                if (world.Map.GetBlock(new Vector3I(block.X, block.Y, block.Z + 1)) == Block.Air)
                {
                    for (int z = block.Z + 1; z < world.Map.Bounds.ZMax; z++)
                    {
                        if (world.Map.GetBlock(new Vector3I(block.X, block.Y, z + 1)) != Block.Air)
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            return false;
        }
    }
}
