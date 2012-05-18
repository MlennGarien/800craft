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
using RandomMaze;

namespace fCraft
{
    internal static class FunCommands
    {
        internal static void Init()
        {
            CommandManager.RegisterCommand(CdRandomMaze);
            CommandManager.RegisterCommand(CdMazeCuboid);
            //CommandManager.RegisterCommand(CdGame);
            CommandManager.RegisterCommand(CdFirework);
            CommandManager.RegisterCommand(CdLife);
            CommandManager.RegisterCommand(CdStopLife);
            CommandManager.RegisterCommand(CdStartLife);
            CommandManager.RegisterCommand(CdKillLife);
        }

        static readonly CommandDescriptor CdLife = new CommandDescriptor
                {
                    Name = "Life",
                    Category = CommandCategory.Fun,
                    Permissions = new[] { Permission.DrawAdvanced },
                    IsConsoleSafe = false,
                    NotRepeatable = true,
                    Usage = "/Life <name> [empty block] [normal block] [dead block] [newborn block]",
                    Help = "Google Conwey's Game of Life\n(c) 2012 LaoTszy",
                    UsableByFrozenPlayers = false,
                    Handler = LifeHandler,
                };
        static readonly CommandDescriptor CdStopLife = new CommandDescriptor
        {
            Name = "StopLife",
            Category = CommandCategory.Fun,
            Permissions = new[] { Permission.DrawAdvanced },
            IsConsoleSafe = false,
            NotRepeatable = true,
            Usage = "/StopLife <LifeName>",
            Help = "Stops game of life with the given name\n(c) 2012 LaoTszy",
            UsableByFrozenPlayers = false,
            Handler = StopLifeHandler,
        };

        static readonly CommandDescriptor CdStartLife = new CommandDescriptor
        {
            Name = "StartLife",
            Category = CommandCategory.Fun,
            Permissions = new[] { Permission.DrawAdvanced },
            IsConsoleSafe = false,
            NotRepeatable = true,
            Usage = "/StartLife <LifeName>",
            Help = "Starts previously stopped game of life with the given name\n(c) 2012 LaoTszy",
            UsableByFrozenPlayers = false,
            Handler = StartLifeHandler,
        };

        static readonly CommandDescriptor CdKillLife = new CommandDescriptor
        {
            Name = "KillLife",
            Category = CommandCategory.Fun,
            Permissions = new[] { Permission.DrawAdvanced },
            IsConsoleSafe = false,
            NotRepeatable = true,
            Usage = "/KillLife <LifeName>",
            Help = "Removes game of life with the given name\n(c) 2012 LaoTszy",
            UsableByFrozenPlayers = false,
            Handler = RemoveLifeHandler,
        };


        static readonly CommandDescriptor CdFirework = new CommandDescriptor
        {
            Name = "Firework",
            Category = CommandCategory.Fun,
            Permissions = new[] { Permission.Fireworks },
            IsConsoleSafe = false,
            NotRepeatable = false,
            Usage = "/Firework",
            Help = "Toggles Firework Mode on/off for yourself. " +
            "All Gold blocks will be replaced with fireworks if " +
            "firework physics are enabled for the current world.",
            UsableByFrozenPlayers = false,
            Handler = FireworkHandler
        };

        static void FireworkHandler(Player player, Command cmd)
        {
            if (player.fireworkMode)
            {
                player.fireworkMode = false;
                player.Message("Firework Mode has been turned off.");
                return;
            }
            else
            {
                player.fireworkMode = true;
                player.Message("Firework Mode has been turned on. " +
                    "All Gold blocks are now being replaced with Fireworks.");
            }
        }


        static readonly CommandDescriptor CdGame = new CommandDescriptor
        {
            Name = "Game",
            Category = CommandCategory.World,
            Permissions = new Permission[] { Permission.Games },
            IsConsoleSafe = false,
            Usage = "/Unfinished command.",
            Handler = GameHandler
        };

        private static void GameHandler(Player player, Command cmd)
        {
            World world = player.World;
            if (world == WorldManager.MainWorld)
            {
                player.Message("/Game cannot be used on the main world"); return;
            }
            if (world.GameOn)
            {
                Games.MineChallenge.Stop(player);
            }
            else
            {
                Games.MineChallenge.Start(player, player.World);
            }
        }
        static readonly CommandDescriptor CdRandomMaze = new CommandDescriptor
        {
            Name = "RandomMaze",
            Aliases = new string[] { "3dmaze" },
            Category = CommandCategory.Fun,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help =
                "Choose the size (width, length and height) and it will draw a random maze at the chosen point. " +
                "Optional parameters tell if the lifts are to be drawn and if hint blocks (log) are to be added. \n(C) 2012 Lao Tszy",
            Usage = "/randommaze <width> <length> <height> [nolifts] [hints]",
            Handler = MazeHandler
        };
        static readonly CommandDescriptor CdMazeCuboid = new CommandDescriptor
        {
            Name = "MazeCuboid",
            Aliases = new string[] { "Mc", "Mz", "Maze" },
            Category = CommandCategory.Fun,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            Help =
                "Draws a cuboid with the current brush and with a random maze inside.(C) 2012 Lao Tszy",
            Usage = "/MazeCuboid [block type]",
            Handler = MazeCuboidHandler,
        };

