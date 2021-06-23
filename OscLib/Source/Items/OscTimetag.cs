using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Represens an OSC Timetag, both as a 64 bit fixed-point NTP timestamp and a tick value compatible with .Net classes like DateTime and TimeSpan.
    /// </summary>
    public readonly struct OscTimetag
    {   
        /// <summary> This timetag represented as a NTP-compliant fixed-point 64 bit timestamp. </summary>
        public readonly ulong NtpTimestamp;

        /// <summary> This timetag represented as ticks - compatible with DateTime and TimeSpan classes, based on Coordinated Universal Time (UTC). </summary>
        public readonly long Ticks;     

        /// <summary>
        /// Creates an OSC Timetag out of the provided DateTime object.
        /// </summary>
        /// <param name="time"> The source DateTime object. </param>
        public OscTimetag(DateTime time)
        {
            Ticks = time.ToUniversalTime().Ticks;
       
            long tickMinusEpoch = Ticks - OscTime.NtpEpochStart;

            // get the seconds out of ticks
            uint seconds = (uint)(tickMinusEpoch / TimeSpan.TicksPerSecond);

            uint tickFraction = (uint)(tickMinusEpoch - (seconds * TimeSpan.TicksPerSecond));

            uint fraction = (uint)(uint.MaxValue * ((double)tickFraction / TimeSpan.TicksPerSecond));

            NtpTimestamp = (ulong)seconds << 32 | fraction;
        }

        /// <summary>
        /// Creates an OSC Timetag using DateTime or TimeSpan ticks. Ticks need to adhere to UTC time in order to produce correct results.
        /// </summary>
        /// <param name="tick"> UTC-based tick value. </param>
        public OscTimetag(long tick)
        {
            Ticks = tick;

            long tickMinusEpoch = Ticks - OscTime.NtpEpochStart;

            // get the seconds out of ticks
            uint seconds = (uint)(tickMinusEpoch / TimeSpan.TicksPerSecond);

            // get fractions of a second out of ticks and map them onto 32-bit uint
            uint tickFraction = (uint)(tickMinusEpoch - (seconds * TimeSpan.TicksPerSecond));

            uint fraction = (uint)(uint.MaxValue * ((double)tickFraction / TimeSpan.TicksPerSecond));

            // bitshift and add to a neat 64-bit ulong containing the result timestamp
            NtpTimestamp = (ulong)seconds << 32 | fraction;

        }

        /// <summary>
        /// Creates an OSC Timetag out of an NTP-format timestamp. 
        /// </summary>
        /// <param name="ntpTimestamp"> An ulong containing a 64-bit fixed-point NTP-format timestamp. </param>
        public OscTimetag(ulong ntpTimestamp)
        {
            NtpTimestamp = ntpTimestamp;

            Ticks = 0;

            uint seconds = (uint)((NtpTimestamp >> 32) & 0xFFFFFFFF);

            uint ntpFraction = (uint)(NtpTimestamp & 0xFFFFFFFF);

            uint tickFraction = (uint)(((double)ntpFraction / uint.MaxValue) * TimeSpan.TicksPerSecond);

            Ticks = OscTime.NtpEpochStart + (seconds * TimeSpan.TicksPerSecond) + tickFraction + 1;
        }


        /// <summary> 
        /// Returns this timetag represented as a DateTime struct, corresponding to the current time zone of the system.
        /// </summary>
        public DateTime GetDateTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(new DateTime(Ticks), TimeZoneInfo.Local);
        }


        /// <summary> 
        /// Returns this timetag represented as a DateTime object, corresponding to UTC time.
        /// </summary>
        public DateTime GetDateTimeUtc()
        {
            return new DateTime(Ticks);
        }


        /// <summary>
        /// Returns this timetag as a string, formatted as "day / month / year  hours : minutes : seconds : milliseconds".
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return GetDateTime().ToString("dd/MM/yyyy HH:mm:ss:fff");
        }


        /// <summary>
        /// Compares this OSC Timetag to an object. Returns "true" if: 
        /// <para> - The object is another Timetag and they match; </para>
        /// <para> - The object is a 64-bit signed integer value that is equal to this Timetag's number of ticks; </para>
        /// <para> - The object is a 64-bit unsigned integer value that is equal to this Timetag's NTP timestamp value. </para>
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is OscTimetag otherTimetag)
            {
                return otherTimetag == this;
            }
            else if (obj is long ticks)
            {
                return ticks == this.Ticks;
            }
            else if (obj is ulong ntp)
            {
                return ntp == this.NtpTimestamp;
            }
            else
            {
                return false;
            }

        }


        /// <summary>
        /// Compares two OSC Timetags.
        /// </summary>
        public static bool operator ==(OscTimetag timetagOne, OscTimetag timetagTwo)
        {
            return timetagOne.NtpTimestamp == timetagTwo.NtpTimestamp;
        }


        /// <summary>
        /// Compares two OSC Timetags.
        /// </summary>
        public static bool operator !=(OscTimetag timetagOne, OscTimetag timetagTwo)
        {
            return timetagOne.NtpTimestamp != timetagTwo.NtpTimestamp;
        }


        /// <summary>
        /// Checks whether the Timetag on the left refers to an earier moment in time than the one on the right.
        /// </summary>
        public static bool operator <(OscTimetag timetagOne, OscTimetag timetagTwo)
        {
            return timetagOne.Ticks < timetagTwo.Ticks;
        }


        /// <summary>
        /// Checks whether the Timetag on the left refers to a later moment in time than the one on the right.
        /// </summary>
        public static bool operator > (OscTimetag timetagOne, OscTimetag timetagTwo)
        {
            return timetagOne.Ticks > timetagTwo.Ticks;
        }


        /// <summary>
        /// Returns the hash code of this Timestamp.
        /// </summary>
        public override int GetHashCode()
        {
            return this.NtpTimestamp.GetHashCode();
        }

    }

}
