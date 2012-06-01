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
using System;
using System.Threading;
using fCraft.Physics;
using System.Diagnostics;
using System.Collections.Generic;
using Util = RandomMaze.MazeUtil;
using fCraft.Drawing;

namespace fCraft
{
	public enum TaskCategory
	{
		Physics,
		Life,
	}

public class PhysScheduler
	{
		private MinBinaryHeap<PhysicsTask, Int64> _tasks = new MinBinaryHeap<PhysicsTask, Int64>();
		private Stopwatch _watch = new Stopwatch(); //a good counter of elapsed milliseconds
		private World _owner;
		private EventWaitHandle _continue = new EventWaitHandle(false, EventResetMode.AutoReset);
		private EventWaitHandle _stop = new EventWaitHandle(false, EventResetMode.AutoReset);
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
			WaitHandle[] handles = new WaitHandle[] { _continue, _stop };
			int timeout = Timeout.Infinite;
			for (; ; )
			{
				int w = WaitHandle.WaitAny(handles, timeout);
				if (1 == w) //stop
					break;
				PhysicsTask task;
				//check if there is a due task
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
						timeout = (int)(task.DueTime - now); 
						continue;
					}
				}
				int delay;
				//preform it
				try
				{
					delay = task.Deleted ? 0 : task.Perform(); //dont perform deleted tasks 
				}
				catch (Exception e)
				{
					delay = 0;
					Logger.Log(LogType.Error, "ProcessPhysicsTasks: " + e);
				}
				//decide what's next
				lock (_tasks)
				{
					Int64 now = _watch.ElapsedMilliseconds;
					if (delay > 0)
					{
						task.DueTime = now + delay;
						_tasks.Add(task);
					}
					timeout = _tasks.Size > 0 ? Math.Max((int)(_tasks.Head().DueTime - now), 0) : Timeout.Infinite; 
				}
			}
		}

		public void Start()
		{
			if (null!=_thread)
			{
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
			task.DueTime = _watch.ElapsedMilliseconds + delay;
			lock (_tasks)
			{
				_tasks.Add(task);
			}
			_continue.Set();
		}
	}
}

