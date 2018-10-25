# GDUT-Timetable
广东工业大学大学城校区课程表导出工具

它可以从教务系统获取课程表和考试安排，然后保存为 ICS (iCalendar) 文件，便于导入到其它日程表程序/服务，如 Outlook、Google Calendar 等。

安装 .Net Core 2.1 SDK，然后在命令行下执行：
```
$ cd Program
$ dotnet restore
$ dotnet build
```

## 运行
```
$ dotnet run
```

## 备注
- 程序与教务系统之间的连接没有加密，但本程序本身不会保存您的密码
- 导出的 ICS 文件 Program/timetable.ics 和 Program/exam.ics，将两个文件分开，便于导入到两个日历中
- 程序理论可以支持龙洞和东风路校区，但需要手动修改 Core/Utils.cs 中的课程开始时间与课程时长