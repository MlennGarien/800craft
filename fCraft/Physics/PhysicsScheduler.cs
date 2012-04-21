/*800Craft Physics; Copyright Jonty800 and LaoTszy 2012*/
using System;
using System.Threading;
using fCraft.Physics;
using System.Diagnostics;
using System.Collections.Generic;
using Util = RandomMaze.MazeUtil;
using fCraft.Drawing;

namespace fCraft
{
public class PhysScheduler
	{
		private MinBinaryHeap<PhysicsTask, Int64> _tasks = new MinBinaryHeap<PhysicsTask, Int64>();
		private Stopwatch _watch = new Stopwatch(); //a good counter of elapsed milliseconds
		private World _owner;
		private EventWaitHandle _continue = new EventWaitHandle(false, EventResetMode.AutoReset);
		private EventWaitHandle _stop = new EventWaitHandle(false, EventResetMode.ManualReset);
		private Thread _thread;

		public bool Started { get { return null != _thread; } }

		public PhysScheduler(World owner)
		{
			_owner = owner;
			_watch.Reset();
			_watch.Start();
		}

		private void ProcessTasks()
		{
			try
			{
				WaitHandle[] handles = new WaitHandle[] { _continue, _stop };
				int timeout = Timeout.Infinite;
				for (; ; )
				{
					int w = WaitHandle.WaitAny(handles, timeout);
					if (1 == w) //stop
						break;
					PhysicsTask task;
					lock (_tasks)
					{
						if (_tasks.Size == 0) //sanity check
						{
							timeout = Timeout.Infinite;
							continue; //nothing to do
						}
						task = _tasks.Head();
						Int64 now = _watch.ElapsedMilliseconds;
						if (task.DueTime <= now) //due time!
							_tasks.RemoveHead();
						else
						{
							timeout = (int)(task.DueTime - now); //here the time difference should not exceed 24 days :)
							continue;
						}
					}
					int delay = task.Deleted ? 0 : task.Perform(); //dont perform deleted tasks 
					lock (_tasks)
					{
						Int64 now = _watch.ElapsedMilliseconds;
						if (delay > 0)
						{
							task.DueTime = now + delay;
							_tasks.Add(task);
						}
						timeout = _tasks.Size > 0 ? Math.Max((int)(_tasks.Head().DueTime - now), 0) : Timeout.Infinite; //here the time difference should not exceed 24 days :)
					}
				}
			}
			catch (Exception e)
			{
                Logger.Log(LogType.Error, "ProcessPhysicsTasks: " + e);
			}
		}

		public void Start()
		{
			if (null!=_thread)
			{
				//log error, running already
				return;
			}
			if (_tasks.Size>0)
				_continue.Set();
			_thread = new Thread(ProcessTasks);
			_thread.Start();
		}

		public void Stop()
		{
			if (null==_thread)
			{
				//log error, not running
				return;
			}
			_stop.Set();
			if (_thread.Join(10000))
			{
				//blocked?
				_thread.Abort(); //very bad
			}
			_thread = null;
			_tasks.Clear();
		}

		public void AddTask(PhysicsTask task, int delay)
		{
			task.DueTime += _watch.ElapsedMilliseconds + delay;
			lock (_tasks)
			{
				_tasks.Add(task);
			}
			_continue.Set();
		}
	}

	/// <summary>
	/// Base class for physic tasks
	/// </summary>
    public abstract class PhysicsTask : IHeapKey<Int64>
    {
		/// <summary>
		/// the task due time in milliseconds since some start moment
		/// </summary>
		public Int64 DueTime;

		/// <summary>
		/// The flag indicating that task must not be performed when due.
		/// This flag is introduced because the heap doesnt allow deletion of elements.
		/// It is possible to implement but far too complicated than just marking elements like that.
		/// </summary>
		public bool Deleted=false;

		protected World _world;
		protected Map _map;
		
		protected PhysicsTask(World world) //a task must be created under the map syn root
        {
			lock (world.SyncRoot)
			{
				_world = world;
				_map = world.Map;
			}
        }
		
		public Int64 GetHeapKey()
		{
			return DueTime;
		}

		/// <summary>
		/// Performs the action. The returned value is used as the rescadule delay. If 0 - the task is completed and
		/// should not be rescheduled
		/// </summary>
		/// <returns></returns>
		public abstract int Perform(); 
    }

	public class GrassTask : PhysicsTask //one per world
	{
		private struct Coords //Tuple class is a class thus imposing a significant overhead
		{
			public short X;
			public short Y;
		}
		private const int Delay = 25;
		private short _i = 0; //current position
		private Random _r = new Random();