        private static void MazeHandler(Player p, Command cmd)
        {
            try
            {
                RandomMazeDrawOperation op = new RandomMazeDrawOperation(p, cmd);
                BuildingCommands.DrawOperationBegin(p, cmd, op);
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, "Error: " + e.Message);
            }
        }
        private static void MazeCuboidHandler(Player p, Command cmd)
        {
            try
            {
                MazeCuboidDrawOperation op = new MazeCuboidDrawOperation(p);
                BuildingCommands.DrawOperationBegin(p, cmd, op);
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, "Error: " + e.Message);
            }
        }
        private static void LifeHandler(Player p, Command cmd)
        {
            string name = cmd.Next();
            if (String.IsNullOrWhiteSpace(name))
            {
                p.Message("&WLife zone name is missing or empty");
                return;
            }

            World w = p.World;
            if (null == w)
                return;

            lock (w.SyncRoot)
            {
                if (null == w.Map)
                    return;
                if (w.Map.LifeZones.ContainsKey(name.ToLower()))
                {
                    p.Message("&WLife zone with such name exists already, choose another name");
                    return;
                }
            }

            List<Block> l = new List<Block>();
            Block b = Block.Undefined;
            for (int i = 0; i < 4; ++i)
            {
                b = cmd.NextBlock(p);
                if (b == Block.Undefined)
                    b = Life2DZone.DefaultBlocks[i];
                l.Add(b);
            }

            p.SelectionStart(2, LifeCallback, new LifeCBData() { Name = name, Blocks = l }, Permission.DrawAdvanced);
            p.MessageNow("Life zone: Place a block or type /Mark to use your location.");
        }

        private struct LifeCBData
        {
            public string Name;
            public List<Block> Blocks;
        }
        static void LifeCallback(Player player, Vector3I[] marks, object state)
        {
            LifeCBData data = (LifeCBData)state;
            List<Block> l = data.Blocks;

            try
            {
                World w = player.World;
                if (null == w)
                    return;

                lock (w.SyncRoot)
                {
                    if (null == w.Map)
                        return;
                    if (w.Map.LifeZones.ContainsKey(data.Name.ToLower()))
                    {
                        player.Message("&WLife zone with such name exists already, choose another name");
                        return;
                    }
                    player.World.StartScheduler(TaskCategory.Scripting);
                    Life2DZone zone = new Life2DZone(player.World, marks, l[Life2DZone.EmptyIdx], l[Life2DZone.NormalIdx],
                                                     l[Life2DZone.DeadIdx], l[Life2DZone.NewbornIdx], Life2DZone.DefaultHalfStepDelay,
                                                     Life2DZone.DefaultDelay) { Name = data.Name };
                    w.Map.LifeZones.Add(data.Name, zone);
                    zone.Start();
                }

            }
            catch (Exception e)
            {
                player.Message("&WLife error: " + e.Message);
            }

        }

        private static void StopLifeHandler(Player p, Command cmd)
        {
            string name = cmd.Next();
            if (String.IsNullOrWhiteSpace(name))
            {
                p.Message("&WLife zone name is missing or empty");
                return;
            }

            World w = p.World;
            if (null == w)
                return;

            Life2DZone life;
            lock (w.SyncRoot)
            {
                if (null == w.Map)
                    return;

                if (!w.Map.LifeZones.TryGetValue(name.ToLower(), out life))
                {
                    p.Message("&WCan not find life " + name);
                    return;
                }
            }
            life.Stop();
            p.Message("Life " + name + " was stopped");
        }

        private static void StartLifeHandler(Player p, Command cmd)
        {
            string name = cmd.Next();
            if (String.IsNullOrWhiteSpace(name))
            {
                p.Message("&WLife zone name is missing or empty");
                return;
            }

            World w = p.World;
            if (null == w)
                return;

            Life2DZone life;
            lock (w.SyncRoot)
            {
                if (null == w.Map)
                    return;

                if (!w.Map.LifeZones.TryGetValue(name.ToLower(), out life))
                {
                    p.Message("&WCan not find life " + name);
                    return;
                }
            }
            life.Start();
            p.Message("Life " + name + " was started");
        }

        private static void RemoveLifeHandler(Player p, Command cmd)
        {
            string name = cmd.Next();
            if (String.IsNullOrWhiteSpace(name))
            {
                p.Message("&WLife zone name is missing or empty");
                return;
            }

            World w = p.World;
            if (null == w)
                return;

            Life2DZone life;
            lock (w.SyncRoot)
            {
                if (null == w.Map)
                    return;

                if (!w.Map.LifeZones.TryGetValue(name.ToLower(), out life))
                {
                    p.Message("&WCan not find life " + name);
                    return;
                }
                life.Stop();
                w.Map.LifeZones.Remove(name.ToLower());
            }

            p.Message("Life " + name + " was killed");
        }
    }
}
