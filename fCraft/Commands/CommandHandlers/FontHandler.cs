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
        public List<Block> last = new List<Block>();
        public Player player;
        public int blockCount;

        public const int spacebar = 4;
        public static List<bool>[] chars = new List<bool>[126 - 32];
        public static World world;
        public Vector3I[] marks;

        public FontHandler(Block textColor, Vector3I[] Marks, World world_, Player p)
        {
            blockCount = 0;
            world = world_;
            marks = Marks;
            player = p;
            PixelPos.X = marks[0].X;
            PixelPos.Y = marks[0].Y;
            PixelPos.Z = marks[0].Z;
            PixelPos.pixel = textColor;
            PixelPos.space = Block.Air;
        }

        //Init's the font upon server start-up, needs a If FileExists added
        public static void Init(string image)
        {
            List<List<bool>> pixels = new List<List<bool>>();
            Bitmap bmp = new Bitmap(image);
            for (int x = 0; x < bmp.Width; x++)
            {
                pixels.Add(new List<bool>());
                for (int y = 0; y < bmp.Height; y++)
                {
                    pixels[x].Add(bmp.GetPixel(x, y).GetBrightness() > 0.5);
                }
            }
            for (int ch = 33; ch < 126; ch++)
            {
                List<bool> charBlocks = new List<bool>();
                bool space = true;
                int OffsetX = ((ch - 32) % 16) * 8;
                int OffsetY = ((int)((ch - 32) / 16)) * 8;
                for (int X = 7; X >= 0; X--)
                {
                    for (int Y = 0; Y < 8; Y++)
                    {
                        bool type = pixels[X + OffsetX][Y + OffsetY];
                        if (space)
                        {
                            if (type)
                            {
                                Y = -1;
                                space = false;
                            }
                            continue;
                        }
                        else
                        {
                            charBlocks.Add(type);
                        }
                    }
                }
                charBlocks.Reverse();
                chars[ch - 32] = charBlocks;
            }
            chars[0] = new List<bool>();
            for (int i = 0; i < (spacebar - 2) * 8; i++)
            {
                chars[0].Add(false);
            }
        }

        public void Render(string text)
        {
            List<Block> buffer = new List<Block>();
            for (int pixel = 0; pixel < text.Length; pixel++)
            {
                char ch = text[pixel];
                List<bool> charTemp = chars[ch - 32];
                for (int i = 0; i < charTemp.Count; i++)
                {
                    if (charTemp[i])
                    {
                        buffer.Add(PixelPos.pixel);
                    }
                    else
                    {
                        buffer.Add(PixelPos.space);
                    }
                }
                if (pixel != text.Length - 1)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        buffer.Add(PixelPos.space);
                    }
                }
            }
           
            for (int i = 0; i < buffer.Count; i++)
            {
                if (i < last.Count && (last[i] == buffer[i]) && (getMapNextBlock(i) == buffer[i]))
                {
                    continue;
                }
                blockUpdate(i, buffer[i]);
            }
            last = buffer;
        }

        public void Render(string text, Block t)
        {
            Block temp = PixelPos.pixel;
            PixelPos.pixel = t;
            Render(text);
            PixelPos.pixel = temp;
        }

        public void blockUpdate(int index, Block type)
        {
            short x = 0;
            short y = 0;
            short z = 0;

            if (Math.Abs(marks[1].X - marks[0].X) > Math.Abs(marks[1].Y - marks[0].Y))
            {
                if (marks[0].X < marks[1].X)
                {
                    x = (short)(PixelPos.X + (index / 8));
                    y = (short)PixelPos.Y;
                    z = (short)(PixelPos.Z + (index % 8));
                }
                else
                {
                    x = (short)(PixelPos.X - (index / 8));
                    y = (short)PixelPos.Y;
                    z = (short)(PixelPos.Z + (index % 8));
                }
            }
            else if (Math.Abs(marks[1].X - marks[0].X) < Math.Abs(marks[1].Y - marks[0].Y))
            {
                if (marks[0].Y < marks[1].Y)
                {
                    x = (short)PixelPos.X;
                    y = (short)(PixelPos.Y + (index / 8));
                    z = (short)(PixelPos.Z + (index % 8));
                }
                else
                {
                    x = (short)PixelPos.X;
                    y = (short)(PixelPos.Y - (index / 8));
                    z = (short)(PixelPos.Z + (index % 8));
                }
            }
            else return;

            if (type != Block.Air) //exclude air / gaps in the cuboid
            {
                Player.RaisePlayerPlacedBlockEvent(player, 
                    world.Map, new Vector3I(x, y, z - 1), 
                    world.Map.GetBlock(x, y, z - 1), type, 
                    BlockChangeContext.Drawn);
                world.Map.QueueUpdate(new BlockUpdate(null, x, y, (short)(z - 1), type));
                blockCount++;
            }
        }

        public Block getMapNextBlock(int index)
        {
            return world.Map.GetBlock(PixelPos.X + (index / 8), PixelPos.Y, PixelPos.Z + (index % 8));
        }

        public struct PixelPos
        {
            public static int X;
            public static int Y;
            public static int Z;
            public static Block pixel;
            public static Block space; //air blocks
        }

        public void Clear()
        {
            last.Clear();
        }
    }
}