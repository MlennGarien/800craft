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

        public static void PlayerMoved(object sender, PlayerMovedEventArgs e)
        {
            if (e.Player.Info.InGame)
            {
                if (GameManager.BlueTeam.Count > 0)
                {
                    if (GameManager.GameIsOn)
                    {
                        foreach (Player B in GameManager.BlueTeam)
                        {
                            if (GameManager.BlueTeam.Contains(e.Player))
                            {
                                if (B.Position.ToVector3I() == e.Player.Position.ToVector3I())
                                {
                                    //kill b send to blue spawn
                                }
                            }
                        }
                    }
                }
            }

            if (GameManager.RedTeam.Count > 0)
            {
                if (e.Player.Info.InGame)
                {
                    if (GameManager.GameIsOn)
                    {
                        foreach (Player R in GameManager.RedTeam)
                        {
                            if (GameManager.BlueTeam.Contains(e.Player))
                            {
                                if (R.Position.ToVector3I() == e.Player.Position.ToVector3I())
                                {
                                    //kill R send to red spawn
                                }
                            }
                        }
                    }
                }
            }
            else return;
        }

        public static void GameChecker(World world)
        {
            if (GameManager.GameIsOn)
            {
                if (GameManager.BlueBaseCount == 6 && !GameManager.IsStopping)
                {
                    TFMinecraftHandler.Stop(Player.Console);
                    world.Players.Message("&SThe &CRed Team &Sloses. The &9Blue Team &Shave captured all the Red bases");
                    return;
                }
                if (GameManager.RedBaseCount == 6 && !GameManager.IsStopping)
                {
                    TFMinecraftHandler.Stop(Player.Console);
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
                                p.Message("Adding you to the &CRed &Steam!");
                                p.Info.InGame = true;
                            }
                            else if (GameManager.RedTeam.Count > GameManager.BlueTeam.Count)
                            {
                                GameManager.BlueTeam.Add(p);
                                p.Message("Adding you to the &9Blue &Steam!");
                                p.Info.InGame = true;
                            }
                            else
                            {
                                GameManager.RedTeam.Add(p);
                                p.Message("Adding you to the &9Blue &Steam!");
                                p.Info.InGame = true;
                            }
                        }
                    }

                    else
                    {
                        if (p.Info.InGame)
                        {
                            if (GameManager.BlueTeam.Contains(p))
                            {
                                GameManager.BlueTeam.Remove(p);
                                p.Message("You left the world, removing you from game.");
                                p.Info.InGame = false;
                            }

                            if (GameManager.RedTeam.Contains(p))
                            {
                                GameManager.RedTeam.Remove(p);
                                p.Message("You left the world, removing you from game.");
                                p.Info.InGame = false;
                            }
                        }
                    }
                }
            }
        }

        static readonly object Lock = new object();
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
                else return;
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
                                        CaptureBase(zone, e.Player);
                                        Red1Click = 0;
                                        zone.Name = "bluecaptured1";
                                        e.Player.World.Players.Message("The &9Blue Team &Scaptured &CRed Base 1");
                                        GameManager.BlueBaseCount++;
                                        GameManager.RedBaseCount--;
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
                                        CaptureBase(zone, e.Player);
                                        BlueCapturedClick = 0;
                                        zone.Name = "redbase1";
                                        e.Player.World.Players.Message("The &CRed Team &Stook back &CRed Base 1");
                                        GameManager.BlueBaseCount--;
                                        GameManager.RedBaseCount++;
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
                                        CaptureBase(zone, e.Player);
                                        Red2Click = 0;
                                        zone.Name = "bluecaptured2";
                                        e.Player.World.Players.Message("The &9Blue Team &Scaptured &CRed Base 2");
                                        GameManager.BlueBaseCount++;
                                        GameManager.RedBaseCount--;
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
                                        CaptureBase(zone, e.Player);
                                        BlueCapturedClick2 = 0;
                                        zone.Name = "redbase2";
                                        e.Player.World.Players.Message("The &CRed Team &Stook back &CRed Base 2");
                                        GameManager.BlueBaseCount--;
                                        GameManager.RedBaseCount++;
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
                                        CaptureBase(zone, e.Player);
                                        Red3Click = 0;
                                        zone.Name = "bluecaptured3";
                                        e.Player.World.Players.Message("The &9Blue Team &Scaptured &CRed Base 3");
                                        GameManager.BlueBaseCount++;
                                        GameManager.RedBaseCount--;
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
                                        CaptureBase(zone, e.Player);
                                        BlueCapturedClick3 = 0;
                                        zone.Name = "redbase3";
                                        e.Player.World.Players.Message("The &CRed Team &Stook back &CRed Base 2");
                                        GameManager.BlueBaseCount--;
                                        GameManager.RedBaseCount++;
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
                                        CaptureBase(zone, e.Player);
                                        Blue1Click = 0;
                                        zone.Name = "redcaptured1";
                                        e.Player.World.Players.Message("The &CRed Team &Scaptured &9Blue Base 1");
                                        GameManager.BlueBaseCount--;
                                        GameManager.RedBaseCount++;
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
                                        CaptureBase(zone, e.Player);
                                        RedCapturedClick = 0;
                                        zone.Name = "bluebase1";
                                        e.Player.World.Players.Message("The &9Blue Team &Stook back &9Blue Base 1");
                                        GameManager.BlueBaseCount++;
                                        GameManager.RedBaseCount--;
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
                                        CaptureBase(zone, e.Player);
                                        Blue2Click = 0;
                                        zone.Name = "redcaptured2";
                                        e.Player.World.Players.Message("The &CRed Team &Scaptured &9Blue Base 2");
                                        GameManager.BlueBaseCount--;
                                        GameManager.RedBaseCount++;
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
                                        CaptureBase(zone, e.Player);
                                        RedCapturedClick2 = 0;
                                        zone.Name = "bluebase2";
                                        e.Player.World.Players.Message("The &9Blue Team &Stook back &9Blue Base 2");
                                        GameManager.BlueBaseCount++;
                                        GameManager.RedBaseCount--;
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
                                        CaptureBase(zone, e.Player);
                                        Blue3Click = 0;
                                        zone.Name = "redcaptured3";
                                        e.Player.World.Players.Message("The &CRed Team &Scaptured &9Blue Base 3");
                                        GameManager.BlueBaseCount--;
                                        GameManager.RedBaseCount++;
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
                                        CaptureBase(zone, e.Player);
                                        RedCapturedClick3 = 0;
                                        zone.Name = "bluebase3";
                                        e.Player.World.Players.Message("The &9Blue Team &Stook back &9Blue Base 3");
                                        GameManager.BlueBaseCount++;
                                        GameManager.RedBaseCount--;
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

        static void CaptureBase(Zone zone, Player player)
        {
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
                        if(zone.Name.Contains("red"))
                        player.WorldMap.QueueUpdate(new BlockUpdate(Player.Console, new Vector3I(x, y, z), Block.Blue));
                        if (zone.Name.Contains("blue"))
                            player.WorldMap.QueueUpdate(new BlockUpdate(Player.Console, new Vector3I(x, y, z), Block.Red));
                        counter++;
                    }
                }
            }
        }
    }
}

