using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
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
            CommandManager.RegisterCommand(CdDraw2D);
            CommandManager.RegisterCommand(CdSetFont);
            //CommandManager.RegisterCommand(CdFeed);
            //CommandManager.RegisterCommand(CdBot);
            //CommandManager.RegisterCommand(CdSpell);
             //CommandManager.RegisterCommand(CdGame);
        }


        static string[] GetFontSectionList() {
            if( Directory.Exists( Paths.FontsPath ) ) {
                string[] sections = Directory.GetFiles( Paths.FontsPath, "*.ttf", SearchOption.TopDirectoryOnly )
                                             .Select( name => Path.GetFileNameWithoutExtension( name ) )
                                             .Where( name => !String.IsNullOrEmpty( name ) )
                                             .ToArray();
                if( sections.Length != 0 ) {
                    return sections;
                }
            }
            return null;
        }

        static CommandDescriptor CdSetFont = new CommandDescriptor()
        {
            Name = "SetFont",
            Aliases = new[] { "FontSet", "Font", "Sf"},
            Category = CommandCategory.Building,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            IsConsoleSafe = false,
            Help = "Sets the properties for /Write, such as: font, style and size",
            Handler = SetFontHandler,
            Usage = "/SetFont <Font | Size | Style> <Variable>"
        };

        static void SetFontHandler(Player player, Command cmd){
            string Param = cmd.Next();
            if (Param == null){
                CdSetFont.PrintUsage(player);
                return;
            }
            if (Param.ToLower() == "font"){
                string sectionName = cmd.NextAll();
                if (!Directory.Exists(Paths.FontsPath)){
                    Directory.CreateDirectory(Paths.FontsPath);
                    player.Message("There are no fonts available for this server. Font is set to default: {0}", player.font.FontFamily.Name);
                    return;
                }
                string fontFileName = null;
                string[] sectionFiles = Directory.GetFiles(Paths.FontsPath, "*.ttf", SearchOption.TopDirectoryOnly);
                if (sectionName.Length < 1){
                    var sectionList = GetFontSectionList();
                    player.Message("{0} Fonts Available: {1}", sectionList.Length, sectionList.JoinToString()); //print the folder contents
                    return;
                }
                for (int i = 0; i < sectionFiles.Length; i++){
                    string sectionFullName = Path.GetFileNameWithoutExtension(sectionFiles[i]);
                    if (sectionFullName == null) continue;
                    if (sectionFullName.StartsWith(sectionName, StringComparison.OrdinalIgnoreCase)){
                        if (sectionFullName.Equals(sectionName, StringComparison.OrdinalIgnoreCase)){
                            fontFileName = sectionFiles[i];
                            break;
                        }else if (fontFileName == null){
                            fontFileName = sectionFiles[i];
                        }else{
                            var matches = sectionFiles.Select(f => Path.GetFileNameWithoutExtension(f))
                                                      .Where(sn => sn != null && sn.StartsWith(sectionName, StringComparison.OrdinalIgnoreCase));
                            player.Message("Multiple font files matched \"{0}\": {1}",
                                            sectionName, matches.JoinToString());
                            return;
                        }
                    }
                }
                if (fontFileName != null){
                    string sectionFullName = Path.GetFileNameWithoutExtension(fontFileName);
                    player.Message("Your font has changed to \"{0}\":", sectionFullName);
                    //change font here
                    player.font = new System.Drawing.Font(player.LoadFontFamily(fontFileName), player.font.Size);
                }else{
                    var sectionList = GetFontSectionList();
                    if (sectionList == null){
                        player.Message("No fonts have been found.");
                    }else{
                        player.Message("No fonts found for \"{0}\". Available fonts: {1}",
                                        sectionName, sectionList.JoinToString());
                    }
                }
            }
            if (Param.ToLower() == "size"){
                int Size = -1;
                if (cmd.NextInt(out Size)){
                    if (Size > 48 || Size < 10){
                        player.Message("&WIncorrect size: Size needs to be between 10 and 48"); 
                        return;
                    }
                    player.Message("SetFont: Size changed from {0} to {1} ({2})", player.font.Size, Size, player.font.FontFamily.Name);
                    player.font = new System.Drawing.Font(player.font.FontFamily, Size);
                }else{
                    player.Message("&WInvalid size, use /SetFont Size FontSize. Example: /SetFont Size 14");
                    return;
                }
                return;
            }
            if (Param.ToLower() == "style"){
                string StyleType = cmd.Next();
                bool Append = false; //remove this, change to if(hasNext()) bold, italic, UL. if font.canhavestyle(bold) else message
                if (StyleType == null){
                    CdSetFont.PrintUsage(player);
                    return;
                }
                if (StyleType.StartsWith("+")){
                    Append = true;
                    StyleType = StyleType.Replace("+", "");
                }
                System.Drawing.FontStyle OldStyle = player.font.Style;
                if (StyleType.ToLower() == "bold" || StyleType.ToLower() == "b"){
                    if (Append){
                        player.font = new System.Drawing.Font(player.font, player.font.Style | System.Drawing.FontStyle.Bold);
                    }else{
                        player.font = new System.Drawing.Font(player.font, System.Drawing.FontStyle.Bold);
                    }
                }
                if (StyleType.ToLower() == "italic" || StyleType.ToLower() == "i"){
                    if (Append){
                        player.font = new System.Drawing.Font(player.font, player.font.Style | System.Drawing.FontStyle.Italic);
                    }else{
                        player.font = new System.Drawing.Font(player.font, System.Drawing.FontStyle.Italic);
                    }
                }
                if (StyleType.ToLower() == "underlined" || StyleType.ToLower() == "u"){
                    if (Append){
                        player.font = new System.Drawing.Font(player.font, player.font.Style | System.Drawing.FontStyle.Underline);
                    }else{
                        player.font = new System.Drawing.Font(player.font, System.Drawing.FontStyle.Underline);
                    }
                }else{
                    player.font = new System.Drawing.Font(player.font, System.Drawing.FontStyle.Regular);
                    
                }
                if (OldStyle == player.font.Style)
                {
                    player.Message("&WSetFont: Chosen style is not compatible with this font");
                    return;
                }
                player.Message("SetFont: Font Style changed from {0} to {1}", OldStyle, player.font.Style);
            }
        }

        static readonly CommandDescriptor CdDraw2D = new CommandDescriptor
        {
            Name = "Draw2D",
            Aliases = new[] { "D2d"},
            Category = CommandCategory.Building,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            IsConsoleSafe = true,
            Help = "/Draw2D, then select a shape (Polygon, spiral, star). You can then choose a radius "+
            " for the shape before selecting two points."+
            "Example: /Draw2d Polygon 50. Polygon and triangle can be used with any number of points "+
            "exceeding 3, which should follow the radius argument",
            Usage = "/Draw2D <Shape> <Radius> <Points> <Fill(true/false)>",
            Handler = Draw2DHandler,
        };

        static void Draw2DHandler(Player player, Command cmd)
        {
            string Shape = cmd.Next();
            if (Shape == null)
            {
                CdDraw2D.PrintUsage(player);
                return;
            }
            switch (Shape.ToLower())
            {
                case "polygon":
                case "star":
                case "spiral":
                    break;
                default:
                    CdDraw2D.PrintUsage(player);
                return;
            }
            int radius = 0;
            int Points = 0;
            if (!cmd.NextInt(out radius))
            {
                radius = 20;
            }
            if (!cmd.NextInt(out Points))
            {
                Points = 5;
            }
            bool fill = true;
            if (cmd.HasNext)
            {
                fill = bool.Parse(cmd.Next());
            }
            Draw2DData tag = new Draw2DData() { Shape = Shape, Points = Points, Radius = radius, Fill = fill };
            player.Message("Draw2D({0}): Click 2 blocks or use &H/Mark&S to set direction.", Shape);
            player.SelectionStart(2, Draw2DCallback, tag, Permission.Draw);
        }

        struct Draw2DData
        {
            public int Radius;
            public int Points;
            public string Shape;
            public bool Fill;
        }

        static void Draw2DCallback(Player player, Vector3I[] marks, object tag)
        {
            Block block = new Block();
            Draw2DData data = (Draw2DData)tag;
            int radius = data.Radius;
            int Points = data.Points;
            bool fill = data.Fill;
            string Shape = data.Shape;
            if (player.LastUsedBlockType == Block.Undefined)
            {
                block = Block.Stone;
            }
            else
            {
                block = player.LastUsedBlockType;
            }
            //find the direction (needs attention)
            Direction direction = DirectionFinder.GetDirection(marks); 
            try
            {
                ShapesLib lib = new ShapesLib(block, marks, player, radius, direction);
                switch (Shape.ToLower())
                {
                    case "polygon":
                        lib.DrawRegularPolygon(Points, 18, fill);
                        break;
                    case "star":
                        lib.DrawStar(Points, radius, fill);
                        break;
                    case "spiral":
                        lib.DrawSpiral();
                        break;
                    default:
                        player.Message("&WUnknown shape");
                        CdDraw2D.PrintUsage(player);
                        lib = null;
                        return;
                }
                
                if (lib.blockCount > 0)
                {
                    player.Message("/Draw2D: Drawing {0} with a radius '{1}' using {2} blocks of {3}",
                        Shape,
                        radius,
                        lib.blockCount,
                        block.ToString());
                }
                else
                {
                    player.Message("&WNo direction was set");
                }
                lib= null; //get lost
            }
            catch (Exception e)
            {
                player.Message(e.Message);
            }
        }
        static readonly CommandDescriptor CdWrite = new CommandDescriptor
        {
            Name = "Write",
            Aliases = new[]{ "Text", "Wt" },
            Category = CommandCategory.Building,
            Permissions = new Permission[] { Permission.DrawAdvanced },
            RepeatableSelection = true,
            IsConsoleSafe = false,
            Help = "/Write, then click 2 blocks. The first is the starting point, the second is the direction",
            Usage = "/Write Sentence",
            Handler = WriteHandler,
        };

        //TODO: add a collection of fonts. Performance++
        static void WriteHandler(Player player, Command cmd)
        {
            string sentence = cmd.NextAll();
            if (sentence.Length < 1){
                CdWrite.PrintUsage(player);
                return;
            }else{
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
            Direction direction = DirectionFinder.GetDirection(marks);
            try{
                Drawing.DrawImg img = new Drawing.DrawImg();
                img.DrawImage(1, direction, marks[0], player, "http://i.imgur.com/yBhoD.png");
                /*FontHandler render = new FontHandler(block, marks, player, direction); //create new instance
                render.CreateGraphicsAndDraw(sentence); //render the sentence
                if (render.blockCount > 0){
                    player.Message("/Write (Size {0}, Font {1}: Writing '{2}' using {3} blocks of {4}", 
                        player.font.Size, 
                        player.font.FontFamily.Name, 
                        sentence, render.blockCount, 
                        block.ToString());
                }else{
                    player.Message("&WNo direction was set");
                }
                render = null; //get lost*/
            }catch (Exception e){
                player.Message(e.Message);
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
                    ZombieGame game = new ZombieGame(player.World); //move to world
                    game.Start();
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
