using System;
using System.Linq;
using fCraft.Events;

namespace fCraft {

    internal class ZombieGame {

        /// <summary>
        /// The name for the zombie skin
        /// </summary>
        private const string _zomb = "&8_Infected_";

        /// <summary>
        /// The gamemode world
        /// </summary>
        private static World _world;

        /// <summary>
        /// A count of all unifected players, updated on Interval()
        /// </summary>
        private static int _humanCount = 0;

        /// <summary>
        /// A repeating game task
        /// </summary>
        private static SchedulerTask _task;

        /// <summary>
        /// The start time of the game
        /// </summary>
        public static DateTime startTime;

        /// <summary>
        /// The time when player count was last announced
        /// </summary>
        public static DateTime lastChecked;

        /// <summary>
        /// bool, true = game has started
        /// </summary>
        private static bool _started = false;

        /// <summary>
        /// Class instance
        /// </summary>
        /// <param name="world">Game world</param>
        public ZombieGame( World world ) {
            _world = world;
            startTime = DateTime.UtcNow;
            _humanCount = _world.Players.Length;
            _task = new SchedulerTask( Interval, false ).RunForever( TimeSpan.FromSeconds( 1 ) );
            _world.gameMode = GameMode.ZombieSurvival;
            Player.Moved += OnPlayerMoved;
            Player.JoinedWorld += OnChangedWorld;
        }

        /// <summary>
        /// Method to start the game
        /// </summary>
        public void Start() {
            _world.gameMode = GameMode.ZombieSurvival; //set the game mode
            _humanCount = _world.Players.Count(p => p.iName != _zomb); //count all players
            Scheduler.NewTask( t => _world.Players.Message( "&WThe game will be starting soon..." ) )
                .RunRepeating( TimeSpan.FromSeconds( 0 ), TimeSpan.FromSeconds( 20 ), 2 );
        }

        /// <summary>
        /// Method to stop the game
        /// </summary>
        /// <param name="player">Player stopping the game. If null, presume game has finished by itself</param>
        public void Stop( Player player ) {
            if ( player != null ) {
                _world.Players.Message( "{0}&S stopped the game of Infection on world {1}",
                    player.ClassyName, _world.ClassyName );
            }
            RevertNames();
            _world.gameMode = GameMode.NULL;
            _humanCount = 0;
            _task = null;
            _started = false;
            Player.Moved -= OnPlayerMoved;
            Player.JoinedWorld -= OnChangedWorld;
        }

        /// <summary>
        /// Pick a random player and infect them
        /// </summary>
        public void RandomPick() {
            Random rand = new Random();
            int min = 0;
            int max = _world.Players.Length;
            int num = rand.Next( min, max );
            Player p = _world.Players[num];
            ToZombie( null, p );
        }

        private Random rand = new Random();

