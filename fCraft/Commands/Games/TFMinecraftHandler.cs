using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
    public static class TFMinecraftHandler
    {
        public static void Start(Player player, World world)
        {
            GameManager.GameWorld = world;
            Scheduler.NewTask(t => GameEvents.GameChecker(world)).RunForever(TimeSpan.FromSeconds(1));
            Server.Players.Message("Game started eloloedjfhfjf");
            GameManager.GameIsOn = true;
        }

        public static void Stop(Player player, bool Console)
        {
            World world = GameManager.GameWorld;
            
            if (!Console && !GameManager.GameIsOn)
            {
                player.Message("A game is not running on your world");
                return;
            }
            if (GameManager.GameIsOn)
            { 
                GameManager.GameIsOn = false;
                Scheduler.NewTask(t => GameEvents.GameChecker(world)).IsStopped = true;
                Scheduler.NewTask(t => GameEvents.GameChecker(world)).Stop().RunOnce(TimeSpan.FromSeconds(0));
                Scheduler.UpdateCache();
                GameManager.IsStopping = true;
               
                Server.Players.Message("Game Ended eloloedjfhfjf");
                foreach (Zone z in world.Map.Zones)
                {
                    if (z.Name.Contains("redcaptured1"))
                        z.Name = "bluebase1";
                    else if (z.Name.Contains("redcaptured2"))
                        z.Name = "bluebase2";
                    else if (z.Name.Contains("redcaptured3"))
                        z.Name = "bluebase3";
                    else if (z.Name.Contains("bluecaptured1"))
                        z.Name = "redbase1";
                    else if (z.Name.Contains("bluecaptured2"))
                        z.Name = "redbase2";
                    else if (z.Name.Contains("bluecaptured3"))
                        z.Name = "redbase3";

                    int sx = z.Bounds.XMin;
                    int ex = z.Bounds.XMax;
                    int sy = z.Bounds.YMin;
                    int ey = z.Bounds.YMax;
                    int sz = z.Bounds.ZMin;
                    int ez = z.Bounds.ZMax;

                    Block[] buffer = new Block[z.Bounds.Volume];
                    if (z.Name.Contains("bluebase")  || z.Name.Contains("bluecaptured"))
                    {
                        int counter = 0;
                        for (int x = sx; x <= ex; x++)
                        {
                            for (int y = sy; y <= ey; y++)
                            {
                                for (int zz = sz; zz <= ez; zz++)
                                {
                                    buffer[counter] = world.Map.GetBlock(x, y, zz);
                                    world.Map.QueueUpdate(new BlockUpdate(null, new Vector3I(x, y, zz), Block.Blue));
                                    counter++;
                                }
                            }
                        }
                    }

                    if (z.Name.Contains("redbase")  || z.Name.Contains("redcaptured"))
                    {
                        int counter = 0;
                        for (int x = sx; x <= ex; x++)
                        {
                            for (int y = sy; y <= ey; y++)
                            {
                                for (int zz = sz; zz <= ez; zz++)
                                {
                                    buffer[counter] = world.Map.GetBlock(x, y, zz);
                                    world.Map.QueueUpdate(new BlockUpdate(null, new Vector3I(x, y, zz), Block.Red));
                                    counter++;
                                }
                            }
                        }
                    }
                }
                
                GameEvents.Red1Click = 0;
                GameEvents.BlueCapturedClick = 0;
                GameEvents.Red2Click = 0;
                GameEvents.BlueCapturedClick2 = 0;
                GameEvents.Red3Click = 0;
                GameEvents.BlueCapturedClick3 = 0;
                GameEvents.Blue1Click = 0;
                GameEvents.RedCapturedClick = 0;
                GameEvents.Blue2Click = 0;
                GameEvents.RedCapturedClick2 = 0;
                GameEvents.Blue3Click = 0;
                GameEvents.RedCapturedClick3 = 0;

                GameManager.RedBaseCount = 3;
                GameManager.BlueBaseCount = 3;
                GameManager.GameWorld = null;
                GameManager.IsStopping = false;
                if (!Console)
                {
                    world.Players.Message("{0} ended the game", player.ClassyName);
                }

                if (GameManager.RedTeam.Count > 0)
                {
                    foreach (Player R in GameManager.RedTeam)
                    {
                        R.Info.InGame = false;
                        R.Message("Removing you from game");
                    }
                }
                if (GameManager.BlueTeam.Count > 0)
                {
                    foreach (Player B in GameManager.BlueTeam)
                    {
                        B.Info.InGame = false;
                        B.Message("Removing you from game");
                    }
                }
                GameManager.RedTeam.Clear();
                GameManager.BlueTeam.Clear();
                
                foreach (Player u in GameManager.GameWorld.Players)
                {
                    u.Send(PacketWriter.MakeAddEntity(255, u.ListName, u.Position));
                    
                }
                foreach (Player u in GameManager.GameWorld.Players)
                {
                    u.ResetVisibleEntities2();
                }
            }
        }

        public static void BaseRevert()
        {
            foreach (World w in WorldManager.Worlds)
            {
                if (w.Map.Zones.FindExact("redcaptured1") != null)
                    w.Map.Zones.FindExact("redcaptured1").Name = "bluebase1";
                if (w.Map.Zones.FindExact("redcaptured2") != null)
                    w.Map.Zones.FindExact("redcaptured2").Name = "bluebase2";
                if (w.Map.Zones.FindExact("redcaptured3") != null)
                    w.Map.Zones.FindExact("redcaptured3").Name = "bluebase3";

                if (w.Map.Zones.FindExact("bluecaptured1") != null)
                    w.Map.Zones.FindExact("bluecaptured1").Name = "redbase1";
                if (w.Map.Zones.FindExact("bluecaptured2") != null)
                    w.Map.Zones.FindExact("bluecaptured2").Name = "redbase2";
                if (w.Map.Zones.FindExact("bluecaptured3") != null)
                    w.Map.Zones.FindExact("bluecaptured3").Name = "redbase3";
            }
        }


        public static void BaseAdd(Player player, Vector3I[] marks, object tag)
        {
            int sx = Math.Min(marks[0].X, marks[1].X);
            int ex = Math.Max(marks[0].X, marks[1].X);
            int sy = Math.Min(marks[0].Y, marks[1].Y);
            int ey = Math.Max(marks[0].Y, marks[1].Y);
            int sh = Math.Min(marks[0].Z, marks[1].Z);
            int eh = Math.Max(marks[0].Z, marks[1].Z);

            int volume = (ex - sx + 1) * (ey - sy + 1) * (eh - sh + 1); //just incase

            Zone Base = (Zone)tag;
            Base.Create(new BoundingBox(marks[0], marks[1]), player.Info);
            player.WorldMap.Zones.Add(Base);
            player.Message("Base created: {0}x{1}x{2}", Base.Bounds.Dimensions.X,
                                                        Base.Bounds.Dimensions.Y,
                                                        Base.Bounds.Dimensions.Z);
            int ax = Base.Bounds.XMin;
            int bx = Base.Bounds.XMax;
            int ay = Base.Bounds.YMin;
            int by = Base.Bounds.YMax;
            int az = Base.Bounds.ZMin;
            int bz = Base.Bounds.ZMax;

            Block[] buffer = new Block[Base.Bounds.Volume];
            if (Base.Name.Contains("bluebase"))
            {
                int counter = 0;
                for (int x = ax; x <= bx; x++)
                {
                    for (int y = ay; y <= by; y++)
                    {
                        for (int zz = az; zz <= bz; zz++)
                        {
                            buffer[counter] = player.World.Map.GetBlock(x, y, zz);
                            player.World.Map.QueueUpdate(new BlockUpdate(null, new Vector3I(x, y, zz), Block.Blue));
                            counter++;
                        }
                    }
                }
            }

            if (Base.Name.Contains("redbase"))
            {
                int counter = 0;
                for (int x = ax; x <= bx; x++)
                {
                    for (int y = ay; y <= by; y++)
                    {
                        for (int zz = az; zz <= bz; zz++)
                        {
                            buffer[counter] = player.World.Map.GetBlock(x, y, zz);
                            player.World.Map.QueueUpdate(new BlockUpdate(null, new Vector3I(x, y, zz), Block.Red));
                            counter++;
                        }
                    }
                }
            }
            //setting up game tasks
            if (player.WorldMap.Zones.FindExact("redbase2") == null)
            {
                RedBase2(player);
                return;
            }
            if (player.WorldMap.Zones.FindExact("redbase3") == null)
            {
                RedBase3(player);
                return;
            }
            if (player.WorldMap.Zones.FindExact("bluebase1") == null)
            {
                BlueBase1(player);
                return;
            }
            if (player.WorldMap.Zones.FindExact("bluebase2") == null)
            {
                BlueBase2(player);
                return;
            }
            if (player.WorldMap.Zones.FindExact("bluebase3") == null)
            {
                BlueBase3(player);
                return;
            }
        }

       public static void RedBase2(Player player)
       {
           Zone BaseRed2 = new Zone();
           BaseRed2.Name = "redbase2";
           BaseRed2.Controller.MinRank = RankManager.HighestRank;
           foreach (PlayerInfo p in PlayerDB.PlayerInfoList.Where(t => t.Rank == RankManager.HighestRank))
               BaseRed2.Controller.Exclude(p);
           player.SelectionStart(2, TFMinecraftHandler.BaseAdd, BaseRed2, WorldCommands.CdBase.Permissions);
           player.Message("Red Base 2: Place 2 blocks to cuboid a Red base");
       }

       public static void RedBase3(Player player)
       {
           Zone BaseRed3 = new Zone();
           BaseRed3.Name = "redbase3";
           BaseRed3.Controller.MinRank = RankManager.HighestRank;
           foreach (PlayerInfo p in PlayerDB.PlayerInfoList.Where(t => t.Rank == RankManager.HighestRank))
               BaseRed3.Controller.Exclude(p);
           player.SelectionStart(2, TFMinecraftHandler.BaseAdd, BaseRed3, WorldCommands.CdBase.Permissions);
           player.Message("Red Base 3: Place 2 blocks to cuboid a Red base");
       }

      public static void BlueBase1(Player player)
       {
           Zone BaseBlue1 = new Zone();
           BaseBlue1.Name = "bluebase1";
           BaseBlue1.Controller.MinRank = RankManager.HighestRank;
           foreach (PlayerInfo p in PlayerDB.PlayerInfoList.Where(t => t.Rank == RankManager.HighestRank))
               BaseBlue1.Controller.Exclude(p);
           player.SelectionStart(2, TFMinecraftHandler.BaseAdd, BaseBlue1, WorldCommands.CdBase.Permissions);
           player.Message("Blue Base 1: Place 2 blocks to cuboid a Blue base");
       }

       public static void BlueBase2(Player player)
       {
           Zone BaseBlue2 = new Zone();
           BaseBlue2.Name = "bluebase2";
           BaseBlue2.Controller.MinRank = RankManager.HighestRank;
           foreach (PlayerInfo p in PlayerDB.PlayerInfoList.Where(t => t.Rank == RankManager.HighestRank))
               BaseBlue2.Controller.Exclude(p);
           player.SelectionStart(2, TFMinecraftHandler.BaseAdd, BaseBlue2, WorldCommands.CdBase.Permissions);
           player.Message("Blue Base 2: Place 2 blocks to cuboid a Blue base");
       }

       public static void BlueBase3(Player player)
       {
           Zone BaseBlue3 = new Zone();
           BaseBlue3.Name = "bluebase3";
           BaseBlue3.Controller.MinRank = RankManager.HighestRank;
           foreach (PlayerInfo p in PlayerDB.PlayerInfoList.Where(t => t.Rank == RankManager.HighestRank))
               BaseBlue3.Controller.Exclude(p);
           player.SelectionStart(2, TFMinecraftHandler.BaseAdd, BaseBlue3, WorldCommands.CdBase.Permissions);
           player.Message("Blue Base 3: Place 2 blocks to cuboid a Blue base");
       }
    }
}
