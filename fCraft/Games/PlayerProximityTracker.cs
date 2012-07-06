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

//Copyright (C) <2012> Lao Tszy (lao_tszy@yahoo.co.uk)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fCraft.Events;

namespace fCraft
{
	public class PlayersAtDistanceArgs : EventArgs
	{
		public Player FromPlayer;
		public IEnumerable<Player> Others;
	}

	class TrackerUsageExample
	{
		private World _world;
		private PlayerProximityTracker _tracker = null;
		private object _lock=new object();

		public TrackerUsageExample(World world)
		{
			if (null==world)
				throw new ArgumentNullException("world");
			lock (world.SyncRoot)
			{
				if (null==world.Map)
					throw new ArgumentException("world.Map is null");
				_world = world;
				lock (_lock)
				{
					PlayerProximityTracker tracker = new PlayerProximityTracker(world.Map.Width, world.Map.Length, world);
					_tracker.OnPlayersAtDistance += OnPlayersAtDistance;
					//_tracker.SetCallEvents(true, 1, (p1, p2) => p1.IsZombi != p2.IsZombi);

					Player.Moved += OnPlayerMoved;
					Player.Disconnected += OnPlayerDisconnected;
					Player.JoinedWorld += OnPlayerJoined;
				}
			}
		}

		public void OnPlayerMoved(object sender, PlayerMovedEventArgs args)
		{
			lock (_lock)
			{
				if (args.Player.World!=_world)
				{//he left....chicken
					//_tracker.RemovePlayer(args.Player); remove will not work since the guy definitely has different position now
					//he will be removed from book keeping later in PlayerProximityTracker.FindPlayersAtDistance
					return;
				}
				_tracker.MovePlayer(args.OldPosition.ToBlockCoords(), args.NewPosition.ToBlockCoords(), args.Player);
			}
		}

		public void OnPlayerDisconnected(object sender, PlayerDisconnectedEventArgs args)
		{
			lock (_lock)
			{
				//try to
				_tracker.RemovePlayer(args.Player);
			}
		}

		public void OnPlayerJoined(object sender, PlayerJoinedWorldEventArgs args)
		{
			lock (_lock)
			{
				if (args.NewWorld != args.OldWorld)
				{
					if (_world == args.NewWorld)
						_tracker.AddPlayer(args.Player, args.Player.Position.ToBlockCoords());
				}
			}
		}

		public void OnPlayersAtDistance(object sender, PlayersAtDistanceArgs args)
		{
			//called from OnPlayerMoved/OnPlayerJoined under the lock

			//do smth with players from args and call 
			//FindPlayersAtDistance(Player p) for those whos status was changed 
		}
	}
	/// <summary>
	/// Not thread safe.
	/// </summary>
	public class PlayerProximityTracker
	{
		private List<Player>[,] _players;
		
		public event EventHandler<PlayersAtDistanceArgs> OnPlayersAtDistance;
		
		private bool _callEvents=false;
		private double _distanceInBlocks;
		private Func<Player, Player, bool> _takePair;
		private World _world = null; //to be able to remove players left the game

		public PlayerProximityTracker(int xSize, int ySize, World world)
		{
			_players=new List<Player>[xSize, ySize];	
			foreach (Player p in world.Players)
			{
				AddPlayer(p, p.Position.ToBlockCoords());
			}
		}

		public void AddPlayer(Player p, Vector3I pos)
		{
			if (null == p)
			{
				Logger.Log(LogType.Trace, "PlayerProximityTracker.AddPlayer: Player is null");
				return;
			}
			CheckCoords(ref pos);
			if (null == _players[pos.X, pos.Y])
				_players[pos.X, pos.Y] = new List<Player>();
			_players[pos.X, pos.Y].Add(p);

			if (_callEvents)
				CallEvent(p);
		}

		public void RemovePlayer(Player p)
		{
			if (null == p)
			{
				Logger.Log(LogType.Trace, "PlayerProximityTracker.RemovePlayer: Player is null");
				return;
			}
			Vector3I pos = p.Position.ToBlockCoords();
			CheckCoords(ref pos);
			if (null == _players[pos.X, pos.Y] || !_players[pos.X, pos.Y].Remove(p))
				Logger.Log(LogType.Trace, "PlayerProximityTracker.RemovePlayer: Player " + p.Name + " is not found at its position");
		}

		public void MovePlayer(Vector3I oldPos, Vector3I newPos, Player p)
		{
			//the new pos is given as an argument (assumed from PlayerMoved event args) so that the new position would match the previous in the next moved event
			CheckCoords(ref oldPos);
            CheckCoords(ref newPos);
            if (newPos.X == oldPos.X && newPos.Y == oldPos.Y) //nothing to do?
				return;

			if (null == _players[oldPos.X, oldPos.Y] || !_players[oldPos.X,oldPos.Y].Remove(p))
			{//this is not a fatal error, the player, even when existing at some wrong position will not be returned by the find call looking around this wrong position
				Logger.Log(LogType.Error, "PlayerProximityTracker.MovePlayer: Player " + p.Name + " is not found at its previous position");
			}
            AddPlayer(p, newPos);
		}

		//may return null
		public IEnumerable<Player> FindPlayersAtDistance(Player p)
		{
			return FindPlayersAtDistance(p, _distanceInBlocks, _takePair);
		}
		//may return null
		public IEnumerable<Player> FindPlayersAtDistance(Player p, double distInBlocks, Func<Player, Player, bool> takePair)
		{
			int d = (int)Math.Ceiling(distInBlocks);
			d *= d*32*32; //squared distance in position coords
			
			List<Player> players=null;
			
			Vector3I pos = p.Position.ToBlockCoords();
			for (int x=Math.Max(0, pos.X-d); x<=Math.Min(_players.GetLength(0)-1, pos.X+d); ++x)
				for (int y=Math.Max(0, pos.Y-d); y<=Math.Min(_players.GetLength(1)-1, pos.Y+d); ++y)
				{
					if (null==_players[x, y])
						continue;
					for (int i = 0; i < _players[x, y].Count; ++i)
					{
						Player player = _players[x, y][i];
						if (ReferenceEquals(p, player)) //found THE player
							continue;
						if (!ReferenceEquals(_world, player.World)) //player has left the game world
						{
							_players[x, y].RemoveAt(i);
							--i;
							continue;
						}
						if (null != takePair && !takePair(p, player))
							continue;
						if ((p.Position.ToVector3I() - player.Position.ToVector3I()).LengthSquared > d) //too far away
							continue;
						if (null == players) //lasy instantiation
							players = new List<Player>();
						players.Add(player);
					}
				}

			return players;
		}

		public void SetCallEvents(bool call, double distanceInBlocks, Func<Player, Player, bool> takePair)
		{
			_callEvents = call;
			_distanceInBlocks = distanceInBlocks;
			_takePair = takePair;
		}

		private void CallEvent(Player p)
		{
			IEnumerable<Player> players = FindPlayersAtDistance(p, _distanceInBlocks, _takePair);
			if (null!=players)
			{
				EventHandler<PlayersAtDistanceArgs> evt = OnPlayersAtDistance;
				if (null!=evt)
				{
					evt(this, new PlayersAtDistanceArgs(){FromPlayer=p, Others=players});
				}
			}
		}

		private void CheckCoords(ref Vector3I pos)
		{
			CheckDim(ref pos.X, 0);
			CheckDim(ref pos.Y, 1);
		}

		private void CheckDim(ref int coord, int dim)
		{
			if (coord < 0)
				coord = 0;
			if (coord >= _players.GetLength(dim))
				coord = _players.GetLength(dim) - 1;
		}
	}
}
