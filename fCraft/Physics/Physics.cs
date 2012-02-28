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
    public static class Physics
    {

        //junk
        public const int Tick = 250; //in ms
        public static int size = 3;

        //init
        public static void Load()
        {
            Player.Clicking += GunTest.ClickedGlass;//
            Player.Moving += GunTest.gunMove;//
            Player.Moving += GunTest.movePortal;//
            SchedulerTask checkGrass = Scheduler.NewBackgroundTask(PlantPhysics.grassChecker).RunForever(TimeSpan.FromSeconds(new Random().Next(1, 4)));
            SchedulerTask checkSand = Scheduler.NewBackgroundTask(SandPhysics.checkSand).RunForever(TimeSpan.FromSeconds(1));
            SchedulerTask checkSandQueue = Scheduler.NewBackgroundTask(SandPhysics.processSand).RunForever(TimeSpan.FromSeconds(1));
            SchedulerTask checkWater = Scheduler.NewBackgroundTask(WaterPhysics.waterChecker).RunForever(TimeSpan.FromSeconds(1));
            Player.PlacingBlock += PlantPhysics.TreeGrowing;
            // Player.PlacingBlock += PlantPhysics.test; (drawimg)
            Player.PlacingBlock += ExplodingPhysics.TNTDrop;
            Player.Clicked += ExplodingPhysics.TNTClick;
            Player.PlacingBlock += ExplodingPhysics.Firework;
            Player.PlacingBlock += WaterPhysics.playerPlacedWater;
            Player.PlacingBlock += WaterPhysics.blockFloat;
            Player.PlacingBlock += WaterPhysics.blockSink;
        }


        //physics helpers & bools

        public static bool MoveSand(Vector3I block, World world)
        {
            if (world.Map != null)
            {
                if (world.Map.InBounds(block.X, block.Y, block.Z))
                {
                    if (world.Map.GetBlock(new Vector3I(block.X, block.Y, block.Z)) != Block.Sand)
                    {
                        return false;
                    }
                    else
                    {
                        if(world.Map.GetBlock(block.X, block.Y, block.Z -1) != Block.Air)
                        { 
                            return false;
                        }
                        return true;
                    }
                }
            }
            return false;
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

        public static bool makeShadow( Block block ) {
            switch( block ) {
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
