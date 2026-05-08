namespace GP.Domain.Common;

public static class AppTime
{
    private static readonly TimeZoneInfo ScheduleTimeZone = ResolveScheduleTimeZone();

    public static DateTime AsUtc(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public static DateTime AsSchedule(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
    }

    public static DateTime GetScheduleNow()
    {
        var scheduleNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ScheduleTimeZone);
        return AsSchedule(scheduleNow);
    }

    private static TimeZoneInfo ResolveScheduleTimeZone()
    {
        var preferredTimeZoneIds = new[] { "Africa/Cairo", "Egypt Standard Time" };

        foreach (var timeZoneId in preferredTimeZoneIds)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                // Try the next timezone id for cross-platform compatibility.
            }
            catch (InvalidTimeZoneException)
            {
                // Try the next timezone id for cross-platform compatibility.
            }
        }

        return TimeZoneInfo.Utc;
    }
}