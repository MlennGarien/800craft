using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;

namespace fCraft
{
    public static class GameEvents
    {
        public static int Red1Click = 0;
        public static int BlueCapturedClick = 0;

        public static int Red2Click = 0;
        public static int BlueCapturedClick2 = 0;

        public static int Red3Click = 0;
        public static int BlueCapturedClick3 = 0;

        public static int Blue1Click = 0;
        public static int RedCapturedClick = 0;

        public static int Blue2Click = 0;
        public static int RedCapturedClick2 = 0;

        public static int Blue3Click = 0;
        public static int RedCapturedClick3 = 0;

        public static void Shutdown(object sender, ShutdownEventArgs e)
        {
            if(GameManager.GameIsOn)
            TFMinecraftHandler.BaseRevert();
        }

        public static void GameChecker(World world)
        {
            if (GameManager.GameIsOn)
            {
                if (GameManager.BlueBaseCount == 6 && !GameManager.IsStopping)
                {
                    TFMinecraftHandler.Stop(Player.Console, true);
                    world.Players.Message("&SThe &CRed Team &Sloses. The &9Blue Team &Shave captured all the Red bases");
                    return;
                }
                if (GameManager.RedBaseCount == 6 && !GameManager.IsStopping)
                {
                    TFMinecraftHandler.Stop(Player.Console, true);
                    world.Players.Message("&SThe &9Blue Team &Sloses. The &CRed Team &Shave captured all the Blue bases");
                    return;
                }

                foreach (Player p in Server.Players)
                {
                    if (p.World == GameManager.GameWorld)
                    {
                        if (!p.Info.InGame)
                        {
                            if (GameManager.BlueTeam.Count == 0 && GameManager.RedTeam.Count == 0)
                            {
                                GameManager.RedTeam.Add(p);
                                p.Message("&SAdding you to the &CRed &Steam!");
                                p.Info.InGame = true;
                                p.Send(PacketWriter.MakeAddEntity(255, p.ListName, p.Position));
                                p.TeleportTo(GameManager.RedSpawn);
                                foreach (Player u in GameManager.GameWorld.Players)
                                {
                                    u.UpdateVisibleEntities();
                                }
                            }
                            else if (GameManager.RedTeam.Count > GameManager.BlueTeam.Count)
                            {
                                GameManager.BlueTeam.Add(p);
                                p.Message("&SAdding you to the &9Blue &Steam!");
                                p.Info.InGame = true;
                                p.Send(PacketWriter.MakeAddEntity(255, p.ListName, p.Position));
                                p.TeleportTo(GameManager.BlueSpawn);
                                foreach (Player u in GameManager.GameWorld.Players)
                                {
                                    u.UpdateVisibleEntities();
                                }
                            }
                            else
                            {
                                GameManager.RedTeam.Add(p);
                                p.Message("&SAdding you to the &CRed &Steam!");
                                p.Info.InGame = true;
                                p.Send(PacketWriter.MakeAddEntity(255, p.ListName, p.Position));
                                p.TeleportTo(GameManager.RedSpawn);
                                foreach (Player u in GameManager.GameWorld.Players)
                                {
                                    u.UpdateVisibleEntities();
                                }
                            }
                        }
                    }

                    else if (p.Info.InGame)
                    {
                        for (int j = 1; j < GameManager.GameWorld.Players.Length; j++)
                        {
                            Player Player2 = GameManager.GameWorld.Players[j];
                            if (GameManager.RedTeam.Contains(p) && p != Player2 && Player2 != null
                                && GameManager.BlueTeam.Contains(Player2))
                            {
                                Position p2 = Player2.Position;
                                Position d = new Position(
                                    (short)(p.Position.X - p2.X),
                                    (short)(p.Position.Y - p2.Y),
                                    (short)(p.Position.Z - p2.Z),
                                    (byte)0,
                                    (byte)0);

                                if (d.X * d.X + d.Y * d.Y <= 1296 && Math.Abs(d.Z) <= 52)
                                {
                                    Random random = new Random();
                                    int n = random.Next(1, 3);
                                    if (n == 1)
                                    {
                                        Player2.Message("{0}&S killed you", Color.Red + p.Name);
                                        p.Message("You killed {0}", Color.Blue + Player2.Name);
                                        Player2.TeleportTo(GameManager.BlueSpawn);
                                        return;
                                    }
                                    else
                                        p.Message("{0}&S killed you", Color.Blue + Player2.Name);
                                        Player2.Message("You killed {0}", Color.Red + p.Name);
                                        p.TeleportTo(GameManager.RedSpawn);
                                }
                            }
                        }
                    }

                    else if (p.Info.InGame && p.World != GameManager.GameWorld)
                    {
                        if (GameManager.BlueTeam.Contains(p))
                        {
                            GameManager.BlueTeam.Remove(p);
                            p.Message("&SYou left the world, removing you from game.");
                            p.Info.InGame = false;
                            p.Send(PacketWriter.MakeAddEntity(255, p.ListName, p.Position));
                            foreach (Player u in p.World.Players)
                            {
                                u.ResetVisibleEntities2();
                            }
                        }

                        if (GameManager.RedTeam.Contains(p))
                        {
                            GameManager.RedTeam.Remove(p);
                            p.Message("&SYou left the world, removing you from game.");
                            p.Info.InGame = false;
                            p.Send(PacketWriter.MakeAddEntity(255, p.ListName, p.Position));
                            foreach (Player u in p.World.Players)
                            {
                                u.ResetVisibleEntities2();
                            }
                        }
                    }
                }
            }
        }

