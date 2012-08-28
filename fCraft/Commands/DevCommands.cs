using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
//Copyright (C) <2012> <Jon Baker, Glenn Mariën and Lao Tszy>

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
namespace fCraft
{
    static class DevCommands
    {

        public static void Init()
        {
            CommandManager.RegisterCommand(CdWrite);
            CommandManager.RegisterCommand(CdSetFont);
            //CommandManager.RegisterCommand(CdFeed);
            //CommandManager.RegisterCommand(CdBot);
            //CommandManager.RegisterCommand(CdSpell);
            //CommandManager.RegisterCommand(CdGame);

            //Player.JoinedWorld += fCraft.Events.FeedEvents.PlayerJoiningWorld;
        }

        static CommandDescriptor CdSetFont = new CommandDescriptor()
        {
            Name = "SetFont",
            Aliases = new[] { "FontSet", "Font"},
            Category = CommandCategory.Building,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            IsConsoleSafe = false,
            Help = "Sets the properties for /Write",
            Handler = SetFontHandler,
            Usage = "/SetFont <Font | Size | Style> <Variable>"
        };

        static void SetFontHandler(Player player, Command cmd)
        {
            string Param = cmd.Next();
            string Variable = cmd.Next();
            if (Param == null || Variable == null)
            {
                CdSetFont.PrintUsage(player);
                return;
            }
            if (Param.ToLower() == "size")
            {
                int Size = -1;
                int.TryParse(Variable, out Size);
                if (Size == -1)
                {
                    player.Message("&WInvalid size, use a number");
                    return;
                }
                player.font = new System.Drawing.Font(player.font.FontFamily, Size);
                player.Message("Done");
            }
        }
        static CommandDescriptor CdFeed = new CommandDescriptor()
        {
            Name = "Feed",
            Category = CommandCategory.Building,
            Permissions = new Permission[] { Permission.EditPlayerDB },
            RepeatableSelection = true,
            IsConsoleSafe = false,
            Help = "/Feed then click two positions. Will create a 60 block feed in the direction of the 2nd block",
            Handler = FeedHandler,
        };

        static void FeedHandler(Player player, Command cmd)
        {
            string Option = cmd.Next();
            if (Option == null)
            {
                player.Message("Argument cannot be null, try /Feed <create | remove | list>");
                return;
            }
            if (!File.Exists("plugins/font.png"))
            {
                player.Message("The font file could not be found");
                return;
            }

            if (Option.ToLower() == "create")
            {
                player.Message("Feed: Click 2 blocks or use &H/Mark&S to set direction.");
                player.SelectionStart(2, FeedCallback, Option, Permission.Draw);
            }
            if (Option.ToLower() == "list")
            {
                FeedData[] list = FeedData.FeedList.OrderBy(feed => feed.Id).ToArray();
                if (list.Length == 0)
                {
                    player.Message("No feeds running.");
                }
                else
                {
                    player.Message("There are {0} feeds running:", list.Length);
                    foreach (FeedData data in list)
                    {
                        player.Message("  #{0} ({1},{2},{3}) World: {4}",
                                        data.Id, data.StartPos.X, data.StartPos.Y, data.StartPos.Z, data.world.ClassyName);
                    }
                }
                return;
            }

            if (Option.ToLower() == "remove" || Option.ToLower() == "stop")
            {
                int Id;
                if (cmd.NextInt(out Id))
                {
                    FeedData data = FeedData.FindFeedById(Id);
                    if (data == null)
                    {
                        player.Message("Given feed (#{0}) does not exist.", Id);
                    }
                    else
                    {
                        data.started = false;
                        FeedData.RemoveFeedFromList(data);
                        player.Message("Feed #" + Id + " has been removed");
                    }
                }
                else
                {
                    player.Message("&WUnable to remove any feeds. Try /Feed remove <ID>");
                }
                return;
            }
            else
            {
                player.Message("&WUnknown argument. Check /Help Feed");
                return;
            }
        }


