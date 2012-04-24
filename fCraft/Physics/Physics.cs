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
            //Player.PlacingBlock += PlantPhysics.TreeGrowing;
            //Player.PlacingBlock += PlantPhysics.blockSquash;
            //Player.PlacingBlock += ExplodingPhysics.TNTDrop;
            //Player.Clicked += ExplodingPhysics.TNTClick;
            //Player.PlacingBlock += ExplodingPhysics.Firework;
            //Player.PlacingBlock += WaterPhysics.blockFloat;
            Player.PlacingBlock += WaterPhysics.blockSink;
            SchedulerTask drownCheck = Scheduler.NewBackgroundTask(WaterPhysics.drownCheck).RunForever(TimeSpan.FromSeconds(3));
            Player.PlacingBlock += WaterPhysics.towerInit;
            Player.Clicking += WaterPhysics.towerRemove;
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

        public static bool BasicPhysics(Block type)
        {
            switch (type)
            {
                case Block.Water:
                case Block.Lava:
                case Block.Sponge:
                case Block.Sand:
                case Block.Gravel:
                case Block.Plant:
                case Block.TNT:
                case Block.Dirt:
                case Block.Grass:
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