        /// <summary>
        /// Repeating task, checks things
        /// </summary>
        /// <param name="task">Task in question</param>
        public void Interval( SchedulerTask task ) {
            //check to stop Interval
            if ( _world.gameMode != GameMode.ZombieSurvival || _world == null ) {
                _world = null;
                task.Stop();
                return;
            }
            if ( !_started ) {
                if ( startTime != null && ( DateTime.UtcNow - startTime ).TotalMinutes > 1 ) {
                    /*if (_world.Players.Length < 3){
                        _world.Players.Message("&WThe game failed to start: 2 or more players need to be in the world");
                        Stop(null);
                        return;
                    }*/
                    ShufflePlayerPositions();
                    _started = true;
                    RandomPick();
                    lastChecked = DateTime.UtcNow;
                    return;
                }
            }
            //calculate humans
            _humanCount = _world.Players.Count(p => p.iName != _zomb);
            //check if zombies have won already
            if ( _started ) {
                if ( _humanCount == 1 && _world.Players.Count() == 1 ) {
                    _world.Players.Message( "&WThe Zombies have failed to infect everyone... &9HUMANS WIN!" );
                    Stop( null );
                    return;
                }
                if ( _humanCount == 0 ) {
                    _world.Players.Message( "&WThe Humans have failed to survive... &9ZOMBIES WIN!" );
                    Stop( null );
                    return;
                }
            }
            //check if 5mins is up and all zombies have failed
            if ( _started && startTime != null && ( DateTime.UtcNow - startTime ).TotalMinutes >= 5 ) {
                _world.Players.Message( "&WThe Zombies have failed to infect everyone... &9HUMANS WIN!" );
                Stop( null );
                return;
            }
            //if no one has won, notify players of their status every ~30s
            if ( lastChecked != null && ( DateTime.UtcNow - lastChecked ).TotalSeconds >= 30 ) {
                _world.Players.Message( "&WThere are {0} humans", _humanCount.ToString() );
                foreach ( Player p in _world.Players ) {
                    if ( p.iName == _zomb )
                        p.Message( "&8You are " + _zomb );
                    else
                        p.Message( "&8You are a Human" );
                }
                lastChecked = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Event: Manages a player changing world
        /// </summary>
        private void OnChangedWorld( object sender, PlayerJoinedWorldEventArgs e ) {
            if ( e.OldWorld != null ) {
                if ( e.OldWorld.gameMode == GameMode.ZombieSurvival && e.NewWorld != e.OldWorld ) {
                    if ( e.Player.iName != null )
                        RevertPlayerName( e.Player );
                } else if ( e.OldWorld.gameMode != GameMode.ZombieSurvival && e.NewWorld.gameMode == GameMode.ZombieSurvival ) {
                    if ( e.NewWorld.Players.Any(p => p.iName == _zomb) ) {
                        e.Player.iName = _zomb;
                        e.Player.entityChanged = true;
                        e.Player.Message( "&WYou arrived late, so you are " + _zomb );
                    }
                    if ( e.Player.IsUsingWoM )
                        e.Player.Message( "&HUsing WoM? Be sure to turn off hacks" );
                }
            }
        }

        /// <summary>
        /// Event: Manages a player's movement
        /// </summary>
        private void OnPlayerMoved( object sender, PlayerMovedEventArgs e ) {
            if ( e.Player.World.gameMode == GameMode.ZombieSurvival ) {
                if ( e.Player.World.Name == _world.Name && _world != null ) {
                    if ( e.NewPosition != null ) {
                        Vector3I oldPos = new Vector3I( e.OldPosition.X / 32, e.OldPosition.Y / 32, e.OldPosition.Z / 32 );
                        Vector3I newPos = new Vector3I( e.NewPosition.X / 32, e.NewPosition.Y / 32, e.NewPosition.Z / 32 );

                        //check player bounds, revert if needed
                        if ( oldPos.X != newPos.X || oldPos.Y != newPos.Y || oldPos.Z != newPos.Z ) {
                            if ( !_world.Map.InBounds( newPos ) ) {
                                e.Player.TeleportTo( _world.Map.Spawn );
                                newPos = ( Vector3I )_world.Map.Spawn;
                            }
                            if ( oldPos.X - newPos.X > 1 || oldPos.Y - newPos.Y > 1 || newPos.X - oldPos.X > 1 || newPos.Y - oldPos.Y > 1 ) {
                                e.Player.TeleportTo( e.OldPosition );
                                newPos = oldPos;
                            }
                        }

                        //if they are inbounds, check infection zone (33*33) and infect if needed
                        if ( _world.Map.InBounds( newPos ) ) {
                            if ( oldPos.X != newPos.X || oldPos.Y != newPos.Y || oldPos.Z != newPos.Z ) {
                                foreach ( Player p in _world.Players ) {
                                    Vector3I pos = p.Position.ToBlockCoords();
                                    if ( e.NewPosition.DistanceSquaredTo( pos.ToPlayerCoords() ) <= 33 * 33 ) {
                                        ToZombie( e.Player, p );
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Used to spawn all players at a random position on the map
        /// </summary>
        private void ShufflePlayerPositions() {
            foreach ( Player p in _world.Players ) {
                int x = rand.Next( 2, _world.Map.Width );
                int y = rand.Next( 2, _world.Map.Length );
                int z1 = 0;
                for ( int z = _world.Map.Height - 1; z > 0; z-- ) {
                    if ( _world.Map.GetBlock( x, y, z ) != Block.Air ) {
                        z1 = z + 3;
                        break;
                    }
                }
                p.TeleportTo( new Position( x, y, z1 + 2 ).ToVector3I().ToPlayerCoords() );
            }
        }

        /// <summary>
        /// Infects a player
        /// </summary>
        /// <param name="infector">Player infecting other</param>
        /// <param name="target">Guy getting infected</param>
        public void ToZombie( Player infector, Player target ) {
            if ( infector == null ) {
                _world.Players.Message( "{0}&S has been the first to get &cInfected. &9Panic!!!!!",
                        target.ClassyName );
                target.iName = _zomb;
                target.entityChanged = true;
                return;
            }
            if ( infector.iName == _zomb && target.iName != _zomb ) {
                target.iName = _zomb;
                target.entityChanged = true;
                _world.Players.Message( "{0}&S was &cInfected&S by {1}",
                    target.ClassyName, infector.ClassyName );
                target.Message( "&WYou are now a Zombie!" );
                return;
            } else if ( infector.iName != _zomb && target.iName == _zomb ) {
                infector.iName = _zomb;
                infector.entityChanged = true;
                _world.Players.Message( "{0}&S was &cInfected&S by {1}",
                    infector.ClassyName, target.ClassyName );
                infector.Message( "&WYou are now a Zombie!" );
            }
        }

        /// <summary>
        /// Reverts all names (disinfects all)
        /// </summary>
        private void RevertNames() {
            foreach ( Player p in _world.Players ) {
                RevertPlayerName( p );
            }
        }

        /// <summary>
        /// Disinfects one player
        /// </summary>
        /// <param name="p">Player in question</param>
        public void RevertPlayerName( Player p ) {
            if ( p.iName == _zomb ) {
                p.iName = null;
                p.entityChanged = true;
                p.Message( "You are being disinfected" );
            }
        }
    }
}