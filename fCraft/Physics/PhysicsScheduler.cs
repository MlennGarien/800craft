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
    public bool Deleted = false;

    protected World _world;
    protected Map _map;

    protected PhysicsTask(World world) //a task must be created under the map syn root
    {
        if (null == world)
            throw new ArgumentNullException("world");
        //if the map is null the task will not be rescheduled
        //if (null == world.Map)
        //    throw new ArgumentException("world has no map");
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
    /// Performs the action. The returned value is used as the reschedule delay. If 0 - the task is completed and
    /// should not be rescheduled
    /// </summary>
    /// <returns></returns>
    public int Perform()
    {
        lock (_world.SyncRoot)
        {
            if (null == _map || !ReferenceEquals(_map, _world.Map))
                return 0;
            return PerformInternal();
        }
    }
    /// <summary>
    /// The real implementation of the action
    /// </summary>
    /// <returns></returns>
    protected abstract int PerformInternal();
}

#region Sand Physics
public class SandTask : PhysicsTask
{
    private const int Delay = 200;
    private Vector3I _pos;
    private int _nextPos;
    private bool _firstMove = true;
    private Block _type;
    private Block _toReplace;
    public SandTask(World world, Vector3I position, Block Type)
        : base(world)
    {
        _pos = position;
        _nextPos = position.Z - 1;
        _type = Type;
    }

    protected override int PerformInternal()
    {
        lock (_world.SyncRoot)
        {
            if (_world.sandPhysics)
            {
                Block nblock = _world.Map.GetBlock(_pos.X, _pos.Y, _nextPos);
                _toReplace = _world.Map.GetBlock(_pos.X, _pos.Y, _nextPos - 1);
                if (_firstMove)
                {
                    if (_world.Map.GetBlock(_pos) != _type)
                    {
                        return 0;
                    }
                    if (_world.Map.GetBlock(_pos.X, _pos.Y, _nextPos) == Block.Air)
                    {
                        _world.Map.QueueUpdate(new BlockUpdate(null, _pos, Block.Air));
                        _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)_nextPos, _type));
                        _nextPos--;
                        _firstMove = false;
                        return Delay;
                    }
                }
                if (_world.Map.GetBlock(_pos.X, _pos.Y, _nextPos) == Block.Air)
                {
                    return 0;
                }
                if (Physics.Physics.BlockThrough(nblock))
                {
                    _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)(_nextPos + 1), Block.Air));
                    _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)_nextPos, _type));
                    _nextPos--;
                }
            }
            return Delay;
        }
    }
}
    
#endregion

#region Plant Physics

public class GrassTask : PhysicsTask //one per world
{
    private struct Coords //System.Tuple is a class and comparing to this struct causes a significant overhead, thus not used here
    {
        public short X;
        public short Y;
    }
    private const int Delay = 150; //not too often, since it has to scan the whole column at some (x, y)
    private short _i = 0; //current position

    private Coords[] _rndCoords;

    public GrassTask(World world)
        : base(world)
    {
        int w, l;
        lock (world.SyncRoot)
        {
            w = _map.Width;
            l = _map.Length;
        }
        _rndCoords = new Coords[w * l]; //up to 250K per world with grass physics
        for (short i = 0; i < w; ++i)
            for (short j = 0; j < l; ++j)
                _rndCoords[i * l + j] = new Coords() { X = i, Y = j };
        Util.RndPermutate(_rndCoords);
    }

    protected override int PerformInternal()
    {
        if (!_world.plantPhysics)
            return 0;

        Coords c = _rndCoords[_i];
        if (++_i >= _rndCoords.Length)
            _i = 0;

        bool shadowed = false;
        for (short z = (short)(_map.Height - 1); z >= 0; --z)
        {
            Block b = _map.GetBlock(c.X, c.Y, z);

            if (!shadowed && Block.Dirt == b) //we have found dirt and there were nothing casting shadows above, so change it to grass and return
            {
                _map.QueueUpdate(new BlockUpdate(null, c.X, c.Y, z, Block.Grass));
                shadowed = true;
                continue;
            }

            //since we scan the whole world anyway add the plant task for each not shadowed plant found - it will not harm
            if (!shadowed && Block.Plant == b)
            {
                _world.AddPlantTask(c.X, c.Y, z);
                continue;
            }

            if (shadowed && Block.Grass == b) //grass should die when shadowed
            {
                _map.QueueUpdate(new BlockUpdate(null, c.X, c.Y, z, Block.Dirt));
                continue;
            }

            if (!shadowed)
                shadowed = CastsShadow(b); //check if the rest of the column is under a block which casts shadow and thus prevents plants from growing and makes grass to die
        }
        return Delay;
    }

    public static bool CastsShadow(Block block)
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

    public class PlantTask : PhysicsTask
    {
        private const int MinDelay = 3000;
        private const int MaxDelay = 8000;
        private static Random _r = new Random();

        private enum TreeType
        {
            NoGrow,
            Normal,
            Palm,
        }

