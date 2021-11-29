# YtDownloader
yt看视频下载


# **一、油猴插件**

![image](https://user-images.githubusercontent.com/49520996/143842058-c4f0148d-2250-4baa-b133-c7ba80ea7f9e.png)

```
(function() {
    'use strict';
    // Your code here...
       (function () {
           $("body").append($('<link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap.min.css">'));
           $('header').after('<div class="panel panel-default">'+
           '<div class="panel-heading" style="text-align: center;">加载成功'+
           '</div>'+
           '<div class="panel-body" id="item_box">'+
           '</div>'+
           '</div>')
            var innerHtml = $('body').html();
            var spt = innerHtml.match('<script[^><]*>([^><]*)</script>')[1]
            spt = spt.match('quality: \\[([^><]*)\\],')[1]
            spt = '[' + spt + ']'
            spt = spt.replace(/[\r\n]/g,"").replace(/[ ]/g,"");
			spt = spt.replace(/(?:\s*['"]*)?([a-zA-Z0-9]+)(?:['"]*\s*)?:/g, "'$1':");
			var json = eval('('+ spt + ')');
            console.log(json);
            for (const item in json) {
                console.log(json[item]);
                $('#item_box').append('<div class="col-lg-6">'+
                                    '<div class="input-group">'+
                                    '<input type="text" class="form-control" value="'+json[item].url+'">'+
                                    '<span class="input-group-btn">'+
                                    '<button class="btn btn-default" id="video_download" type="button">下载' + json[item].name +'</button>'+
                                    '</span>'+
                                    '</div>'+
                                    '</div>')
            }
       })();

       $('body').on('click', '#video_download', function () {
           var url = 'https://www.gvpass.com' + $(this).parent().prev().val()
           console.log(url);
        //    checkM3u8Url(url)
           var data = {
               topic: 'aelous_downLoadM3u8',
               name: $('.content-wrap .content .sptitle h1').text(),
               url : url
           }
           GM.setClipboard(JSON.stringify(data))

       });
})();
```

因为被墙，所以插件源码自己下载一下 加载到浏览器中！！

# 二、M3u8downloader
编译好将所有文件放在YtDownloader的realease目录

![image](https://user-images.githubusercontent.com/49520996/143842231-53cb59f0-03a8-4c9c-b39f-6af2e668d62b.png)


# 三、YtDownloader
C#写的UI界面，与M3u8downloader的交互通过Socket，所以不要让端口被占用啦

```
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
```