        static void FeedCallback(Player player, Vector3I[] marks, object tag)
        {
            Direction direction = Direction.Null;
            if (Math.Abs(marks[1].X - marks[0].X) > Math.Abs(marks[1].Y - marks[0].Y))
            {
                if (marks[0].X < marks[1].X)
                {
                    direction = Direction.one;
                }
                else
                {
                    direction = Direction.two;
                }
            }
            else if (Math.Abs(marks[1].X - marks[0].X) < Math.Abs(marks[1].Y - marks[0].Y))
            {
                if (marks[0].Y < marks[1].Y)
                {
                    direction = Direction.three;
                }
                else
                {
                    direction = Direction.four;
                }
            }
            else return;
            FeedData data = new FeedData(Block.Lava, marks[0], "plugins/font.png", player.World, direction);
            data.StartPos = marks[0];
            int x1 = 0, y1 = 0, z1 = 0;
            switch (direction)
            {
                case Direction.one:
                    for (int x = data.StartPos.X; x < data.StartPos.X + 60; x++)
                    {
                        for (int z = data.StartPos.Z - 1; z < data.StartPos.Z + 9; z++)
                        {
                            player.World.Map.QueueUpdate(new BlockUpdate(null, (short)x, (short)data.StartPos.Y, (short)z, Block.Black));
                            x1 = x; z1 = z;
                        }
                    }
                    data.EndPos = new Vector3I(x1, marks[0].Y, z1);
                    data.FinishPos = new Vector3I(x1, marks[0].Y, z1);
                    break;

                case Direction.two:
                    for (int x = data.StartPos.X; x > data.StartPos.X - 60; x--)
                    {
                        for (int z = data.StartPos.Z - 1; z < data.StartPos.Z + 9; z++)
                        {
                            player.World.Map.QueueUpdate(new BlockUpdate(null, (short)x, (short)data.StartPos.Y, (short)z, Block.Black));
                            x1 = x; z1 = z;
                        }
                    }
                    data.EndPos = new Vector3I(x1, marks[0].Y, z1);
                    data.FinishPos = new Vector3I(x1, marks[0].Y, z1);
                    break;
                case Direction.three:
                    for (int y = data.StartPos.Y; y < data.StartPos.Y + 60; y++)
                    {
                        for (int z = data.StartPos.Z - 1; z < data.StartPos.Z + 9; z++)
                        {
                            player.World.Map.QueueUpdate(new BlockUpdate(null, (short)data.StartPos.X, (short)y, (short)z, Block.Black));
                            y1 = y; z1 = z;
                        }
                    }
                    data.EndPos = new Vector3I(marks[0].X, y1, z1);
                    data.FinishPos = new Vector3I(marks[0].X, y1, z1);
                    break;
                case Direction.four:
                    for (int y = data.StartPos.Y; y > data.StartPos.Y - 60; y--)
                    {
                        for (int z = data.StartPos.Z - 1; z < data.StartPos.Z + 9; z++)
                        {
                            player.World.Map.QueueUpdate(new BlockUpdate(null, (short)data.StartPos.X, (short)y, (short)z, Block.Black));
                            y1 = y; z1 = z;
                        }
                    }
                    data.EndPos = new Vector3I(marks[0].X, y1, z1);
                    data.FinishPos = new Vector3I(marks[0].X, y1, z1);
                    break;
            }
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
                player.Message("Write: Click 2 blocks or use &H/Mark&S to set direction.");
                player.SelectionStart(2, WriteCallback, sentence, Permission.Draw);
            }
        }

        static void WriteCallback(Player player, Vector3I[] marks, object tag)
        {
            Block block = new Block();
            string sentence = (string)tag;
            //block bugfix kinda
            if (player.LastUsedBlockType == Block.Undefined){
                block = Block.Stone;
            }else{
                block = player.LastUsedBlockType;
            }
            //find the direction (needs attention)
            Direction direction = Direction.Null;
            if (Math.Abs(marks[1].X - marks[0].X) > Math.Abs(marks[1].Y - marks[0].Y)){
                if (marks[0].X < marks[1].X){
                    direction = Direction.one;
                }else{
                    direction = Direction.two;
                }
            }else if (Math.Abs(marks[1].X - marks[0].X) < Math.Abs(marks[1].Y - marks[0].Y)){
                if (marks[0].Y < marks[1].Y){
                    direction = Direction.three;
                }else{
                    direction = Direction.four;
                }
            }
            FontHandler render = new FontHandler(block, marks, player, direction); //create new instance
            render.CreateGraphicsAndDraw(player, sentence); //render the sentence
            if (render.blockCount > 0){
                player.Message("/Write: Writing '{0}' using {1} blocks of {2}", sentence, render.blockCount, block.ToString());
            }else{
                player.Message("&WNo direction was set");
            }
            render = null; //get lost
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
