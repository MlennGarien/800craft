using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;
using fCraft.Physics;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace fCraft.Physics
{
    class PlantPhysics
    {
        private static Thread plantThread;
        private static Thread checkGrass;

        /*#region drawimg
        public static void test(object sender, Events.PlayerPlacingBlockEventArgs e)
        {
            string url = "http://www.deviantart.com/download/182426032/cod_black_ops_game_icon_by_wolfangraul-d30m0ts.png";
            Bitmap img = processImg(url);
            World world = e.Player.World;
            Block block = Block.Black;
            plantThread = new Thread(new ThreadStart(delegate
              {
                  for (int x = 0; x < img.Height; x++)
                  {
                      for (int y = 0; y < img.Width; y++)
                      {
                          byte r = img.GetPixel(y, x).R;
                          byte g = img.GetPixel(y, x).G;
                          byte b = img.GetPixel(y, x).B;
                          if (r == 116 && g == 116 && b == 116) block = Block.Stone;
                          if (r == 121 && g == 85 && b == 58) block = Block.Dirt;
                          if (r == 106 && g == 106 && b == 106) block = Block.Cobblestone;
                          if (r == 144 && g == 115 && b == 72) block = Block.Wood;
                          if (r == 220 && g == 214 && b == 158) block = Block.Sand;
                          if (r == 108 && g == 94 && b == 95) block = Block.Gravel;
                          if (r == 82 && g == 66 && b == 39) block = Block.Log;
                          if (r == 82 && g == 66 && b == 39) block = Block.Sand;
                          if (r == 211 && g == 47 && b == 47) block = Block.Red;
                          if (r == 244 && g == 137 && b == 50) block = Block.Orange;
                          if (r == 193 && g == 193 && b == 43) block = Block.Yellow;
                          if (r == 139 && g == 228 && b == 51) block = Block.Lime;
                          if (r == 54 && g == 241 && b == 54) block = Block.Green;
                          if (r == 50 && g == 224 && b == 224) block = Block.Aqua;
                          if (r == 47 && g == 208 && b == 208) block = Block.Cyan;
                          if (r == 110 && g == 172 && b == 234) block = Block.Blue;
                          if (r == 121 && g == 121 && b == 224) block = Block.Magenta;
                          if (r == 120 && g == 44 && b == 196) block = Block.Indigo;
                          if (r == 166 && g == 70 && b == 211) block = Block.Violet;
                          if (r == 215 && g == 48 && b == 215) block = Block.Magenta;
                          if (r == 231 && g == 52 && b == 141) block = Block.Pink;
                          if (r == 71 && g == 71 && b == 71) block = Block.Black;
                          if (r == 138 && g == 138 && b == 138) block = Block.Gray;
                          if (r == 253 && g == 253 && b == 253) block = Block.White;
                          if (r == 201 && g == 185 && b == 57) block = Block.Gold;
                          if (r == 189 && g == 189 && b == 189) block = Block.Iron;
                          if (r == 11 && g == 11 && b == 18) block = Block.Obsidian;
                          if ((r + g + b) / 3 < (256 / 4))
                          {
                              block = Block.Obsidian;
                          }
                          else if (((r + g + b) / 3) >= (256 / 4) && ((r + g + b) / 3) < (256 / 4) * 2)
                          {
                              block = Block.Black;
                          }
                          else if (((r + g + b) / 3) >= (256 / 4) * 2 && ((r + g + b) / 3) < (256 / 4) * 3)
                          {
                              block = Block.Gray;
                          }
                          else
                          {
                              block = Block.White;
                          }
                          world.Map.QueueUpdate(new BlockUpdate(null, (short)e.Coords.X, (short)(e.Coords.Y + y), (short)(e.Coords.Z + x), block));
                      }
                  }
                  img.Dispose();
              })); plantThread.Start();
        }

        public static Bitmap processImg(string url)
        {
            WebClient client = new WebClient();
            Stream stream = client.OpenRead(url);
            Bitmap img = new Bitmap(ResizeImage(System.Drawing.Image.FromStream(stream), 50, 50, true));
            stream.Flush();
            stream.Close();
            client.Dispose();
            img.RotateFlip(RotateFlipType.Rotate180FlipNone);
            return img;
        }


        public static System.Drawing.Image ResizeImage(System.Drawing.Image FullsizeImage, int NewWidth, int MaxHeight, bool OnlyResizeIfWider)
        {
            if (OnlyResizeIfWider)
            {
                if (FullsizeImage.Width <= NewWidth)
                {
                    NewWidth = FullsizeImage.Width;
                }
            }

            int NewHeight = FullsizeImage.Height * NewWidth / FullsizeImage.Width;
            if (NewHeight > MaxHeight)
            {
                // Resize with height instead
                NewWidth = FullsizeImage.Width * MaxHeight / FullsizeImage.Height;
                NewHeight = MaxHeight;
            }

            System.Drawing.Image NewImage = FullsizeImage.GetThumbnailImage(NewWidth, NewHeight, null, IntPtr.Zero);

            // Clear 
            FullsizeImage.Dispose();

            // You mad bro?
            return NewImage;
            // He mad son...
        }
        #endregion
*/

        public static void blockSquash(object sender, PlayerPlacingBlockEventArgs e)
        {
            try
            {
                Player player = e.Player;
                World world = player.World;
                if (world != null && world.IsLoaded && world.plantPhysics)
                {
                    Vector3I z = new Vector3I(e.Coords.X, e.Coords.Y, e.Coords.Z - 1);
                    if (world.Map.GetBlock(z) == Block.Grass)
                    {
                        world.Map.QueueUpdate(new BlockUpdate(null, z, Block.Dirt));
                    }
                    else if (Physics.CanSquash(world.Map.GetBlock(z)))
                    {
                        e.Result = CanPlaceResult.Revert;
                        Player.RaisePlayerPlacedBlockEvent(player, world.Map, z, world.Map.GetBlock(z), e.NewBlock, BlockChangeContext.Physics);
                        world.Map.QueueUpdate(new BlockUpdate(null, z, e.NewBlock));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.SeriousError, "" + ex);
            }
        }
        public static void TreeGrowing(object sender, PlayerPlacingBlockEventArgs e)
        {
            try
            {
                World world = e.Player.World;
                if (!world.plantPhysics)
                    return;
                if (world.Map != null && world.IsLoaded)
                {
                    if (e.Context == BlockChangeContext.Manual)
                    {
                        if (e.NewBlock == Block.Plant)
                        {
                            Random rand = new Random();
                            int Height = rand.Next(4, 7);
                            for (int x = e.Coords.X; x < e.Coords.X + 5; x++)
                            {
                                for (int y = e.Coords.Y; y < e.Coords.Y + 5; y++)
                                {
                                    for (int z = e.Coords.Z; z < e.Coords.Z + Height + 1; z++)
                                    {
                                        if (world.Map.GetBlock(x, y, z) != Block.Air)
                                            return;
                                    }
                                }
                            }

                            for (int x = e.Coords.X; x > e.Coords.X - 5; x--)
                            {
                                for (int y = e.Coords.Y; y > e.Coords.Y - 5; y--)
                                {
                                    for (int z = e.Coords.Z; z < e.Coords.Z + Height + 1; z++)
                                    {
                                        if (world.Map.GetBlock(x, y, z) != Block.Air)
                                            return;
                                    }
                                }
                            }

                            plantThread = new Thread(new ThreadStart(delegate
                            {
                                Thread.Sleep(rand.Next(5000, 8000));
                                if (e.Player.WorldMap.GetBlock(e.Coords) == Block.Plant)
                                {
                                    string type = null;
                                    if (e.Player.WorldMap.GetBlock(e.Coords.X, e.Coords.Y, e.Coords.Z - 1) == Block.Grass)
                                    {
                                        type = "grass";
                                    }
                                    else if (e.Player.WorldMap.GetBlock(e.Coords.X, e.Coords.Y, e.Coords.Z - 1) == Block.Sand)
                                    {
                                        type = "sand";
                                    }
                                    else
                                    {
                                        return;
                                    }
                                    MakeTrunks(world, e.Coords, Height, type);
                                }
                            }));
                            plantThread.Start();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.SeriousError, "" + ex);
            }
        }


        public static void MakeTrunks(World w, Vector3I Coords, int Height, string type)
        {
            try
            {
                if (w.plantPhysics)
                {
                    if (w.Map != null && w.IsLoaded)
                    {
                        for (int i = 0; i < Height; i++)
                        {
                            if (w.Map != null && w.IsLoaded)
                            {
                                Thread.Sleep(Physics.Tick);
                                w.Map.QueueUpdate(new BlockUpdate(null, (short)Coords.X, (short)Coords.Y, (short)(Coords.Z + i), Block.Log));
                            }
                        }
                        if (type.Equals("grass"))
                        {
                            TreeGeneration.MakeNormalFoliage(w, Coords, Height + 1);
                        }
                        else if (type.Equals("sand"))
                        {
                            TreeGeneration.MakePalmFoliage(w, Coords, Height);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.SeriousError, "" + ex);
            }
        }


        public static void grassChecker(SchedulerTask task)
        {
            try
            {
                //imma put this here and be cheeky
                if ((Server.CPUUsageTotal * 100) >= 25 || Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024) > 1200)
                {
                    int count = 0;
                    foreach (World world in WorldManager.Worlds)
                    {
                        if (world.Map != null && world.IsLoaded)
                        {
                            if (world.waterPhysics || world.plantPhysics || world.fireworkPhysics ||
                                world.tntPhysics || world.sandPhysics || world.grassPhysics)
                            {
                                world.waterPhysics = false;
                                world.plantPhysics = false;
                                world.fireworkPhysics = false;
                                world.tntPhysics = false;
                                world.sandPhysics = false;
                                world.grassPhysics = false;
                                count++;
                            }
                        }
                    }
                    if (count > 0)
                    {
                        Server.Players.Message("&WPhysics has been shutdown on all worlds: High memory usage");
                    }
                }

                //grass physics
                if (checkGrass != null)
                {
                    if (checkGrass.ThreadState != System.Threading.ThreadState.Stopped) //stops multiple threads from opening
                    {
                        return;
                    }
                }
                checkGrass = new Thread(new ThreadStart(delegate
                {
                    checkGrass.Priority = ThreadPriority.Lowest;
                    foreach (World world in WorldManager.Worlds)
                    {
                        if (world.Map != null && world.IsLoaded) //for all loaded worlds
                        {
                            if (world.grassPhysics)
                            {
                                Map map = world.Map;
                                for (int x = world.Map.Bounds.XMin; x <= world.Map.Bounds.XMax; x++)
                                {
                                    if (world.Map != null && world.IsLoaded)
                                    {
                                        for (int y = world.Map.Bounds.YMin; y <= world.Map.Bounds.YMax; y++)
                                        {
                                            if (world.Map != null && world.IsLoaded)
                                            {
                                                for (int z = world.Map.Bounds.ZMin; z <= world.Map.Bounds.ZMax; z++)
                                                {
                                                    if (world.Map != null && world.IsLoaded)
                                                    {
                                                        if (world.grassPhysics)
                                                        {
                                                            if (Physics.CanPutGrassOn(new Vector3I(x, y, z), world)) //shadow detection
                                                            {
                                                                if (new Random().Next(1, 45) > new Random().Next(15, 35)) //random seed generation lolz
                                                                {
                                                                    map.QueueUpdate(new BlockUpdate(null,
                                                                        (short)x,
                                                                        (short)y,
                                                                        (short)z,
                                                                        Block.Grass));
                                                                }
                                                                Thread.Sleep(new Random().Next(3, 8)); //throttle, slow down horsey
                                                            } //0.3 - 0.7% cpu average, better than the original ~17%
                                                            //has not been tested with more than 5 maps loaded at once
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                })); checkGrass.Start();
            }
            catch (Exception ex) {
                Logger.Log(LogType.SeriousError, "" + ex);
            }
        }    
    }
}
