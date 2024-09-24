using System;

namespace ExamProcessManage.Utils
{
    public class DateTimeFormat
    {
        // Phương thức chuyển từ OffsetDateTime (DateTimeOffset) sang DateOnly
        public static DateOnly ConvertToDateOnly(DateTimeOffset dateTimeOffset)
        {
            return DateOnly.FromDateTime(dateTimeOffset.DateTime);
        }

        // Phương thức chuyển từ chuỗi OffsetDateTime (DateTimeOffset) sang DateOnly
        public static DateOnly ConvertToDateOnly(string dateTimeOffsetString)
        {
            if (DateTimeOffset.TryParse(dateTimeOffsetString, out DateTimeOffset dateTimeOffset))
            {
                return ConvertToDateOnly(dateTimeOffset);
            }
            throw new FormatException("Chuỗi DateTimeOffset không hợp lệ.");
        }

   
        public static DateTime ConvertToDateTime(string dateTimeOffsetString)
        {
            if (DateTime.TryParse(dateTimeOffsetString, out DateTime dateTime))
            {
                return dateTime;
            }
            throw new FormatException("Chuỗi DateTimeOffset không hợp lệ.");
        }

        // Phương thức chuyển từ DateOnly sang OffsetDateTime (DateTimeOffset)
        public static DateTimeOffset ConvertToDateTimeOffset(DateOnly dateOnly, TimeSpan offset)
        {
            return new DateTimeOffset(dateOnly.ToDateTime(TimeOnly.MinValue), offset);
        }

        // Phương thức chuyển từ DateOnly sang chuỗi OffsetDateTime (DateTimeOffset)
        public static string ConvertToDateTimeOffsetString(DateOnly dateOnly, TimeSpan offset)
        {
            return ConvertToDateTimeOffset(dateOnly, offset).ToString("o"); // "o" format: round-trip date/time pattern (ISO 8601)
        }
    }
}


