using System;
using TimeTable.Core;
using System.Threading.Tasks;
using System.IO;
using System.Text;
namespace Program
{
    class Program
    {

        static void Main(string[] args)
        {

            Task.Run(async () => {
                var session = await Session.Connect();
                

                Console.Write("学  号: ");
                var username = Console.ReadLine();

                Console.Write("密  码: ");
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

                await File.WriteAllBytesAsync("captcha.png", await session.GetCaptchaAsync());

                Console.WriteLine();
                Console.Write("验证码: ");
                var captcha = Console.ReadLine();

                try
                {
                    await session.LoginAsync(username, passwordBuffer.ToString(), captcha);
                }
                catch (LoginException ex)
                {
                    Console.WriteLine(ex.Message);
                    return;
                }
                
                Console.WriteLine("学期号为 四位学年 + 两位学期序号");
                Console.WriteLine("如 2017 学年第二学期 201702");
                Console.Write("学  期: ");
                var time =int.Parse(Console.ReadLine());

                var lectures = await session.GetTimeTableAsync(time);
                if (lectures == null)
                {
                    Console.WriteLine("暂无该学期课表.");
                    return;
                }
                    
                var weekOneMon = await session.GetWeekOneMondayAsync(time);
                var ics = lectures.ToICS(weekOneMon);
                await File.WriteAllTextAsync("timetable.ics", ics, Encoding.UTF8);

                Console.WriteLine("课程表已经导出到 timetable.ics");
                Console.Write("按任意键退出...");
                Console.ReadKey(true);
            }).Wait();
        }
    }
}
