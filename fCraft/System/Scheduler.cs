// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using System.IO;
using System.Diagnostics;

namespace fCraft {
    /// <summary> A general-purpose task scheduler. </summary>
    public static class Scheduler {
        static readonly HashSet<SchedulerTask> Tasks = new HashSet<SchedulerTask>();
        static SchedulerTask[] taskCache;
        static readonly Queue<SchedulerTask> BackgroundTasks = new Queue<SchedulerTask>();
        static readonly object TaskListLock = new object(),
                               BackgroundTaskListLock = new object();

        static Thread schedulerThread,
                      backgroundThread;


        internal static void Start() {
#if DEBUG_SCHEDULER
            Logger.Log( LogType.Debug, "Scheduler: Starting..." );
#endif
            schedulerThread = new Thread( MainLoop ) {
                Name = "fCraft.Main"
            };
            schedulerThread.Start();
            backgroundThread = new Thread( BackgroundLoop ) {
                Name = "fCraft.Background"
            };
            backgroundThread.Start();
        }


        static void MainLoop() {
            while( !Server.IsShuttingDown ) {
                DateTime ticksNow = DateTime.UtcNow;

                SchedulerTask[] taskListCache = taskCache;

                for( int i = 0; i < taskListCache.Length && !Server.IsShuttingDown; i++ ) {
                    SchedulerTask task = taskListCache[i];
                    if( task.IsStopped || task.NextTime > ticksNow ) continue;
                    if( task.IsRecurring && task.AdjustForExecutionTime ) {
                        task.NextTime += task.Interval;
                    }

                    if( task.IsBackground ) {
                        lock( BackgroundTaskListLock ) {
                            BackgroundTasks.Enqueue( task );
                        }
                    } else {
                        task.IsExecuting = true;

#if DEBUG_SCHEDULER
                        FireEvent( TaskExecuting, task );
#endif

#if DEBUG
                        task.Callback( task );
                        task.IsExecuting = false;
#else
                        try {
                            task.Callback( task );
                        } catch( Exception ex ) {
                            Logger.LogAndReportCrash( "Exception thrown by ScheduledTask callback", "fCraft", ex, false );
                        } finally {
                            task.IsExecuting = false;
                        }
#endif

#if DEBUG_SCHEDULER
                        FireEvent( TaskExecuted, task );
#endif
                    }

                    if( !task.IsRecurring || task.MaxRepeats == 1 ) {
                        task.Stop();
                        continue;
                    }
                    task.MaxRepeats--;

                    ticksNow = DateTime.UtcNow;
                    if( !task.AdjustForExecutionTime ) {
                        task.NextTime = ticksNow.Add( task.Interval );
                    }
                }

                Thread.Sleep( 10 );
            }
        }


        static void BackgroundLoop() {
            while( !Server.IsShuttingDown ) {
                if( BackgroundTasks.Count > 0 ) {
                    SchedulerTask task;
                    lock( BackgroundTaskListLock ) {
                        task = BackgroundTasks.Dequeue();
                    }
                    task.IsExecuting = true;
#if DEBUG_SCHEDULER
                    FireEvent( TaskExecuting, task );
#endif

#if DEBUG
                    task.Callback( task );
#else
                    try {
                        task.Callback( task );
                    } catch( Exception ex ) {
                        Logger.LogAndReportCrash( "Exception thrown by ScheduledTask callback", "fCraft", ex, false );
                    } finally {
                        task.IsExecuting = false;
                    }
#endif

#if DEBUG_SCHEDULER
                    FireEvent( TaskExecuted, task );
#endif
                }
                Thread.Sleep( 10 );
            }
        }


        /// <summary> Schedules a given task for execution. </summary>
        /// <param name="task"> Task to schedule. </param>
        internal static void AddTask( [NotNull] SchedulerTask task ) {
            if( task == null ) throw new ArgumentNullException( "task" );
            lock( TaskListLock ) {
                if( Server.IsShuttingDown ) return;
                task.IsStopped = false;
#if DEBUG_SCHEDULER
                FireEvent( TaskAdded, task );
                if( Tasks.Add( task ) ) {
                    UpdateCache();
                    Logger.Log( LogType.Debug,
                                "Scheduler.AddTask: Added {0}", task );
                }else{
                    Logger.Log( LogType.Debug,
                                "Scheduler.AddTask: Added duplicate {0}", task );
                }
#else
                if( Tasks.Add( task ) ) {
                    UpdateCache();
                }
#endif
            }
        }


        /// <summary> Creates a new SchedulerTask object to run in the main thread.
        /// Use this if your task is time-sensitive or frequent, and your callback won't take too long to execute. </summary>
        /// <param name="callback"> Method to call when the task is triggered. </param>
        /// <returns> Newly created SchedulerTask object. </returns>
        public static SchedulerTask NewTask( [NotNull] SchedulerCallback callback ) {
            return new SchedulerTask( callback, false );
        }


