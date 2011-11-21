using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

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
        private static Thread checkGrass;

        public static void Load()
        {
            SchedulerTask checkGrass = Scheduler.NewBackgroundTask(CheckGrass).RunForever(TimeSpan.FromSeconds(20));
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
                                            int randomDelay = new Random().Next(1, 3000);
                                            Thread.Sleep(randomDelay);
                                            BlockUpdate update = new BlockUpdate(null, new Vector3I(x, y, z), Block.Grass);
                                            world.Map.QueueUpdate(update);
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

        public static bool CanPutGrassOn(Vector3I block, World world)
        {
            if (world.Map.InBounds(block.X, block.Y, block.Z + 1))
            {
                if (world.Map.GetBlock(new Vector3I(block.X, block.Y, block.Z + 1)) == Block.Air)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
