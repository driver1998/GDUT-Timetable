using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Globalization;

namespace TimeTable.Core
{
    
    public class JWEvent
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public string Teacher { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string ToICSEvent()
            => Utils.NewICSEvent(Name, Location, Teacher, StartTime, EndTime);
    }

    // 时间表
    public class JWList : List<JWEvent>
    {
        // 将整个表导出为 ICS
        public string ToICS()
        {
            var sb = new StringBuilder();

            sb.AppendLine(
                "BEGIN:VCALENDAR\r\n"                     + 
                "METHOD:PUBLISH\r\n"                      + 
                "PRODID:GDUT TimeTable by driver1998\r\n" + 
                "VERSION:2.0"
            );
            foreach (var o in this)
                sb.Append(o.ToICSEvent());         
            sb.AppendLine("END:VCALENDAR");

            return sb.ToString();
        }
    }

    // 解析教务系统 json 的静态类
    public static class JWJsonParser
    {        
        // 解析教务系统的课程表 json
        public static JWList FromLectureJson(string json, DateTime weekOneMonday)
        {
            var jArray = JArray.Parse(json);
            var lectures = new JWList();

            foreach (JObject o in jArray)
            {
                // 课在星期几上
                var weekday = (int)o["xq"];

                // 上课周列表，升序排列
                var weekSplit = ((string)o["zcs"]).Split(new[] {','});
                var weeks = weekSplit.Select(p => int.Parse(p)).ToArray();

                // 第几节
                var sectSplit = ((string)o["jcdm2"]).Split(new[] {','});
                var sections = sectSplit.Select(p => int.Parse(p)).OrderBy(p=>p).ToArray();

                // 确定开始和结束时间
                var start = Utils.LectureStart[sections.First()];
                var end = Utils.LectureStart[sections.Last()] + Utils.LectureDuration;

                // 每周创建一个日程条目
                foreach (int week in weeks)
                {
                    // 加周数和星期几
                    var day = weekOneMonday.AddDays((week-1)*7 + (weekday-1));

                    lectures.Add(new JWEvent() {
                        Name = (string)o["kcmc"],
                        Location = (string)o["jxcdmcs"],
                        Teacher = (string)o["teaxms"],
                        StartTime = day + start,
                        EndTime = day + end
                    });
                }
            }

            return lectures;
        }

        // 解析教务系统的考试安排 json
        public static JWList FromExamJson(string json)
        {
            var jObject = JObject.Parse(json);
            var jArray = (JArray)jObject["rows"];
            var exams = new JWList();

            foreach (JObject o in jArray)
            {
                var examDate = DateTime.ParseExact(
                    (string)o["ksrq"], "yyyy-MM-dd", CultureInfo.InvariantCulture
                );
                
                var split = ((string)o["kssj"]).Split(
                    new[] {"--"}, 2, StringSplitOptions.RemoveEmptyEntries
                );

                var start = DateTime.Parse(split[0]);
                var end = DateTime.Parse(split[1]);
                exams.Add(new JWEvent() {
                    Name = $"{(string)o["kcmc"]} 考试",
                    Location = (string)o["kscdmc"],
                    Teacher = (string)o["jkteaxms"],
                    StartTime = examDate + new TimeSpan(start.Hour, start.Minute, 0),
                    EndTime = examDate + new TimeSpan(end.Hour, end.Minute, 0),
                });
            }

            return exams;
        }
    }
}