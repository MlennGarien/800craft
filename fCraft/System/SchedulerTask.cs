// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> A task to be executed by the Scheduler.
    /// Stores timing information and state. </summary>
    public sealed class SchedulerTask {
        static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes( 1 );

        SchedulerTask() {
            AdjustForExecutionTime = true;
            Delay = TimeSpan.Zero;
            Interval = DefaultInterval;
            MaxRepeats = -1;
        }


        internal SchedulerTask( [NotNull] SchedulerCallback callback, bool isBackground )
            : this() {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            Callback = callback;
            IsBackground = isBackground;
        }


        internal SchedulerTask( [NotNull] SchedulerCallback callback, bool isBackground, [CanBeNull] object userState )
            : this() {
            if( callback == null ) throw new ArgumentNullException( "callback" );
            Callback = callback;
            IsBackground = isBackground;
            UserState = userState;
        }

        /// <summary> Next scheduled execution time (UTC). </summary>
        public DateTime NextTime { get; set; }

        /// <summary> Initial execution delay. </summary>
        public TimeSpan Delay { get; private set; }

        /// <summary> Whether the task is one-use or recurring. </summary>
        public bool IsRecurring { get; set; }

        /// <summary> Whether the task should be ran in the background. </summary>
        public bool IsBackground { get; set; }

        /// <summary> Whether the task has stopped.
        /// RunOnce tasks stop after first execution.
        /// RunForever and RunManual tasks only stop manually.
        /// RunRepeating stops after max number of repeats is reached. </summary>
        public bool IsStopped { get; internal set; }

        /// <summary> Whether the task is currently being executed. </summary>
        public bool IsExecuting { get; internal set; }

        /// <summary> Whether to adjust Interval for execution time (for recurring tasks).
        /// If enabled, the Interval timer is started as soon as execution starts.
        /// Total delay between executions is then equal to Interval.
        /// If disabled, the Interval timer is started only after execution finishes.
        /// Total delay between executions is then (time to execute + Interval). </summary>
        public bool AdjustForExecutionTime { get; set; }

        /// <summary> Interval between executions (for recurring tasks). </summary>
        public TimeSpan Interval { get; set; }

        /// <summary> Maximum number of repeats for RunRepeating tasks.
        /// Set to -1 to run forever. </summary>
        public int MaxRepeats { get; set; }

        /// <summary> Method to call to execute the task. </summary>
        [NotNull]
        public SchedulerCallback Callback { get; set; }

        /// <summary> General-purpose persistent state object,
        /// can be used for anything you want. </summary>
        public object UserState { get; set; }


        #region Run Once

        /// <summary> Runs the task once, as quickly as possible.
        /// Callback is invoked from the Scheduler thread. </summary>
        public SchedulerTask RunOnce() {
            NextTime = DateTime.UtcNow.Add( Delay );
            IsRecurring = false;
            Scheduler.AddTask( this );
            return this;
        }


        /// <summary> Runs the task once, after a given delay. </summary>
        public SchedulerTask RunOnce( TimeSpan delay ) {
            Delay = delay;
            return RunOnce();
        }


        /// <summary> Runs the task once at a given date.
        /// If the given date is in the past, the task is ran immediately. </summary>
        public SchedulerTask RunOnce( DateTime time ) {
            Delay = time.Subtract( DateTime.UtcNow );
            NextTime = time;
            IsRecurring = false;
            Scheduler.AddTask( this );
            return this;
        }


        /// <summary> Runs the task once, after a given delay. </summary>
        public SchedulerTask RunOnce( object userState, TimeSpan delay ) {
            UserState = userState;
            return RunOnce( delay );
        }


        /// <summary> Runs the task once at a given date.
        /// If the given date is in the past, the task is ran immediately. </summary>
        public SchedulerTask RunOnce( object userState, DateTime time ) {
            UserState = userState;
            return RunOnce( time );
        }

        #endregion


        #region Run Forever

        SchedulerTask RunForever() {
            IsRecurring = true;
            NextTime = DateTime.UtcNow.Add( Delay );
            Scheduler.AddTask( this );
            return this;
        }


        /// <summary> Runs the task forever at a given interval, until manually stopped. </summary>
        public SchedulerTask RunForever( TimeSpan interval ) {
            if( interval.Ticks < 0 ) throw new ArgumentException( "Interval must be positive", "interval" );
            Interval = interval;
            return RunForever();
        }


        /// <summary> Runs the task forever at a given interval after an initial delay, until manually stopped. </summary>
        public SchedulerTask RunForever( TimeSpan interval, TimeSpan delay ) {
            if( interval.Ticks < 0 ) throw new ArgumentException( "Interval must be positive", "interval" );
            Interval = interval;
            Delay = delay;
            return RunForever();
        }


        /// <summary> Runs the task forever at a given interval after an initial delay, until manually stopped. </summary>
        public SchedulerTask RunForever( object userState, TimeSpan interval, TimeSpan delay ) {
            UserState = userState;
            return RunForever( interval, delay );
        }

        #endregion


        #region Run Repeating

        /// <summary> Runs the task a given number of times, at a given interval after an initial delay. </summary>
        public SchedulerTask RunRepeating( TimeSpan delay, TimeSpan interval, int times ) {
            if( times < 1 ) throw new ArgumentException( "Must be ran at least 1 time.", "times" );
            MaxRepeats = times;
            return RunForever( interval, delay );
        }


        /// <summary> Runs the task a given number of times, at a given interval after an initial delay. </summary>
        public SchedulerTask RunRepeating( [CanBeNull] object userState, TimeSpan delay, TimeSpan interval, int times ) {
            if( times < 1 ) throw new ArgumentException( "Must be ran at least 1 time.", "times" );
            UserState = userState;
            MaxRepeats = times;
            return RunForever( interval, delay );
        }

        #endregion


        #region Run Manual

        static readonly TimeSpan CloseEnoughToForever = TimeSpan.FromDays( 36525 ); // >100 years

        /// <summary> Executes the task once immediately, and suspends (but does not stop).
        /// A SchedulerTask object can be reused many times if ran manually. </summary>
        public SchedulerTask RunManual() {
            Delay = TimeSpan.Zero;
            IsRecurring = true;
            NextTime = DateTime.UtcNow;
            MaxRepeats = -1;
            Interval = CloseEnoughToForever;
            Scheduler.AddTask( this );
            return this;
        }

        /// <summary> Executes the task once after a delay, and suspends (but does not stop).
        /// A SchedulerTask object can be reused many times if ran manually. </summary>
        public SchedulerTask RunManual( TimeSpan delay ) {
            Delay = delay;
            IsRecurring = true;
            NextTime = DateTime.UtcNow.Add( Delay );
            MaxRepeats = -1;
            Interval = CloseEnoughToForever;
            Scheduler.AddTask( this );
            return this;
        }

        /// <summary> Executes the task once at a given time, and suspends (but does not stop).
        /// A SchedulerTask object can be reused many times if ran manually. </summary>
        public SchedulerTask RunManual( DateTime time ) {
            Delay = time.Subtract( DateTime.UtcNow );
            IsRecurring = true;
            NextTime = time;
            MaxRepeats = -1;
            Interval = CloseEnoughToForever;
            Scheduler.AddTask( this );
            return this;
        }

        #endregion


        /// <summary> Stops the task, and removes it from the schedule. </summary>
        public SchedulerTask Stop() {
            IsStopped = true;
            return this;
        }


        public override string ToString() {
            StringBuilder sb = new StringBuilder( "Task(" );

            if( IsStopped ) {
                sb.Append( "STOPPED " );
            }

            if( Callback.Target != null ) {
                sb.Append( Callback.Target ).Append( "::" );
            }
            sb.Append( Callback.Method.DeclaringType.Name );
            sb.Append( '.' );
            sb.Append( Callback.Method.Name );
            sb.Append( " @ " );

            if( IsRecurring ) {
                sb.Append( Interval.ToCompactString() );
            }
            sb.Append( "+" ).Append( Delay.ToCompactString() );

            if( UserState != null ) {
                sb.Append( " -> " );
                sb.Append( UserState );
            }
            sb.Append( ')' );
            return sb.ToString();
        }
    }


    public delegate void SchedulerCallback( [NotNull] SchedulerTask task );


#if DEBUG_SCHEDULER
    public class SchedulerTaskEventArgs : EventArgs {
        public SchedulerTaskEventArgs( SchedulerTask task ) {
            Task = task;
        }
        public SchedulerTask Task { get; private set; }
    }
#endif
}
