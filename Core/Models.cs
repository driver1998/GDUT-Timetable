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
    
    // 教务系统的登录返回 json
    public class JWResponse
    {
        public int code { get; set; }
        public string data { get; set; }
        public string message { get;set; }

        public override string ToString()
            => $"code:{code}, data:{data}, message:{message}";        
    }

    // 教务系统中的一个条目
    // 对应一个 ICS 日程
    public interface JWEvent
    {
        string ToICSEvent();
    }

    // 课程对象
    public class JWLecture : JWEvent
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
        
        // 第一周星期一，用于计算上课时间
        DateTime weekOneMonday;
        public DateTime WeekOneMonday
        {
            // 只要日期值
            get => weekOneMonday;
            set => weekOneMonday = value.Date;
        }
    
        // 将课程转换为 ICS 日程
        public string ToICSEvent()
        {
            var sb = new StringBuilder();

            // 确定开始和结束时间，sections已经事先排序
            var start = Utils.LectureStart[Sections.First()];
            var end = Utils.LectureStart[Sections.Last()] + Utils.LectureDuration;

            // 每周创建一个日程条目
            foreach (int week in this.Weeks)
            {
                // 加周数和星期几
                var day = WeekOneMonday.AddDays((week-1)*7 + (Weekday-1));

                sb.Append(
                    Utils.NewICSEvent(Name, Location, Teacher, day+start, day+end)
                );
            }

            return sb.ToString();
        }
    }

    public class JWExam : JWEvent
    {
        // 考试科目名
        public string Name { get; set; }
        
        //考试地点
        public string Location { get; set; }

        // 监考老师
        public string Teacher { get; set; }

        // 开考时间
        public DateTime StartTime { get; set; }

        // 考试结束时间
        public DateTime EndTime { get; set; }

        public string ToICSEvent()
            => Utils.NewICSEvent($"{Name} 考试", Location, Teacher, StartTime, EndTime);
    }

    // 时间表
    public class JWList<T> : List<T> where T : JWEvent
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
        public static JWList<JWLecture> FromLectureJson(string json, DateTime weekOneMonday)
        {
            var jArray = JArray.Parse(json);
            var lectures = new JWList<JWLecture>();

            foreach (JObject o in jArray)
            {
                var weekSplit = ((string)o["zcs"]).Split(new[] {','});
                var sectSplit = ((string)o["jcdm2"]).Split(new[] {','});

                lectures.Add(new JWLecture() {
                    Name = (string)o["kcmc"],
                    Location = (string)o["jxcdmcs"],
                    Teacher = (string)o["teaxms"],
                    Weekday = (int)o["xq"],
                    WeekOneMonday = weekOneMonday,
                    Weeks = weekSplit.Select(p => int.Parse(p)).ToArray(),
                    Sections = sectSplit.Select(p => int.Parse(p)).OrderBy(p=>p).ToArray()
                });
            }

            return lectures;
        }

        // 解析教务系统的考试安排 json
        public static JWList<JWExam> FromExamJson(string json)
        {
            var jObject = JObject.Parse(json);
            var jArray = (JArray)jObject["rows"];
            var exams = new JWList<JWExam>();

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
                exams.Add(new JWExam() {
                    Name = (string)o["kcmc"],
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