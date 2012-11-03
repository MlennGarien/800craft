//Copyright (C) <2012>  <Jon Baker, Glenn Mariën and Lao Tszy>

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

//Copyright (C) <2012> Glenn Mariën (http://project-vanilla.com)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace fCraft.Portals {
    class PortalHandler {

        private static PortalHandler instance;

        private PortalHandler () {
            // Empty, singleton
        }

        public static PortalHandler GetInstance () {
            if ( instance == null ) {
                instance = new PortalHandler();
                Player.Moved += new EventHandler<Events.PlayerMovedEventArgs>( Player_Moved );
                Player.JoinedWorld += new EventHandler<Events.PlayerJoinedWorldEventArgs>( Player_JoinedWorld );
                Player.PlacingBlock += new EventHandler<Events.PlayerPlacingBlockEventArgs>( Player_PlacingBlock );
            }

            return instance;
        }

        static void Player_PlacingBlock ( object sender, Events.PlayerPlacingBlockEventArgs e ) {
            try {
                if ( e.Player.World.Map.Portals != null && e.Player.World.Map.Portals.Count > 0 && e.Context != BlockChangeContext.Portal ) {
                    lock ( e.Player.World.Map.Portals.SyncRoot ) {

                        foreach ( Portal portal in e.Player.World.Map.Portals ) {
                            if ( portal.IsInRange( e.Coords ) ) {
                                BlockUpdate update = new BlockUpdate( null, e.Coords, e.OldBlock );
                                e.Player.World.Map.QueueUpdate( update );
                                e.Player.Message( "You can not place a block inside portal: " + portal.Name );
                            }
                        }
                    }
                }

                if ( e.NewBlock == Block.Red ) {
                    if ( e.Player.PortalCache.Name != null ) {
                        e.Player.PortalCache.DesiredOutput = new Position(
                            e.Coords.ToPlayerCoords().X,
                            e.Coords.ToPlayerCoords().Y,
                            ( short )( (e.Coords.Z + 1) / 32 ), //posfix
                            e.Player.Position.L,
                            e.Player.Position.R );
                        e.Player.PortalCache.World = e.Player.World.Name;
                        PortalHandler.CreatePortal( e.Player.PortalCache, WorldManager.FindWorldExact( e.Player.PortalWorld ) );
                        e.Player.Message( " Portal finalized: Exit point at {0} on world {1}", e.Coords.ToString(), e.Player.World.ClassyName );
                        e.Player.PortalCache = new Portal();
                        e.Result = CanPlaceResult.Revert;
                    }
                }
            } catch ( Exception ex ) {
                Logger.Log( LogType.Error, "PortalHandler.Player_PlacedBlock: " + ex );
            }
        }

        static void Player_JoinedWorld ( object sender, Events.PlayerJoinedWorldEventArgs e ) {
            try {
                // Player can use portals again
                e.Player.CanUsePortal = true;
                e.Player.LastUsedPortal = DateTime.UtcNow;
            } catch ( Exception ex ) {
                Logger.Log( LogType.Error, "PortalHandler.Player_JoinedWorld: " + ex );
            }
        }

        static void Player_Moved ( object sender, Events.PlayerMovedEventArgs e ) {
            try {
                if ( e.Player.PortalsEnabled ) {
                    lock ( e.Player.PortalLock ) {
                        if ( e.Player.CanUsePortal ) {
                            if ( ( e.OldPosition.X != e.NewPosition.X ) || ( e.OldPosition.Y != e.NewPosition.Y ) || ( e.OldPosition.Z != ( e.NewPosition.Z ) ) ) {
                                if ( e.Player.Can( Permission.UsePortal ) ) {
                                    if ( PortalHandler.GetInstance().GetPortal( e.Player ) != null && !e.Player.StandingInPortal ) {
                                        if ( e.Player.LastUsedPortal != null && ( DateTime.UtcNow - e.Player.LastUsedPortal ).TotalSeconds < 5 ) {
                                            // To prevent portal loops
                                            if ( e.Player.LastWarnedPortal == null || ( DateTime.UtcNow - e.Player.LastWarnedPortal ).TotalSeconds > 2 ) {
                                                e.Player.LastWarnedPortal = DateTime.UtcNow;
                                                e.Player.Message( "You cannot use portals for another {0} seconds.", 5 - ( DateTime.UtcNow - e.Player.LastUsedPortal ).Seconds );
                                            }
                                            return;
                                        }

                                        // Make sure this method isn't called twice
                                        e.Player.CanUsePortal = false;

                                        e.Player.StandingInPortal = true;
                                        Portal portal = PortalHandler.GetInstance().GetPortal( e.Player );

                                        World world = WorldManager.FindWorldExact( portal.World );
                                        // Teleport player, portal protection
                                        switch ( world.AccessSecurity.CheckDetailed( e.Player.Info ) ) {
                                            case SecurityCheckResult.Allowed:
                                            case SecurityCheckResult.WhiteListed:
                                                if ( world.IsFull ) {
                                                    e.Player.Message( "Cannot join {0}&S: world is full.", world.ClassyName );
                                                    return;
                                                }
                                                e.Player.StopSpectating();
                                                if ( portal.World == e.Player.World.Name ) {
                                                    if ( !portal.HasDesiredOutput) {
                                                        e.Player.TeleportTo( e.Player.World.Map.Spawn );
                                                    } else {
                                                        e.Player.TeleportTo( portal.DesiredOutput );
                                                    }
                                                    e.Player.LastWarnedPortal = DateTime.UtcNow;
                                                    e.Player.StandingInPortal = false;
                                                    e.Player.CanUsePortal = true;
                                                    e.Player.LastUsedPortal = DateTime.UtcNow;
                                                } else {
                                                    if ( !portal.HasDesiredOutput) {
                                                        e.Player.JoinWorld( WorldManager.FindWorldExact( portal.World ), WorldChangeReason.Portal );
                                                    } else {
                                                        e.Player.JoinWorld( WorldManager.FindWorldExact( portal.World ), WorldChangeReason.Portal, portal.DesiredOutput );
                                                    }
                                                }
                                                e.Player.Message( "You used portal: " + portal.Name );
                                                break;

                                            case SecurityCheckResult.BlackListed:
                                                e.Player.Message( "Cannot join world {0}&S: you are blacklisted.",
                                                    world.ClassyName );
                                                break;
                                            case SecurityCheckResult.RankTooLow:
                                                e.Player.Message( "Cannot join world {0}&S: must be {1}+",
                                                             world.ClassyName, world.AccessSecurity.MinRank.ClassyName );
                                                break;
                                        }
                                    } else {
                                        e.Player.StandingInPortal = false;
                                    }
                                }
                            }
                        }
                    }
                }
            } catch ( Exception ex ) {
                Logger.Log( LogType.Error, "PortalHandler.Player_Moved: " + ex );
            }
        }

        public Portal GetPortal ( Player player ) {
            Portal portal = null;

            try {
                if ( player.World.Map.Portals != null && player.World.Map.Portals.Count > 0 ) {
                    lock ( player.World.Map.Portals.SyncRoot ) {
                        foreach ( Portal possiblePortal in player.World.Map.Portals ) {
                            if ( possiblePortal.IsInRange( player ) ) {
                                return possiblePortal;
                            }
                        }
                    }
                }
            } catch ( Exception ex ) {
                Logger.Log( LogType.Error, "PortalHandler.GetPortal: " + ex );
            }

            return portal;
        }

        public Portal GetPortal ( World world, Vector3I block ) {
            Portal portal = null;

            try {
                if ( world.Map.Portals != null && world.Map.Portals.Count > 0 ) {
                    lock ( world.Map.Portals.SyncRoot ) {
                        foreach ( Portal possiblePortal in world.Map.Portals ) {
                            if ( possiblePortal.IsInRange( block ) ) {
                                return possiblePortal;
                            }
                        }
                    }
                }
            } catch ( Exception ex ) {
                Logger.Log( LogType.Error, "PortalHandler.GetPortal: " + ex );
            }

            return portal;
        }

        public static void CreatePortal ( Portal portal, World source ) {
            World world = WorldManager.FindWorldExact( portal.World );

            if ( source.Map.Portals == null ) {
                source.Map.Portals = new ArrayList();
            }

            lock ( source.Map.Portals.SyncRoot ) {
                source.Map.Portals.Add( portal );
            }

            //PortalDB.Save();
        }

        public static bool IsInRangeOfSpawnpoint ( World world, Vector3I block ) {
            try {
                int Xdistance = ( world.Map.Spawn.X / 32 ) - block.X;
                int Ydistance = ( world.Map.Spawn.Y / 32 ) - block.Y;
                int Zdistance = ( world.Map.Spawn.Z / 32 ) - block.Z;

                if ( Xdistance <= 10 && Xdistance >= -10 ) {
                    if ( Ydistance <= 10 && Ydistance >= -10 ) {
                        if ( Zdistance <= 10 && Zdistance >= -10 ) {
                            return true;
                        }
                    }
                }
            } catch ( Exception ex ) {
                Logger.Log( LogType.Error, "PortalHandler.IsInRangeOfSpawnpoint: " + ex );
            }

            return false;
        }
    }
}
