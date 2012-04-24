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

	public class PlantTask : PhysicsTask //one per world
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

		public PlantTask(World world) : base(world)
		{
			lock (world.SyncRoot)
			{
				_rndCoords = new Coords[_map.Width * _map.Length]; //up to 250K per world with grass physics
				for (short i = 0; i < _map.Width; ++i)
					for (short j = 0; j < _map.Length; ++j)
						_rndCoords[i*_map.Length + j] = new Coords() {X = i, Y = j};
				Util.RndPermutate(_rndCoords);
			}
		}

        public override int Perform()
        {
            lock (_world.SyncRoot)
            {
                if (_world.plantPhysics)
                {
                    #region Grass
                    if (_r.NextDouble() > 0.5) //half
                        return Delay;
                    Coords c = _rndCoords[_i];
                    if (++_i >= _rndCoords.Length)
                        _i = 0;
                    for (short z = (short)_map.Height; z >= 0; --z)
                    {
                        if (_world != null)
                        {
                            if (_world.Map.GetBlock(c.X, c.Y, z) == Block.Dirt)
                            {
                                for (int z1 = z; z1 < _world.Map.Bounds.ZMax; z1++)
                                {
                                    Block toCheck = _world.Map.GetBlock(new Vector3I(c.X, c.Y, z1 + 1));
                                    if (makeShadow(toCheck))
                                    {
                                        return Delay;
                                    }
                                    _map.QueueUpdate(new BlockUpdate(null, c.X, c.Y, z, Block.Grass));
                                }
                            }
                        }
                        //Spread to 4 random blocks
                        /*for (int i = 0; i < 4; i++)
                        {
                            int x2 = _r.Next(c.X - 1, c.X + 2);
                            int y2 = _r.Next(c.Y - 1, c.Y + 2);
                            if (_world.Map.GetBlock(x2, y2, z) == Block.Dirt)
                            {
                                for (int z1 = z; z1 < _world.Map.Bounds.ZMax; z1++)
                                {
                                    if (_world != null)
                                    {
                                        Block toCheck = _world.Map.GetBlock(new Vector3I(x2, y2, z1 + 1));
                                        if (makeShadow(toCheck))
                                        {
                                            return Delay;
                                        }
                                        _map.QueueUpdate(new BlockUpdate(null, (short)x2, (short)y2, z, Block.Grass));
                                    }
                                }
                            }
                        }*/
                    #endregion
                        #region Trees
                        if (_world.Map.GetBlock(new Vector3I(c.X, c.Y, z)) == Block.Plant)
                        {
                            Random rand = new Random();
                            int Height = rand.Next(4, 7);
                            for (int x = c.X; x < c.X + 5; x++)
                            {
                                for (int y = c.Y; y < c.Y + 5; y++)
                                {
                                    for (int z1 = z + 1; z1 < z + Height; z1++)
                                    {
                                        if (_world.Map.GetBlock(x, y, z1) != Block.Air)
                                        {
                                            return Delay;
                                        }
                                    }
                                }
                            }

                            for (int x = c.X; x > c.X - 5; x--)
                            {
                                for (int y = c.Y; y > c.Y - 5; y--)
                                {
                                    for (int z1 = z + 1; z1 < z + Height; z1++)
                                    {
                                        if (_world.Map.GetBlock(x, y, z1) != Block.Air)
                                        {
                                            return Delay;
                                        }
                                    }
                                }
                            }
                            if (_world.Map.GetBlock(new Vector3I(c.X, c.Y, z)) == Block.Plant)
                            {
                                string type = null;
                                if (_world.Map.GetBlock(c.X, c.Y, z - 1) == Block.Grass ||
                                    _world.Map.GetBlock(c.X, c.Y, z - 1) == Block.Dirt)
                                {
                                    type = "grass";
                                }
                                else if (_world.Map.GetBlock(c.X, c.Y, z - 1) == Block.Sand)
                                {
                                    type = "sand";
                                }
                                else
                                {
                                    return Delay;
                                }
                                MakeTrunks(_world, new Vector3I(c.X, c.Y, z), Height, type);
                            }
                        }
                        #endregion
                    }
                    return Delay;
                }
                return 0; //do nothing
            }
        }

        public static void MakeTrunks(World w, Vector3I Coords, int Height, string type)
        {
            for (int i = 0; i < Height; i++)
            {
                if (w.Map != null && w.IsLoaded)
                {
                    w.Map.QueueUpdate(new BlockUpdate(null, (short)Coords.X, (short)Coords.Y, (short)(Coords.Z + i), Block.Log));
                }
            }
            if (type.Equals("grass"))
            {
                TreeGeneration.MakeNormalFoliage(w, Coords, Height + 1);
            }
            else if (type.Equals("sand"))
            {
                TreeGeneration.MakePalmFoliage(w, Coords, Height);
            }
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
                if (_world.tntPhysics)
                {
                    if (_world.Map.GetBlock(_pos) == Block.TNT)
                    {
                        _world.Map.QueueUpdate(new BlockUpdate(null, _pos, Block.Air));
                        int Seed = new Random().Next(1, 50);
                        startExplosion(_pos, _owner, _world, Seed);
                        Scheduler.NewTask(t => removeLava(_pos, _owner, _world, Seed)).RunOnce(TimeSpan.FromMilliseconds(300));
                        _world._tntTask = null;
                    }
                    return Delay;
                }
                return 0;  //do nothing
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

    public class BlockSink : PhysicsTask
    {
        private const int Delay = 200;
        private Vector3I _pos; //tnt position
        private int _nextPos;
        private bool _firstMove = true;
        private Block type;
        public BlockSink(World world, Vector3I position, Block Type)
            : base(world)
        {
            _pos = position;
            _nextPos = position.Z - 1;
            type = Type;
        }

        public override int Perform()
        {
            if (_world.waterPhysics)
            {
                if (_firstMove)
                {
                    if (_world.Map.GetBlock(_pos) != type)
                    {
                        return 0;
                    }
                    if (_world.Map.GetBlock(_pos.X, _pos.Y, _nextPos) == Block.Water)
                    {
                        _world.Map.QueueUpdate(new BlockUpdate(null, _pos, Block.Water));
                        _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)_nextPos, type));
                        _nextPos--;
                        _firstMove = false;
                        return Delay;
                    }
                }
                if (_world.Map.GetBlock(_pos.X, _pos.Y, _nextPos + 1) != type)
                {
                    return 0;
                }
                if (_world.Map.GetBlock(_pos.X, _pos.Y, _nextPos) == Block.Water)
                {
                    _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)(_nextPos + 1), Block.Water));
                    _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)_nextPos, type));
                    _nextPos--;
                }
            }
            return Delay;
        }
    }

    public class BlockFloat : PhysicsTask
    {
        private const int Delay = 200;
        private Vector3I _pos; //tnt position
        private int _nextPos;
        private bool _firstMove = true;
        private Block type;

        public BlockFloat(World world, Vector3I position, Block Type)
            : base(world)
        {
            _pos = position;
            _nextPos = position.Z + 1;
            type = Type;
        }

        public override int Perform()
        {
            if (_world.waterPhysics)
            {
                if (_firstMove)
                {
                    if (_world.Map.GetBlock(_pos) != type)
                    {
                        return 0;
                    }
                    if (_world.Map.GetBlock(_pos.X, _pos.Y, _nextPos) == Block.Water)
                    {
                        _world.Map.QueueUpdate(new BlockUpdate(null, _pos, Block.Water));
                        _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)_nextPos, type));
                        _nextPos++;
                        _firstMove = false;
                        return Delay;
                    }
                }
                if (_world.Map.GetBlock(_pos.X, _pos.Y, _nextPos - 1) != type)
                {
                    return 0;
                }
                if (_world.Map.GetBlock(_pos.X, _pos.Y, _nextPos) == Block.Water)
                {
                    _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)(_nextPos - 1), Block.Water));
                    _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)_nextPos, type));
                    _nextPos++;
                }
            }
            return Delay;
        }
    }

    public class Bullet : PhysicsTask
    {
        private const int Delay = 50;
        public Position p;
        public Block _type = Block.Undefined;
        public double rSin;
        public double rCos;
        public double lCos;
        public Position nextPos;
        public short startX;
        public short startY;
        public short startZ;
        public int startB = 4;
        private bool hit = false;
        private Player _sender;
        fCraft.Collections.ConcurrentDictionary<String, Vector3I> bullets = new fCraft.Collections.ConcurrentDictionary<String, Vector3I>();
        public Bullet(World world, Vector3I startPos, Position playerPos, Player sender)
            : base(world)
        {
            p = playerPos;
            _sender = sender;
            startX = (short)startPos.X;
            startY = (short)startPos.Y;
            startZ = (short)startPos.Z;
            nextPos.R = playerPos.R;
            nextPos.L = playerPos.L;
        }

        public override int Perform()
        {
            lock (_world.SyncRoot)
            {
                removal(bullets, _world.Map);
                if (hit)
                {
                    return 0;
                }
                if (_type == Block.Undefined)
                {
                    _type = Block.Admincrete;
                    if (_sender.LastUsedBlockType == Block.Orange)
                        _type = Block.Lava;
                    if (_sender.LastUsedBlockType == Block.Blue)
                        _type = Block.Water;
                    if (_sender.LastUsedBlockType == Block.TNT)
                        _type = Block.TNT;
                }
                hit = false;
                rSin = Math.Sin(((double)(128 - p.R) / 255) * 2 * Math.PI);
                rCos = Math.Cos(((double)(128 - p.R) / 255) * 2 * Math.PI);
                lCos = Math.Cos(((double)(p.L + 64) / 255) * 2 * Math.PI);
                nextPos.X = (short)Math.Round((startX + (double)(rSin * startB))); //math.round improves accuracy
                nextPos.Y = (short)Math.Round((startY + (double)(rCos * startB)));
                nextPos.Z = (short)Math.Round((startZ + (double)(lCos * startB)));
                if (_world.Map.GetBlock(nextPos.X, nextPos.Y, nextPos.Z) == Block.Air || _world.Map.GetBlock(nextPos.X, nextPos.Y, nextPos.Z) == _type)
                {
                    startB++;
                    bullets.TryAdd(new Vector3I(nextPos.X, nextPos.Y, nextPos.Z).ToString(), new Vector3I(nextPos.X, nextPos.Y, nextPos.Z));
                    _world.Map.QueueUpdate(new BlockUpdate(null,
                        (short)nextPos.X,
                        (short)nextPos.Y,
                        (short)nextPos.Z,
                        _type));
                    foreach (Player player in _world.Players)
                    {
                        if ((player.Position.X / 32) == nextPos.X || (player.Position.X / 32 + 1) == nextPos.X || (player.Position.X / 32 - 1) == nextPos.X)
                        {
                            if ((player.Position.Y / 32) == nextPos.Y || (player.Position.Y / 32 + 1) == nextPos.Y || (player.Position.Y / 32 - 1) == nextPos.Y)
                            {
                                if ((player.Position.Z / 32) == nextPos.Z || (player.Position.Z / 32 + 1) == nextPos.Z || (player.Position.Z / 32 - 1) == nextPos.Z)
                                {
                                    if (_world.tntPhysics && _type == Block.TNT && _type != Block.Water && _type != Block.Lava)
                                    {
                                        if (player.LastTimeKilled == null || (DateTime.Now - player.LastTimeKilled).TotalSeconds > 15)
                                        {
                                            int seed = new Random().Next(1, 6);
                                            player.LastTimeKilled = DateTime.Now;
                                            TNT.startExplosion(new Vector3I(nextPos.X, nextPos.Y, nextPos.Z), _sender, _world, seed);
                                            _world.Players.Message("{0}&S was blown up by {1}", player.ClassyName, _sender.ClassyName);
                                            player.TeleportTo(_world.Map.Spawn);
                                            Thread.Sleep(Physics.Physics.Tick);
                                            TNT.removeLava(new Vector3I(nextPos.X, nextPos.Y, nextPos.Z), _sender, _world, seed);
                                            removal(bullets, _world.Map);
                                            hit = true;
                                        }
                                    }

                                    else
                                    {
                                        if (player.LastTimeKilled == null || (DateTime.Now - player.LastTimeKilled).TotalSeconds > 15)
                                        {
                                            player.LastTimeKilled = DateTime.Now;
                                            _world.Players.Message("{0}&S was shot by {1}", player.ClassyName, _sender.ClassyName);
                                            player.TeleportTo(_world.Map.Spawn);
                                            removal(bullets, _world.Map);
                                            hit = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    //tnt
                    if (_world.tntPhysics && _type == Block.TNT)
                    {
                        int seed = new Random().Next(1, 6);
                        TNT.startExplosion(new Vector3I((int)nextPos.X, (int)nextPos.Y, (int)nextPos.Z), _sender, _world, seed);
                        Thread.Sleep(Physics.Physics.Tick);
                        TNT.removeLava(new Vector3I((int)nextPos.X, (int)nextPos.Y, (int)nextPos.Z), _sender, _world, seed);
                        removal(bullets, _world.Map);
                        hit = true;
                    }
                    if (_sender.bluePortal.Count > 0)
                    {
                        if (new Vector3I(nextPos.X, nextPos.Y, nextPos.Z) == _sender.bluePortal[0] ||
                            new Vector3I(nextPos.X, nextPos.Y, nextPos.Z) == _sender.bluePortal[1])
                        {
                            return 0;
                        }
                    }
                    if (_sender.orangePortal.Count > 0)
                    {
                        if (new Vector3I(nextPos.X, nextPos.Y, nextPos.Z) == _sender.orangePortal[0] ||
                            new Vector3I(nextPos.X, nextPos.Y, nextPos.Z) == _sender.orangePortal[1])
                        {
                            return 0;
                        }
                    }
                    //blue portal
                    if (_type == Block.Water)
                    {
                        if (CanPlacePortal(nextPos.X, nextPos.Y, nextPos.Z, _world.Map))
                        {
                            if (_sender.bluePortal.Count > 0)
                            {
                                int i = 0;
                                foreach (Vector3I block in _sender.bluePortal)
                                {
                                    _world.Map.QueueUpdate(new BlockUpdate(null, block, _sender.blueOld[i]));
                                    i++;
                                }
                                _sender.blueOld.Clear();
                                _sender.bluePortal.Clear();
                            }

                            _sender.blueOld.Add(_world.Map.GetBlock(nextPos.X, nextPos.Y, nextPos.Z));
                            _sender.blueOld.Add(_world.Map.GetBlock(nextPos.X, nextPos.Y, nextPos.Z + 1));
                            _sender.orangeOut = nextPos.R;
                            for (double z = nextPos.Z; z < nextPos.Z + 2; z++)
                            {
                                _world.Map.QueueUpdate(new BlockUpdate(null, (short)(nextPos.X), (short)(nextPos.Y), (short)z, Block.Water));
                                _sender.bluePortal.Add(new Vector3I((int)nextPos.X, (int)nextPos.Y, (int)z));
                            }
                            return 0;
                        }
                    }

                        //orange portal
                    else if (_type == Block.Lava)
                    {
                        if (CanPlacePortal(nextPos.X, nextPos.Y, nextPos.Z, _world.Map))
                        {
                            if (_sender.orangePortal.Count > 0)
                            {
                                int i = 0;
                                foreach (Vector3I block in _sender.orangePortal)
                                {
                                    _world.Map.QueueUpdate(new BlockUpdate(null, block, _sender.orangeOld[i]));
                                    i++;
                                }
                                _sender.orangeOld.Clear();
                                _sender.orangePortal.Clear();
                            }
                            _sender.orangeOld.Add(_world.Map.GetBlock(nextPos.X, nextPos.Y, nextPos.Z));
                            _sender.orangeOld.Add(_world.Map.GetBlock(nextPos.X, nextPos.Y, nextPos.Z + 1));
                            _sender.blueOut = nextPos.R;
                            for (double z = nextPos.Z; z < nextPos.Z + 2; z++)
                            {
                                _world.Map.QueueUpdate(new BlockUpdate(null, (short)(nextPos.X), (short)(nextPos.Y), (short)z, Block.Lava));
                                _sender.orangePortal.Add(new Vector3I((int)nextPos.X, (int)nextPos.Y, (int)z));
                            }
                            return 0;
                        }
                    }
                    else
                    {
                        return 0;
                    }
                }
                return Delay;
            }
        }
        public static bool CanPlacePortal(short x, short y, short z, Map map)
        {
            int Count = 0;
            for (short Z = z; Z < z + 2; Z++)
            {
                Block check = map.GetBlock(x, y, Z);
                if (check != Block.Air && check != Block.Water && check != Block.Lava)
                {
                    Count++;
                }
            }
            if (Count == 2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public void removal(fCraft.Collections.ConcurrentDictionary<String, Vector3I> bullets, Map map)
        {
            if(bullets.Values.Count > 0)
            {
                foreach (Vector3I bp in bullets.Values)
                {
                    map.QueueUpdate(new BlockUpdate(null,
                        (short)bp.X,
                        (short)bp.Y,
                        (short)bp.Z,
                        Block.Air));
                    Vector3I removed;
                    bullets.TryRemove(bp.ToString(), out removed);
                }
            }
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

