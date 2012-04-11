// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;

namespace fCraft {
    public sealed class ChatTimer {
        public static readonly TimeSpan MinDuration = TimeSpan.FromSeconds( 1 );
        static readonly TimeSpan Hour = TimeSpan.FromHours( 1 );

        public readonly int Id;

        public bool IsRunning { get; private set; }

        [CanBeNull]
        public string Message { get; private set; }

        public DateTime StartTime { get; private set; }

        public DateTime EndTime { get; private set; }

        public TimeSpan Duration { get; private set; }

        public TimeSpan TimeLeft {
            get {
                return EndTime.Subtract( DateTime.UtcNow );
            }
        }

        [NotNull]
        public string StartedBy { get; private set; }


        readonly SchedulerTask task;
        int announceIntervalIndex, lastHourAnnounced;


        ChatTimer( TimeSpan duration, [CanBeNull] string message, [NotNull] string startedBy ) {
            if( startedBy == null ) throw new ArgumentNullException( "startedBy" );
            StartedBy = startedBy;
            Message = message;
            StartTime = DateTime.UtcNow;
            EndTime = StartTime.Add( duration );
            Duration = duration;
            int oneSecondRepeats = (int)duration.TotalSeconds + 1;
            if( duration > Hour ) {
                announceIntervalIndex = AnnounceIntervals.Length - 1;
                lastHourAnnounced = (int)duration.TotalHours;
            } else {
                for( int i = 0; i < AnnounceIntervals.Length; i++ ) {
                    if( duration <= AnnounceIntervals[i] ) {
                        announceIntervalIndex = i - 1;
                        break;
                    }
                }
            }
            task = Scheduler.NewTask( TimerCallback, this );
            Id = Interlocked.Increment( ref timerCounter );
            AddTimerToList( this );
            IsRunning = true;
            task.RunRepeating( TimeSpan.Zero,
                               TimeSpan.FromSeconds( 1 ),
                               oneSecondRepeats );
        }

        static void TimerCallback( [NotNull] SchedulerTask task ) {
            if( task == null ) throw new ArgumentNullException( "task" );
            ChatTimer timer = (ChatTimer)task.UserState;
            if( task.MaxRepeats == 1 ) {
                if( String.IsNullOrEmpty( timer.Message ) ) {
                    Chat.SendSay( Player.Console, "(Timer Up)" );
                } else {
                    Chat.SendSay( Player.Console, "(Timer Up) " + timer.Message );
                }
                timer.Stop();

            } else if( timer.announceIntervalIndex >= 0 ) {
                if( timer.lastHourAnnounced != (int)timer.TimeLeft.TotalHours ) {
                    timer.lastHourAnnounced = (int)timer.TimeLeft.TotalHours;
                    timer.Announce( TimeSpan.FromHours( Math.Ceiling( timer.TimeLeft.TotalHours ) ) );
                }
                if( timer.TimeLeft <= AnnounceIntervals[timer.announceIntervalIndex] ) {
                    timer.Announce( AnnounceIntervals[timer.announceIntervalIndex] );
                    timer.announceIntervalIndex--;
                }
            }
        }

        void Announce( TimeSpan timeLeft ) {
            if( String.IsNullOrEmpty( Message ) ) {
                Chat.SendSay( Player.Console, "(Timer) " + timeLeft.ToMiniString() );
            } else {
                Chat.SendSay( Player.Console,
                              String.Format( "(Timer) {0} until {1}",
                                             timeLeft.ToMiniString(),
                                             Message ) );
            }
        }

        public void Stop() {
            IsRunning = false;
            task.Stop();
            RemoveTimerFromList( this );
        }


        #region Static

        public static ChatTimer Start( TimeSpan duration, [CanBeNull] string message, [NotNull] string startedBy ) {
            if( startedBy == null ) throw new ArgumentNullException( "startedBy" );
            if( duration < MinDuration ) {
                throw new ArgumentException( "Timer duration should be at least 1s", "duration" );
            }
            return new ChatTimer( duration, message, startedBy );
        }

        static readonly TimeSpan[] AnnounceIntervals = new[] {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(20),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(40),
            TimeSpan.FromSeconds(50),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromMinutes(3),
            TimeSpan.FromMinutes(4),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(20),
            TimeSpan.FromMinutes(30),
            TimeSpan.FromMinutes(40),
            TimeSpan.FromMinutes(50)
        };

        static int timerCounter;
        static readonly object TimerListLock = new object();
        static readonly Dictionary<int, ChatTimer> Timers = new Dictionary<int, ChatTimer>();

        static void AddTimerToList( [NotNull] ChatTimer timer ) {
            if( timer == null ) throw new ArgumentNullException( "timer" );
            lock( TimerListLock ) {
                Timers.Add( timer.Id, timer );
            }
        }


        static void RemoveTimerFromList( [NotNull] ChatTimer timer ) {
            if( timer == null ) throw new ArgumentNullException( "timer" );
            lock( TimerListLock ) {
                Timers.Remove( timer.Id );
            }
        }


        public static ChatTimer[] TimerList {
            get {
                lock( TimerListLock ) {
                    return Timers.Values.ToArray();
                }
            }
        }

        [CanBeNull]
        public static ChatTimer FindTimerById( int id ) {
            lock( TimerListLock ) {
                ChatTimer result;
                if( Timers.TryGetValue( id, out result ) ) {
                    return result;
                } else {
                    return null;
                }
            }
        }

        #endregion
    }
}