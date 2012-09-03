using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Net;
using System.IO;

namespace fCraft.Drawing
{
    public class DrawImg
    {
        public DrawImg() 
{

        }

        int blocks = 0, //drawn blocks
            blocksDenied = 0; //denied blocks (zones, ect)
        fCraft.Drawing.UndoState undoState; //undostate

        public void DrawImage(byte popType, Direction direct, Vector3I cpos, Player player, string url)
        {
            undoState = player.DrawBegin(null);
            Bitmap myBitmap = null;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            // Check that the remote file was found. The ContentType
            // check is performed since a request for a non-existent
            // image file might be redirected to a 404-page, which would
            // yield the StatusCode "OK", even though the image was not
            // found.
            if ((response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Moved ||
                response.StatusCode == HttpStatusCode.Redirect) &&
                response.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
            {
                // if the remote file was found, download oit
                using (Stream inputStream = response.GetResponseStream())
                {
                    myBitmap = new Bitmap(inputStream);
                }
            }
            if (myBitmap == null) return;
            myBitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            int direction = 0;
            if (direct == Direction.one) direction = 0;
            if (direct == Direction.two) direction = 1;
            if (direct == Direction.three) direction = 2;
            if (direct == Direction.four) direction = 3;
            bool layer = false;
            if (layer)
            {
                if (popType == 1) popType = 2;
                if (popType == 3) popType = 4;
            }
            List<ColorBlock> refCol = popRefCol(popType);
            ColorBlock colblock;
            double[] distance = new double[refCol.Count]; // Array of distances between color pulled from image to the referance colors.

            int position; // This is the block selector for when we find which distance is the shortest.

            for (int k = 0; k < myBitmap.Width; k++)
            {
                for (int i = 0; i < myBitmap.Height; i++)
                {
                    if (layer)
                    {
                        colblock.z = cpos.Z;
                        if (direction <= 1)
                        {
                            if (direction == 0) { colblock.x = (ushort)(cpos.X + k); colblock.y = (ushort)(cpos.Y - i); }
                            else { colblock.x = (ushort)(cpos.X - k); colblock.y = (ushort)(cpos.Y + i); }
                            //colblock.z = (ushort)(cpos.z - i);
                        }
                        else
                        {
                            if (direction == 2) { colblock.y = (ushort)(cpos.Y + k); colblock.x = (ushort)(cpos.X + i); }
                            else { colblock.y = (ushort)(cpos.Y - k); colblock.x = (ushort)(cpos.X - i); }
                            //colblock.x = (ushort)(cpos.x - i);
                        }
                    }
                    else
                    {
                        colblock.z = (ushort)(cpos.Z + i);
                        if (direction <= 1)
                        {

                            if (direction == 0) colblock.x = (ushort)(cpos.X + k);
                            else colblock.x = (ushort)(cpos.X - k);
                            colblock.y = cpos.Y;
                        }
                        else
                        {
                            if (direction == 2) colblock.y = (ushort)(cpos.Y + k);
                            else colblock.y = (ushort)(cpos.Y - k);
                            colblock.x = cpos.X;
                        }
                    }


                    colblock.r = myBitmap.GetPixel(k, i).R;
                    colblock.g = myBitmap.GetPixel(k, i).G;
                    colblock.b = myBitmap.GetPixel(k, i).B;
                    colblock.a = myBitmap.GetPixel(k, i).A;

                    if (popType == 6)
                    {
                        if ((colblock.r + colblock.g + colblock.b) / 3 < (256 / 4))
                        {
                            colblock.type = (byte)Block.Obsidian;
                        }
                        else if (((colblock.r + colblock.g + colblock.b) / 3) >= (256 / 4) && ((colblock.r + colblock.g + colblock.b) / 3) < (256 / 4) * 2)
                        {
                            colblock.type = (byte)Block.Black;
                        }
                        else if (((colblock.r + colblock.g + colblock.b) / 3) >= (256 / 4) * 2 && ((colblock.r + colblock.g + colblock.b) / 3) < (256 / 4) * 3)
                        {
                            colblock.type = (byte)Block.Gray;
                        }
                        else
                        {
                            colblock.type = (byte)Block.White;
                        }
                    }
                    else
                    {
                        for (int j = 0; j < distance.Length; j++) // Calculate distances between the colors in the image and the set referance colors, and store them.
                        {
                            distance[j] = Math.Sqrt(Math.Pow((colblock.r - refCol[j].r), 2) + Math.Pow((colblock.b - refCol[j].b), 2) + Math.Pow((colblock.g - refCol[j].g), 2));
                        }

                        position = 0;
                        double minimum = distance[0];
                        for (int h = 1; h < distance.Length; h++) // Find the smallest distance in the array of distances.
                        {
                            if (distance[h] < minimum)
                            {
                                minimum = distance[h];
                                position = h;
                            }
                        }


                        colblock.type = refCol[position].type; // Set the block we found closest to the image to the block we are placing.

                        if (popType == 1)
                        {
                            if (position <= 20)
                            {
                                if (direction == 0)
                                {
                                    colblock.y = (ushort)(colblock.y + 1);
                                }
                                else if (direction == 2)
                                {
                                    colblock.x = (ushort)(colblock.x - 1);
                                }
                                else if (direction == 1)
                                {
                                    colblock.y = (ushort)(colblock.y - 1);
                                }
                                else if (direction == 3)
                                {
                                    colblock.x = (ushort)(colblock.x + 1);
                                }
                            }
                        }
                        else if (popType == 3)
                        {
                            if (position <= 3)
                            {
                                if (direction == 0)
                                {
                                    colblock.y = (ushort)(colblock.y + 1);
                                }
                                else if (direction == 2)
                                {
                                    colblock.x = (ushort)(colblock.x - 1);
                                }
                                else if (direction == 1)
                                {
                                    colblock.y = (ushort)(colblock.y - 1);
                                }
                                else if (direction == 3)
                                {
                                    colblock.x = (ushort)(colblock.x + 1);
                                }
                            }
                        }
                    }

                    //ALPHA HANDLING (REAL HARD STUFF, YO)
                    if (colblock.a < 20) colblock.type = (byte)Block.Air;
                    DrawOneBlock(player, player.World.Map, (Block)colblock.type,
                                      new Vector3I((colblock.x), colblock.y, (colblock.z)), BlockChangeContext.Drawn,
                                      ref blocks, ref blocksDenied, undoState);
                    //FindReference.placeBlock(p.level, p, colblock.x, colblock.y, colblock.z, colblock.type);
                }
            }
        }
            
            

        public static List<ColorBlock> popRefCol(byte popType)
        {
            ColorBlock tempref = new ColorBlock();
            List<ColorBlock> refCol = new List<ColorBlock>();
            refCol.Clear();
            //FRONT LAYER BLOCKS
            if (popType == 1)   //poptype 1 = 2-layer color image
            {
                //FRONT LAYER BLOCKS
                tempref.r = 128;
                tempref.g = 86;
                tempref.b = 57;
                tempref.type = 3;
                refCol.Add(tempref);
                tempref.r = 162;
                tempref.g = 129;
                tempref.b = 75;
                tempref.type = 5;
                refCol.Add(tempref);
                tempref.r = 244;
                tempref.g = 237;
                tempref.b = 174;
                tempref.type = 12;
                refCol.Add(tempref);
                tempref.r = 226;
                tempref.g = 31;
                tempref.b = 38;
                tempref.type = 21;
                refCol.Add(tempref);
                tempref.r = 223;
                tempref.g = 135;
                tempref.b = 37;
                tempref.type = 22;
                refCol.Add(tempref);
                tempref.r = 230;
                tempref.g = 241;
                tempref.b = 25;
                tempref.type = 23;
                refCol.Add(tempref);
                tempref.r = 127;
                tempref.g = 234;
                tempref.b = 26;
                tempref.type = 24;
                refCol.Add(tempref);
                tempref.r = 25;
                tempref.g = 234;
                tempref.b = 20;
                tempref.type = 25;
                refCol.Add(tempref);
                tempref.r = 31;
                tempref.g = 234;
                tempref.b = 122;
                tempref.type = 26;
                refCol.Add(tempref);
                tempref.r = 27;
                tempref.g = 239;
                tempref.b = 225;
                tempref.type = 27;
                refCol.Add(tempref);
                tempref.r = 99;
                tempref.g = 166;
                tempref.b = 226;
                tempref.type = 28;
                refCol.Add(tempref);
                tempref.r = 111;
                tempref.g = 124;
                tempref.b = 235;
                tempref.type = 29;
                refCol.Add(tempref);
                tempref.r = 126;
                tempref.g = 34;
                tempref.b = 218;
                tempref.type = 30;
                refCol.Add(tempref);
                tempref.r = 170;
                tempref.g = 71;
                tempref.b = 219;
                tempref.type = 31;
                refCol.Add(tempref);
                tempref.r = 227;
                tempref.g = 39;
                tempref.b = 225;
                tempref.type = 32;
                refCol.Add(tempref);
                tempref.r = 234;
                tempref.g = 39;
                tempref.b = 121;
                tempref.type = 33;
                refCol.Add(tempref);
                tempref.r = 46;
                tempref.g = 68;
                tempref.b = 47;
                tempref.type = 34;
                refCol.Add(tempref);
                tempref.r = 135;
                tempref.g = 145;
                tempref.b = 130;
                tempref.type = 35;
                refCol.Add(tempref);
                tempref.r = 230;
                tempref.g = 240;
                tempref.b = 225;
                tempref.type = 36;
                refCol.Add(tempref);

                // BACK LAYER BLOCKS

                tempref.r = 57;
                tempref.g = 38;
                tempref.b = 25;
                tempref.type = 3;
                refCol.Add(tempref);
                tempref.r = 72;
                tempref.g = 57;
                tempref.b = 33;
                tempref.type = 5;
                refCol.Add(tempref);
                tempref.r = 109;
                tempref.g = 105;
                tempref.b = 77;
                tempref.type = 12;
                refCol.Add(tempref);
                tempref.r = 41;
                tempref.g = 31;
                tempref.b = 16;
                tempref.type = 17;
                refCol.Add(tempref);
                tempref.r = 101;
                tempref.g = 13;
                tempref.b = 16;
                tempref.type = 21;
                refCol.Add(tempref);
                tempref.r = 99;
                tempref.g = 60;
                tempref.b = 16;
                tempref.type = 22;
                refCol.Add(tempref);
                tempref.r = 102;
                tempref.g = 107;
                tempref.b = 11;
                tempref.type = 23;
                refCol.Add(tempref);
                tempref.r = 56;
                tempref.g = 104;
                tempref.b = 11;
                tempref.type = 24;
                refCol.Add(tempref);
                tempref.r = 11;
                tempref.g = 104;
                tempref.b = 8;
                tempref.type = 25;
                refCol.Add(tempref);
                tempref.r = 13;
                tempref.g = 104;
                tempref.b = 54;
                tempref.type = 26;
                refCol.Add(tempref);
                tempref.r = 12;
                tempref.g = 106;
                tempref.b = 100;
                tempref.type = 27;
                refCol.Add(tempref);
                tempref.r = 44;
                tempref.g = 74;
                tempref.b = 101;
                tempref.type = 28;
                refCol.Add(tempref);
                tempref.r = 49;
                tempref.g = 55;
                tempref.b = 105;
                tempref.type = 29;
                refCol.Add(tempref);
                tempref.r = 56;
                tempref.g = 15;
                tempref.b = 97;
                tempref.type = 30;
                refCol.Add(tempref);
                tempref.r = 75;
                tempref.g = 31;
                tempref.b = 97;
                tempref.type = 31;
                refCol.Add(tempref);
                tempref.r = 101;
                tempref.g = 17;
                tempref.b = 100;
                tempref.type = 32;
                refCol.Add(tempref);
                tempref.r = 104;
                tempref.g = 17;
                tempref.b = 54;
                tempref.type = 33;
                refCol.Add(tempref);
                tempref.r = 20;
                tempref.g = 30;
                tempref.b = 21;
                tempref.type = 34;
                refCol.Add(tempref);
                tempref.r = 60;
                tempref.g = 64;
                tempref.b = 58;
                tempref.type = 35;
                refCol.Add(tempref);
                tempref.r = 102;
                tempref.g = 107;
                tempref.b = 100;
                tempref.type = 36;
                refCol.Add(tempref);
                tempref.r = 0;
                tempref.g = 0;
                tempref.b = 0;
                tempref.type = 49;
                refCol.Add(tempref);
            }
            else if (popType == 2) // poptype 2 = 1 layer color image
            {
                tempref.r = 128;
                tempref.g = 86;
                tempref.b = 57;
                tempref.type = 3;
                refCol.Add(tempref);
                tempref.r = 162;
                tempref.g = 129;
                tempref.b = 75;
                tempref.type = 5;
                refCol.Add(tempref);
                tempref.r = 244;
                tempref.g = 237;
                tempref.b = 174;
                tempref.type = 12;
                refCol.Add(tempref);
                tempref.r = 93;
                tempref.g = 70;
                tempref.b = 38;
                tempref.type = 17;
                refCol.Add(tempref);
                tempref.r = 226;
                tempref.g = 31;
                tempref.b = 38;
                tempref.type = 21;
                refCol.Add(tempref);
                tempref.r = 223;
                tempref.g = 135;
                tempref.b = 37;
                tempref.type = 22;
                refCol.Add(tempref);
                tempref.r = 230;
                tempref.g = 241;
                tempref.b = 25;
                tempref.type = 23;
                refCol.Add(tempref);
                tempref.r = 127;
                tempref.g = 234;
                tempref.b = 26;
                tempref.type = 24;
                refCol.Add(tempref);
                tempref.r = 25;
                tempref.g = 234;
                tempref.b = 20;
                tempref.type = 25;
                refCol.Add(tempref);
                tempref.r = 31;
                tempref.g = 234;
                tempref.b = 122;
                tempref.type = 26;
                refCol.Add(tempref);
                tempref.r = 27;
                tempref.g = 239;
                tempref.b = 225;
                tempref.type = 27;
                refCol.Add(tempref);
                tempref.r = 99;
                tempref.g = 166;
                tempref.b = 226;
                tempref.type = 28;
                refCol.Add(tempref);
                tempref.r = 111;
                tempref.g = 124;
                tempref.b = 235;
                tempref.type = 29;
                refCol.Add(tempref);
                tempref.r = 126;
                tempref.g = 34;
                tempref.b = 218;
                tempref.type = 30;
                refCol.Add(tempref);
                tempref.r = 170;
                tempref.g = 71;
                tempref.b = 219;
                tempref.type = 31;
                refCol.Add(tempref);
                tempref.r = 227;
                tempref.g = 39;
                tempref.b = 225;
                tempref.type = 32;
                refCol.Add(tempref);
                tempref.r = 234;
                tempref.g = 39;
                tempref.b = 121;
                tempref.type = 33;
                refCol.Add(tempref);
                tempref.r = 46;
                tempref.g = 68;
                tempref.b = 47;
                tempref.type = 34;
                refCol.Add(tempref);
                tempref.r = 135;
                tempref.g = 145;
                tempref.b = 130;
                tempref.type = 35;
                refCol.Add(tempref);
                tempref.r = 230;
                tempref.g = 240;
                tempref.b = 225;
                tempref.type = 36;
                refCol.Add(tempref);
                tempref.r = 0;
                tempref.g = 0;
                tempref.b = 0;
                tempref.type = 49;
                refCol.Add(tempref);
            }
            else if (popType == 3) //2-Layer Gray Scale
            {
                //FRONT LAYER
                tempref.r = 46;
                tempref.g = 68;
                tempref.b = 47;
                tempref.type = 34;
                refCol.Add(tempref);
                tempref.r = 135;
                tempref.g = 145;
                tempref.b = 130;
                tempref.type = 35;
                refCol.Add(tempref);
                tempref.r = 230;
                tempref.g = 240;
                tempref.b = 225;
                tempref.type = 36;
                refCol.Add(tempref);
                //BACK LAYER
                tempref.r = 20;
                tempref.g = 30;
                tempref.b = 21;
                tempref.type = 34;
                refCol.Add(tempref);
                tempref.r = 60;
                tempref.g = 64;
                tempref.b = 58;
                tempref.type = 35;
                refCol.Add(tempref);
                tempref.r = 102;
                tempref.g = 107;
                tempref.b = 100;
                tempref.type = 36;
                refCol.Add(tempref);
                tempref.r = 0;
                tempref.g = 0;
                tempref.b = 0;
                tempref.type = 49;
                refCol.Add(tempref);
            }
            else if (popType == 4) //1-Layer grayscale
            {
                tempref.r = 46;
                tempref.g = 68;
                tempref.b = 47;
                tempref.type = 34;
                refCol.Add(tempref);
                tempref.r = 135;
                tempref.g = 145;
                tempref.b = 130;
                tempref.type = 35;
                refCol.Add(tempref);
                tempref.r = 230;
                tempref.g = 240;
                tempref.b = 225;
                tempref.type = 36;
                refCol.Add(tempref);
                tempref.r = 0;
                tempref.g = 0;
                tempref.b = 0;
                tempref.type = 49;
                refCol.Add(tempref);
            }
            else if (popType == 5) // Black and white 1 layer
            {
                tempref.r = 255;
                tempref.g = 255;
                tempref.b = 255;
                tempref.type = 36;
                refCol.Add(tempref);
                tempref.r = 0;
                tempref.g = 0;
                tempref.b = 0;
                tempref.type = 49;
                refCol.Add(tempref);
            }
            return refCol;
        }

        public struct ColorBlock
        {
            public int x, y, z; public byte type, r, g, b, a;
        }

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
    }
    
}
