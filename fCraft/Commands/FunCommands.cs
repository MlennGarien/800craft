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
            CommandManager.RegisterCommand(CdGame);
            CommandManager.RegisterCommand(CdFirework);
            CommandManager.RegisterCommand(CdLife);
            CommandManager.RegisterCommand(CdSpell);
        }

        static readonly CommandDescriptor CdSpell = new CommandDescriptor
        {
            Name = "Spell",
            Category = CommandCategory.Fun,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = false,
            NotRepeatable = true,
            Usage = "/Spell",
            Help = "Penis",
            UsableByFrozenPlayers = false,
            Handler = SpellHandler,
        };
        public static SpellStartBehavior particleBehavior = new SpellStartBehavior();
        internal static void SpellHandler(Player player, Command cmd)
        {
            World world = player.World;
            Vector3I pos1 = player.Position.ToBlockCoords();
            Random _r = new Random();
            int n = _r.Next(8, 12);
            for (int i = 0; i < n; ++i)
            {
                double phi = -_r.NextDouble() + -player.Position.L * 2 * Math.PI;
                double ksi = -_r.NextDouble() + player.Position.R * Math.PI - Math.PI / 2.0;

                Vector3F direction = (new Vector3F((float)(Math.Cos(phi) * Math.Cos(ksi)), (float)(Math.Sin(phi) * Math.Cos(ksi)), (float)Math.Sin(ksi))).Normalize();
                world.AddPhysicsTask(new Particle(world, (pos1 + 2 * direction).Round(), direction, player, Block.Obsidian, particleBehavior), 0);
            }
        }


        static readonly CommandDescriptor CdLife = new CommandDescriptor
        {
            Name = "Life",
            Category = CommandCategory.Fun,
            Permissions = new[] { Permission.DrawAdvanced },
            IsConsoleSafe = false,
            NotRepeatable = true,
            Usage = "/Life <command> [params]",
            Help = "Google Conwey's Game of Life\n'/Life help' for more usage info\n(c) 2012 LaoTszy",
            UsableByFrozenPlayers = false,
            Handler = LifeHandlerFunc,
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
            string GameMode = cmd.Next();
            string Option = cmd.Next();
            World world = player.World;
            /*if (world == WorldManager.MainWorld){
                player.Message("/Game cannot be used on the main world"); 
                return;
            }*/

            if(GameMode.ToLower() == "zombie"){
                if (Option.ToLower() == "start")
                {
                    ZombieGame.GetInstance(player.World);
                    ZombieGame.Start();
                    return;
                }
                else
                {
                    CdGame.PrintUsage(player);
                    return;
                }
            }
            if (GameMode.ToLower() == "minefield")
            {
                if (Option.ToLower() == "start")
                {
                    if (WorldManager.FindWorldExact("Minefield") != null)
                    {
                        player.Message("&WA game of Minefield is currently running and must first be stopped");
                        return;
                    }
                    MineField.GetInstance();
                    MineField.Start(player);
                    return;
                }
                else if (Option.ToLower() == "stop")
                {
                    if (WorldManager.FindWorldExact("Minefield") == null)
                    {
                        player.Message("&WA game of Minefield is currently not running");
                        return;
                    }
                    MineField.Stop(player, false);
                    return;
                }
                else
                {
                    CdGame.PrintUsage(player);
                    return;
                }
            }
            else
            {
                CdGame.PrintUsage(player);
                return;
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
        private static void LifeHandlerFunc(Player p, Command cmd)
        {
        	try
        	{
				LifeHandler.ProcessCommand(p, cmd);
        	}
        	catch (Exception e)
        	{
				Logger.Log(LogType.Error, "Error: " + e.Message);
        	}
        }

        
    }
}
