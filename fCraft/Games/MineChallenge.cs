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

//Copyright (C) <2012> Jon Baker(http://au70.net)
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft;
using fCraft.Events;
using System.Threading;

namespace fCraft.Games
{
    public class MineChallenge
    {
        //stuff
        public static Thread GameThread;
        public static World world;
        public static int redRoundsWon;
        public static int blueRoundsWon;
        public static ConcurrentDictionary<String, Vector3I> platform = new ConcurrentDictionary<string, Vector3I>();
        public static ConcurrentDictionary<String, Vector3I> randomBlocks = new ConcurrentDictionary<string, Vector3I>();
        public static List<Player> completed = new List<Player>();
        public static int answer = 0;
        public static GameMode mode = GameMode.NULL;

        public enum GameMode
        {
            math1,
            math2,
            getOffGrass,
            pinkplatform,
            shootblack,
            NULL
        }

        //init
        public static void Init(World world_)
        {
            world = world_;
            Player.JoinedWorld += WorldChanging;
            Player.Clicked += playerClicking;
            world.Games = new List<Action>();
            mode = GameMode.NULL;
            redRoundsWon = 0;
            blueRoundsWon = 0;
        }

        //games

        public static void math1()
        {
            world.Games.Remove(math1);
            world.Players.Message("&WKeyboards at the ready...");
            Thread.Sleep(5000);
            mode = GameMode.math1;
            int a = new Random().Next(1, 15);
            int b = new Random().Next(1, 15);
            world.Players.Message("&WWhat is... {0} X {1}?", a.ToString(), b.ToString());
            answer = a * b;
            Thread.Sleep(15000);
            world.Players.Message("&WThe correct answer was {0}",answer.ToString());
            answer = 0;
            scoreCounter();
            completed.Clear();
            interval();
            gamePicker();
        }

        public static void math2()
        {
            world.Games.Remove(math2);
            world.Players.Message("&WKeyboards at the ready... stuff is about to happen!");
            Thread.Sleep(5000);
            mode = GameMode.math2;
            int a = new Random().Next(1, 100);
            int b = new Random().Next(1, 200);
            world.Players.Message("&WWhat is... {0} + {1}?", a.ToString(), b.ToString());
            answer = a + b;
            Thread.Sleep(15000);
            world.Players.Message("&WThe correct answer was {0}", answer.ToString());
            answer = 0;
            scoreCounter();
            completed.Clear();
            interval();
            gamePicker();
        }

        public static void getOffGrass()
        {
            world.Games.Remove(getOffGrass);
            mode = GameMode.getOffGrass;
            Map map = world.Map;
            world.Players.Message("&WKEEP OFF THE GRASS!!!");
            wait(6000);
            foreach (Player p in world.Players)
            {
                if (world.Map.GetBlock(p.Position.X / 32, p.Position.Y / 32, (short)(p.Position.Z / 32 - 1.59375)) != Block.Grass)
                {
                    p.Message("&8Thanks.");
                    if (world.blueTeam.Contains(p) && !completed.Contains(p))
                    {
                        world.blueScore++;
                        completed.Add(p);
                    }
                    else
                    {
                        world.redScore++;
                        completed.Add(p);
                    }
                }
                else
                {
                    p.Message("&8I told you to stay off the grass...");
                }
            }
            scoreCounter();
            completed.Clear();
            interval();
            gamePicker();
        }

        public static void pinkPlatform()
        {
            world.Games.Remove(pinkPlatform);
            mode = GameMode.pinkplatform;
            Map map = world.Map;
            int startX = ((map.Bounds.XMin + map.Bounds.XMax) / 2) + new Random().Next(10, 50);
            int startY = ((map.Bounds.YMin + map.Bounds.YMax) / 2) + new Random().Next(10, 50);
            int startZ = ((map.Bounds.ZMin + map.Bounds.ZMax) / 2) + new Random().Next(10, 30);
            for (int x = startX; x <= startX + 5; x++){
                for (int y = startY; y <= startY + 5; y++){
                    for (int z = startZ; z <= startZ + 1; z++){
                        world.Map.QueueUpdate(new BlockUpdate(null, (short)x, (short)y, (short)z, Block.Pink));
                        platform.TryAdd(new Vector3I(x, y, z).ToString(), new Vector3I(x, y, z));
                    }
                }
            }
            world.Players.Message("&WYou have 30 seconds to get onto the PINK platform.... &AGO!");
            wait(20000);
            world.Players.Message("&WYou have 10 seconds left to get onto the PINK platform.");
            wait(10000);
            PositionCheck();
            scoreCounter();
            foreach (Vector3I block in platform.Values){
                if (world.Map != null && world.IsLoaded){
                    world.Map.QueueUpdate(new BlockUpdate(null, block, Block.Air));
                    Vector3I removed;
                    platform.TryRemove(block.ToString(), out removed);
                }
            }
            completed.Clear();
            interval();
            gamePicker();
        }