		private Coords[] _rndCoords;

		public GrassTask(World world) : base(world)
		{
			lock (world.SyncRoot)
			{
				_rndCoords = new Coords[_map.Width * _map.Length]; //up to 250K per world with grass physics
				for (short i = 0; i < _map.Width; ++i)
					for (short j = 0; j < _map.Length; ++j)
						_rndCoords[i*_map.Length + j] = new Coords() {X = i, Y = j};
				Util.RndPermutate(_rndCoords);
			}
            Perform();
		}

		public override int Perform()
		{
			lock (_world.SyncRoot)
			{
				if (_r.NextDouble() > 0.5) //half
					return Delay;
				Coords c = _rndCoords[_i];
				if (++_i >= _rndCoords.Length)
					_i = 0;
                for (short z = (short)_map.Height; z >= 0; --z)
                {
                    if (CanPutGrassOn(new Vector3I(c.X, c.Y, z), _world))
                    {
                        _map.QueueUpdate(new BlockUpdate(null, c.X, c.Y, z, Block.Grass));
                    }
                    //Spread to 4 random blocks
                    for (int i = 0; i < 4; i++)
                    {
                        int x2 = _r.Next(c.X - 1, c.X + 2);
                        int y2 = _r.Next(c.Y - 1, c.Y + 2);
                        if (CanPutGrassOn(new Vector3I(x2, y2, z), _world))
                        {
                            _map.QueueUpdate(new BlockUpdate(null, (short)x2, (short)y2, z, Block.Grass));
                        }
                    }
                }
				return Delay;
			}
		}

        public static bool CanPutGrassOn(Vector3I block, World world)
        {
            if (world.Map.GetBlock(block) != Block.Dirt)
            {
                return false;
            }
            for (int z = block.Z; z < world.Map.Bounds.ZMax; z++)
            {
                Block toCheck = world.Map.GetBlock(new Vector3I(block.X, block.Y, z + 1));
                if (makeShadow(toCheck))
                {
                    return false;
                }
            }
            return true;
        }
        public static bool makeShadow(Block block)
        {
            switch (block)
            {
                case Block.Air:
                case Block.Glass:
                case Block.Leaves:
                case Block.YellowFlower:
                case Block.RedFlower:
                case Block.BrownMushroom:
                case Block.RedMushroom:
                case Block.Plant:
                    return false;
                default:
                    return true;
            }
        }
	}

    public class TNT : PhysicsTask
    {
        private const int Delay = 3000;
        private Vector3I _pos; //tnt position
        private Player _owner;

        public TNT(World world, Vector3I position, Player owner)
            : base(world)
        {
            _pos = position;
            _owner = owner;
        }
        public override int Perform()
        {
            lock (_world.SyncRoot)
            {
                if (_world.Map.GetBlock(_pos) == Block.TNT)
                {
                    _world.Map.QueueUpdate(new BlockUpdate(null, _pos, Block.Air));
                    int Seed = new Random().Next(1, 50);
                    startExplosion(_pos, _owner, _world, Seed);
                    Scheduler.NewTask(t => removeLava(_pos, _owner, _world, Seed)).RunOnce(TimeSpan.FromMilliseconds(300));
                }
                return Delay;
            }
        }
        public static void startExplosion(Vector3I Coords, Player player, World world, int Seed)
        {
            if (world.Map != null && world.IsLoaded)
            {
                SphereDrawOperation operation = new SphereDrawOperation(player);
                MarbledBrush brush = new MarbledBrush(Block.Lava, 1);
                Vector3I secPos = new Vector3I(Coords.X + 4, Coords.Y, Coords.Z);
                Vector3I[] marks = { Coords, secPos };
                operation.Brush = brush;
                brush.Seed = Seed;
                operation.Prepare(marks);
                operation.AnnounceCompletion = false;
                operation.Context = BlockChangeContext.Explosion;
                operation.Begin();
            }
        }


