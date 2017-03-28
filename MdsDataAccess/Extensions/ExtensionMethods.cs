using System;

namespace MdsDataAccess.Extensions
{
    public static class ExtensionMethods
    {
        public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
        {
            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }
    }
}
