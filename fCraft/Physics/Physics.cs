﻿using System;
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
            Player.PlacingBlock += PlantPhysics.blockSquash;
            SchedulerTask drownCheck = Scheduler.NewBackgroundTask(WaterPhysics.drownCheck).RunForever(TimeSpan.FromSeconds(3));
            Player.PlacingBlock += WaterPhysics.towerInit;
            Player.Clicking += WaterPhysics.towerRemove;
            Player.PlacedBlock += PlayerPlacedPhysics;
            Player.Clicked += PlayerClickedPhysics;
        }

        public static void PlayerClickedPhysics(object sender, Events.PlayerClickedEventArgs e)
        {
            World world = e.Player.World;
            if (world.Map.GetBlock(e.Coords) == Block.TNT)
            {
                lock (world.SyncRoot)
                {
                    world.AddTask(new TNTTask(world, e.Coords, e.Player, false), 0);
                }
            }
        }
        public static void PlayerPlacedPhysics(object sender, PlayerPlacedBlockEventArgs e)
        {
            World world = e.Player.World;
            if (e.NewBlock == Block.Gold)
            {
                if (e.Context == BlockChangeContext.Manual)
                {
                    if (e.Player.fireworkMode && world.fireworkPhysics)
                    {
                        world.AddTask(new Firework(world, e.Coords), 300);
                    }
                }
            }
            if (e.NewBlock == Block.TNT)
            {
                if (world.tntPhysics)
                {
                    if (e.Context == BlockChangeContext.Manual)
                    {
                        lock (world.SyncRoot)
                        {
                            world.AddTask(new TNTTask(world, e.Coords, e.Player, false), 3000);
                            return;
                        }
                    }
                }
            }
            if (e.NewBlock == Block.Sand || e.NewBlock == Block.Gravel)
            {
                if (e.Context == BlockChangeContext.Manual)
                {
                    if (world.sandPhysics)
                    {
                        lock (world.SyncRoot)
                        {
                            world.AddTask(new SandTask(world, e.Coords, e.NewBlock), 150);
                            return;
                        }
                    }
                }
            }
            if (Physics.CanFloat(e.NewBlock))
            {
                if (world.waterPhysics)
                {
                    if (e.Context == BlockChangeContext.Manual)
                    {
                        world.AddTask(new BlockFloat(world, e.Coords, e.NewBlock), 200);
                        return;
                    }
                }
            }
            if (!Physics.CanFloat(e.NewBlock)
                && e.NewBlock != Block.Air
                && e.NewBlock != Block.Water
                && e.NewBlock != Block.Lava
                && e.NewBlock != Block.BrownMushroom
                && e.NewBlock != Block.RedFlower
                && e.NewBlock != Block.RedMushroom
                && e.NewBlock != Block.YellowFlower
                && e.NewBlock != Block.Plant)
            {
                if (e.Context == BlockChangeContext.Manual)
                {
                    if (world.waterPhysics)
                    {
                        world.AddTask(new BlockSink(world, e.Coords, e.NewBlock), 200);
                        return;
                    }
                }
            }
        }

        //physics helpers & bools
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