//Copyright (C) <2012>  <Jon Baker, Glenn Mariën and Lao Tszy>

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections;
using fCraft.Events;
using System.Xml.Linq;

namespace fCraft
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
        //init
        public static void Load()
        {
            Player.PlacingBlock += PlantPhysics.blockSquash;
            SchedulerTask drownCheck = Scheduler.NewBackgroundTask(WaterPhysics.drownCheck).RunForever(TimeSpan.FromSeconds(3));
            Player.PlacingBlock += PlayerPlacingPhysics;
        }

        #region Serialization

        public const string XmlRootName = "Physics";
        public const string XmlRootName2 = "OtherPhysics";

        public static XElement SaveSettings(World world)
        {
            return SaveSettings(XmlRootName, world);
        }

        public static XElement SaveOtherSettings(World world)
        {
            return SaveOtherSettings(XmlRootName2, world);
        }

        public static XElement SaveSettings(string rootName, World world)
        {
            XElement root = new XElement(rootName);
            if (world.plantPhysics)
            {
                root.Add(new XAttribute("plantPhysics", true));
            }
            if (world.tntPhysics)
            {
                root.Add(new XAttribute("tntPhysics", true));
            }
            if (world.waterPhysics)
            {
                root.Add(new XAttribute("waterPhysics", true));
            }
            return root;
        }
        public static XElement SaveOtherSettings(string rootName, World world)
        {
            XElement root = new XElement(rootName);
            if (world.fireworkPhysics)
            {
                root.Add(new XAttribute("fireworkPhysics", true));
            }
            if (world.sandPhysics)
            {
                root.Add(new XAttribute("sandPhysics", true));
            }
            if (world.gunPhysics)
            {
                root.Add(new XAttribute("gunPhysics", true));
            }
            return root;
        }
        

        public static void LoadSettings(XElement el, World world)
        {
            XAttribute temp;
            if ((temp = el.Attribute("plantPhysics")) != null)
            {
                bool isPhy;
                if (bool.TryParse(temp.Value, out isPhy))
                {
                    world.EnablePlantPhysics(Player.Console, false);
                }
            }
            if ((temp = el.Attribute("tntPhysics")) != null)
            {
                bool isPhy;
                if (bool.TryParse(temp.Value, out isPhy))
                {
                    world.EnableTNTPhysics(Player.Console, false);
                }
            }
            if ((temp = el.Attribute("waterPhysics")) != null)
            {
                bool isPhy;
                if (bool.TryParse(temp.Value, out isPhy))
                {
                    world.EnableWaterPhysics(Player.Console, false);
                }
            }
        }

        public static void LoadOtherSettings(XElement el, World world)
        {
            XAttribute temp;
            if ((temp = el.Attribute("sandPhysics")) != null)
            {
                bool isPhy;
                if (bool.TryParse(temp.Value, out isPhy))
                {
                    world.EnableSandPhysics(Player.Console, false);
                }
            }
            if ((temp = el.Attribute("fireworkPhysics")) != null)
            {
                bool isPhy;
                if (bool.TryParse(temp.Value, out isPhy))
                {
                    world.EnableFireworkPhysics(Player.Console, false);
                }
            }
            if ((temp = el.Attribute("gunPhysics")) != null)
            {
                bool isPhy;
                if (bool.TryParse(temp.Value, out isPhy))
                {
                    world.EnableGunPhysics(Player.Console, false);
                }
            }
        }

        #endregion

        public static void PlayerPlacingPhysics(object sender, PlayerPlacingBlockEventArgs e)
        {
            World world = e.Player.World;
            if (e.Result != CanPlaceResult.Allowed){
                return;
            }
            if (e.NewBlock == Block.Gold)
            {
                if (e.Context == BlockChangeContext.Manual)
                {
                    if (e.Player.fireworkMode && world.fireworkPhysics)
                    {
                        if (world.FireworkCount > 10)
                        {
                            e.Result = CanPlaceResult.Revert;
                            e.Player.Message("&WThere are too many active fireworks on this world");
                            return;
                        }
                        else
                        {
                            world.FireworkCount++;
                            world.AddPhysicsTask(new Firework(world, e.Coords), 300);
                        }
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
                            world.AddPhysicsTask(new TNTTask(world, e.Coords, e.Player, false, true), 3000);
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
                            world.AddPhysicsTask(new SandTask(world, e.Coords, e.NewBlock), 150);
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
                        world.AddPhysicsTask(new BlockFloat(world, e.Coords, e.NewBlock), 200);
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
                        world.AddPhysicsTask(new BlockSink(world, e.Coords, e.NewBlock), 200);
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