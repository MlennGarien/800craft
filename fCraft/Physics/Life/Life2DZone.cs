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
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace fCraft
{
	public class CircularBuffer<T> where T:IEquatable<T>
	{
		private T[] _a;
		private int _current;
		private bool _full = false;

		public CircularBuffer(int size)
		{
			_a=new T[size];
			_current = 0;
		}

		public bool Contains(T t)
		{
			for (int i=0; i<(_full?_a.Length:_current); ++i)
				if (t.Equals(_a[i]))
					return true;
			return false;
		}

		public void Add(T t)
		{
			_a[_current] = t;
			if (++_current>=_a.Length)
			{
				_current = 0;
				_full = true;
			}
		}

		public void Clear()
		{
			_full = false;
			_current = 0;
		}
	}

	public enum AutoResetMethod
	{
		None,
		ToInitial,
		ToRandom,
	}

	public class Life2DZone
	{
		public int MaxSize = 2500;

		public static readonly Block[] DefaultBlocks = {Block.Air,Block.Red,Block.Black,Block.Brick};
		public const int EmptyIdx = 0;
		public const int NormalIdx = 1;
		public const int DeadIdx = 2;
		public const int NewbornIdx = 3;

		public const int DefaultDelay = 200;
		public const int DefaultHalfStepDelay = 50;

		public const int LongDelay = 1000;

		private const int LastStateHashesSize = 10;
		
		public string Name { get; private set; }

		private enum Orientation
		{
			X, Y, Z,	
		}
		private enum State
		{
			Starting, HalfStep, FinalizedStep, Stopped, Resetting, Reinit,
		}

		private Map _map;
		private BoundingBox _bounds;
		private Orientation _orientation;
		private Life2d _life2d;
		private Vector3I _coords=new Vector3I(); //used for remapping of axes, see XXXByOrientation methods below

		private Block _normal;
		private Block _empty;
		private Block _dead;
		private Block _newborn;

		private State _state=State.Stopped;

		private int _halfStepDelay;
		private int _delay;
		private AutoResetMethod _autoReset;
		private byte[,] _initialState;
		private CircularBuffer<int> _stateHashes = new CircularBuffer<int>(LastStateHashesSize);

		public bool Stopped { get { lock (_life2d) { return State.Stopped == _state; } } }

		public Block Normal { get { lock (_life2d) { return _normal; } } set { lock (_life2d) { _normal = value; } } }
		public Block Empty { get { lock (_life2d) { return _empty; } } set { lock (_life2d) { _empty = value; } } }
		public Block Dead { get { lock (_life2d) { return _dead; } } set { lock (_life2d) { _dead = value; } } }
		public Block Newborn { get { lock (_life2d) { return _newborn; } } set { lock (_life2d) { _newborn = value; } } }
		public int HalfStepDelay { get { lock (_life2d) { return _halfStepDelay; } } set { lock (_life2d) { _halfStepDelay = value; } } }
		public int Delay { get { lock (_life2d) { return _delay; } } set { lock (_life2d) { _delay = value; } } }
		public bool Torus { get { lock (_life2d) { return _life2d.Torus; } } set { lock (_life2d) { _life2d.Torus = value; } } }
		public AutoResetMethod AutoReset { get { lock (_life2d) { return _autoReset; } } set { lock (_life2d) { _autoReset = value; } } }

		public string CreatorName { get; set; }
		public string MinRankToChange { get; set; }
		
		public Life2DZone(string name, Map map, Vector3I[] marks, Player creator, string minRankToChange)
		{
			_map = map;
			_bounds=new BoundingBox(marks[0], marks[1]);
			if (_bounds.Dimensions.X == 1 && _bounds.Dimensions.Y > 1 && _bounds.Dimensions.Z > 1)
			{
				_orientation = Orientation.X;
				_life2d = new Life2d(_bounds.Dimensions.Y, _bounds.Dimensions.Z);
				_coords.X = _bounds.XMin;
			}
			else if (_bounds.Dimensions.X > 1 && _bounds.Dimensions.Y == 1 && _bounds.Dimensions.Z > 1)
			{
				_orientation = Orientation.Y;
				_life2d = new Life2d(_bounds.Dimensions.X, _bounds.Dimensions.Z);
				_coords.Y = _bounds.YMin;
			}
			else if (_bounds.Dimensions.X > 1 && _bounds.Dimensions.Y > 1 && _bounds.Dimensions.Z == 1)
			{
				_orientation = Orientation.Z;
				_life2d = new Life2d(_bounds.Dimensions.X, _bounds.Dimensions.Y);
				_coords.Z = _bounds.ZMin;
			}
			else
				throw new ArgumentException("bounds must be a 2d rectangle");

			if (_bounds.Dimensions.X*_bounds.Dimensions.Y*_bounds.Dimensions.Z>MaxSize)
				throw new ArgumentException("The life if too large. Width*Length must be less or equal than "+MaxSize);

			CheckPermissionsToDraw(creator);

			Name = name;
			CreatorName = creator.Name;
			MinRankToChange = minRankToChange;
			_normal = DefaultBlocks[NormalIdx];
			_empty = DefaultBlocks[EmptyIdx];
			_dead = DefaultBlocks[DeadIdx];
			_newborn = DefaultBlocks[NewbornIdx];

			_halfStepDelay = DefaultHalfStepDelay;
			_delay = DefaultDelay;
			Torus = false;
			_autoReset = AutoResetMethod.ToRandom;
            _initialState = _life2d.GetArrayCopy();
		}

		private void CheckPermissionsToDraw(Player creator)
		{
			for (int i=_bounds.XMin; i<=_bounds.XMax; ++i)
				for (int j=_bounds.YMin; j<=_bounds.YMax; ++j)
					for (int k=_bounds.ZMin; k<=_bounds.ZMax; ++k)
						if (creator.CanPlace(_map, new Vector3I(i, j, k), DefaultBlocks[EmptyIdx], BlockChangeContext.Physics)!=CanPlaceResult.Allowed)
							throw new ArgumentException("This life intersects with prohibited zones/blocks. Creation denied.");
		}

		public void Stop()
		{
			lock (_life2d)
			{
				if (!Stopped)
				{
					_state = State.Stopped;
				}
			}
		}

		public void Start()
		{
			lock (_life2d)
			{
				if (!Stopped)
					return;
				_state = State.Starting;
			}
			
			World w = _map.World;
			if (null == w)
			{
				Logger.Log(LogType.Error, "Life: cant start life in a map without a world");
				return;
			}
			w.AddTask(TaskCategory.Life, new Task(w, this), 0);
		}

		public void Resume() //the map with this life was just loaded from the file
		{
			lock (_life2d)
			{
				if (Stopped)
					return;
			}
			World w = _map.World;
			if (null == w)
			{
				Logger.Log(LogType.Error, "Life: cant resume life in a map without a world");
				return;
			}
			w.AddTask(TaskCategory.Life, new Task(w, this), 0);
		}

		private class Task : PhysicsTask
		{
			private Life2DZone _life;
			public Task(World w, Life2DZone life) : base(w) {_life = life;}
			protected override int PerformInternal()
			{
				return _life.PerformInternal();
			}
		}

		private void ReadToArray(ref int i, ref int j, int minI, int maxI, int minJ, int maxJ)
		{
			_life2d.Clear();
			for (i = minI; i <= maxI; ++i)
				for (j = minJ; j <= maxJ; ++j)
				{
					if (_empty != _map.GetBlock(_coords))
						_life2d.Set(i - minI, j - minJ);
				}
		}

		private void Draw(ref int i, ref int j, int minI, int maxI, int minJ, int maxJ)
		{
			for (i = minI; i <= maxI; ++i)
				for (j = minJ; j <= maxJ; ++j)
				{
					switch (_life2d.Get(i-minI, j-minJ))
					{
						case Life2d.Nothing:
							SetBlock(_empty);
							break;
						case Life2d.Normal:
							SetBlock(_normal);
							break;
						case Life2d.Newborn:
							SetBlock(_newborn);
							break;
						case Life2d.Dead:
							SetBlock(_dead);
							break;
					}
				}
		}

		private void SetBlock(Block b)
		{
			if (_map.GetBlock(_coords)!=b)
				_map.QueueUpdate(new BlockUpdate(null, _coords, b));
		}

		private void ReadToArrayByOrientation()
		{
			switch (_orientation)
			{
				case Orientation.X:
					ReadToArray(ref _coords.Y, ref _coords.Z, _bounds.YMin, _bounds.YMax, _bounds.ZMin, _bounds.ZMax);
					return;
				case Orientation.Y:
					ReadToArray(ref _coords.X, ref _coords.Z, _bounds.XMin, _bounds.XMax, _bounds.ZMin, _bounds.ZMax);
					return;
				case Orientation.Z:
					ReadToArray(ref _coords.X, ref _coords.Y, _bounds.XMin, _bounds.XMax, _bounds.YMin, _bounds.YMax);
					return;
			}
		}
		private void DrawByOrientation()
		{
			switch (_orientation)
			{
				case Orientation.X:
					Draw(ref _coords.Y, ref _coords.Z, _bounds.YMin, _bounds.YMax, _bounds.ZMin, _bounds.ZMax);
					return;
				case Orientation.Y:
					Draw(ref _coords.X, ref _coords.Z, _bounds.XMin, _bounds.XMax, _bounds.ZMin, _bounds.ZMax);
					return;
				case Orientation.Z:
					Draw(ref _coords.X, ref _coords.Y, _bounds.XMin, _bounds.XMax, _bounds.YMin, _bounds.YMax);
					return;
			}
		}

		private int PerformInternal()
		{
			lock (_life2d)
			{
				switch (_state)
				{
					case State.Stopped:
						return 0;
					case State.Starting:

						ReadToArrayByOrientation();
						_initialState = _life2d.GetArrayCopy();
						_stateHashes.Clear();
						
						_state = State.HalfStep;
						goto case State.HalfStep; //fall through c# stile
					case State.HalfStep:
						_life2d.HalfStep();
						_state = State.FinalizedStep;
						if (_halfStepDelay > 0)
						{
							DrawByOrientation();
							return _halfStepDelay;
						}
						goto case State.FinalizedStep; //fall through c# stile
					case State.FinalizedStep:
						_state = _life2d.FinalizeStep() ? State.HalfStep : (_autoReset!=AutoResetMethod.None ? State.Resetting : State.Stopped);
						if (_autoReset!=AutoResetMethod.None && State.HalfStep==_state)
						{//check short periodical repetition
							if (_stateHashes.Contains(_life2d.Hash))
								_state = State.Resetting;
							else
								_stateHashes.Add(_life2d.Hash);
						}
						DrawByOrientation();
						return _delay;
					
					case State.Resetting:
					    if (_autoReset==AutoResetMethod.None) //has been just changed?
					    {
					    	_state = State.Stopped;
					    	return 0;
					    }
						_life2d.Clear();
						_stateHashes.Clear();
						DrawByOrientation();
						_state = State.Reinit;
						return LongDelay;

					case State.Reinit:
						if (_autoReset == AutoResetMethod.None) //has been just changed?
						{
							_state = State.Stopped;
							return 0;
						}
						Reinit();
						_state = State.HalfStep;
						DrawByOrientation();
						return _delay;
				}
			}
			return 0;
		}

		private void Reinit()
		{
			switch (_autoReset)
			{
				case AutoResetMethod.ToInitial:
					if (null == _initialState)
						goto case AutoResetMethod.ToRandom;
					_life2d.SetState(_initialState);
					return;
				case AutoResetMethod.ToRandom:
					_life2d.SetStateToRandom();
					return;
			}
		}

		public string Serialize()
		{
			SerializedData data=new SerializedData(this);
			DataContractSerializer serializer = new DataContractSerializer(typeof(SerializedData));
			MemoryStream s = new MemoryStream();
			serializer.WriteObject(s, data);
			return Convert.ToBase64String(s.ToArray());
		}

		private Life2DZone(string name, Map map)
		{
			_map = map;
			Name = name;
		}

		public static Life2DZone Deserialize(string name, string sdata, Map map)
		{
			Life2DZone life=new Life2DZone(name, map);
			
			byte[] bdata = Convert.FromBase64String(sdata);
			DataContractSerializer serializer = new DataContractSerializer(typeof(SerializedData));
			MemoryStream s=new MemoryStream(bdata);
			SerializedData data = (SerializedData)serializer.ReadObject(s);
			
			data.UpdateLife2DZone(life);
			return life;
		}

		[DataContract]
		private class SerializedData
		{
			[DataMember] public BoundingBox Bounds;
			[DataMember] public Orientation Orient;

			[DataMember] public Block Normal;
			[DataMember] public Block Empty;
			[DataMember] public Block Dead;
			[DataMember] public Block Newborn;

			[DataMember] public State RuntimeState;

			[DataMember] public int HalfStepDelay;
			[DataMember] public int Delay;
			[DataMember] public AutoResetMethod AutoReset;
			[DataMember] public byte[] CurrentState;
			[DataMember] public byte[] InitialState;

			[DataMember] public int Dim0, Dim1;

			[DataMember] public string CreatorName;
			[DataMember] public string MinRankToChange;

			public SerializedData(Life2DZone life)
			{
				lock (life._life2d)
				{
					Bounds = life._bounds;
					Orient = life._orientation;
					
					Normal = life._normal;
					Empty = life._empty;
					Dead = life._dead;
					Newborn = life._newborn;

					RuntimeState = life._state;
					HalfStepDelay = life._halfStepDelay;
					Delay = life._delay;
					AutoReset = life._autoReset;

					CreatorName = life.CreatorName;
					MinRankToChange = life.MinRankToChange;

					CurrentState = To1DArray(life._life2d.GetArrayCopy(), out Dim0, out Dim1);
					InitialState = To1DArray((byte[,])life._initialState.Clone(), out Dim0, out Dim1);
				}
			}

			public void UpdateLife2DZone(Life2DZone life)
			{
				//the life is not running, no locks needed
				life._bounds = Bounds;
				//we only will be needed one assignment depending on the orientation (see the public life constructor) 
				//but it doesnt hurt to assign all variables, which is shorter than a switch statement
				life._coords.X = Bounds.XMin;
				life._coords.Y = Bounds.YMin;
				life._coords.Z = Bounds.ZMin;
				life._orientation = Orient;

				life._normal = Normal;
				life._empty = Empty;
				life._dead = Dead;
				life._newborn = Newborn;

				life._state = RuntimeState;
				life._halfStepDelay = HalfStepDelay;
				life._delay = Delay;
				life._autoReset = AutoReset;

				life.CreatorName = CreatorName;
				life.MinRankToChange = MinRankToChange;

				life._life2d=new Life2d(Dim0, Dim1);
				life._life2d.SetState(To2DArray(CurrentState, Dim0, Dim1));
				life._initialState = To2DArray(InitialState, Dim0, Dim1);
			}

			private static byte[] To1DArray(byte[,] a, out int dim0, out int dim1)
			{
				byte[] aa=new byte[a.GetLength(0)*a.GetLength(1)];
				int idx = 0;
				dim0 = a.GetLength(0);
				dim1 = a.GetLength(1);
				for (int i = 0; i < dim0; ++i)
					for (int j = 0; j < dim1; ++j)
						aa[idx++] = a[i, j];
				return aa;
			}

			private static byte[,] To2DArray(byte[] a, int dim0, int dim1)
			{
				if (a.Length!=dim0*dim1)
					throw new ArgumentException("wrong dimensions");
				byte[,] aa=new byte[dim0, dim1];
				int idx = 0;
				for (int i = 0; i < dim0; ++i)
					for (int j = 0; j < dim1; ++j)
						a[idx++] = aa[i, j];
				return aa;
			}
		}
	}


}
