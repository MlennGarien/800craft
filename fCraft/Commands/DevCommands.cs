using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LibNbt;
using LibNbt.Exceptions;
using LibNbt.Queries;
using LibNbt.Tags;

namespace fCraft
{
    static class DevCommands
    {

        public static void Init()
        {
            CommandManager.RegisterCommand(CdWrite);
            //CommandManager.RegisterCommand(CdBot);
            //CommandManager.RegisterCommand(CdSpell);
            // CommandManager.RegisterCommand(CdGame);
        }
        static readonly CommandDescriptor CdWrite = new CommandDescriptor
        {
            Name = "Write",
            Category = CommandCategory.Building,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            IsConsoleSafe = true,
            Help = "/Write message then click 2 blocks. The first is the starting point, the second is the direction",
            Handler = WriteHandler,
        };

        //TODO: Use SizeF and add a collection of fonts. Performance++
        static void WriteHandler(Player player, Command cmd)
        {
            string sentence = cmd.NextAll();
            if (sentence.Length < 1)
            {
                player.Message("&A/Write Sentence");
                return;
            }
            else
            {
                if (!File.Exists("font.png"))
                {
                    player.Message("The font file could not be found. Font.png needs to be in the server root.");
                    return;
                }
                FontHandler.Init("font.png");//move to system
                player.Message("Write: Click 2 blocks or use &H/Mark&S to set direction.");
                player.SelectionStart(2, WriteCallback, sentence, Permission.Draw);
            }
        }

        static void WriteCallback(Player player, Vector3I[] marks, object tag)
        {
            Block block = new Block();
            string sentence = (string)tag;
            if (sentence.Contains("g") || sentence.Contains("j") || sentence.Contains("q") || sentence.Contains("p") || sentence.Contains("y"))
            {
                marks[0].Z++;
            }
            if (player.LastUsedBlockType == Block.Undefined)
            {
                block = Block.Stone;
            }
            else
            {
                block = player.LastUsedBlockType;
            }
            FontHandler render = new FontHandler(block, marks, player.World, player);
            render.Render(sentence);
            if (render.blockCount > 0)
            {
                player.Message("/Write: Writing '{0}' using {1} blocks of {2}", sentence, render.blockCount, block.ToString());
            }
            else
            {
                player.Message("&WNo direction was set");
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

            if (GameMode.ToLower() == "zombie")
            {
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

        static readonly CommandDescriptor CdBot = new CommandDescriptor
        {
            Name = "Bot",
            Category = CommandCategory.Fun,
            Permissions = new[] { Permission.Chat },
            IsConsoleSafe = false,
            NotRepeatable = true,
            Usage = "/Spell",
            Help = "Penis",
            UsableByFrozenPlayers = false,
            Handler = BotHandler,
        };
        internal static void BotHandler(Player player, Command cmd)
        {
            Bot bot = player.Bot;
            string yes = cmd.Next();
            if (yes.ToLower() == "create")
            {
                string Name = cmd.Next();
                Position Pos = new Position(player.Position.X, player.Position.Y, player.Position.Z, player.Position.R, player.Position.L);
                player.Bot = new Bot(Name, Pos, 1, player.World);
                //player.Bot.SetBot();
            }
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
    }
}
