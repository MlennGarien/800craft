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
                Player.JoinedWorld += new EventHandler<Events.PlayerJoinedWorldEventArgs>(Player_JoinedWorld);
                Player.PlacedBlock += new EventHandler<Events.PlayerPlacedBlockEventArgs>(Player_PlacedBlock);
                PortalDB.StartSaveTask();
            }

            return instance;
        }

        static void Player_PlacedBlock(object sender, Events.PlayerPlacedBlockEventArgs e)
        {
            try
            {
                if (e.Player.World.Portals != null && e.Player.World.Portals.Count > 0 && e.Context != BlockChangeContext.Portal)
                {
                    lock (e.Player.World.Portals.SyncRoot)
                    {
                        foreach (Portal portal in e.Player.World.Portals)
                        {
                            if (portal.IsInRange(e.Coords))
                            {
                                BlockUpdate update = new BlockUpdate(null, e.Coords, e.OldBlock);
                                e.Player.World.Map.QueueUpdate(update);
                                e.Player.Message("You can not place a block inside portal: " + portal.Name);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, "PortalHandler.Player_PlacedBlock: " + ex);
            }
        }

        static void Player_JoinedWorld(object sender, Events.PlayerJoinedWorldEventArgs e)
        {
            try
            {
                // Player can use portals again
                e.Player.CanUsePortal = true;
                e.Player.LastUsedPortal = DateTime.Now;
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, "PortalHandler.Player_JoinedWorld: " + ex);
            }
        }

        static void Player_Moved(object sender, Events.PlayerMovedEventArgs e)
        {
            try
            {
                if (e.Player.PortalsEnabled)
                {
                    lock (e.Player.PortalLock)
                    {
                        if (e.Player.CanUsePortal)
                        {
                            if ((e.OldPosition.X != e.NewPosition.X) || (e.OldPosition.Y != e.NewPosition.Y) || (e.OldPosition.Z != (e.NewPosition.Z)))
                            {
                                if (e.Player.Can(Permission.UsePortal))
                                {
                                    if (PortalHandler.GetInstance().GetPortal(e.Player) != null && !e.Player.StandingInPortal)
                                    {
                                        if (e.Player.LastUsedPortal != null && (DateTime.Now - e.Player.LastUsedPortal).TotalSeconds < 5)
                                        {
                                            // To prevent portal loops
                                            if (e.Player.LastWarnedPortal == null || (DateTime.Now - e.Player.LastWarnedPortal).TotalSeconds > 2)
                                            {
                                                e.Player.LastWarnedPortal = DateTime.Now;
                                                e.Player.Message("You can not use portals within 5 seconds of joining a world.");
                                            }

                                            return;
                                        }

                                        // Make sure this method isn't called twice
                                        e.Player.CanUsePortal = false;

                                        e.Player.StandingInPortal = true;
                                        Portal portal = PortalHandler.GetInstance().GetPortal(e.Player);

                                        World world = new World(portal.World);
                                        // Teleport player, portal protection
                                        switch( world.AccessSecurity.CheckDetailed( e.Player.Info ) ) {
                                        case SecurityCheckResult.BlackListed:
                                            e.Player.Message( "Cannot join world {0}&S: you are blacklisted.",
                                                world.ClassyName );
                                            break;
                                        case SecurityCheckResult.RankTooLow:
                                            e.Player.Message( "Cannot join world {0}&S: must be {1}+",
                                                         world.ClassyName, world.AccessSecurity.MinRank.ClassyName );
                                            break;
                                        case SecurityCheckResult.Allowed:
                                        case SecurityCheckResult.WhiteListed:
                                            if (world.IsFull)
                                            {
                                                e.Player.Message("Cannot join {0}&S: world is full.", world.ClassyName);
                                                return;
                                            }
                                            e.Player.StopSpectating();
                                            break;
                                        }

                                        e.Player.JoinWorldNow(WorldManager.FindWorldExact(portal.World), true, WorldChangeReason.Portal);
                                        e.Player.Message("You used portal: " + portal.Name);
                                        e.Player.StandingInPortal = false;
                                    }
                                    else
                                    {
                                        e.Player.StandingInPortal = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogType.Error, "PortalHandler.Player_Moved: " + ex);
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

        public Portal GetPortal(World world, Vector3I block)
        {
            Portal portal = null;

            try
            {
                if (world.Portals != null && world.Portals.Count > 0)
                {
                    lock (world.Portals.SyncRoot)
                    {
                        foreach (Portal possiblePortal in world.Portals)
                        {
                            if (possiblePortal.IsInRange(block))
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

        public static void CreatePortal(Portal portal, World source)
        {
            World world = WorldManager.FindWorldExact(portal.World);

            if (source.Portals == null)
            {
                source.Portals = new ArrayList();
            }

            lock (source.Portals.SyncRoot)
            {
                source.Portals.Add(portal);
            }

            PortalDB.Save();
        }

        public static bool IsInRangeOfSpawnpoint(World world, Vector3I block)
        {
            try
            {
                int Xdistance = (world.Map.Spawn.X / 32) - block.X;
                int Ydistance = (world.Map.Spawn.Y / 32) - block.Y;
                int Zdistance = (world.Map.Spawn.Z / 32) - block.Z;

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