        private short _x, _y, _z;

        public PlantTask(World w, short x, short y, short z)
            : base(w)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        static public int GetRandomDelay()
        {
            return (_r.Next(MinDelay, MaxDelay));
        }

        protected override int PerformInternal()
        {
            if (_map.GetBlock(_x, _y, _z) != Block.Plant) //superflous task added by grass scanner or deleted plant. just forget it
                return 0;

            TreeType type = TypeByBlock(_map.GetBlock(_x, _y, _z - 1));
            if (TreeType.NoGrow == type)
                return 0;

            short height = (short)_r.Next(4, 7);
            if (CanGrow(height))
                MakeTrunks(height, type);

            return 0; //non-repeating task
        }

        private bool CanGrow(int height) //no shadows and enough space
        {
            for (int z = _z + 1; z < _map.Height; ++z)
            {
                if (GrassTask.CastsShadow(_map.GetBlock(_x, _y, z)))
                    return false;
            }

            for (int x = _x - 5; x < _x + 5; ++x)
            {
                for (int y = _y - 5; y < _y + 5; ++y)
                {
                    for (int z = _z + 1; z < _z + height; ++z)
                    {
                        Block b = _map.GetBlock(x, y, z);
                        if (Block.Air != b && Block.Leaves != b)
                            return false;
                    }
                }
            }

            return true;
        }

        private static TreeType TypeByBlock(Block b)
        {
            switch (b)
            {
                case Block.Grass:
                case Block.Dirt:
                    return TreeType.Normal;
                case Block.Sand:
                    return TreeType.Palm;
            }
            return TreeType.NoGrow;
        }

        private void MakeTrunks(short height, TreeType type)
        {
            for (short i = 0; i < height; ++i)
            {
                _map.QueueUpdate(new BlockUpdate(null, _x, _y, (short)(_z + i), Block.Log));
            }
            if (TreeType.Normal == type)
                TreeGeneration.MakeNormalFoliage(_world, new Vector3I(_x, _y, _z), height + 1);
            else
                TreeGeneration.MakePalmFoliage(_world, new Vector3I(_x, _y, _z), height);
        }
    }
#endregion

    #region Exploding Physics

    public class Firework : PhysicsTask
    {
        private const int Delay = 150;
        private Vector3I _pos;
        private int _z;
        private bool _notSent = true;

        public Firework(World world, Vector3I position)
            : base(world)
        {
            _pos = position;
            _z = position.Z + 1;
        }
        protected override int PerformInternal()
        {
            lock (_world.SyncRoot)
            {
                if (_world.fireworkPhysics)
                {
                    if (_world.Map.GetBlock(_pos.X, _pos.Y, _z) != Block.Air)
                    {
                        _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)_z, Block.Air));
                        _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)(_z - 1), Block.Air));
                        //Explode(_pos.X, _pos.Y, _z);
                        return 0;
                    }
                    if (_notSent)
                    {
                        if (_world.Map.GetBlock(_pos) != Block.Gold)
                            return 0;
                    }
                    _notSent = false;
                    _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)_z, Block.Gold));
                    _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)(_z - 1), Block.Lava));
                    if (_world.Map.GetBlock(_pos.X, _pos.Y, _z - 2) == Block.Lava)
                    {
                        _world.Map.QueueUpdate(new BlockUpdate(null, (short)_pos.X, (short)_pos.Y, (short)(_z - 2), Block.Air));
                    }
                    _z++;
                    return Delay;
                }
                return 0;
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
        protected override int PerformInternal()
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

        protected override int PerformInternal()
        {
            lock (_world.SyncRoot)
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
    }

#endregion

    #region Water Physics

    public class BlockFloat : PhysicsTask
    {
        private const int Delay = 200;
        private Vector3I _pos;
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

        protected override int PerformInternal()
        {
            lock (_world.SyncRoot)
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
    }

    #endregion

    #region Gun Physics

    public class Bullet : PhysicsTask
    {
        private const int Delay = 50;
        private Position p;
        private Block _type = Block.Undefined;
        private double rSin;
        private double rCos;
        private double lCos;
        private Position nextPos;
        private short startX;
        private short startY;
        private short startZ;
        private int startB = 4;
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

        protected override int PerformInternal()
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
                                    if (_world.tntPhysics && _type == Block.TNT)
                                    {
                                        if (player.LastTimeKilled == null || (DateTime.Now - player.LastTimeKilled).TotalSeconds > 15)
                                        {
                                            int seed = new Random().Next(1, 6);
                                            player.LastTimeKilled = DateTime.Now;
                                            _world._physScheduler.AddTask(new TNT(_world, new Vector3I(nextPos.X, nextPos.Y, nextPos.Z), _sender), 0);
                                            player.TeleportTo(_world.Map.Spawn);
                                            _world.Players.Message("{0}&S was blown up by {1}", player.ClassyName, _sender.ClassyName);
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
    #endregion


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

