namespace GP.Application.Common
{
    public static class AppTime
    {
        public static DateTime AsUtc(DateTime value)
        {
            return GP.Domain.Common.AppTime.AsUtc(value);
        }

        public static DateTime AsSchedule(DateTime value)
        {
            return GP.Domain.Common.AppTime.AsSchedule(value);
        }

        public static DateTime GetScheduleNow()
        {
            return GP.Domain.Common.AppTime.GetScheduleNow();
        }
    }
}