        public static void removeLava(Vector3I Coords, Player player, World world, int Seed)
        {
            if (world.Map != null && world.IsLoaded)
            {
                SphereDrawOperation operation = new SphereDrawOperation(player);
                MarbledBrush brush = new MarbledBrush(Block.Air, 1);
                Vector3I secPos = new Vector3I(Coords.X + 4, Coords.Y, Coords.Z);
                Vector3I[] marks = { Coords, secPos };
                operation.Brush = brush;
                brush.Seed = Seed;
                operation.Prepare(marks);
                operation.AnnounceCompletion = false;
                operation.Context = BlockChangeContext.Explosion;
                operation.Begin();
            }
        }
        public static void TNTClick(object sender, Events.PlayerClickedEventArgs e)
        {
            World world = e.Player.World;
            if (world.Map.GetBlock(e.Coords) == Block.TNT)
            {
                lock (world.SyncRoot)
                {
                    world.Map.QueueUpdate(new BlockUpdate(null, e.Coords, Block.Air));
                    int Seed = new Random().Next(1, 50);
                    startExplosion(e.Coords, e.Player, world, Seed);
                    Scheduler.NewTask(t => removeLava(e.Coords, e.Player, world, Seed)).RunOnce(TimeSpan.FromMilliseconds(300));
                }
            }
        }
    }


	public class Bullet : PhysicsTask
	{
		private const int Delay = 50;
		private Vector3I _pos; //current position
		private Vector3F _direction;
		private Player _owner;

		public Bullet(World world, Vector3I position, Vector3F direction, Player owner)
			: base(world) //under sync root
		{
			_pos = position;
			_direction = direction;
			_owner = owner;
		}

		public override int Perform()
		{
			//under sync root
			//move bullet

            /*if reached border
            if (!_world.Map.InBounds()){
                return 0;*/
			//}else{
			//	return Delay;
            return 0;
		}
	}

	public interface IHeapKey<out T> where T : IComparable
	{
		T GetHeapKey();
	}

	/// <summary>
	/// Min binary heap implementation.
	/// Note that the base array size is never decreased, i.e. Clear just moves the internal pointer to the first element.
	/// </summary>
	public class MinBinaryHeap<T, K> where T:IHeapKey<K> where K:IComparable
	{
		private readonly List<T> _heap = new List<T>();
		private int _free = 0;

		/// <summary>
		/// Adds an element.
		/// </summary>
		/// <param name="t">The t.</param>
		public void Add(T t)
		{
			int me = _free++;
			if (_heap.Count > me)
				_heap[me] = t;
			else
				_heap.Add(t);
			K myKey = t.GetHeapKey();
			while (me > 0)
			{
				int parent = ParentIdx(me);
				if (_heap[parent].GetHeapKey().CompareTo(myKey)<0)
					break;
				Swap(me, parent);
				me = parent;
			}
		}

		/// <summary>
		/// Head of this heap. This call assumes that size was checked before accessing the head element.
		/// </summary>
		/// <returns>Head element.</returns>
		public T Head()
		{
			return _heap[0];
		}

		/// <summary>
		/// Removes the head. This call assumes that size was checked before removing the head element.
		/// </summary>
		public void RemoveHead()
		{
			_heap[0] = _heap[--_free];
			_heap[_free] = default(T); //to enable garbage collection for the deleted item when necessary
			if (0 == _free)
				return;
			int me = 0;
			K myKey = _heap[0].GetHeapKey();

			for (; ; )
			{
				int kid1, kid2;
				Kids(me, out kid1, out kid2);
				if (kid1 >= _free)
					break;
				int minKid;
				K minKidKey;
				if (kid2 >= _free)
				{
					minKid = kid1;
					minKidKey = _heap[minKid].GetHeapKey();
				}
				else
				{
					K key1 = _heap[kid1].GetHeapKey();
					K key2 = _heap[kid2].GetHeapKey();
					if (key1.CompareTo(key2) < 0)
					{
						minKid = kid1;
						minKidKey = key1;
					}
					else
					{
						minKid = kid2;
						minKidKey = key2;
					}
				}
				if (myKey.CompareTo(minKidKey)>0)
				{
					Swap(me, minKid);
					me = minKid;
				}
				else
					break;
			}
		}

		/// <summary>
		/// Heap size.
		/// </summary>
		public int Size
		{
			get
			{
				return _free;
			}
		}

		public void Clear()
		{
			for (int i = 0; i < _free; ++i)
				_heap[i] = default(T); //enables garbage collecting for the deleted elements
			_free = 0;
		}

		private static int ParentIdx(int idx)
		{
			return (idx - 1) / 2;
		}

		private static void Kids(int idx, out int kid1, out int kid2)
		{
			kid1 = 2 * idx + 1;
			kid2 = kid1 + 1;
		}

		private void Swap(int i1, int i2)
		{
			T t = _heap[i1];
			_heap[i1] = _heap[i2];
			_heap[i2] = t;
		}
	}
}