        public static void PlayerDisconnected(object sender, PlayerDisconnectedEventArgs e)
        {
            if (GameManager.GameIsOn)
            {
                if (GameManager.BlueTeam.Contains(e.Player))
                {
                    GameManager.BlueTeam.Remove(e.Player);
                    e.Player.Info.InGame = false;
                }
                else if (GameManager.RedTeam.Contains(e.Player))
                {
                    GameManager.RedTeam.Remove(e.Player);
                    e.Player.Info.InGame = false;
                }
            }
        }
        public static void PlayerClicked(object sender, PlayerClickedEventArgs e)
        {
            if (GameManager.GameIsOn)
            {
                if (e.Player.World.Equals(GameManager.GameWorld))
                {
                    Zone[] allowed, denied;
                    if (e.Player.WorldMap.Zones.CheckDetailed(e.Coords, e.Player, out allowed, out denied))
                    {
                        foreach (Zone zone in denied)
                        {
                            if (zone.Name.EndsWith("redbase1"))
                            {
                                if (GameManager.BlueTeam.Contains(e.Player))
                                {
                                    Red1Click++;
                                    if (Red1Click > 29)
                                    {
                                        CaptureBase(zone, e.Player, "bluecaptured1");
                                        Red1Click = 0;
                                        e.Player.World.Players.Message("&SThe &9Blue Team &Scaptured &CRed Base 1");
                                        
                                    }
                                    return;
                                }

                                else if (GameManager.RedTeam.Contains(e.Player))
                                {
                                    if (Red1Click > 0)
                                        Red1Click--;
                                    return;
                                }
                            }

                            if (zone.Name.EndsWith("bluecaptured1"))
                            {
                                if (GameManager.RedTeam.Contains(e.Player))
                                {
                                    BlueCapturedClick++;
                                    if (BlueCapturedClick > 29)
                                    {
                                        CaptureBase(zone, e.Player, "redbase1");
                                        BlueCapturedClick = 0;
                                        e.Player.World.Players.Message("&SThe &CRed Team &Stook back &CRed Base 1");
                                    }
                                    return;
                                }

                                else if (GameManager.BlueTeam.Contains(e.Player))
                                {
                                    if (BlueCapturedClick > 0)
                                        BlueCapturedClick--;
                                    return;
                                }
                            }

                            //--------------------
                            if (zone.Name.EndsWith("redbase2"))
                            {
                                if (GameManager.BlueTeam.Contains(e.Player))
                                {
                                    Red2Click++;
                                    if (Red2Click > 29)
                                    {
                                        CaptureBase(zone, e.Player, "bluecaptured2");
                                        Red2Click = 0;
                                        e.Player.World.Players.Message("&SThe &9Blue Team &Scaptured &CRed Base 2");
                                    }
                                    return;
                                }

                                else if (GameManager.RedTeam.Contains(e.Player))
                                {
                                    if (Red2Click > 0)
                                        Red2Click--;
                                    return;
                                }
                            }

                            if (zone.Name.EndsWith("bluecaptured2"))
                            {
                                if (GameManager.RedTeam.Contains(e.Player))
                                {
                                    BlueCapturedClick2++;
                                    if (BlueCapturedClick2 > 29)
                                    {
                                        CaptureBase(zone, e.Player, "redbase2");
                                        BlueCapturedClick2 = 0;
                                        e.Player.World.Players.Message("&SThe &CRed Team &Stook back &CRed Base 2");
                                    }
                                    return;
                                }

                                else if (GameManager.BlueTeam.Contains(e.Player))
                                {
                                    if (BlueCapturedClick2 > 0)
                                        BlueCapturedClick2--;
                                    return;
                                }
                            }

                            //---------------
                            if (zone.Name.EndsWith("redbase3"))
                            {
                                if (GameManager.BlueTeam.Contains(e.Player))
                                {
                                    Red3Click++;
                                    if (Red3Click > 29)
                                    {
                                        CaptureBase(zone, e.Player, "bluecaptured3");
                                        Red3Click = 0;
                                        e.Player.World.Players.Message("&SThe &9Blue Team &Scaptured &CRed Base 3");
                                    }
                                    return;
                                }

                                else if (GameManager.RedTeam.Contains(e.Player))
                                {
                                    if (Red3Click > 0)
                                        Red3Click--;
                                    return;
                                }
                            }

                            if (zone.Name.EndsWith("bluecaptured3"))
                            {
                                if (GameManager.RedTeam.Contains(e.Player))
                                {
                                    BlueCapturedClick3++;
                                    if (BlueCapturedClick3 > 29)
                                    {
                                        CaptureBase(zone, e.Player, "redbase3");
                                        BlueCapturedClick3 = 0;
                                        e.Player.World.Players.Message("&SThe &CRed Team &Stook back &CRed Base 2");
                                    }
                                    return;
                                }

                                else if (GameManager.BlueTeam.Contains(e.Player))
                                {
                                    if (BlueCapturedClick3 > 0)
                                        BlueCapturedClick3--;
                                    return;
                                }
                            }

                            //----------blue----------

                            if (zone.Name.EndsWith("bluebase1"))
                            {
                                if (GameManager.RedTeam.Contains(e.Player))
                                {
                                    Blue1Click++;
                                    if (Blue1Click > 29)
                                    {
                                        CaptureBase(zone, e.Player, "redcaptured1");
                                        Blue1Click = 0;
                                        e.Player.World.Players.Message("&SThe &CRed Team &Scaptured &9Blue Base 1");
                                    }
                                    return;
                                }


                                else if (GameManager.BlueTeam.Contains(e.Player))
                                {
                                    if (Blue1Click > 0)
                                        Blue1Click--;
                                    return;
                                }
                            }

                            if (zone.Name.EndsWith("redcaptured1"))
                            {
                                if (GameManager.BlueTeam.Contains(e.Player))
                                {
                                    RedCapturedClick++;
                                    if (RedCapturedClick > 29)
                                    {
                                        CaptureBase(zone, e.Player, "bluebase1");
                                        RedCapturedClick = 0;
                                        e.Player.World.Players.Message("&SThe &9Blue Team &Stook back &9Blue Base 1");
                                    }
                                    return;
                                }

                                else if (GameManager.RedTeam.Contains(e.Player))
                                {
                                    if (RedCapturedClick > 0)
                                        RedCapturedClick--;
                                    return;
                                }
                            }

                            //--------------------
                            if (zone.Name.EndsWith("bluebase2"))
                            {
                                if (GameManager.RedTeam.Contains(e.Player))
                                {
                                    Blue2Click++;
                                    if (Blue2Click > 29)
                                    {
                                        CaptureBase(zone, e.Player, "redcaptured2");
                                        Blue2Click = 0;
                                        e.Player.World.Players.Message("&SThe &CRed Team &Scaptured &9Blue Base 2");
                                    }
                                    return;
                                }

                                else if (GameManager.BlueTeam.Contains(e.Player))
                                {
                                    if (Blue2Click > 0)
                                        Blue2Click--;
                                    return;
                                }
                            }

                            if (zone.Name.EndsWith("redcaptured2"))
                            {
                                if (GameManager.BlueTeam.Contains(e.Player))
                                {
                                    RedCapturedClick2++;
                                    if (RedCapturedClick2 > 29)
                                    {
                                        CaptureBase(zone, e.Player, "redcaptured2");
                                        RedCapturedClick2 = 0;
                                        e.Player.World.Players.Message("&SThe &9Blue Team &Stook back &9Blue Base 2");
                                    }
                                    return;
                                }

                                else if (GameManager.RedTeam.Contains(e.Player))
                                {
                                    if (RedCapturedClick2 > 0)
                                        RedCapturedClick2--;
                                    return;
                                }
                            }

                            //---------------
                            if (zone.Name.EndsWith("bluebase3"))
                            {
                                if (GameManager.RedTeam.Contains(e.Player))
                                {
                                    Blue3Click++;
                                    if (Blue3Click > 29)
                                    {
                                        CaptureBase(zone, e.Player, "redcaptured3");
                                        Blue3Click = 0;
                                        e.Player.World.Players.Message("&SThe &CRed Team &Scaptured &9Blue Base 3");
                                    }
                                    return;
                                }

                                else if (GameManager.BlueTeam.Contains(e.Player))
                                {
                                    if (Blue3Click > 0)
                                        Blue3Click--;
                                    return;
                                }
                            }

                            if (zone.Name.EndsWith("redcaptured3"))
                            {
                                if (GameManager.BlueTeam.Contains(e.Player))
                                {
                                    RedCapturedClick3++;
                                    if (RedCapturedClick3 > 29)
                                    {
                                        CaptureBase(zone, e.Player, "bluebase3");
                                        RedCapturedClick3 = 0;
                                        e.Player.World.Players.Message("&SThe &9Blue Team &Stook back &9Blue Base 3");
                                    }
                                    return;
                                }

                                else if (GameManager.RedTeam.Contains(e.Player))
                                {
                                    if (RedCapturedClick3 > 0)
                                        RedCapturedClick3--;
                                    return;
                                }
                            }

                            else return;
                        }
                    }
                }
            }
        }

