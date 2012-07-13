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
        private Thread _botThread; //bot thread
        private bool _isMoving; //if the bot can move, can be changed if it is not boxed in ect
        private Direction _direction; //direction the bot is facing, 0 = south

        public Bot(string name, Position pos, int iD, World world_)
        {
            Name = name; //bots should be gray :P
            Pos = pos;
            ID = iD;
            world = world_;
            _isMoving = false;
            _direction = Direction.South; //start off at south
            StartNewAIMovement(); //start the while loop in a new thread. I want to change the way the thread works
            //its only like this for testing purposes
        }

        public enum Direction
        {
            North,
            South,
            East,
            West
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
            world.Players.Send(PacketWriter.MakeAddEntity(this.ID, Color.Gray + this.Name, this.Pos));
        }
        //makes the bot walk in the south direction (-32 on the Y. -32 because block size)
        public void WalkSouth()
        {
            if (CheckBounds())
            {
                _direction = Direction.East;
                return;
            }
            Pos.R = 0; //face south
            Position oldPos = Pos; //curent pos
            Position newPos = new Position(oldPos.X, (short)(oldPos.Y - 32), oldPos.Z, oldPos.R, oldPos.L); //desired pos
            Position delta = new Position //delta of new - old
            {
                X = (short)(newPos.X - oldPos.X),
                Y = (short)(newPos.Y - oldPos.Y),
                Z = (short)(newPos.Z - oldPos.Z),
                R = (byte)Math.Abs(newPos.R - oldPos.R),
                L = (byte)Math.Abs(newPos.L - oldPos.L)
            };
             //set the packet
            Packet packet = PacketWriter.MakeMoveRotate(ID, new Position
            {
                X = delta.X,
                Y = delta.Y,
                Z = delta.Z,
                R = newPos.R,
                L = newPos.L
            });
            //send packet to everyone in the world
            world.Players.Send(packet);
            Pos = newPos;
        }

        public bool CheckBounds()
        {
            switch (_direction)
            {
                case Direction.South:
                    if (!WorldMap.InBounds(Pos.X / 32, (Pos.Y / 32) - 1, Pos.Z / 32))
                    {
                        return true;
                    }
                    break;
                case Direction.East:
                    if (!WorldMap.InBounds((Pos.Y / 32) + 1, Pos.Y / 32, Pos.Z / 32))
                    {
                        return true;
                    }
                    break;
            }
            return false;
        }

        public void WalkEast()
        {
            if(CheckBounds())
            {
                _direction = Direction.North;
                return;
            }
            //unsure Pos.R = -90; //face east
            Position oldPos = Pos; //curent pos
            Position newPos = new Position((short)(oldPos.X - 32), oldPos.Y, oldPos.Z, oldPos.R, oldPos.L); //desired pos
            Position delta = new Position //delta of new - old
            {
                X = (short)(newPos.X - oldPos.X),
                Y = (short)(newPos.Y - oldPos.Y),
                Z = (short)(newPos.Z - oldPos.Z),
                R = (byte)Math.Abs(newPos.R - oldPos.R),
                L = (byte)Math.Abs(newPos.L - oldPos.L)
            };
            //set the packet
            Packet packet = PacketWriter.MakeMoveRotate(ID, new Position
            {
                X = delta.X,
                Y = delta.Y,
                Z = delta.Z,
                R = newPos.R,
                L = newPos.L
            });
            //send packet to everyone in the world
            world.Players.Send(packet);
            Pos = newPos;
        }

        public void StartNewAIMovement()
        {
            _botThread = new Thread(new ThreadStart(delegate
            {
                _isMoving = true;
                while (_isMoving)
                {
                    
                    if (_direction == Direction.South){
                        WalkSouth();
                    }
                    else if (_direction == Direction.East)
                    {
                        WalkEast();
                    }
                    Thread.Sleep(350);
                }

            }));
            _botThread.Start();
        }
    }
    
}
