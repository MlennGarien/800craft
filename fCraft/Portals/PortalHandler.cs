/*        ----
        Copyright (c) 2011-2013 Jon Baker, Glenn Marien and Lao Tszy <Jonty800@gmail.com>
        All rights reserved.

        Redistribution and use in source and binary forms, with or without
        modification, are permitted provided that the following conditions are met:
         * Redistributions of source code must retain the above copyright
              notice, this list of conditions and the following disclaimer.
            * Redistributions in binary form must reproduce the above copyright
             notice, this list of conditions and the following disclaimer in the
             documentation and/or other materials provided with the distribution.
            * Neither the name of 800Craft or the names of its
             contributors may be used to endorse or promote products derived from this
             software without specific prior written permission.

        THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
        ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
        WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
        DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
        DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
        (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
        LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
        ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
        (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
        SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
        ----*/
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
                                e.Result = CanPlaceResult.Revert;
                                e.Player.Message( "You can not place a block inside portal: " + portal.Name );
                                return;
                            }
                        }
                    }
                }

                if ( e.NewBlock == Block.Red ) {
                    if ( e.Context == BlockChangeContext.Manual ) {
                        if ( e.Player.PortalCache.Name != null ) {
                            if ( e.Player.PortalCache.AffectedBlocks != null ) {
                                if ( e.Player.PortalCache.AffectedBlocks.Contains( e.Coords ) ) { //stop output being inside the unfinished portal
                                    e.Result = CanPlaceResult.Revert;
                                    e.Player.Message( "You can not place a block inside a portal" );
                                    return;
                                }
                                e.Player.PortalCache.DesiredOutputX = e.Coords.ToPlayerCoords().X;
                                e.Player.PortalCache.DesiredOutputY = e.Coords.ToPlayerCoords().Y;
                                e.Player.PortalCache.DesiredOutputZ = ( e.Coords.Z + 2 ) * 32;
                                    e.Player.PortalCache.DesiredOutputR = e.Player.Position.R;
                                    e.Player.PortalCache.DesiredOutputL = e.Player.Position.L;

                                e.Player.PortalCache.Name = Portal.GenerateName( e.Player.PortalCache.World, true );
                                string oldWorld = e.Player.PortalCache.World;
                                e.Player.PortalCache.World = e.Player.World.Name;
                                PortalHandler.CreatePortal( e.Player.PortalCache, WorldManager.FindWorldExact( oldWorld ), true );
                                e.Player.Message( " Portal finalized: Exit point at {0} on world {1}", e.Coords.ToString(), e.Player.World.ClassyName );
                                e.Player.PortalCache = new Portal();
                                e.Result = CanPlaceResult.Revert;
                            }
                        }
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
                                        if ( e.Player.LastUsedPortal != null && ( DateTime.UtcNow - e.Player.LastUsedPortal ).TotalSeconds < 4 ) {
                                            // To prevent portal loops
                                            if ( e.Player.LastWarnedPortal == null || ( DateTime.UtcNow - e.Player.LastWarnedPortal ).TotalSeconds > 2 ) {
                                                e.Player.LastWarnedPortal = DateTime.UtcNow;
                                                e.Player.Message( "You cannot use portals for another {0} seconds.", 4 - ( DateTime.UtcNow - e.Player.LastUsedPortal ).Seconds );
                                            }
                                            return;
                                        }

                                        // Make sure this method isn't called twice
                                        e.Player.CanUsePortal = false;

                                        e.Player.StandingInPortal = true;
                                        Portal portal = PortalHandler.GetInstance().GetPortal( e.Player );

                                        World world = WorldManager.FindWorldExact( portal.World );
                                        if ( world == null ) return;
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
                                                    if ( !portal.HasDesiredOutput ) {
                                                        e.Player.TeleportTo( e.Player.World.Map.Spawn );
                                                    } else {
                                                        e.Player.TeleportTo( new Position((short)portal.DesiredOutputX, (short)portal.DesiredOutputY, (short)portal.DesiredOutputZ, portal.DesiredOutputR, portal.DesiredOutputL) );
                                                    }
                                                    e.Player.LastWarnedPortal = DateTime.UtcNow;
                                                    e.Player.StandingInPortal = false;
                                                    e.Player.CanUsePortal = true;
                                                    e.Player.LastUsedPortal = DateTime.UtcNow;
                                                } else {
                                                    if ( !portal.HasDesiredOutput ) {
                                                        e.Player.JoinWorld( WorldManager.FindWorldExact( portal.World ), WorldChangeReason.Portal );
                                                    } else {
                                                        e.Player.JoinWorld( WorldManager.FindWorldExact( portal.World ), WorldChangeReason.Portal, new Position( ( short )portal.DesiredOutputX, ( short )portal.DesiredOutputY, ( short )portal.DesiredOutputZ, portal.DesiredOutputR, portal.DesiredOutputL ) );
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

        public static void CreatePortal ( Portal portal, World source, bool Custom ) {
            try {
                if ( Custom ) {
                    if ( !source.IsLoaded ) {
                        source.LoadMap();
                    }
                }
                if ( source.Map.Portals == null ) {
                    source.Map.Portals = new ArrayList();
                }

                lock ( source.Map.Portals.SyncRoot ) {
                    source.Map.Portals.Add( portal );
                }
                if ( Custom ) {
                    if ( source.IsLoaded ) {
                        source.UnloadMap( true );
                    }
                }
            } catch ( Exception e ) {
                Logger.Log( LogType.Error, "PortalCreation: " + e );
            }
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