        static void CaptureBase(Zone zone, Player player, string NewName)
        {
            zone.Name = NewName;
            if (NewName.Contains("bluebase") || NewName.Contains("bluecaptured"))
            {
                GameManager.BlueBaseCount++;
                GameManager.RedBaseCount--;
            }

            else if (NewName.Contains("redbase") || NewName.Contains("redcaptured"))
            {
                GameManager.BlueBaseCount--;
                GameManager.RedBaseCount++;
            }

            int sx = zone.Bounds.XMin;
            int ex = zone.Bounds.XMax;
            int sy = zone.Bounds.YMin;
            int ey = zone.Bounds.YMax;
            int sz = zone.Bounds.ZMin;
            int ez = zone.Bounds.ZMax;

            Block[] buffer = new Block[zone.Bounds.Volume];

            int counter = 0;
            for (int x = sx; x <= ex; x++)
            {
                for (int y = sy; y <= ey; y++)
                {
                    for (int z = sz; z <= ez; z++)
                    {
                        buffer[counter] = player.WorldMap.GetBlock(x, y, z);

                        if(NewName.Contains("blue"))
                        player.WorldMap.QueueUpdate(new BlockUpdate(Player.Console, new Vector3I(x, y, z), Block.Blue));

                        if (NewName.Contains("red"))
                            player.WorldMap.QueueUpdate(new BlockUpdate(Player.Console, new Vector3I(x, y, z), Block.Red));

                        counter++;
                    }
                }
            }
        }
    }
}

