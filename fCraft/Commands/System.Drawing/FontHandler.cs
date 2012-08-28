using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

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
    public class FontHandler
    {
        public Player player; //player using command
        public int blockCount; //blockcount for player message. ++ when drawing
        public Vector3I[] marks; //the marks. [0] is 1st position, [1] is 2nd
        int blocks = 0, //drawn blocks
            blocksDenied = 0; //denied blocks (zones, ect)
        fCraft.Drawing.UndoState undoState; //undostate
        Direction direction; //direction of the blocks (x++-- ect)

        //instance
        public FontHandler(Block textColor, Vector3I[] Marks, Player p, Direction dir)
        {
            direction = dir;
            blockCount = 0;
            marks = Marks;
            player = p;
            PixelData.X = marks[0].X;
            PixelData.Y = marks[0].Y;
            PixelData.Z = marks[0].Z;
            PixelData.BlockColor = textColor;
            undoState = player.DrawBegin(null);
        }

        public void CreateGraphicsAndDraw(Player player, string Sentence)
        {
            SizeF size = MeasureTextSize(Sentence, player.font); //measure the text size to create a bmp)
            using (Bitmap img = new Bitmap((int)size.Width, (int)size.Height)) //IDisposable
            {
                using (Graphics g = Graphics.FromImage(img))
                {
                    g.FillRectangle(Brushes.White, 0, 0, img.Width, img.Height); //make background
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel; //fix to bleeding
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; //not sure if this helps
                    g.DrawString(Sentence, player.font, Brushes.Black, 0, 0); //draw some sexytext
                    Draw(img); //make the blockupdates
                }
            }
        }
        //Measure the size of the string length using IDisposable
        public static SizeF MeasureTextSize(string text, Font font)
        {
            using (Bitmap bmp = new Bitmap(1, 1))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    return g.MeasureString(text, font);
                }
            }
        }

        public void Draw(Bitmap img)
        {
            //TODO gather width offset
            img.RotateFlip(RotateFlipType.Rotate180FlipX); //flip image
            //Check for offsets, due to crappy rotationflip
            int HeightOffset = 0;
            for (int x = 0; x < img.Width; x++)
            {
                for (int y = 0; y < img.Height; y++)
                {
                    if (img.GetPixel(x, y).ToArgb() != System.Drawing.Color.White.ToArgb())
                    {
                        HeightOffset = y;
                        break;
                    }
                }
            }
            switch (direction)
            {
                case Direction.one:
                    for (int x = 0; x < img.Width; x++)
                    {
                        for (int z = 0; z < img.Height; z++)
                        {
                            if (img.GetPixel(x, z).ToArgb() != System.Drawing.Color.White.ToArgb())
                            {
                                DrawOneBlock(player, player.World.Map, PixelData.BlockColor,
                                      new Vector3I(PixelData.X + x, PixelData.Y, (PixelData.Z + z) - HeightOffset), BlockChangeContext.Drawn,
                                      ref blocks, ref blocksDenied, undoState);
                                blockCount++;
                            }
                        }
                    }
                    break;
                case Direction.two:
                    for (int x = 0; x < img.Width; x++)
                    {
                        for (int z = 0; z < img.Height; z++)
                        {
                            if (img.GetPixel(x, z).ToArgb() != System.Drawing.Color.White.ToArgb())
                            {
                                DrawOneBlock(player, player.World.Map, PixelData.BlockColor,
                                      new Vector3I(PixelData.X - x, PixelData.Y, (PixelData.Z + z) - HeightOffset), BlockChangeContext.Drawn,
                                      ref blocks, ref blocksDenied, undoState);
                                blockCount++;
                            }
                        }
                    }
                    break;
                case Direction.three:
                    for (int y = 0; y < img.Width; y++)
                    {
                        for (int z = 0; z < img.Height; z++)
                        {
                            if (img.GetPixel(y, z).ToArgb() != System.Drawing.Color.White.ToArgb())
                            {
                                DrawOneBlock(player, player.World.Map, PixelData.BlockColor,
                                      new Vector3I(PixelData.X, PixelData.Y + y, (PixelData.Z + z) - HeightOffset), BlockChangeContext.Drawn,
                                      ref blocks, ref blocksDenied, undoState);
                                blockCount++;
                            }
                        }
                    }
                    break;
                case Direction.four:
                    for (int y = 0; y < img.Width; y++)
                    {
                        for (int z = 0; z < img.Height; z++)
                        {
                            if (img.GetPixel(y, z).ToArgb() != System.Drawing.Color.White.ToArgb())
                            {
                                DrawOneBlock(player, player.World.Map, PixelData.BlockColor,
                                      new Vector3I(PixelData.X, (PixelData.Y)  - y, (PixelData.Z + z) - HeightOffset), BlockChangeContext.Drawn,
                                      ref blocks, ref blocksDenied, undoState);
                                blockCount++;
                            }
                        }
                    }
                    break;
            }
        }
        //stores information needed for each pixel
        public struct PixelData
        {
            public static int X;
            public static int Y;
            public static int Z;
            public static Block BlockColor;
        }

        //stolen from BuildingCommands
        #region DrawOneBlock
        static void DrawOneBlock(Player player, Map map, Block drawBlock, Vector3I coord,
                                 BlockChangeContext context, ref int blocks, ref int blocksDenied, fCraft.Drawing.UndoState undoState)
        {
            if (map == null) return;
            if (player == null) throw new ArgumentNullException("player");

            if (!map.InBounds(coord)) return;
            Block block = map.GetBlock(coord);
            if (block == drawBlock) return;

            if (player.CanPlace(map, coord, drawBlock, context) != CanPlaceResult.Allowed)
            {
                blocksDenied++;
                return;
            }

            map.QueueUpdate(new BlockUpdate(null, coord, drawBlock));
            Player.RaisePlayerPlacedBlockEvent(player, map, coord, block, drawBlock, context);

            if (!undoState.IsTooLargeToUndo)
            {
                if (!undoState.Add(coord, block))
                {
                    player.Message("NOTE: This draw command is too massive to undo.");
                    player.LastDrawOp = null;
                }
            }
            blocks++;
        }
        #endregion
    }
}