using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace TimeTable.Core
{
    // 教务系统的登录返回 json
    public class JWResponse
    {
        public int code { get; set; }
        public string data { get; set; }
        public string message { get;set; }

        public override string ToString()
            => $"code:{code}, data:{data}, message:{message}";        
    }

    // 课程对象
    public class JWLecture
    {
        // 名称
        public string Name { get; set; }

        // 上课周
        public int[] Weeks { get; set; }

        // 地点
        public string Location { get; set; }

        // 教师
        public string Teacher { get; set; }

        // 星期几
        public int Weekday { get; set; }

        // 第几节
        public int[] Sections { get; set; }
        
        public override string ToString()
            => $"{{{Name}, {Location}, {Teacher}, {Weeks}, {Weekday}, {Weeks}, {Sections}}}";

        // 将课程转换为 ICS 日程
        public string ToICSEvent(DateTime weekOneMonday)
        {
            var sb = new StringBuilder();

            // 把时间去掉，只保留日期值
            weekOneMonday = weekOneMonday.Date;

            // 确定开始和结束时间，sections已经事先排序
            var start = Utils.LectureStart[Sections.First()];
            var end = Utils.LectureStart[Sections.Last()] + Utils.LectureDuration;

            // 创建日程的时间戳
            var timeStamp = Utils.DateTimeToICSTimeStamp(DateTime.Now);

            // 每周创建一个日程条目
            foreach (int week in this.Weeks)
            {
                // 加周数和星期几
                var day = weekOneMonday.AddDays((week-1)*7 + (Weekday-1));
                
                // 计算实际起始时间并转换为时间戳
                var startStamp = Utils.DateTimeToICSTimeStamp(day + start);
                var endStamp = Utils.DateTimeToICSTimeStamp(day + end);

                sb.AppendLine(  "BEGIN:VEVENT");
                sb.AppendLine($"UID:GDUT-{Guid.NewGuid()}");
                sb.AppendLine($"SUMMARY:{Name}");
                sb.AppendLine($"DTSTART:{startStamp}");
                sb.AppendLine($"DTEND:{endStamp}");
                sb.AppendLine($"DTSTAMP:{timeStamp}");
                sb.AppendLine($"STATUS:CONFIRMED");
                sb.AppendLine($"LOCATION:{Location} {Teacher}");
                sb.AppendLine( "END:VEVENT");
            }

            return sb.ToString();
        }
    }

    // 课程表对象
    public class JWLectureList : List<JWLecture>
    {
        // 解析教务系统的课程表 json
        public static JWLectureList FromJson(string json)
        {
            var jArray = JArray.Parse(json);
            var lectures = new JWLectureList();

            foreach (JObject o in jArray)
            {
                var weekSplit = ((string)o["zcs"]).Split(new[] {','});
                var sectSplit = ((string)o["jcdm2"]).Split(new[] {','});

                lectures.Add(new JWLecture() {
                    Name = (string)o["kcmc"],
                    Location = (string)o["jxcdmcs"],
                    Teacher = (string)o["teaxms"],
                    Weekday = (int)o["xq"],
                    Weeks = weekSplit.Select(p => int.Parse(p)).ToArray(),
                    Sections = sectSplit.Select(p => int.Parse(p)).OrderBy(p=>p).ToArray()
                });
            }

            return lectures;
        }

        // 将整个课程表导出为 ICS
        public string ToICS(DateTime weekOneMonday)
        {
            var sb = new StringBuilder();

            sb.AppendLine(
                "BEGIN:VCALENDAR\r\n"                     + 
                "METHOD:PUBLISH\r\n"                      + 
                "PRODID:GDUT TimeTable by driver1998\r\n" + 
                "VERSION:2.0"
            );
            foreach (var o in this)
                sb.Append(o.ToICSEvent(weekOneMonday));         
            sb.AppendLine("END:VCALENDAR");

            return sb.ToString();
        }
    }
}