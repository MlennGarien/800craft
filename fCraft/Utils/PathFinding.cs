using System;
using System.Collections;
using System.Threading;

namespace fCraft
{
	public class Bot
	{
		public string name { get; protected set; }
		public Position pos;
		public byte playerID { get; protected set; }
		public byte heading { get; protected set; }
		public byte pitch { get; protected set; }
		
		public delegate void BotSpawnHandler(Bot sender);
		public event BotSpawnHandler Spawn;
		
		public delegate void BotMoveHandler(Bot player, Position dest, byte heading, byte pitch);
		public event BotMoveHandler Move;
		
		public delegate void BotDisconnectHandler(Bot sender);
		public event BotDisconnectHandler Disconnect;
		
		private double time;
		private bool update;
        public Map map;

		public Bot(string name, Position Pos)
		{
			this.name = name;
            this.playerID = 1;
            pos = Pos;
			heading = 0;
			pitch = 0;
			time = 0;
			update = true;
		}
		
		~Bot() {
			//Player.InUseIDs.Remove(playerID);
		}
		
		public void Start()
		{
			if(Spawn != null) {
				Spawn(this);
			}
		}
		
		public void Stop()
		{
			if(Disconnect != null) {
				Disconnect(this);
			}
		}

		public void Update()
		{
			update = !update;
			if(!update) return;
			
			time += 0.03 / 2;
			if (time >= 6) {
				time = 0;
			}
				
			if (time < 3) {
				pos.X += 1;
				heading = 64;
			} else if (time < 6) {
				pos.X -= 1;
				heading = 196;
			}
			
			if(Move != null) Move(this, pos, heading, pitch);
		}
	}
}