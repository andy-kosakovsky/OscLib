using System;
using System.Diagnostics;

namespace OscLib
{
    /// <summary>
    /// Manages timekeeping and timetags. 
    /// Provides the GlobalTick, based on the .Net DateTime and Stopwatch ticks - the main source of timing data for the rest of this library's components.  
    /// </summary>
    /// <remarks>
    /// By default, uses the current UTC time as provided by the system. Can be reconfigured to start the count from any point in time after the
    /// beginning of the current NTP epoch (the lowest possible value for an OSC timetag, seeing as they are NTP-based).
    /// </remarks>
    public static class OscTime
    {
        private static Stopwatch _sessionTimer;
        private static long _sessionStart;

        private static readonly long _ticksPerSecond;

        private static readonly OscTimetag _immediately;
        private static readonly byte[] _immediatelyBytes;

        private static readonly long _ntpEpochStart;

        /// <summary> Provides the number of ticks elapsed since "session start" - the time of the first call to this class, by default. </summary>
        public static long SessionTick { get => _sessionTimer.Elapsed.Ticks; }

        /// <summary> Provides the current tick. By default, it encodes the current UTC time, as far as the system is aware. </summary>
        public static long GlobalTick { get => _sessionTimer.Elapsed.Ticks + _sessionStart; }

        /// <summary> Provides the OSC-compliant "DO IT. DO IT NOW." timetag. </summary>
        public static OscTimetag Immediately { get => _immediately; }

        /// <summary> Provides a "pre-rendered" byte representation of the "DO IT NOW" OSC Timetag. </summary>
        public static byte[] ImmediatelyBytes { get => _immediatelyBytes; }

        /// <summary> Provides an OSC Timetag representing the current time (as far as the system is aware). </summary>
        public static OscTimetag Now { get => new OscTimetag(GlobalTick); }

        /// <summary> The start of the current NTP epoch - 00:00 01/01/1900 at the moment. </summary>
        public static long NtpEpochStart { get => _ntpEpochStart; }

        static OscTime()
        {
            _sessionTimer = new Stopwatch();
            
            _ntpEpochStart = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
            _ticksPerSecond = TimeSpan.TicksPerSecond;
            _immediately = new OscTimetag((ulong)1);
            _immediatelyBytes = OscSerializer.GetBytes(_immediately);

            ResetTime();
            
        }


        /// <summary>
        /// Sets custom session start time. Can't be set before the start of the current NTP epoch (the midnight of 01/01/1900).
        /// </summary>
        /// <param name="newStart"> The DateTime struct containing the new session start time. </param>
        public static void SetSessionStart(DateTime newStart)
        {
            long newTicks = newStart.Ticks;

            if (newTicks > _ntpEpochStart)
            {
                _sessionStart = newTicks;
            }
            else
            {
                _sessionStart = _ntpEpochStart;
            }

        }


        /// <summary>
        /// Resets all time-related settings to their defaults - after calling this method, the GlobalTick property will encode the current UTC time once again. 
        /// </summary>
        public static void ResetTime()
        {
            _sessionStart = DateTime.UtcNow.Ticks;

            RestartSessionTimer();
        }


        /// <summary>
        /// Restarts the session timer. 
        /// </summary>
        public static void RestartSessionTimer()
        {
            if (_sessionTimer.IsRunning)
            {
                _sessionTimer.Restart();
            }
            else
            {
                _sessionTimer.Start();
            }

        }

         
        /// <summary>
        /// Returns an OSC Timetag that occurs after the provided number of seconds has passed, counting from the current GlobalTick.
        /// </summary>
        public static OscTimetag AfterSeconds(float seconds)
        {
            long waitTicks = (long)(seconds * _ticksPerSecond);

            return new OscTimetag(GlobalTick + waitTicks);
        }


        /// <summary>
        /// Returns an OSC Timetag that occurs after the provided number of seconds has passed, counting from the specified OSC Timetag.
        /// </summary>
        public static OscTimetag AfterSeconds(this OscTimetag me, float seconds)
        {
            long waitTicks = (long)(seconds * _ticksPerSecond);

            return new OscTimetag(me.Ticks + waitTicks);
        }

    }

}
