using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Text;
using System.Collections;

namespace fCraft.Portals
{
    class PortalHandler
    {
        private static PortalHandler instance;

        private PortalHandler()
        {
            // Empty, singleton
        }

        public static PortalHandler GetInstance()
        {
            if (instance == null)
            {
                instance = new PortalHandler();
                Player.Moved += new EventHandler<Events.PlayerMovedEventArgs>(Player_Moved);
                PortalDB.StartSaveTask();
            }

            return instance;
        }

        static void Player_Moved(object sender, Events.PlayerMovedEventArgs e)
        {
            if ((e.OldPosition.X != e.NewPosition.X) || (e.OldPosition.Y != e.NewPosition.Y) || (e.OldPosition.Z != (e.NewPosition.Z)))
            {
                if (e.Player.Can(Permission.UsePortal))
                {
                    if (PortalHandler.GetInstance().GetPortal(e.Player) != null && !e.Player.StandingInPortal)
                    {
                        e.Player.StandingInPortal = true;
                        Portal portal = PortalHandler.GetInstance().GetPortal(e.Player);

                        // Teleport player
                        e.Player.JoinWorldNow(WorldManager.FindWorldExact(portal.World), true, WorldChangeReason.Portal);
                    }
                    else
                    {
                        e.Player.StandingInPortal = false;
                    }
                }
            }
        }

        public Portal GetPortal(Player player)
        {
            Portal portal = null;

            try
            {
                if (player.World.Portals != null && player.World.Portals.Count > 0)
                {
                    lock (player.World.Portals.SyncRoot)
                    {
                        foreach (Portal possiblePortal in player.World.Portals)
                        {
                            if (possiblePortal.IsInRange(player))
                            {
                                return possiblePortal;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, "PortalHandler.GetPortal: " + ex);
            }

            return portal;
        }

        public static void CreatePortal(Portal portal)
        {
            World world = WorldManager.FindWorldExact(portal.World);

            if (world.Portals == null)
            {
                world.Portals = new ArrayList();
            }

            lock (world.Portals.SyncRoot)
            {
                world.Portals.Add(portal);
            }
        }

        public static bool IsInRangeOfSpawnpoint(World world, Vector3I block)
        {
            try
            {
                int Xdistance = (world.Map.Spawn.X / 32) - block.X;
                int Ydistance = (world.Map.Spawn.Y / 32) - block.Y;
                int Zdistance = (world.Map.Spawn.Z / 32) - block.Z;

                Server.Message("{0},{1},{2}", Xdistance, Ydistance, Zdistance);

                if (Xdistance <= 10 && Xdistance >= -10)
                {
                    if (Ydistance <= 10 && Ydistance >= -10)
                    {
                        if (Zdistance <= 10 && Zdistance >= -10)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, "PortalHandler.IsInRangeOfSpawnpoint: " + ex);
            }

            return false;
        }
    }
}
