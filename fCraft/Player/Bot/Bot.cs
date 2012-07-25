using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using fCraft.Events;

namespace fCraft
{
    public sealed partial class Bot
    {
        public string Name; //name of the bot
        public Position Pos; //bots current position
        public int ID; //ID of the bot, should not change
        public World world; //world the bot is on
        private bool _isMoving; //if the bot can move, can be changed if it is not boxed in ect
        private Position nextPos;
        private Position oldPos;

        public Bot(string name, Position pos, int iD, World world_)
        {
            Name = name; //bots should be gray :P
            Pos = pos;
            ID = iD;
            world = world_;
            _isMoving = true;
            SetBot();
            Scheduler.NewBackgroundTask(t => StartNewAIMovement()).RunForever(TimeSpan.FromMilliseconds(500));
        }

        public Map WorldMap
        {
            get
            {
                World world_ = world;
                return world_.LoadMap();
            }
        }
        // creates the bot. Need to add a list so bots can be shown to people 
        // reloading the world / joined after it was created
        public void SetBot()
        {
            world.Players.Send(PacketWriter.MakeAddEntity(this.ID, Color.Gray + this.Name, new Position(Pos.X, Pos.Y, Pos.Z, Pos.R, Pos.L)));
        }

        public void MoveBot()
        {
            Position oldPos = Pos; //curent pos
            Position delta = new Position(
                (short)(nextPos.X - oldPos.X),
                (short)(nextPos.Y - oldPos.Y),
                (short)(nextPos.Z - oldPos.Z));

            //set the packet
            Packet packet = PacketWriter.MakeMoveRotate(ID, new Position
            {
                X = delta.X,
                Y = delta.Y,
                Z = delta.Z,
                R = Pos.R,
                L = 0
            });
            //send packet to everyone in the world
            if (nextPos == oldPos && oldPos != null)
            {
                world.Players.Send(PacketWriter.MakeTeleport(ID, new Position(world.Map.Spawn.X, world.Map.Spawn.Y, world.Map.Spawn.Z)));
                Pos = new Position(world.Map.Spawn.X, world.Map.Spawn.Y, world.Map.Spawn.Z, Pos.R, Pos.L);
            }
            else
            {
                world.Players.Send(packet);
                Pos = nextPos;
            }
            world.Players.Message(Pos.ToBlockCoords().ToString());
        }

        public bool CheckVelocity(Block block)
        {
            if (block == Block.Air)
            {
                Pos.Z -= 32;
                world.Players.Send(PacketWriter.MakeMove(ID, Pos));
                return true;
            }
            return false;
        }
        public void CheckIfCanMove()
        {
            double ksi = 2.0 * Math.PI * (-Pos.L) / 256.0;
            double phi = 2.0 * Math.PI * (Pos.R - 64) / 256.0;
            double sphi = Math.Sin(phi);
            double cphi = Math.Cos(phi);
            double sksi = Math.Sin(ksi);
            double cksi = Math.Cos(ksi);
            Position movePos;
            Vector3I BlockPos = new Vector3I((int)(cphi * cksi * 2 - sphi * (0.5 + 1) - cphi * sksi * (0.5 + 1)),
										  (int)(sphi * cksi * 2 + cphi * (0.5 + 1) - sphi * sksi * (0.5 + 1)),
										  (int)(sksi * 2 + cksi * (0.5 + 1)));
            BlockPos += Pos.ToBlockCoords();
            movePos = new Position((short)(BlockPos.X * 32), (short)(BlockPos.Y *32), (short)(BlockPos.Z* 32), Pos.R, Pos.L);
            oldPos = movePos;
            switch (world.Map.GetBlock(BlockPos.X, BlockPos.Y, BlockPos.Z - 2))
            {
                case Block.Air:
                case Block.Water:
                case Block.Lava:
                case Block.Plant:
                case Block.RedFlower:
                case Block.RedMushroom:
                case Block.YellowFlower:
                case Block.BrownMushroom:
                    nextPos = movePos;
                    MoveBot();
                    break;
                default:
                    Pos.R -= 90;
                    world.Players.Send(PacketWriter.MakeRotate(ID, Pos));
                    break;
            }
        }

        public void StartNewAIMovement()
        {
            if (!_isMoving)
            {
                return;
            }
            CheckIfCanMove();
            int Rand = new Random().Next(1, 100);
            if (Rand > 95) MakeRandomDecision();
        }

        public void MakeRandomDecision()
        {
            int rand = new Random().Next(1, 5);
            switch (rand)
            {
                case 1:
                    Pos.R+=15;
                    world.Players.Send(PacketWriter.MakeRotate(ID, Pos));
                    break;
            case 2:
                    Pos.R += 90;
                    world.Players.Send(PacketWriter.MakeRotate(ID, Pos));
                    break;
            case 3:
                    int player = new Random().Next(0, world.Players.Count() - 1);
                    world.Players.Send(PacketWriter.MakeTeleport(ID, world.Players[player].Position));
                    Pos = world.Players[player].Position;
                break;
                default:
                break;
            }
        }
    }
}
