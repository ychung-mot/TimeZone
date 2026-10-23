using System.Globalization;
using System.Text.RegularExpressions;

namespace TimeZone.Controllers
{
    public static class TimeZones
    {
        public const string VancouverTimeZone = "America/Vancouver";
        public const string BritishColumbiaTimeZone = "British Columbia Standard Time";
        public const string BritishColumbiaSqlTimeZone = "British Columbia Standard Time";
        public static string BritishColumbiaTimeZoneId => OperatingSystem.IsWindows()
            ? BritishColumbiaTimeZone
            : VancouverTimeZone;
    }

    public static class DateUtils
    {
        private static readonly TimeSpan BcUtcMinusSevenOffset = TimeSpan.FromHours(-7);

        public static string CovertToString(DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }
        public static string CovertToStandardUtcString(DateTime date)
        {
            return date.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
        }

        public static DateTime? ConvertUtcToBcTime(DateTime? utcDate)
        {
            if (utcDate == null)
                return null;

            var utcValue = DateTime.SpecifyKind((DateTime)utcDate, DateTimeKind.Unspecified);

            var date = ConvertTimeFromUtc(utcValue, TimeZones.BritishColumbiaTimeZoneId);

            if (date != null)
                return (DateTime)date;

            return utcValue;
        }

        private static DateTime? ConvertTimeFromUtc(DateTime date, string timeZoneId)
        {
            try
            {
                var timezone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return TimeZoneInfo.ConvertTimeFromUtc(date, timezone);
            }
            catch (TimeZoneNotFoundException)
            {
                return null;
            }
            catch (InvalidTimeZoneException)
            {
                return null;
            }
        }

        public static DateTime? ConvertBcTimeToUtc(DateTime? pstDate)
        {
            if (pstDate == null)
                return null;

            var date = ConvertTimeToUtc((DateTime)pstDate, TimeZones.BritishColumbiaTimeZoneId);

            if (date != null)
                return (DateTime)date;

            return pstDate;
        }

        private static DateTime? ConvertTimeToUtc(DateTime date, string timeZoneId)
        {
            try
            {
                var timezone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return TimeZoneInfo.ConvertTimeToUtc(date, timezone);
            }
            catch (TimeZoneNotFoundException)
            {
                return null;
            }
            catch (InvalidTimeZoneException)
            {
                return null;
            }
        }

        public static string ConvertUtcToPacificTime24HourString(DateTime? utcDate)
        {
            var pstDate = ConvertUtcToBcTime(utcDate);

            if (pstDate == null || pstDate == DateTime.MinValue)
                return "";

            return To24HourString((DateTime)pstDate);
        }

        public static string ConvertUtcToPacificTime12HourString(DateTime? utcDate)
        {
            var pstDate = ConvertUtcToBcTime(utcDate);

            if (pstDate == null || pstDate == DateTime.MinValue)
                return "";

            return ((DateTime)pstDate).ToString("yyyy-MM-dd h:mm tt");
        }

        public static string To24HourString(this DateTime date)
        {
            return date.ToString("yyyy-MM-dd HH:mm");
        }

        public static string[] ValidDateTimeFormats()
        {
            return new[]
                    {
                    "yyyy-MM-ddTHH:mm:ssK", // Zulu time, no milliseconds
                    "yyyy-MM-ddTHH:mm:ss.FK", // Zulu time, with milliseconds
                    "yyyy-MM-ddTHH:mm:ss.FFK", // Zulu time, with milliseconds
                    "yyyy-MM-ddTHH:mm:ss.FFFK", // Zulu time, with milliseconds
                    "yyyy-MM-ddTHH:mm:ss.FFFFK", // Zulu time, with milliseconds
                    "yyyy-MM-ddTHH:mm:ss.FFFFFK", // Zulu time, with milliseconds
                    "yyyy-MM-ddTHH:mm:ss.FFFFFFK", // Zulu time, with milliseconds
                    "yyyy-MM-ddTHH:mm:ss.FFFFFFFK", // Zulu time, with milliseconds, CR10X loggers provide milliseconds up to 7 digits
                    "yyyy-MM-ddTHH:mm:ss zzzzzz", // time offset, no milliseconds
                    "yyyy-MM-ddTHH:mm:ss.F zzzzzz", // time offset, with milliseconds
                    "yyyy-MM-ddTHH:mm:ss.FF zzzzzz", // time offset, with milliseconds
                    "yyyy-MM-ddTHH:mm:ss.FFF zzzzzz", // time offset, with milliseconds
                    "yyyy-MM-ddTHH:mm:ss.FFFF zzzzzz", // time offset, with milliseconds
                    "yyyy-MM-ddTHH:mm:ss.FFFFF zzzzzz", // time offset, with milliseconds
                    "yyyy-MM-ddTHH:mm:ss.FFFFFF zzzzzz", // time offset, with milliseconds
                    "yyyy-MM-ddTHH:mm:ss.FFFFFFF zzzzzz", // time offset, with milliseconds, CR10X loggers provide milliseconds up to 7 digits
                };
        }

