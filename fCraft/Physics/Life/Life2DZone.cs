using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
	//public class 
	public class Life2DZone : PhysicsTask
	{
		public static readonly Block[] DefaultBlocks = {Block.Air,Block.Green,Block.White,Block.Leaves};
		public const int EmptyIdx = 0;
		public const int NormalIdx = 1;
		public const int DeadIdx = 2;
		public const int NewbornIdx = 3;

		public const int DefaultDelay = 1000;
		public const int DefaultHalfStepDelay = 500;

		public string Name { get; set; }

		private enum Orientation
		{
			X, Y, Z,	
		}
		private enum State
		{
			Starting, HalfStep, FinalizedStep, Stopped
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
		public bool Stopped { get { return State.Stopped == _state;}}

		public Life2DZone(World world, Vector3I[] marks, Block empty, Block normal, Block dead, Block newborn, int halfStepDelay, int delay) : base (world)
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
			
			_normal = normal;
			_empty = empty;
			_dead = dead;
			_newborn = newborn;

			_halfStepDelay = halfStepDelay;
			_delay = delay;
		}

		public void Stop()
		{
			if (!Stopped)
			{
				_state = State.Stopped;
			}
		}

		public void Start()
		{
			if (Stopped)
			{
				_state = State.Starting;
				_world.AddTask(TaskCategory.Scripting, this, 0);
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

		protected override int PerformInternal()
		{
			switch (_state)
			{
				case State.Stopped:
					return 0;
				case State.Starting:
					ReadToArrayByOrientation();
					_state = State.HalfStep;
					goto case State.HalfStep; //fall through c# stile
				case State.HalfStep:
					_life2d.HalfStep();
					_state = State.FinalizedStep;
					if (_halfStepDelay>0)
					{
						DrawByOrientation();
						return _halfStepDelay;
					}
					goto case State.FinalizedStep; //fall through c# stile
				case State.FinalizedStep:
					_state = _life2d.FinalizeStep() ? State.HalfStep : State.Stopped;
					DrawByOrientation();
					return _delay;
			}
			return 0;
		}
	}
}
