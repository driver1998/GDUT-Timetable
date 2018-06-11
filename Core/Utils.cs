using System;
using System.Text;
namespace TimeTable.Core
{
    public static class Utils
    {
        // 大学城上课时间 按节数排序 第0位空置
        public static TimeSpan[] LectureStart = new TimeSpan[] {
            TimeSpan.MinValue,
            new TimeSpan( 8, 30, 0),
            new TimeSpan( 9, 20, 0),
            new TimeSpan(10, 25, 0),
            new TimeSpan(11, 15, 0),
            new TimeSpan(13, 50, 0),
            new TimeSpan(14, 40, 0),
            new TimeSpan(15, 30, 0),
            new TimeSpan(16, 30, 0),
            new TimeSpan(17, 20, 0),
            new TimeSpan(18, 30, 0),
            new TimeSpan(19, 20, 0),
            new TimeSpan(20, 10, 0)
        };

        // 大学城每节课时长
        public static TimeSpan LectureDuration = new TimeSpan(0, 45, 0);

        // DateTime 转换为 ICS 中的时间戳（为方便用 UTC）
        // 输入参数为本地时（北京时间）
        public static string DateTimeToICSTimeStamp(DateTime dt)
        {
            var utc = dt.ToUniversalTime();
            return string.Format("{0:yyyyMMdd}T{1:HHmmss}Z", utc, utc);
        }

        // 创建 ICS 日程
        public static string NewICSEvent(string eventName, string location, string additionalInfo, DateTime start, DateTime end)
        {
            var timeStamp = Utils.DateTimeToICSTimeStamp(DateTime.Now);
            var startStamp = Utils.DateTimeToICSTimeStamp(start);
            var endStamp = Utils.DateTimeToICSTimeStamp(end);

            var sb = new StringBuilder();
            sb.AppendLine(  "BEGIN:VEVENT");
            sb.AppendLine($"UID:GDUT-{Guid.NewGuid()}");
            sb.AppendLine($"SUMMARY:{eventName}");
            sb.AppendLine($"DTSTART:{startStamp}");
            sb.AppendLine($"DTEND:{endStamp}");
            sb.AppendLine($"DTSTAMP:{timeStamp}");
            sb.AppendLine($"STATUS:CONFIRMED");
            sb.AppendLine($"LOCATION:{location} {additionalInfo}");
            sb.AppendLine( "END:VEVENT");
            
            return sb.ToString();
        }
    }
}