        public static void shootBlack()
        {
            world.Games.Remove(shootBlack);
            mode = GameMode.shootblack;
            GunModeOn();
            foreach (Player p in world.Players)
            {
                Vector3I block = new Vector3I(p.Position.X / 32, p.Position.Y /32, (p.Position.Z /32) + 8);
                if (world.Map.InBounds(block))
                {
                    randomBlocks.TryAdd(block.ToString(), block);
                    world.Map.QueueUpdate(new BlockUpdate(null, block, Block.Black));
                }
            }
            world.Players.Message("&WYou have 20 seconds to shoot all &8BLACK &Wblocks.... &AGO!");
            wait(10000);
            world.Players.Message("&WYou have 10 seconds left to shoot all &8BLACK &Wblocks.");
            wait(10000);
            GunModeOff();
            scoreCounter();
            foreach (Vector3I block in platform.Values)
            {
                if (world.Map != null && world.IsLoaded)
                {
                    world.Map.QueueUpdate(new BlockUpdate(null, block, Block.Air));
                    Vector3I removed;
                    platform.TryRemove(block.ToString(), out removed);
                }
            }
            interval();
            gamePicker();
        }

        //events
        public static void playerClicking(object sender, PlayerClickedEventArgs e)
        {
            if (e.Player.World.GameOn)
            {
                if (Games.MineChallenge.mode == Games.MineChallenge.GameMode.NULL)
                {
                    if (platform.Values.Count > 0)
                    {
                        foreach (Vector3I block in platform.Values)
                        {
                            if (e.Coords == block)
                            {
                                Player.RaisePlayerPlacedBlockEvent(e.Player, world.Map, block, world.Map.GetBlock(e.Coords), world.Map.GetBlock(e.Coords), BlockChangeContext.Manual);
                                world.Map.QueueUpdate(new BlockUpdate(null, block, Block.Pink));
                            }
                        }
                    }
                }
            }
        }

        public static void PositionCheck()
        {
            foreach (Player player in world.Players)
            {
                foreach (Vector3I platBlock in platform.Values)
                {
                    if ((player.Position.X / 32) == platBlock.X)
                    {
                        if ((player.Position.Y / 32) == platBlock.Y)
                        {
                            if ((player.Position.Z / 32 - 2) == platBlock.Z || (player.Position.Z / 32 - 1) == platBlock.Z)
                            {
                                player.Message("&8You did it!");
                                if (world.blueTeam.Contains(player) && !completed.Contains(player))
                                {
                                    world.blueScore++;
                                    completed.Add(player);
                                }
                                else
                                {
                                    world.redScore++;
                                    completed.Add(player);
                                }
                            }
                        }
                    }
                }
            }
        }

        

        public static void WorldChanging(object sender, PlayerJoinedWorldEventArgs e)
        {
            if (e.OldWorld != null)
            {
                if (e.OldWorld != e.NewWorld && e.NewWorld.GameOn)
                {
                    if (!e.NewWorld.blueTeam.Contains(e.Player) && !e.NewWorld.redTeam.Contains(e.Player))
                    {
                        TeamChooser(e.Player, e.NewWorld);
                    }
                }
                else if (e.OldWorld.GameOn && e.OldWorld != e.NewWorld)
                {
                    teamRemover(e.Player, e.OldWorld);
                    e.Player.GunMode = false;
                    foreach (Vector3I block in e.Player.GunCache.Values)
                    {
                        e.Player.Send(PacketWriter.MakeSetBlock(block, world.Map.GetBlock(block)));
                    }
                }
            }
        }
        

        //voids

        public static void teamNotify()
        {
            foreach (Player player in world.Players)
            {
                if (world.blueTeam.Contains(player))
                {
                    player.Message("&9You are on the Blue Team");
                }
                else if(world.redTeam.Contains(player))
                {
                    player.Message("&4You are on the &CRed Team");
                }
            }
        }

