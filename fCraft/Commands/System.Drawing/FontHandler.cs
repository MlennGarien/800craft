using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace fCraft
{
    public class FontHandler
    {
        public Player player;
        public int blockCount;
        public Vector3I[] marks;
        int blocks = 0,
            blocksDenied = 0;
        fCraft.Drawing.UndoState undoState;
        Direction direction;

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
            SizeF size = MeasureTextSize(Sentence, player.font);
            using (Bitmap img = new Bitmap((int)size.Width + 1, (int)size.Height + 1))
            {
                using (Graphics g = Graphics.FromImage(img))
                {
                    g.FillRectangle(Brushes.White, 0, 0, img.Width, img.Height);
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.DrawString(Sentence, player.font, Brushes.Black, new PointF(0, 0));
                    Draw(img);
                }
            }
        }
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
            img.RotateFlip(RotateFlipType.Rotate180FlipX);
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
                                      new Vector3I(PixelData.X + x, PixelData.Y, (PixelData.Z + z) - 4), BlockChangeContext.Drawn,
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
                                      new Vector3I(PixelData.X - x, PixelData.Y, (PixelData.Z + z) - 4), BlockChangeContext.Drawn,
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
                                      new Vector3I(PixelData.X, PixelData.Y + y, (PixelData.Z + z) - 4), BlockChangeContext.Drawn,
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
                                      new Vector3I(PixelData.X, (PixelData.Y)  - y, (PixelData.Z + z) - 4), BlockChangeContext.Drawn,
                                      ref blocks, ref blocksDenied, undoState);
                                blockCount++;
                            }
                        }
                    }
                    break;
            }
        }

        public struct PixelData
        {
            public static int X;
            public static int Y;
            public static int Z;
            public static Block BlockColor;
        }


        #region DrawOneBlock
        static void DrawOneBlock(Player player, Map map, Block drawBlock, Vector3I coord,
                                 BlockChangeContext context, ref int blocks, ref int blocksDenied, fCraft.Drawing.UndoState undoState)
        {
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