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
        const string JWURL = "http://jxfw.gdut.edu.cn";
        const string AuthURL = "http://authserver.gdut.edu.cn";
        
        private string lt;
        private string dllt;
        private string execution;
        private string eventID;
        private string rmShown;

        private CookieContainer cookies = new CookieContainer();
        private Session() {}
        HttpWebRequest GetWebRequest(string URL, string method) {
            var webRequest = (HttpWebRequest)WebRequest.Create(URL);
            webRequest.Method = method;
            webRequest.UserAgent = UserAgent;
            webRequest.CookieContainer = cookies;
            webRequest.Referer = JWURL;
            webRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            return webRequest;
        }

        // 连接统一认证服务
        private async static Task<Session> GetSessionAsync() {
            var session = new Session();
            var targetURL =$"{JWURL}/new/ssoLogin".Replace("/", "%2F").Replace(":", "%3A");
            var webRequest = session.GetWebRequest($"{AuthURL}/authserver/login?service={targetURL}", "GET");

            string html;
            using (var response = await webRequest.GetResponseAsync())
            {
                var reader = new StreamReader(response.GetResponseStream());
                html = await reader.ReadToEndAsync(); 
            }

            var regex = new Regex(@"input type=""hidden"" name=""(.*?)"" value=""(.*?)""");
            var matches = regex.Matches(html);

            if (matches.Count != 5 || session.cookies.Count == 0) 
                throw new LoginException("统一认证服务异常, 请稍后重试.");

            session.lt = matches[0].Groups[2].Value;
            session.dllt = matches[1].Groups[2].Value;
            session.execution = matches[2].Groups[2].Value;
            session.eventID = matches[3].Groups[2].Value;
            session.rmShown = matches[4].Groups[2].Value;
            
            return session;
        }

        // 登录
        public static async Task<Session> LoginAsync(string username, string password) {
            var session = await GetSessionAsync();

            var authURI = new Uri(AuthURL);
            var targetURL =$"{JWURL}/new/ssoLogin".Replace("/", "%2F").Replace(":", "%3A");

            var webRequest = session.GetWebRequest(
                $"{AuthURL}/authserver/login?service={targetURL}",
                "POST"
            );
            using (var reqStream = await webRequest.GetRequestStreamAsync())
            {
                var writer = new StreamWriter(reqStream);
                await writer.WriteAsync(
                    $"username={username}&password={password}&" +
                    $"lt={session.lt}&dllt={session.dllt}&execution={session.execution}&" + 
                    $"_eventId={session.eventID}&rmShown={session.rmShown}"
                );
                writer.Close();
            }
            
            using (var response = await webRequest.GetResponseAsync())
            {
                if (response.ResponseUri.Host == authURI.Host)
                    throw new LoginException("登录失败, 请检查用户名和密码.");
            }

            return session;
        }

        // 取课程表
        // time: 四位数年份 + 01上学期/02下学期
        // eg: 2017学年下学期 201702
        public async Task<JWList> GetTimeTableAsync(string time)
        {
            var weekOneMonday = await GetWeekOneMondayAsync(time);

            var webRequest = GetWebRequest($"{JWURL}/xsgrkbcx!xsAllKbList.action?xnxqdm={time}", "GET");
            string html;

            using (var response = await webRequest.GetResponseAsync())
            {
                var reader = new StreamReader(response.GetResponseStream());
                html = await reader.ReadToEndAsync(); 
            }

            var regex = new Regex(@"var kbxx = (\[.*?\]);");
            if (!regex.IsMatch(html)) return null;

            var json = regex.Match(html).Groups[1].Value;
            var lectures = JWJsonParser.FromLectureJson(json, weekOneMonday);
            
            return lectures;
        }

        public async Task<JWList> GetExamTimeTableAsync(string time) 
        {
            var webRequest = GetWebRequest($"{JWURL}/xsksap!getDataList.action", "POST");
            string json;

            using (var requestStream = await webRequest.GetRequestStreamAsync()) 
            {
                var writer = new StreamWriter(requestStream);
                await writer.WriteAsync($"xnxqdm={time}");
                writer.Close();
            }

            using (var response = await webRequest.GetResponseAsync())
            {
                var reader = new StreamReader(response.GetResponseStream());
                json = await reader.ReadToEndAsync();
            }

            var exams = JWJsonParser.FromExamJson(json);
            return exams;
        }

        // 取第一周星期一的日期，便于计算时间
        private async Task<DateTime> GetWeekOneMondayAsync(string time)
        {
            var webRequest = GetWebRequest($"{JWURL}/xsbjkbcx!getKbRq.action?xnxqdm={time}&zc=1", "GET");
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
