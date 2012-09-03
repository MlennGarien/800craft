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
    public class ShapesLib
    {
        public Player player; //player using command
        public int blockCount; //blockcount for player message. ++ when drawing
        int blocks = 0, //drawn blocks
            blocksDenied = 0; //denied blocks (zones, ect)
        fCraft.Drawing.UndoState undoState; //undostate
        Direction direction; //direction of the blocks (x++-- ect)
        Vector3I[] marks;
        int Radius = 0;

        //instance
        public ShapesLib(Block BlockColor, Vector3I[] Marks, Player p, int radius, Direction dir)
        {
            marks = Marks;
            direction = dir;
            blockCount = 0;
            player = p;
            PixelData.X = Marks[0].X;
            PixelData.Y = Marks[0].Y;
            PixelData.Z = Marks[0].Z;
            PixelData.BlockColor = BlockColor;
            undoState = player.DrawBegin(null);
            Radius = radius;
        }

        public void CreateGraphicsAndDraw()
        {
            DrawBitmap(Radius, Radius);
        }

        private void DrawBitmap(int Width, int Height)
        {
            // Determine the first rectangle's orientation and dimensions.
            double phi = (1 + Math.Sqrt(5)) / 2;
            RectOrientations orientation;
            int client_wid = Width;
            int client_hgt = Height;
            double wid, hgt;                // The rectangle's size.
            if (client_wid > client_hgt)
            {
                // Horizontal rectangle.
                orientation = RectOrientations.RemoveLeft;
                if (client_wid / (double)client_hgt > phi)
                {
                    hgt = client_hgt;
                    wid = hgt * phi;
                }
                else
                {
                    wid = client_wid;
                    hgt = wid / phi;
                }
            }
            else
            {
                // Vertical rectangle.
                orientation = RectOrientations.RemoveTop;
                if (client_hgt / (double)client_wid > phi)
                {
                    wid = client_wid;
                    hgt = wid * phi;
                }
                else
                {
                    hgt = client_hgt;
                    wid = hgt / phi;
                }
            }

            // Allow a margin.
            wid *= 0.9f;
            hgt *= 0.9f;

            // Center it.
            double x = (client_wid - wid) / 2;
            double y = (client_hgt - hgt) / 2;

            // Make the Bitmap.
            Bitmap bm = new Bitmap(client_wid, client_hgt);
            {

                // Draw the rectangles.
                using (Graphics gr = Graphics.FromImage(bm))
                {
                    gr.FillRectangle(Brushes.White, 0, 0, bm.Width, bm.Height);
                    // Draw the rectangles.
                    gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    List<PointF> points = new List<PointF>();
                    DrawPhiRectanglesOnGraphics(gr, points, x, y, wid, hgt, orientation);

                    // Draw the true spiral.
                    PointF start = points[0];
                    PointF origin = points[points.Count - 1];
                    float dx = start.X - origin.X;
                    float dy = start.Y - origin.Y;
                    double radius = Math.Sqrt(dx * dx + dy * dy);

                    double theta = Math.Atan2(dy, dx);
                    const int num_slices = 1000;
                    double dtheta = Math.PI / 2 / num_slices;
                    double factor = 1 - (1 / phi) / num_slices * 0.78; //@
                    List<PointF> new_points = new List<PointF>();

                    // Repeat until dist is too small to see.
                    while (radius > 0.1)
                    {
                        PointF new_point = new PointF(
                            (float)(origin.X + radius * Math.Cos(theta)),
                            (float)(origin.Y + radius * Math.Sin(theta)));
                        new_points.Add(new_point);
                        theta += dtheta;
                        radius *= factor;
                    }
                    gr.DrawLines(Pens.Blue, new_points.ToArray());
                }
                bm = Crop(bm);
                Draw(bm);
                bm.Dispose();
            }
        }

        private void DrawPhiRectanglesOnGraphics(Graphics gr, List<PointF> points, double x, double y, double wid, double hgt, RectOrientations orientation)
        {
            if ((wid < 1) || (hgt < 1)) return;
            RectangleF rect;
            switch (orientation)
            {
                case RectOrientations.RemoveLeft:
                    rect = new RectangleF(
                        (float)x, (float)y, (float)(2 * hgt), (float)(2 * hgt));
                    break;
                case RectOrientations.RemoveTop:
                    rect = new RectangleF(
                        (float)(x - wid), (float)y, (float)(2 * wid), (float)(2 * wid));
                    break;
                case RectOrientations.RemoveRight:
                    rect = new RectangleF(
                        (float)(x + wid - 2 * hgt),
                        (float)(y - hgt), (float)(2 * hgt), (float)(2 * hgt));
                    break;
                case RectOrientations.RemoveBottom:
                    rect = new RectangleF((float)x, (float)(y + hgt - 2 * wid),
                        (float)(2 * wid), (float)(2 * wid));
                    break;
            }


            // Recursively draw the next rectangle.
            switch (orientation)
            {
                case RectOrientations.RemoveLeft:
                    points.Add(new PointF((float)x, (float)(y + hgt)));
                    x += hgt;
                    wid -= hgt;
                    orientation = RectOrientations.RemoveTop;
                    break;
                case RectOrientations.RemoveTop:
                    points.Add(new PointF((float)x, (float)y));
                    y += wid;
                    hgt -= wid;
                    orientation = RectOrientations.RemoveRight;
                    break;
                case RectOrientations.RemoveRight:
                    points.Add(new PointF((float)(x + wid), (float)y));
                    wid -= hgt;
                    orientation = RectOrientations.RemoveBottom;
                    break;
                case RectOrientations.RemoveBottom:
                    points.Add(new PointF((float)(x + wid), (float)(y + hgt)));
                    hgt -= wid;
                    orientation = RectOrientations.RemoveLeft;
                    break;
            }
            DrawPhiRectanglesOnGraphics(gr, points, x, y, wid, hgt, orientation);
        }
        private enum RectOrientations
        {
            RemoveLeft,
            RemoveTop,
            RemoveRight,
            RemoveBottom
        }
        public void Draw(Bitmap img)
        {
            //guess how big the draw will be
            int Count = 0;
            for (int x = 0; x < img.Width; x++)
            {
                for (int z = 0; z < img.Height; z++)
                {
                    if (img.GetPixel(x, z).ToArgb() != System.Drawing.Color.White.ToArgb())
                    {
                        Count++;
                    }
                }
            }
            //check if player can make the drawing
            if (!player.CanDraw(Count))
            {
                player.MessageNow(String.Format("You are only allowed to run commands that affect up to {0} blocks. This one would affect {1} blocks.",
                                               player.Info.Rank.DrawLimit, Count));
                return;
            }
            //check direction and draw
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
                                      new Vector3I((PixelData.X + x), PixelData.Y, (PixelData.Z + z)), BlockChangeContext.Drawn,
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
                                      new Vector3I((PixelData.X - x), PixelData.Y, (PixelData.Z + z)), BlockChangeContext.Drawn,
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
                                      new Vector3I(PixelData.X, (PixelData.Y + y), (PixelData.Z + z)), BlockChangeContext.Drawn,
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
                                      new Vector3I(PixelData.X, ((PixelData.Y) - y), (PixelData.Z + z)), BlockChangeContext.Drawn,
                                      ref blocks, ref blocksDenied, undoState);
                                blockCount++;
                            }
                        }
                    }
                    break;
                default:
                    break; //if blockcount = 0, message is shown and returned
            }
        }
        #region Helpers
        public static Bitmap Crop(Bitmap bmp)
        {
            int w = bmp.Width;
            int h = bmp.Height;
            Func<int, bool> allWhiteRow = row =>
            {
                for (int i = 0; i < w; ++i)
                    if (bmp.GetPixel(i, row).R != 255)
                        return false;
                return true;
            };
            Func<int, bool> allWhiteColumn = col =>
            {
                for (int i = 0; i < h; ++i)
                    if (bmp.GetPixel(col, i).R != 255)
                        return false;
                return true;
            };
            int topmost = 0;
            for (int row = 0; row < h; ++row)
            {
                if (allWhiteRow(row))
                    topmost = row;
                else break;
            }
            int bottommost = 0;
            for (int row = h - 1; row >= 0; --row)
            {
                if (allWhiteRow(row))
                    bottommost = row;
                else break;
            }
            int leftmost = 0, rightmost = 0;
            for (int col = 0; col < w; ++col)
            {
                if (allWhiteColumn(col))
                    leftmost = col;
                else
                    break;
            }
            for (int col = w - 1; col >= 0; --col)
            {
                if (allWhiteColumn(col))
                    rightmost = col;
                else
                    break;
            }
            if (rightmost == 0) rightmost = w; // As reached left
            if (bottommost == 0) bottommost = h; // As reached top.
            int croppedWidth = rightmost - leftmost;
            int croppedHeight = bottommost - topmost;
            if (croppedWidth == 0)
            {// No border on left or right
                leftmost = 0;
                croppedWidth = w;
            }
            if (croppedHeight == 0)
            {// No border on top or bottom
                topmost = 0;
                croppedHeight = h;
            } try
            {
                var target = new Bitmap(croppedWidth, croppedHeight);
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(bmp,
                      new RectangleF(0, 0, croppedWidth, croppedHeight),
                      new RectangleF(leftmost, topmost, croppedWidth, croppedHeight),
                      GraphicsUnit.Pixel);
                }
                return target;
            }
            catch
            {
                return bmp; //return original image, I guess
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
        #endregion
    }
}