        public static void GameAdder(World world)
        {
            world.Games.Add(pinkPlatform);
            world.Games.Add(shootBlack);
            world.Games.Add(math1);
            world.Games.Add(math2);
            world.Games.Add(getOffGrass);
        }

        public static void scoreCounter()
        {
            if (world.blueScore > world.redScore)
            {
                blueRoundsWon++;
                world.Players.Message("&SThe &9Blues&S won that round: &9{0} &S- &C{1}", world.blueScore, world.redScore);
            } if (world.redScore > world.blueScore)
            {
                redRoundsWon++;
                world.Players.Message("&SThe &CReds&S won that round: &9{0} &S- &C{1}", world.blueScore, world.redScore);
            }
            else
            {
                world.Players.Message("&SIt was a tie! Both teams get a point! &9{0} &S- &C{1}", world.blueScore, world.redScore);
                blueRoundsWon++; redRoundsWon++;
            }
        }

        public static void wait(int time)
        {
            Thread.Sleep(time);
            if (!world.GameOn)
            {
                return;
            }
        }

        public static void interval()
        {
            wait(5000);
            world.Players.Message("&SScores so far: &9Blues {0} &S- &CReds {1}", blueRoundsWon.ToString(), redRoundsWon.ToString());
            teamNotify();
            wait(5000);
        }

        public static void TeamChooser(Player player, World world)
        {
            if (!world.blueTeam.Contains(player) && !world.redTeam.Contains(player))
            {
                if (world.blueTeam.Count() > world.redTeam.Count())
                {
                    world.redTeam.Add(player);
                    player.Message("&SAdding you to the &CRed Team");
                }
                else if (world.blueTeam.Count() < world.redTeam.Count())
                {
                    world.blueTeam.Add(player);
                    player.Message("&SAdding you to the &9Blue Team");
                }
                else
                {
                    world.redTeam.Add(player);
                    player.Message("&SAdding you to the &CRed Team");
                }
            }
        }

        public static void teamRemover(Player player, World world)
        {
            if (world.blueTeam.Contains(player))
            {
                world.blueTeam.Remove(player);
                player.Message("&SRemoving you from the game");
            }
            else if (world.redTeam.Contains(player))
            {
                world.redTeam.Remove(player);
                player.Message("&SRemoving you from the game");
            }
        }
        public static void Start(Player player, World world)
        {
            GameThread = new Thread(new ThreadStart(delegate
             {
               Init(world);
               world.GameOn = true;
               Server.Players.Message("{0}&S Started a game of MineChallenge on world {1}", 
                   player.ClassyName, world.ClassyName);
               foreach (Player p in world.Players){
                   TeamChooser(p, world);
               }
               Server.Players.Message("&SThe game will start in 60 Seconds.");
               GameAdder(world);
              // wait(60000);
               world.Players.Message("&SThe game has started!.");
               wait(2000);
               gamePicker();
               Scheduler.NewTask(t => PositionCheck()).RunForever(TimeSpan.FromSeconds(1));
             })); GameThread.Start();
        }

        public static void gamePicker()
        {
            if (world.Games.Count() < 1)
            {
                Stop(Player.Console);
                return;
            }
            world.redScore = 0;
            world.blueScore = 0;
            Random rand = new Random();
            int i = rand.Next(0, world.Games.Count());
            world.Games[i]();
        }

        public static void Stop(Player player)
        {
            world.Players.Message("&SThe game has ended! The scores are: \n&9Blue Team {0} &S- &CRed Team {1}", blueRoundsWon, redRoundsWon);
            world.GameOn = false;
            Clear();
            GameThread.Abort();
        }

        //needs a rewrite for new gun code
        public static void GunModeOn()
        {
            foreach (Player player in world.Players)
            {
                player.GunMode = true;
                player.Message("GunMode Activated");
            }
        }

        public static void GunModeOff()
        {
            foreach (Player player in world.Players)
            {
                player.GunMode = false;
                foreach (Vector3I block in player.GunCache.Values)
                {
                    player.Send(PacketWriter.MakeSetBlock(block, world.Map.GetBlock(block)));
                }
            }
        }

        public static void Clear()
        {
            world.blueTeam.Clear();
            world.redTeam.Clear();
            world.blueScore = 0;
            world.redScore = 0;
            blueRoundsWon = 0;
            redRoundsWon = 0;
            completed.Clear();
            mode = GameMode.NULL;
        }
    }
}
