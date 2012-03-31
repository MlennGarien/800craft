using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections;
using fCraft.Events;

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
    public static class Physics
    {

        //junk
        public const int Tick = 150; //in ms
        public static int size = 3;
        public static Thread physicsQueue;

        //init
        public static void Load()
        {
            Player.PlacingBlock += ExplodingPhysics.TNTDrop;
            Player.Clicked += ExplodingPhysics.TNTClick;
            Player.PlacingBlock += ExplodingPhysics.Firework;
            //SchedulerTask deQueuePhysics = Scheduler.NewTask(PhysicsQueue).RunForever(TimeSpan.FromSeconds(0.5));
        }

        public static void PhysicsQueue(SchedulerTask task)
        {
            try
            {
                if (physicsQueue != null)
                {
                    if (physicsQueue.ThreadState != System.Threading.ThreadState.Stopped) //stops multiple threads from opening
                    {
                        return;
                    }
                }
                physicsQueue = new Thread(new ThreadStart(delegate
                {
                    physicsQueue.Priority = ThreadPriority.Lowest;
                    foreach (World world in WorldManager.Worlds)
                    {
                        BasicPhysics b = new BasicPhysics(world);
                        if (world.Map != null && world.IsLoaded) //for all loaded worlds
                        {
                            Map map = world.Map;
                            for (int x = world.Map.Bounds.XMin; x <= world.Map.Bounds.XMax; x++)
                            {
                                for (int y = world.Map.Bounds.YMin; y <= world.Map.Bounds.YMax; y++)
                                {
                                    for (int z = world.Map.Bounds.ZMin; z <= world.Map.Bounds.ZMax; z++)
                                    {
                                        if (BasicPhysics(world.Map.GetBlock(x, y, z)))
                                        {
                                            b.Queue(x, y, z, world.Map.GetBlock(x, y, z));
                                        }
                                    }
                                }
                            }
                        }
                        b.Update();
                    }
                })); physicsQueue.Start();
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.SeriousError, "" + ex);
            }
        }

        //physics helpers & bools

        public static bool MoveSand(Vector3I block, World world)
        {
            if (world.Map != null)
            {
                if (world.Map.GetBlock(new Vector3I(block.X, block.Y, block.Z)) != Block.Sand)
                {
                    return false;
                }
                else
                {
                    Block thisBlock = world.Map.GetBlock(block.X, block.Y, block.Z - 1);
                    if (!BlockThrough(thisBlock))
                    {
                        return false;
                    }
                    return true;
                }
            }

            return false;
        }

        public static bool CanSquash(Block block)
        {
            switch (block)
            {
                case Block.BrownMushroom:
                case Block.Plant:
                case Block.RedFlower:
                case Block.RedMushroom:
                case Block.YellowFlower:
                    return true;
            }
            return false;
        }

        //check
        public static bool SetTileNoPhysics(int x, int y, int z, Block type, World world)
        {
            world.Map.Blocks[(z * world.Map.Height + y) * world.Map.Width + x] = (byte)type;
            world.Map.QueueUpdate(new BlockUpdate(null, (short)x, (short)y, (short)z, type));
            return true;
        }

        public static bool CanPutGrassOn(Vector3I block, World world)
        {
            if (world.Map != null)
            {
                if (world.Map.InBounds(block.X, block.Y, block.Z))
                {
                    if (world.Map.GetBlock(new Vector3I(block.X, block.Y, block.Z)) != Block.Dirt)
                    {
                        return false;
                    }
                    else
                    {
                        for (int z = block.Z; z < world.Map.Bounds.ZMax; z++)
                        {
                            Block toCheck = world.Map.GetBlock(new Vector3I(block.X, block.Y, z + 1));
                            if (makeShadow(toCheck))
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool BasicPhysics(Block type)
        {
            switch (type)
            {
                case Block.Water:
                case Block.Lava:
                case Block.Sponge:
                case Block.Sand:
                case Block.Gravel:
                    return true;
                default:
                    return false;
            }
        }

        public static bool Liquid(Block type)
        {
            switch (type)
            {
                case Block.Lava:
                case Block.Water:
                    return true;
                default:
                    return false;
            }
        }

        public static bool AffectedBySponges(Block type)
        {
            switch (type)
            {
                case Block.Water:
                    return true;
                case Block.Lava:
                default:
                    return false;
            }
        }

        public static bool AffectedByGravity(Block type)
        {
            switch (type)
            {
                case Block.Sand:
                case Block.Gravel:
                    return true;
                default:
                    return false;
            }
        }

        public static bool makeShadow(Block block)
        {
            switch (block)
            {
                case Block.Air:
                case Block.Glass:
                case Block.Leaves:
                case Block.YellowFlower:
                case Block.RedFlower:
                case Block.BrownMushroom:
                case Block.RedMushroom:
                case Block.Plant:
                    return false;
                default:
                    return true;
            }
        }


        public static bool BlockThrough(Block block)
        {
            switch (block)
            {
                case Block.Air:
                case Block.Water:
                case Block.Lava:
                case Block.StillWater:
                case Block.StillLava:
                    return true;
                default:
                    return false;
            }
        }

        public static bool CanFloat(Block block)
        {
            switch (block)
            {
                case Block.Red:
                case Block.Orange:
                case Block.Yellow:
                case Block.Lime:
                case Block.Green:
                case Block.Teal:
                case Block.Aqua:
                case Block.Cyan:
                case Block.Blue:
                case Block.Indigo:
                case Block.Violet:
                case Block.Magenta:
                case Block.Pink:
                case Block.Black:
                case Block.Gray:
                case Block.White:
                case Block.Sponge:
                case Block.Wood:
                case Block.Leaves:
                    return true;
                default:
                    return false;
            }
        }
    }
}