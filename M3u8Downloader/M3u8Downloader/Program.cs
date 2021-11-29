// See https://aka.ms/new-console-template for more information

using CommandLine;
using CommandLine.Text;
using N_m3u8DL_CLI;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace M3u8Downloader.NetCore
{
    class Program {

        public static Socket client;

        public delegate bool ControlCtrlDelegate(int CtrlType);
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ControlCtrlDelegate HandlerRoutine, bool Add);
        private static ControlCtrlDelegate cancelHandler = new ControlCtrlDelegate(HandlerRoutine);
        public static bool HandlerRoutine(int CtrlType)
        {
            switch (CtrlType)
            {
                case 0:
                    LOGGER.WriteLine(strings.ExitedCtrlC
                    + "\r\n\r\nTask End: " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")); //Ctrl+C关闭
                    break;
                case 2:
                    LOGGER.WriteLine(strings.ExitedForce
                    + "\r\n\r\nTask End: " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")); //按控制台关闭按钮关闭
                    break;
            }
            return false;
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        private static void CreateSocket()
        {
            //创建socket
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //连接
            //获得ip和端口（读取配置文件）
            IPEndPoint point = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8123);

            //连接服务器
            try
            {
                client.Connect(point);
            }
            catch
            {
            }
        }
       static void Main(string[] args) {
            SetConsoleCtrlHandler(cancelHandler, true);
            CreateSocket();
            ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
            ServicePointManager.DefaultConnectionLimit = 1024;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13
                                   | SecurityProtocolType.Tls
                                   | SecurityProtocolType.Tls11//Tls11  
                                   | SecurityProtocolType.Tls12; //Tls12  
            if (checkFfmpeg()) {
                //setLang();
                Console.WriteLine("   _____         .__                       ");
                Console.WriteLine("  /  _  \\   ____ |  |   ____  __ __  ______");
                Console.WriteLine(" /  /_\\  \\_/ __ \\|  |  /  _ \\|  |  \\/  ___/");
                Console.WriteLine("/    |    \\  ___/|  |_(  <_> )  |  /\\___ \\ ");
                Console.WriteLine("\\____|__  /\\___  >____/\\____/|____//____  >");
                Console.WriteLine("        \\/     \\/                       \\/ ");
                //ReadLine字数上限
                Stream steam = Console.OpenStandardInput();
                Console.SetIn(new StreamReader(steam, Encoding.Default, false, 5000));

                if (args.Length == 0)
                {
                    //没有参数时
                    Global.WriteInit();
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("Aelous");
                    Console.ResetColor();
                    Console.Write(" > ");
                    var cmd = Console.ReadLine();
                    if (string.IsNullOrEmpty(cmd)) Environment.Exit(0);
                    args = Global.ParseArguments(cmd).ToArray();  //解析命令行
                    var cmdParser = new CommandLine.Parser(with => with.HelpWriter = null);
                    var parserResult = cmdParser.ParseArguments<MyOptions>(args);
                    //解析命令行
                    parserResult
                      .WithParsed(o => DoWork(o))
                      .WithNotParsed(errs => DisplayHelp(parserResult, errs));

                }
                else {
                    MyOptions myOptions = new MyOptions();
                    myOptions.EnableDelAfterDone = true;
                    myOptions.MaxThreads = 32;
                    myOptions.MinThreads = 8;
                    myOptions.RetryCount = 15;
                    myOptions.TimeOut = 10;
                    myOptions.Input = args[0];
                    if (args.Length >= 2)
                    {
                        myOptions.SaveName = args[1];
                    }
                    if (args.Length >= 3) {
                        myOptions.WorkDir = args[2];
                    }
                    DoWork(myOptions);
                }

                Console.ReadLine();
            }
        }

        //检测ffmpeg存在
        public static bool checkFfmpeg() {
            //寻找ffmpeg.exe
            if (File.Exists("ffmpeg.exe"))
            {
                FFmpeg.FFMPEG_PATH = Path.Combine(Environment.CurrentDirectory, "ffmpeg.exe");
                return true;
            }
            else if (File.Exists(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "ffmpeg.exe")))
            {
                FFmpeg.FFMPEG_PATH = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "ffmpeg.exe");
                return true;
            }
            else
            {
                try
                {
                    string[] EnvironmentPath = Environment.GetEnvironmentVariable("Path").Split(';');
                    foreach (var de in EnvironmentPath)
                    {
                        if (File.Exists(Path.Combine(de.Trim('\"').Trim(), "ffmpeg.exe")))
                        {
                            FFmpeg.FFMPEG_PATH = Path.Combine(de.Trim('\"').Trim(), "ffmpeg.exe");
                            return true;
                        }
                    }
                }
                catch (Exception)
                {
                    ;
                }

                Console.BackgroundColor = ConsoleColor.Red; //设置背景色
                Console.ForegroundColor = ConsoleColor.White; //设置前景色，即字体颜色
                Console.WriteLine("===========================>Aelous<=======================");
                Console.WriteLine(strings.ffmpegLost);
                Console.ResetColor(); //将控制台的前景色和背景色设为默认值
                Console.WriteLine(strings.ffmpegTip);
                Console.WriteLine();
                Console.WriteLine("http://ffmpeg.org/download.html#build-windows");
                Console.WriteLine();
                Console.WriteLine(strings.pressAnyKeyExit);
                Console.ReadKey();
                Environment.Exit(-1);
                return false;
            }
        }

        //设置语言
        public static void setLang() {
            try
            {
                string loc = "en-US";
                string currLoc = Thread.CurrentThread.CurrentUICulture.Name;
                if (currLoc == "zh-TW" || currLoc == "zh-HK" || currLoc == "zh-MO") loc = "zh-TW";
                else if (currLoc == "zh-CN" || currLoc == "zh-SG") loc = "zh-CN";
                //设置语言
                CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(loc);
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(loc);
            }
            catch (Exception) {; }
        }

        private static void DoWork(MyOptions o) {
            //o.SaveName = N_m3u8DL_CLI.Parser.UnicodeToString(o.SaveName);
            Console.WriteLine("加载配置ing。。。。。。");
            string CURRENT_PATH = Directory.GetCurrentDirectory();
            string fileName = Global.GetValidFileName(o.SaveName);
            string reqHeaders = o.Headers;
            string muxSetJson = o.MuxSetJson ?? "MUXSETS.json";
            string workDir = string.IsNullOrEmpty(o.WorkDir)?CURRENT_PATH + "\\Downloads": o.WorkDir;
            string keyFile = "";
            string keyBase64 = "";
            string keyIV = "";
            string baseUrl = "";
            Global.STOP_SPEED = o.StopSpeed;
            Global.MAX_SPEED = o.MaxSpeed;
            if (!string.IsNullOrEmpty(o.UseKeyBase64)) keyBase64 = o.UseKeyBase64;
            if (!string.IsNullOrEmpty(o.UseKeyIV)) keyIV = o.UseKeyIV;
            if (!string.IsNullOrEmpty(o.BaseUrl)) baseUrl = o.BaseUrl;
            if (o.EnableBinaryMerge) DownloadManager.BinaryMerge = true;
            if (o.DisableDateInfo) FFmpeg.WriteDate = false;
            if (o.NoProxy) Global.NoProxy = true;
            if (o.DisableIntegrityCheck) DownloadManager.DisableIntegrityCheck = true;
            if (o.EnableAudioOnly) Global.VIDEO_TYPE = "IGNORE";
            if (!string.IsNullOrEmpty(o.WorkDir))
            {
                DownloadManager.HasSetDir = true;
            }
            if (!string.IsNullOrEmpty(o.LiveRecDur))
            {
                //时间码
                Regex reg2 = new Regex(@"(\d+):(\d+):(\d+)");
                var t = o.LiveRecDur;
                if (reg2.IsMatch(t))
                {
                    int HH = Convert.ToInt32(reg2.Match(t).Groups[1].Value);
                    int MM = Convert.ToInt32(reg2.Match(t).Groups[2].Value);
                    int SS = Convert.ToInt32(reg2.Match(t).Groups[3].Value);
                    HLSLiveDownloader.REC_DUR_LIMIT = SS + MM * 60 + HH * 60 * 60;
                }
            }
            if (!string.IsNullOrEmpty(o.DownloadRange))
            {
                string p = o.DownloadRange;

                if (p.Contains(":"))
                {
                    //时间码
                    Regex reg2 = new Regex(@"((\d+):(\d+):(\d+))?-((\d+):(\d+):(\d+))?");
                    if (reg2.IsMatch(p))
                    {
                        N_m3u8DL_CLI.Parser.DurStart = reg2.Match(p).Groups[1].Value;
                        N_m3u8DL_CLI.Parser.DurEnd = reg2.Match(p).Groups[5].Value;
                        if (N_m3u8DL_CLI.Parser.DurEnd == "00:00:00") N_m3u8DL_CLI.Parser.DurEnd = "";
                        N_m3u8DL_CLI.Parser.DelAd = false;
                    }
                }
                else
                {
                    //数字
                    Regex reg = new Regex(@"(\d*)-(\d*)");
                    if (reg.IsMatch(p))
                    {
                        if (!string.IsNullOrEmpty(reg.Match(p).Groups[1].Value))
                        {
                            N_m3u8DL_CLI.Parser.RangeStart = Convert.ToInt32(reg.Match(p).Groups[1].Value);
                            N_m3u8DL_CLI.Parser.DelAd = false;
                        }
                        if (!string.IsNullOrEmpty(reg.Match(p).Groups[2].Value))
                        {
                            N_m3u8DL_CLI.Parser.RangeEnd = Convert.ToInt32(reg.Match(p).Groups[2].Value);
                            N_m3u8DL_CLI.Parser.DelAd = false;
                        }
                    }
                }
            }

            int inputRetryCount = 20;

         input:
            string testurl = o.Input;
            //重试太多次，退出
            if (inputRetryCount == 0)
                Environment.Exit(-1);

            if (fileName == "") {
                fileName = Global.GetUrlFileName(testurl) + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            }
            if (testurl.Contains("twitcasting") && testurl.Contains("/fmp4/"))
            {
                DownloadManager.BinaryMerge = true;
            }
            string m3u8Content = string.Empty;
            bool isVOD = true;

            LOGGER.PrintLine($"{strings.fileName}{fileName}");
            LOGGER.PrintLine($"{strings.savePath}{Path.GetDirectoryName(Path.Combine(workDir, fileName))}");

            N_m3u8DL_CLI.Parser parser = new N_m3u8DL_CLI.Parser();
            parser.DownName = fileName;
            parser.DownDir = Path.Combine(workDir, parser.DownName);
            parser.M3u8Url = testurl;
            parser.KeyBase64 = keyBase64;
            parser.KeyIV = keyIV;
            parser.KeyFile = keyFile;
            if (baseUrl != "")
                parser.BaseUrl = baseUrl;
            parser.Headers = reqHeaders;
            string exePath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            LOGGER.LOGFILE = Path.Combine(exePath, "Logs", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff") + ".log");
            LOGGER.InitLog();
            LOGGER.WriteLine(strings.startParsing + testurl);
            LOGGER.PrintLine(strings.startParsing + " " + testurl, LOGGER.Warning);

            if (testurl.EndsWith(".json") && File.Exists(testurl))  //可直接跳过解析
            {
                if (!Directory.Exists(Path.Combine(workDir, fileName)))//若文件夹不存在则新建文件夹   
                    Directory.CreateDirectory(Path.Combine(workDir, fileName)); //新建文件夹  
                File.Copy(testurl, Path.Combine(Path.Combine(workDir, fileName), "meta.json"), true);
            }
            else
            {
                parser.Parse();  //开始解析
            }

            if (File.Exists(Path.Combine(Path.Combine(workDir, fileName), "meta.json")))
            {
                JObject initJson = JObject.Parse(File.ReadAllText(Path.Combine(Path.Combine(workDir, fileName), "meta.json")));
                isVOD = Convert.ToBoolean(initJson["m3u8Info"]["vod"].ToString());
                //传给Watcher总时长
                Watcher.TotalDuration = initJson["m3u8Info"]["totalDuration"].Value<double>();
                LOGGER.PrintLine($"{strings.fileDuration}{Global.FormatTime((int)Watcher.TotalDuration)}");
                LOGGER.PrintLine(strings.segCount + initJson["m3u8Info"]["originalCount"].Value<int>()
                    + $", {strings.selectedCount}" + initJson["m3u8Info"]["count"].Value<int>());
            }
            else
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(workDir, fileName));
                directoryInfo.Delete(true);
                LOGGER.PrintLine(strings.InvalidUri, LOGGER.Error);
                inputRetryCount--;
                goto input;
            }

            //点播
            if (isVOD == true)
            {
                ServicePointManager.DefaultConnectionLimit = 10000;
                DownloadManager md = new DownloadManager();
                md.DownDir = parser.DownDir;
                md.Headers = reqHeaders;
                md.Threads = Environment.ProcessorCount;
                if (md.Threads > o.MaxThreads)
                    md.Threads = (int)o.MaxThreads;
                if (md.Threads < o.MinThreads)
                    md.Threads = (int)o.MinThreads;
                if (File.Exists("minT.txt"))
                {
                    int t = Convert.ToInt32(File.ReadAllText("minT.txt"));
                    if (md.Threads <= t)
                        md.Threads = t;
                }
                md.TimeOut = (int)(o.TimeOut * 1000);
                md.NoMerge = o.NoMerge;
                md.DownName = fileName;
                md.DelAfterDone = o.EnableDelAfterDone;
                md.MuxFormat = "mp4";
                md.RetryCount = (int)o.RetryCount;
                md.MuxSetJson = muxSetJson;
                md.MuxFastStart = o.EnableMuxFastStart;
                md.DoDownload();
            }
            //直播
            if (isVOD == false)
            {
                LOGGER.WriteLine(strings.liveStreamFoundAndRecoding);
                LOGGER.PrintLine(strings.liveStreamFoundAndRecoding);
                //LOGGER.STOPLOG = true;  //停止记录日志
                //开辟文件流，且不关闭。（便于播放器不断读取文件）
                string LivePath = Path.Combine(Directory.GetParent(parser.DownDir).FullName
                    , DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + fileName + ".ts");
                FileStream outputStream = new FileStream(LivePath, FileMode.Append);

                HLSLiveDownloader live = new HLSLiveDownloader();
                live.DownDir = parser.DownDir;
                live.Headers = reqHeaders;
                live.LiveStream = outputStream;
                live.LiveFile = LivePath;
                live.TimerStart();  //开始录制
                Console.ReadKey();
            }

            LOGGER.WriteLineError(strings.downloadFailed);
            LOGGER.PrintLine(strings.downloadFailed, LOGGER.Error);
            Thread.Sleep(3000);
        }

        private static void DisplayHelp(ParserResult<MyOptions> result, IEnumerable<Error> errs)
        {
            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.AdditionalNewLineAfterOption = false;
                h.Copyright = "\r\nUSAGE:\r\n\r\n  AELOUS <URL|JSON|FILE> [OPTIONS]\r\n\r\nOPTIONS:";
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);
            Console.WriteLine(helpText);
        }
    }

}
