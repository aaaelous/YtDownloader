using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace YtDownloader
{
    public partial class Form1 : Form
    {
        IntPtr nextClipboardViewer;

        //已连接上的客户端集合
        List<Socket> clinetSockets;
        //设置数据缓冲区
        private byte[] result = new byte[1024];
        //服务器
        Socket socket;
        public Form1()
        {
            Clipboard.Clear();
            InitializeComponent();
            nextClipboardViewer = (IntPtr)SetClipboardViewer((int)Handle);
            //初始化
            clinetSockets = new List<Socket>();
            //创建socket对象
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //获取ip地址和端口
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            int port = 8123;
            IPEndPoint point = new IPEndPoint(ip, port);

            //绑定ip和端口
            socket.Bind(point);
            //设置最大连接数
            socket.Listen(50);
            //开启新线程监听
            Thread serverThread = new Thread(ListenClientConnect);
            serverThread.IsBackground = true;
            serverThread.Start(socket);
        }

        /// <summary>
        /// 监听传入
        /// </summary>
        /// <param name="ar"></param>
        private void ListenClientConnect(object ar)
        {
            //设置标志
            bool flag = true;
            //获得服务器的Socket
            Socket serverSocket = ar as Socket;
            //轮询
            while (flag)
            {
                //获得连入的客户端socket
                Socket clientSocket = serverSocket.Accept();
                //将新加入的客户端加入列表中
                clinetSockets.Add(clientSocket);

                //向listBox中写入消息
                //listBox1.Invoke(new Action(() => { listBox1.Items.Add(string.Format("客户端{0}已成功连接到服务器\r\n", clientSocket.RemoteEndPoint)); }));
                //开启新的线程，进行监听客户端消息
                var mReveiveThread = new Thread(ReceiveClient);
                mReveiveThread.IsBackground = true;
                mReveiveThread.Start(clientSocket);
            }
        }

        /// <summary>
        /// 接收客户端传来的数据
        /// </summary>
        /// <param name="obj"></param>
        private void ReceiveClient(object obj)
        {
            //获取当前客户端
            //因为客户端不止一个，所有需要使用var实例化新对象
            var mClientSocket = (Socket)obj;
            //循环标志位
            bool flag = true;
            while (flag)
            {
                try
                {
                    //获取数据长度
                    int receiveLength = mClientSocket.Receive(result);
                    //获取客户端消息
                    string clientMessage = Encoding.UTF8.GetString(result, 0, receiveLength);
                    if (clientMessage == "1")
                    {
                        //自身进行更新

                    }
                    //向listbox中写入信息
                    listBox1.Invoke(new Action(() => { listBox1.Items.Add(clientMessage); }));
                }
                catch (Exception e)
                {
                }

            }
        }

        /// <summary>
        /// 要处理的 WindowsSystem.Windows.Forms.Message。
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            // defined in winuser.h
            const int WM_DRAWCLIPBOARD = 0x308;
            const int WM_CHANGECBCHAIN = 0x030D;

            switch (m.Msg)
            {
                case WM_DRAWCLIPBOARD:
                    DisplayClipboardData();
                    SendMessage(nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    break;
                case WM_CHANGECBCHAIN:
                    if (m.WParam == nextClipboardViewer)
                        nextClipboardViewer = m.LParam;
                    else
                        SendMessage(nextClipboardViewer, m.Msg, m.WParam, m.LParam);
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }
        /// <summary>
        /// 显示剪贴板内容
        /// </summary>
        public void DisplayClipboardData()
        {
            try
            {
                IDataObject iData = new DataObject();
                iData = Clipboard.GetDataObject();
                if (iData.GetDataPresent(DataFormats.Text))
                {
                    string str = (string)iData.GetData(DataFormats.Text);
                    if (!string.IsNullOrEmpty(str)) {
                        if (str.Contains("aelous_downLoadM3u8")) {
                            JavaScriptSerializer js = new JavaScriptSerializer();
                            FileInfo list = js.Deserialize<FileInfo>(str);
                            Announce("开始下载->" + list.Name + ".mp4");
                            if (!File.Exists("M3u8Downloader.exe"))
                            {
                                MessageBox.Show("错误->未检测到M3u8Downloader");
                            }
                            else {
                                if (string.IsNullOrEmpty(textBox3.Text))
                                {
                                    string[] args = { list.Url, list.Name };
                                    StartProcess("M3u8Downloader.exe", args);
                                }
                                else {
                                    string[] args = { list.Url, list.Name, textBox3.Text };
                                    StartProcess("M3u8Downloader.exe", args);
                                }
                            }

                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        public bool StartProcess(string filename, string[] args)
        {
            try
            {
                string s = "";
                foreach (string arg in args)
                {
                   s = s + arg + " ";
                }
                s = s.Trim();
                Process myprocess = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo(filename, s);
                myprocess.StartInfo = startInfo;

                //通过以下参数可以控制exe的启动方式，具体参照 myprocess.StartInfo.下面的参数，如以无界面方式启动exe等
                myprocess.StartInfo.CreateNoWindow = true;
                myprocess.StartInfo.UseShellExecute = false;
                myprocess.StartInfo.RedirectStandardOutput = true;
                myprocess.Start();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动应用程序时出错！原因：" + ex.Message);
            }
            return false;
        }

        private void Announce(string str) {
            //if (string.IsNullOrEmpty(richTextBox1.Text))
            //{
            //    richTextBox1.Text = str;
            //}
            //else {
            //    richTextBox1.AppendText( "\n" + str);
            //    richTextBox1.ScrollToCaret();
            //}
            listBox1.Items.Add(str);
        }

        /// <summary>
        /// 关闭程序，从观察链移除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form2_FormClosed(object sender, FormClosedEventArgs e)
        {
            ChangeClipboardChain(Handle, nextClipboardViewer);
        }

        #region WindowsAPI
        /// <summary>
        /// 将CWnd加入一个窗口链，每当剪贴板的内容发生变化时，就会通知这些窗口
        /// </summary>
        /// <param name="hWndNewViewer">句柄</param>
        /// <returns>返回剪贴板观察器链中下一个窗口的句柄</returns>
        [DllImport("User32.dll")]
        protected static extern int SetClipboardViewer(int hWndNewViewer);

        /// <summary>
        /// 从剪贴板链中移出的窗口句柄
        /// </summary>
        /// <param name="hWndRemove">从剪贴板链中移出的窗口句柄</param>
        /// <param name="hWndNewNext">hWndRemove的下一个在剪贴板链中的窗口句柄</param>
        /// <returns>如果成功，非零;否则为0。</returns>
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        /// <summary>
        /// 将指定的消息发送到一个或多个窗口
        /// </summary>
        /// <param name="hwnd">其窗口程序将接收消息的窗口的句柄</param>
        /// <param name="wMsg">指定被发送的消息</param>
        /// <param name="wParam">指定附加的消息特定信息</param>
        /// <param name="lParam">指定附加的消息特定信息</param>
        /// <returns>消息处理的结果</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);
        #endregion

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox3.Text = SelectPath();
        }

        private string SelectPath()
        {
            string path = string.Empty;
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                path = fbd.SelectedPath;
            }
            return path;
        }
    }
}
