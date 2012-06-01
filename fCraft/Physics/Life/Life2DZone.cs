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

	public class Life2DZone : PhysicsTask
	{
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
		private BoundingBox _bounds;
		private Orientation _orientation;
		private Life2d _life2d;
		private Vector3I _coords=new Vector3I(); //for mapping

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
		
		public Life2DZone(string name, World world, Vector3I[] marks) : base (world)
		{
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

			Name = name;
			_normal = DefaultBlocks[NormalIdx];
			_empty = DefaultBlocks[EmptyIdx];
			_dead = DefaultBlocks[DeadIdx];
			_newborn = DefaultBlocks[NewbornIdx];

			_halfStepDelay = DefaultHalfStepDelay;
			_delay = DefaultDelay;
			Torus = false;
			_autoReset = AutoResetMethod.ToRandom;
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
			}
			_state = State.Starting;
			_world.AddTask(TaskCategory.Life, this, 0);
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

		protected override int PerformInternal()
		{
			lock (_life2d)
			{
				switch (_state)
				{
					case State.Stopped:
						_initialState = null;
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
	}
}