        public static long ConvertToEpochMilliseconds(DateTime date)
        {
            return new DateTimeOffset(date).ToUnixTimeMilliseconds();
        }

        public static DateTime ConvertStrDatetimeToUTC(string origDt)
        {
            DateTime dt = DateTime.Parse(origDt);
            return ConvertDatetimeToUTC(dt);
        }
        public static DateTime ConvertDatetimeToUTC(DateTime dt)
        {
            DateTime originalDateTime = dt;
            DateTime dtConverted;
            try
            {
                dtConverted = TimeZoneInfo.ConvertTimeToUtc(dt);
            }
            catch (Exception)
            {
                TimeZoneInfo.AdjustmentRule[] adjustments = TimeZoneInfo.Local.GetAdjustmentRules();
                foreach (TimeZoneInfo.AdjustmentRule adj in adjustments)
                {
                    if (dt < adj.DateEnd && dt > adj.DateStart)
                    {
                        if (dt.Month == adj.DaylightTransitionStart.Month && dt.Hour == adj.DaylightTransitionStart.TimeOfDay.Hour)
                        {
                            dt = dt.Add(adj.DaylightDelta);
                        }

                        if (dt.Month == adj.DaylightTransitionEnd.Month && dt.Hour == adj.DaylightTransitionEnd.TimeOfDay.Hour)
                        {
                            dt = dt.Subtract(adj.DaylightDelta);
                        }

                        // If after all this, the datetime hasn't been changed, something has gone wrong,
                        // so reset it to the default value (DateTime.MinValue).
                        if (dt == originalDateTime)
                        {
                            dt = DateTime.MinValue;
                        }

                        break;
                    }
                }

                dtConverted = TimeZoneInfo.ConvertTimeToUtc(dt);
            }

            return dtConverted;
        }
        public static DateTime TrimSeconds(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, dt.Kind);
        }
        public static DateTime StandardizeTime(string endDate, string dateTimeFormat)
        {
            // check if the datetime strings include time
            Regex timeMatcher = new Regex(@"\d\d\d\d-\d\d-\d\d-\d\d-\d\d");

            //bool startDateIncludesTime = timeMatcher.Match(startDate).Success;
            bool endDateIncludesTime = timeMatcher.Match(endDate).Success;

            //string dateTimeFormat = "yyyy-MM-dd-HH-mm-ss";

            //startDate += startDateIncludesTime ? "-00" : "-00-00-00";
            //DateTime startDateTime = DateTime.ParseExact(startDate, dateTimeFormat, CultureInfo.InvariantCulture);


            DateTime endDateTime = DateTime.ParseExact(endDate, dateTimeFormat, CultureInfo.InvariantCulture);

            return endDateTime;
        }
        public static List<DateTime> StandardizeTimeRange(string startDate, string endDate, string dateTimeFormat)
        {
            // check if the datetime strings include time
            Regex timeMatcher = new Regex(@"\d\d\d\d-\d\d-\d\d-\d\d-\d\d");

            bool startDateIncludesTime = timeMatcher.Match(startDate).Success;
            bool endDateIncludesTime = timeMatcher.Match(endDate).Success;

            //string dateTimeFormat = "yyyy-MM-dd-HH-mm-ss";

            startDate += startDateIncludesTime ? "-00" : "-00-00-00";
            DateTime startDateTime = DateTime.ParseExact(startDate, dateTimeFormat, CultureInfo.InvariantCulture);


            endDate += endDateIncludesTime ? "-59" : "-23-59-59";
            DateTime endDateTime = DateTime.ParseExact(endDate, dateTimeFormat, CultureInfo.InvariantCulture);

            return new List<DateTime>() { startDateTime, endDateTime };
        }
        /// <summary>
        /// This function calculates the closest previous datetime interval for a given datetime, based on a scheduled time.
        /// Eg. Job is scheduled to run at 15:00 and every 5 minutes, the previous scheduled time of 15:03 will be 15:00.
        /// The calculated time will always be before the currentDateTime.
        /// </summary>
        /// <param name="interval">The interval in minutes</param>
        /// <param name="currentDateTime">a given datetime,</param>
        /// <returns>The previous scheduled time</returns>
        public static DateTime CalculatePreviousScheduledTime(int interval, DateTime currentDateTime)
        {
            DateTime baseDatetime = DateTime.Parse("1950-01-01 00:00:00.000"); //The time that scheduled time is based on
            //[Optional (Default = TimeSpan.TicksPerSecond)] Determine the tick precision, the lowest possible value is TicksPerMillisecond
            long tickPrecision = TimeSpan.TicksPerSecond;
            // Reset prevDateTime to tick precision.
            DateTime prevDateTime = new DateTime(((long)currentDateTime.Ticks / tickPrecision) * tickPrecision);
            // (Subtract Difference in modulus time intervals between aftertime base time) + the interval.
            return prevDateTime - TimeSpan.FromMinutes((prevDateTime - baseDatetime).TotalMinutes % interval) - TimeSpan.FromMinutes(interval);
        }

