﻿using System;
using System.Globalization;

namespace Knowte.Core.Utils
{
    public static class DateUtils
    {
        public static int CountDays(DateTime startTime, DateTime endTime)
        {
            TimeSpan span = endTime.Date.Subtract(startTime.Date);

            return span.Days;
        }

        public static string FormatNoteModificationDate(long noteModificationDateTicks, bool showExactDates)
        {
            string retVal = string.Empty;

            var noteModificationDate = new DateTime(noteModificationDateTicks);

            if (showExactDates)
            {
                return noteModificationDate.ToString("D", CultureInfo.CurrentCulture);
            }

            var now = DateTime.Now;

            TimeSpan span = now.Date.Subtract(noteModificationDate.Date);

            if (span.Days > 1 & span.Days < 7)
            {
                retVal = span.Days + " days ago";
            }
            else if (span.Days == 0)
            {
                retVal = "Today";
            }
            else if (span.Days == 1)
            {
                retVal = "Yesterday";
            }
            else if (span.Days >= 7 & span.Days < 14)
            {
                retVal = "Last week";
            }
            else if (span.Days >= 14 & span.Days < 21)
            {
                retVal = "Two weeks ago";
            }
            else if (span.Days >= 21 & span.Days < 31)
            {
                retVal = "Three weeks ago";
            }
            else if (now.Month == noteModificationDate.Month + 1 & now.Year == noteModificationDate.Year)
            {
                retVal = "Last month";
            }
            else
            {
                retVal = "Long ago";
            }

            return retVal;
        }
    }
}
