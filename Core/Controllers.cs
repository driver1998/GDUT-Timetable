using System;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace TimeTable.Core
{

    public class Session
    {


        const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.140 Safari/537.36 Edge/17.17115";
        const string URL = "http://jxfw.gdut.edu.cn";
        private CookieContainer cookies = new CookieContainer();
        private Session() {}
        
        HttpWebRequest GetWebRequest(string location, string method)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create($"{URL}{location}");
            webRequest.Method = method;
            webRequest.UserAgent = UserAgent;
            webRequest.CookieContainer = cookies;
            webRequest.Referer = URL;
            return webRequest;
        }

        public async static Task<Session> Connect() {
        
            var session = new Session();
            var webRequest = session.GetWebRequest("/", "GET");

            (await webRequest.GetResponseAsync()).Close();
            var jSessionID = session.cookies.GetCookies(new Uri(URL))["JSESSIONID"];

            if (jSessionID == null)
                return null;
            else
                return session;
        }

        // 获取验证码
        public async Task<byte[]> GetCaptchaAsync()
        {
            var startTime = new DateTime(1970, 1, 1);
            var timeStamp = (long)(DateTime.Now.ToUniversalTime() - startTime).TotalMilliseconds;

            var webRequest = GetWebRequest($"/yzm?d={timeStamp}", "GET");
            byte[] captcha;

            using (var response = await webRequest.GetResponseAsync())
            {
                var stream = response.GetResponseStream();
                
                var buffer = new byte[1024]; int actual; 
                using (var ms = new MemoryStream())
                {
                    while ((actual = await stream.ReadAsync(buffer, 0, 1024)) > 0) 
                        await ms.WriteAsync(buffer, 0, actual);
                    ms.Position = 0;
                    captcha = ms.ToArray();
                }
            }
            
            return captcha;
        }

        // 登录
        public async Task LoginAsync(string username, string password, string captcha)
        {
            var webRequest = GetWebRequest($"/new/login", "POST");
            webRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            JWResponse msg;

            using (var reqStream = await webRequest.GetRequestStreamAsync())
            {
                var writer = new StreamWriter(reqStream);
                await writer.WriteAsync($"account={username}&pwd={password}&verifycode={captcha}");
                writer.Close();
            }
            
            using (var response = await webRequest.GetResponseAsync())
            {
                var reader = new StreamReader(response.GetResponseStream());
                var json = await reader.ReadToEndAsync();
                msg = JsonConvert.DeserializeObject<JWResponse>(json);
            }

            if (msg.code != 0)
                throw new LoginException(msg.message);
        }

        // 取课程表
        // time: 四位数年份 + 01上学期/02下学期
        // eg: 2017学年下学期 201702
        public async Task<JWLectureList> GetTimeTableAsync(int time)
        {
            var webRequest = GetWebRequest($"/xsgrkbcx!xsAllKbList.action?xnxqdm={time}", "GET");
            string html;

            using (var response = await webRequest.GetResponseAsync())
            {
                var reader = new StreamReader(response.GetResponseStream());
                html = await reader.ReadToEndAsync(); 
            }

            var regex = new Regex(@"var kbxx = (\[.*?\]);");
            if (!regex.IsMatch(html)) return null;

            var json = regex.Match(html).Groups[1].Value;
            var lectures = JWLectureList.FromJson(json);
            
            return lectures;
        }

        // 取第一周星期一的日期，便于计算时间
        public async Task<DateTime> GetWeekOneMondayAsync(int time)
        {
            var webRequest = GetWebRequest($"/xsbjkbcx!getKbRq.action?xnxqdm={time}&zc=1", "GET");
            DateTime date = DateTime.MinValue;
            
            using (var response = await webRequest.GetResponseAsync())
            {
                var reader = new StreamReader(response.GetResponseStream());
                var json = await reader.ReadToEndAsync();
                var jArray = JArray.Parse(json);

                var weekOneMonday = jArray[1].Where(p => (int)p["xqmc"] == 1).FirstOrDefault();
                if (weekOneMonday != null)
                    date = (DateTime)weekOneMonday["rq"];
            }

            return date;
        }
    }

    public class LoginException : ApplicationException
    {
        public LoginException(string msg) : base(msg) {}
    }

}
