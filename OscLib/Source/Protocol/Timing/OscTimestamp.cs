using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Describes an OSC Timestamp both as a 64 bit fixed-point NTP timestamp and ticks used by DateTime and TimeSpan.
    /// </summary>
    public readonly struct OscTimestamp
    {       
        private readonly ulong _ntpTimestamp;
        private readonly long _ticks;

        /// <summary> This timestamp represented in NTP format. </summary>
        public ulong NtpTimestamp { get => _ntpTimestamp; }

        /// <summary> This timestamp represented as DateTime ticks, UTC. </summary>
        public long Ticks { get => _ticks; }

        /// <summary> Returns a DateTime representation of this timestamp, corresponding to the local time of the system. Creates a new DateTime, so careful with that GC, Eugene. </summary>
        public DateTime DateTime { get => TimeZoneInfo.ConvertTimeFromUtc(new DateTime(_ticks), TimeZoneInfo.Local); }

        /// <summary> Returns a DateTime representation of this timestamp, as UTC time. Creates a new DateTime, ditto about being careful with that GC. </summary>
        public DateTime DateTimeUtc { get => new DateTime(_ticks); }
            
        /// <summary>
        /// Creates an OSC Timestamp out of the provided DateTime object.
        /// </summary>
        /// <param name="time"> The DateTime object to create an OSC Timestamp out of. </param>
        public OscTimestamp(DateTime time)
        {

            _ticks = time.ToUniversalTime().Ticks;
       
            long tickMinusEpoch = _ticks - OscTime.NtpEpochStart;

            // get the seconds out of ticks
            uint seconds = (uint)(tickMinusEpoch / TimeSpan.TicksPerSecond);

            uint tickFraction = (uint)(tickMinusEpoch - (seconds * TimeSpan.TicksPerSecond));

            uint fraction = (uint)(uint.MaxValue * ((double)tickFraction / TimeSpan.TicksPerSecond));

            _ntpTimestamp = (ulong)seconds << 32 | fraction;

        }

        /// <summary>
        /// Creates an OSC Timestamp using DateTime or TimeSpan ticks. Ticks need to adhere to UTC in order to produce right results.
        /// </summary>
        /// <param name="tick"> UTC-based tick value. </param>
        public OscTimestamp(long tick)
        {

            _ticks = tick;

            long tickMinusEpoch = _ticks - OscTime.NtpEpochStart;

            // get the seconds out of ticks
            uint seconds = (uint)(tickMinusEpoch / TimeSpan.TicksPerSecond);

            // get fractions of a second out of ticks and map them onto 32-bit uint
            uint tickFraction = (uint)(tickMinusEpoch - (seconds * TimeSpan.TicksPerSecond));

            uint fraction = (uint)(uint.MaxValue * ((double)tickFraction / TimeSpan.TicksPerSecond));

            // bitshift and add to a neat 64-bit ulong containing the result timestamp
            _ntpTimestamp = (ulong)seconds << 32 | fraction;

        }

        /// <summary>
        /// Creates an OSC timestamp out of an NTP-format timestamp. 
        /// </summary>
        /// <param name="ntpTimestamp"> An ulong containing a 64-bit fixed-point NTP-format timestamp. </param>
        public OscTimestamp(ulong ntpTimestamp)
        {
            _ntpTimestamp = ntpTimestamp;
            _ticks = 0;

            uint seconds = (uint)((_ntpTimestamp >> 32) & 0xFFFFFFFF);

            uint ntpFraction = (uint)(_ntpTimestamp & 0xFFFFFFFF);

            uint tickFraction = (uint)(((double)ntpFraction / uint.MaxValue) * TimeSpan.TicksPerSecond);

            _ticks = OscTime.NtpEpochStart + (seconds * TimeSpan.TicksPerSecond) + tickFraction + 1;
        }

        /// <summary>
        /// Returns timestamp as a string, formatted as "day/month/year hours:minutes:seconds:milliseconds".
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return DateTime.ToString("dd/MM/yyyy HH:mm:ss:fff");
        }

    }

}
