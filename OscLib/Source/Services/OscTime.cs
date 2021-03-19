using System;
using System.Diagnostics;

namespace OscLib
{
    /// <summary>
    /// Manages timing and timestamps. Provides a session tick for ease of timekeeping.
    /// </summary>
    public static class OscTime
    {
        private static Stopwatch _sessionTimer;
        private static long _sessionStart;
        private static readonly long _ticksPerSecond;

        private static readonly OscTimestamp _immediately;
        private static readonly byte[] _immediatelyBytes;

        private static readonly long _ntpEpochStart;

        /// <summary> Provides the number of ticks elapsed since the first call for this class. </summary>
        public static long SessionTick { get => _sessionTimer.Elapsed.Ticks; }

        /// <summary> Provides the current "global" UTC tick. </summary>
        public static long GlobalTick { get => _sessionTimer.Elapsed.Ticks + _sessionStart; }

        /// <summary> Provides the OSC-compliant "DO IT. DO IT NOW." timestamp. </summary>
        public static OscTimestamp Immediately { get => _immediately; }

        /// <summary> Provides a "pre-rendered" byte representation of the OSC-compliant "DO IT NOW" timestamp. </summary>
        public static byte[] ImmediatelyBytes { get => _immediatelyBytes; }

        /// <summary> Provides an OSC timestamp representing the current time (as far as the system is aware). </summary>
        public static OscTimestamp Now { get => new OscTimestamp(GlobalTick); }

        /// <summary> The start of the current NTP epoch - 00:00 01/01/1900 at the moment. </summary>
        public static long NtpEpochStart { get => _ntpEpochStart; }

        static OscTime()
        {
            _ntpEpochStart = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
            _ticksPerSecond = TimeSpan.TicksPerSecond;
            _immediately = new OscTimestamp((ulong)1);
            _immediatelyBytes = OscSerializer.GetBytes(_immediately);

            _sessionTimer = new Stopwatch();

            _sessionStart = DateTime.UtcNow.Ticks;
            _sessionTimer.Start();              
        }

        /// <summary>
        /// Returns an OSC timestamp that occurs after the provided number of seconds has passed.
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static OscTimestamp AfterSeconds(float seconds)
        {
            long waitTicks = (long)(seconds * _ticksPerSecond);

            return new OscTimestamp(GlobalTick + waitTicks);
        }

    }

}