        public static string ConvertEnvironmentCanadaUtc(string dtString)
        {
            if (string.IsNullOrWhiteSpace(dtString)) return "";
            // Define the expected date and time format
            string format = "dddd MMMM dd, yyyy 'at' HH:mm 'UTC'";

            // Parse the string to DateTime
            DateTime dt = DateTime.ParseExact(dtString, format, CultureInfo.InvariantCulture);

            // Return the formatted date in "2021-10-19T18:00:00.000Z" format
            return CovertToStandardUtcString(dt);

        }

        /// <summary>
        /// TruncateToQuarterHour.
        /// e.g. 2024-02-08 12:14:59 to 2024-02-08 12:00:00
        /// 2024-02-08 12:49:01 to 2024-02-08 12:45:00
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static DateTime TruncateToQuarterHour(DateTime dateTime)
        {
            int minutes = dateTime.Minute;
            int quarter = minutes / 15; // Determine the quarter of the hour

            // Calculate the truncated minutes
            int truncatedMinutes = quarter * 15;

            // Truncate the minutes part and return the result
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, truncatedMinutes, 0);
        }

        /// <summary>
        /// Get the latest Collection time
        /// </summary>
        /// <param name="utcNow"></param>
        /// <param name="interval">interval minutes</param>
        /// <returns></returns>
        public static DateTime GetLatestCollectionTime(DateTime utcNow, int interval)
        {
            // Calculate the number of intervals that have passed since the start of the day
            int intervalsSinceStartOfDay = (int)(utcNow.TimeOfDay.TotalMinutes / interval);

            // Calculate the latest collection time by multiplying the interval count by the interval duration
            DateTime latestCollectionTime = utcNow.Date.AddMinutes(intervalsSinceStartOfDay * interval);

            return latestCollectionTime;
        }
        /// <summary>
        /// Calculates the closest next collection time (utc) based on the current time and specified interval.
        /// </summary>
        /// <param name="utcNow">The current UTC time.</param>
        /// <param name="interval">The interval in minutes (must be either 15 or 60).</param>
        /// <returns>The next collection time rounded to the nearest interval.</returns>
        public static DateTime GetNextCollectionTime(DateTime utcNow, int interval)
        {
            // Validate the interval is either 15 or 60 minutes
            if (interval != 15 && interval != 60)
            {
                throw new ArgumentException("Interval must be either 15 or 60 minutes.");
            }

            // Calculate the number of minutes since the start of the day
            int minutesSinceStartOfDay = utcNow.Hour * 60 + utcNow.Minute;

            // Calculate the number of complete intervals that have passed since the start of the day plus one
            int intervalsSinceStartOfDay = (minutesSinceStartOfDay / interval) + 1;

            // Calculate the total minutes for the next interval
            int nextIntervalMinutes = intervalsSinceStartOfDay * interval;

            // Create the next collection time by adding the next interval minutes to the start of the day
            DateTime nextCollectionTime = new DateTime(
                utcNow.Year,
                utcNow.Month,
                utcNow.Day,
                0,
                0,
                0,
                utcNow.Kind
            ).AddMinutes(nextIntervalMinutes);

            return nextCollectionTime;
        }
        /// <summary>
        /// Get nearest previous hour for collection time.
        /// e.g.
        /// 2024-06-12 19:00:00 with interval 15  returns 2024-06-12 19:00:00
        /// 2024-06-12 19:15:00 with interval 15  returns 2024-06-12 19:00:00
        /// 2024-06-12 19:30:00 with interval 15  returns 2024-06-12 19:00:00
        /// 2024-06-12 19:45:00 with interval 15  returns 2024-06-12 19:00:00
        /// </summary>
        /// <param name="collectionTime"></param>
        /// <param name="interval">minutes-positive non-zero integer</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static DateTime ConvertToNearestHourCollection(DateTime collectionTime, int interval)
        {
            /// Calculate the total minutes past the nearest past hour mark
            int totalMinutes = collectionTime.Minute % interval;

            // Subtract the excess minutes to align to the nearest past hour mark
            DateTime nearestHourMark = collectionTime.AddMinutes(-totalMinutes).AddSeconds(-collectionTime.Second).AddMilliseconds(-collectionTime.Millisecond);

            // Align to the nearest past full hour
            nearestHourMark = new DateTime(nearestHourMark.Year, nearestHourMark.Month, nearestHourMark.Day, nearestHourMark.Hour, 0, 0);

            return nearestHourMark;
        }
        public static DateTime TruncateToMinute(DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0);
        }

        /// <summary>
        /// Returns BC time using Pacific conversion fallback logic.
        /// </summary>
        /// <param name="utcDate"></param>
        /// <returns></returns>
        public static DateTime? ConvertUtcToBCTime(DateTime? utcDate)
        {
            return ConvertUtcToBcTime(utcDate);
        }

    }
}
