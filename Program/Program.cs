using System;
using TimeTable.Core;
using System.Threading.Tasks;
using System.IO;
using System.Text;
namespace Program
{
    class Program
    {
        private static string ReadPassword() {
            StringBuilder passwordBuffer = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) break;
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (passwordBuffer.Length > 0) {
                        Console.Write('\b');
                        Console.Write(' ');
                        Console.Write('\b');
                        passwordBuffer.Remove(passwordBuffer.Length-1, 1);
                    }
                }
                else if (!char.IsControl(key.KeyChar))
                {   
                    Console.Write('*');
                    passwordBuffer.Append(key.KeyChar);
                }
            }
            return passwordBuffer.ToString();
        }

        static void Main(string[] args)
        {

            Task.Run(async () => {
                Session session = null;

                Console.Write("学  号: ");
                var username = Console.ReadLine();

                Console.Write("密  码: ");
                var password = Program.ReadPassword();

                Console.WriteLine();
                try
                {
                    session = await Session.LoginAsync(username, password);
                }
                catch (LoginException ex)
                {
                    Console.WriteLine(ex.Message);
                    return;
                }
                
                Console.WriteLine("学期号为 四位学年 + 两位学期序号");
                Console.WriteLine("如 2017 学年第二学期 201702");
                Console.Write("学  期: ");
                var time = Console.ReadLine();
                
                var lectures = await session.GetTimeTableAsync(time);
                if (lectures?.Count != 0)
                {
                    await File.WriteAllTextAsync("timetable.ics", lectures.ToICS(), Encoding.UTF8);
                    Console.WriteLine("课程表已经导出到 timetable.ics");
                } 
                else 
                {
                    Console.WriteLine("暂无该学期课表.");
                }
                
                var exams = await session.GetExamTimeTableAsync(time);
                if (exams?.Count != 0)
                {
                    await File.WriteAllTextAsync("exam.ics", exams.ToICS(), Encoding.UTF8);
                    Console.WriteLine("考试安排已经导出到 exams.ics");
                }
                else {
                    Console.WriteLine("暂无该学期考试安排.");
                }
                
                Console.Write("按任意键退出...");
                Console.ReadKey(true);
            }).Wait();
        }
    }
}