        /// <summary> Creates a new SchedulerTask object to run in the background thread.
        /// Use this if your task is not very time-sensitive or frequent, or if your callback is resource-intensive. </summary>
        /// <param name="callback"> Method to call when the task is triggered. </param>
        /// <returns> Newly created SchedulerTask object. </returns>
        public static SchedulerTask NewBackgroundTask( [NotNull] SchedulerCallback callback ) {
            return new SchedulerTask( callback, true );
        }


        /// <summary> Creates a new SchedulerTask object to run in the main thread.
        /// Use this if your task is time-sensitive or frequent, and your callback won't take too long to execute. </summary>
        /// <param name="callback"> Method to call when the task is triggered. </param>
        /// <param name="userState"> Parameter to pass to the method. </param>
        /// <returns> Newly created SchedulerTask object. </returns>
        public static SchedulerTask NewTask( [NotNull] SchedulerCallback callback, [CanBeNull] object userState ) {
            return new SchedulerTask( callback, false, userState );
        }


        /// <summary> Creates a new SchedulerTask object to run in the background thread.
        /// Use this if your task is not very time-sensitive or frequent, or if your callback is resource-intensive. </summary>
        /// <param name="callback"> Method to call when the task is triggered. </param>
        /// <param name="userState"> Parameter to pass to the method. </param>
        /// <returns> Newly created SchedulerTask object. </returns>
        public static SchedulerTask NewBackgroundTask( [NotNull] SchedulerCallback callback, [CanBeNull] object userState ) {
            return new SchedulerTask( callback, true, userState );
        }


        // Removes stopped tasks from the list
        internal static void UpdateCache() {
            List<SchedulerTask> newList = new List<SchedulerTask>();
            List<SchedulerTask> deletionList = new List<SchedulerTask>();
            lock( TaskListLock ) {
                foreach( SchedulerTask task in Tasks ) {
                    if( task.IsStopped ) {
                        deletionList.Add( task );
                    } else {
                        newList.Add( task );
                    }
                }
                for( int i = 0; i < deletionList.Count; i++ ) {
                    Tasks.Remove( deletionList[i] );
#if DEBUG_SCHEDULER
                    FireEvent( TaskRemoved, deletionList[i] );
                    Logger.Log( LogType.Debug,
                                "Scheduler.UpdateCache: Removed {0}", deletionList[i] );
#endif
                }
            }
            taskCache = newList.ToArray();
        }


        // Clears the task list
        internal static void BeginShutdown() {
#if DEBUG_SCHEDULER
            Logger.Log( LogType.Debug, "Scheduler: BeginShutdown..." );
#endif
            if (ConfigKey.HbSaverKey.Enabled())
            {
                if (!Server.IsRestarting)
                {
                    try
                    {
                        if (!File.Exists("heartbeatsaver.exe"))
                        {
                            Logger.Log(LogType.Warning, "heartbeatsaver.exe does not exist and failed to launch");
                            return;
                        }

                        //start the heartbeat saver
                        Process HeartbeatSaver = new Process();
                        Logger.Log(LogType.SystemActivity, "Starting the HeartBeat Saver");
                        HeartbeatSaver.StartInfo.FileName = "heartbeatsaver.exe";
                        HeartbeatSaver.Start();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogType.Error, "HeartBeatSaver: " + ex);
                    }
                }
                else
                    Logger.Log(LogType.SystemActivity, "HeartBeat Saver was not launched");
            }
            lock( TaskListLock ) {
                foreach( SchedulerTask task in Tasks ) {
                    task.Stop();
                }
                Tasks.Clear();
                taskCache = new SchedulerTask[0];
            }
        }


        // Makes sure that both scheduler threads finish and quit.
        internal static void EndShutdown() {
#if DEBUG_SCHEDULER
            Logger.Log( LogType.Debug, "Scheduler: EndShutdown..." );
#endif
            try {
                if( schedulerThread != null && schedulerThread.IsAlive ) {
                    schedulerThread.Join();
                }
                schedulerThread = null;
            } catch( ThreadStateException ) { }
            try {
                if( backgroundThread != null && backgroundThread.IsAlive ) {
                    backgroundThread.Join();
                }
                backgroundThread = null;
            } catch( ThreadStateException ) { }
        }


#if DEBUG_SCHEDULER
        
        public static void PrintTasks( [NotNull] Player player ) {
            if( player == null ) throw new ArgumentNullException( "player" );
            lock( TaskListLock ) {
                foreach( SchedulerTask task in Tasks ) {
                    player.Message( task.ToString() );
                }
            }
        }


        public static event EventHandler<SchedulerTaskEventArgs> TaskAdded;

        public static event EventHandler<SchedulerTaskEventArgs> TaskExecuting;

        public static event EventHandler<SchedulerTaskEventArgs> TaskExecuted;

        public static event EventHandler<SchedulerTaskEventArgs> TaskRemoved;


        static void FireEvent( EventHandler<SchedulerTaskEventArgs> eventToFire, SchedulerTask task ) {
            var h = eventToFire;
            if( h != null ) h( null, new SchedulerTaskEventArgs( task ) );
        }
#endif
    }
}