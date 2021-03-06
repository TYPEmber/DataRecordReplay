# DataRecordReplay
A pure .net core UDP Record&amp;Replay tool.

本项目是一个完全基于 .Net Core 实现的 UDP 报文记录、回放、编辑的工具。

## Console Tools
### UDPRecorder
本模块用于记录 UDP 报文

UDPRecorder.exe -l "5503 192.168.1.1:8912" -p save/ -f data -s "200 600" -n "El Psy Congroo."
UDPRecorder.exe -l "5503 8912" -s "0 600" -i 2.0 -n "El Psy Congroo."

指定需要监听的地址
```
-l [ip:port]
-l 5503                                       监听所有可用网卡的 5503 端口（记录时不区分网卡）
-l 0.0.0.0:5503                               监听所有可用网卡的 5503 端口（记录时不区分网卡）
-l "5503 127.0.0.1:8912 192.168.1.1:19208"    同步监听所有可用网卡的 5503 端口，localhost 的 8912 端口， 192.168.1.1 的 19208 端口
```

指定记录文件路径及文件名
```
-p [path]，默认值为 [/]
-p saveudp/save/          指定文件写入到程序运行路径下的 saveudp/save/ 文件夹中

-f [name]，默认值为 [yyyy-MM-dd_HH-mm-ss]
-f data                   指定文件名为 data

-p saveudp/save -f data   指定将文件名为 data.lcl 的记录文件写入到程序运行路径下的 saveudp/save/ 文件夹中
```

指定记录文件切分规则，默认值为 [2048 3600]
```
-s [size time]
-s "500 3600"     单个文件不超过 500MB 且记录时长不超过 3600s
-s "500 0"        单个文件不超过 500MB
-s "0 0"          不做任何切分
```

指定记录文件打包压缩间隔时长，默认值为 [1.0]
```
-i [interval]
-i 1       每隔 1.0s 打包压缩一次
-i 1.0     每隔 1.0s 打包压缩一次
-i 2.18    每隔 2.18s 打包压缩一次
```

指定记录文件备注，默认值为 ""
```
-n [note]
-n You know where you hope this train will take you...but you can't konw for sure.    指定该记录文件的备注为 "You know where you hope this train will take you...but you can't konw for sure."
```

运行中控制指令
```
c           Close           按下 y 即为结束录制 按下其余任意键则为取消
```

### UDPReplayer
本模块用于回放记录的 UDP 报文

指定回放配置参数文件
```
-t [paraFile]
-t para.txt             指定回放配置参数文件为程序运行目录下的 para.txt
-t save/para/para.txt   指定回放配置参数文件为程序运行目录下的 save/para/para.txt
```

若不指定回放配置参数文件，且不指定任何参数，则等同于
``` 
-t para.txt
```

指定单个文件
```
-f [fileName]
-f data.lcl             指定回放文件为程序运行目录下的 data.lcl
-f save/data.lcl        指定回放文件为程序运行目录下的 save/data.lcl
```

加载文件夹
```
-p [folderNmae]
-p /                    指定从程序运行目录下加载其中所有的记录文件
-p save/data/           指定从程序运行目录下的 save/data/ 加载其中所有的记录文件
```

加载映射关系
```
-m [record=>replay]
-m "5503=>127.0.0.1:5503"                                       将记录文件中 0.0.0.0:5503 的数据映射至 127.0.0.1：5503
-m "127.0.0.1:5503=>192.168.1.1:5503"                           将记录文件中 127.0.0.1:5503 的数据映射至 192.168.1.1:5503
-m "5503=>127.0.0.1:5503 192.168.1.1:8912=>127.0.0.1:8912"      将记录文件中 0.0.0.0:5503 的数据映射至 127.0.0.1：5503 将记录文件中 192.168.1.1:8912 的数据映射至 127.0.0.1:8912
```

运行中控制指令
```
p           Play/Pause
j           JumpTo          输入 50% 即为跳转至 50%处
r           Rate            输入 5.5 即为播放速度为 5.5 倍
c           Close           按下 y 即为结束回放 按下其余任意键则为取消
```

### UDPEditor
本模块用于编辑记录文件

指定单个文件
```
-f [fileName]
-f data.lcl             指定回放文件为程序运行目录下的 data.lcl
-f save/data.lcl        指定回放文件为程序运行目录下的 save/data.lcl
```

加载文件夹
```
-p [folderNmae]
-p /                    指定从程序运行目录下加载其中所有的记录文件
-p save/data/           指定从程序运行目录下的 save/data/ 加载其中所有的记录文件
```

指定记录文件切分规则，默认值为 [2048 3600]
```
-s [size time]
-s "500 3600"     单个文件不超过 500MB 且记录时长不超过 3600s
-s "500 0"        单个文件不超过 500MB
-s "0 0"          不做任何切分
```

指定文件截取选段
```
-r [startIndex endIndex]
-r "0 480"          截取源文件中 Index 范围为 0 至 480 的片段
-r "50 58"          截取源文件中 Index 范围为 50 至 58 的片段
```

指定记录文件路径及文件名
```
-p [path]，默认值为 [/Clip_yyyy-MM-dd_HH-mm-ss/]
-p saveudp/clip/          指定文件写入到程序运行路径下的 saveudp/save/ 文件夹中

-f [name]，默认值为 [data]
-f clipFile                   指定文件名为 clipFile

-f data                        指定将文件名为 data.lcl 的记录文件写入到程序运行路径下的 Clip_yyyy-MM-dd_HH-mm-ss/ 文件夹中
-p saveudp/clip/ -f clipFile   指定将文件名为 clipFile.lcl 的记录文件写入到程序运行路径下的 saveudp/clip/ 文件夹中
```

---

## Core
提供核心功能的实现
### RecordCore
本模块提供数据记录的核心方法

使用流程如下：

实例化 RecordCore 对象
```
RecordCore(double[] segmentPara, string path, string name, string notes, List<IPEndPoint> points, double intervalTime = 1.0, DeleInfoHandler infoHandler = null)

segmentPara: 文件分段参数 [size time] 每段文件大小不超过 size MB，时长不超过 time s，0 表示该项无效 
path: 文件存储路径
name: 文件名
notes: 文件备注
points: 监听端口列表
intervalTime: 打包压缩间隔时长，默认 1s
infoHandler: 指定 core 中运行信息处理委托 delegate void DeleInfoHandler(ReplayInfo info)
struct ReplayInfo
{
    /// <summary>
    /// 当前 UTC 时间戳
    /// </summary>
    public DateTime time;
    /// <summary>
    /// 当前 pkg 中 msg 数量
    /// </summary>
    public int count;
    /// <summary>
    /// 当前 pkg 压缩后大小
    /// </summary>
    public int codedLength;
    /// <summary>
    /// 当前 pkg 未压缩大小
    /// </summary>
    public int originLength;
    /// <summary>
    /// 当前 pkg 生成 UTC 时间戳
    /// </summary>
    public double pkgTime;
}

var core = new Core.RecordCore(segPara, path, name, notes, points, intervalTime: _intervalTime infoHandler: _infoHandler);
```

传入 UDP 报文
```
void Add(double time, byte[] ip, ushort port, byte[] bytes)

time: 本条报文接受时戳（当前 UTC 时间距 1970-01-01 的总秒数）
ip: 本条报文来自该监听 ip
port: 本条报文来自该监听 端口
bytes: UDP 报文

core.Add(time.TotalSeconds(), point.Address.GetAddressBytes(), (ushort)point.Port, rcvBytes);
```

结束记录
```
void WriteComplete()

core.WriteComplete();
```

### ReplayCore
本模块提供数据回放的核心方法

使用流程如下：

实例化 ReplayCore 对象
```
ReplayCore(IEnumerable<string> paths)

paths: 待回放文件队列，传入后会自动拼接，并建立统一索引

var core = ReplayCore(paths);
```

获取文件信息
```
File.Info FileInfo

class File.Info
 {
    public int version_file;
    public int version_code;
    /// <summary>
    /// 起始时间
    /// 当前 UTC 时间从 1970-01-01 的总秒数
    /// 单位：s
    /// </summary>
    public double time;
    /// <summary>
    /// 每个 pkg 时间跨度
    /// 单位：s
    /// </summary>
    public double timeInterval;
    /// <summary>
    /// 该 File 记录的是从这些 IPEndPoint 中收到到的数据
    /// </summary>
    public IPEndPoint[] points { set; get; }
    /// <summary>
    /// 备注
    /// </summary>
    public string notes { set; get; }
    /// <summary>
    /// 总 index 数量
    /// </summary>
    public long totalIndex { set; get; }
}

var fileInfo = core.FileInfo;
```

初始化 ReplayCore
```
ReplayCore Initial(Dictionary<IPEndPoint, IPEndPoint> map, DeleSendHandler sendHandler, DeleInfoHandler infoHandler)

map: 指定将从 key 地址接收到的数据发送至 value 地址
sendHandler: 指定数据发送委托，需尽快返回 delegate void DeleSendHandler(ReadOnlySpan<byte> bytes, IPEndPoint point)
infoHandler: 指定 core 中运行信息处理委托 delegate void DeleInfoHandler(ReplayInfo info)
struct ReplayInfo
{
    /// <summary>
    /// 当前 UTC 时间戳
    /// </summary>
    public DateTime time;
    /// <summary>
    /// 已播放完成的 pkg 的 index 编号
    /// </summary>
    public long index;
    /// <summary>
    /// 已播放完成的 pkg 播放耗时
    /// </summary>
    public double pkgCostTime;
}

core.Initial(_map, _sendHandler, _infoHandler);
```

播放控制
```
void P()                                       播放/暂停
bool JumpTo(long index)                        跳转至 index 处
double SpeedRate                               播放倍率，默认为 1.0
bool IsPlaying                                 播放状态
```

### EditCore
本模块提供记录文件编辑的核心方法

使用流程如下:

实例化 EditCore 对象
```
EditCore(IEnumerable<string> paths)

paths: 待回放文件队列，传入后会自动拼接，并建立统一索引

var core = EditCore(paths);
```

获取文件信息
```
File.Info FileInfo

class File.Info
 {
    public int version_file;
    public int version_code;
    /// <summary>
    /// 起始时间
    /// 当前 UTC 时间从 1970-01-01 的总秒数
    /// 单位：s
    /// </summary>
    public double time;
    /// <summary>
    /// 每个 pkg 时间跨度
    /// 单位：s
    /// </summary>
    public double timeInterval;
    /// <summary>
    /// 该 File 记录的是从这些 IPEndPoint 中收到到的数据
    /// </summary>
    public IPEndPoint[] points { set; get; }
    /// <summary>
    /// 备注
    /// </summary>
    public string notes { set; get; }
    /// <summary>
    /// 总 index 数量
    /// </summary>
    public long totalIndex { set; get; }
}

var fileInfo = core.FileInfo;
```

裁切
```
void Clip(long startIndex, long endIndex, double[] segmentPara, string path, string name, string notes = null)

startIndex: 起始 index，不小于 0
endIndex: 结束 index，不大于总长
segmentPara: 文件分段参数 [size time] 每段文件大小不超过 size MB，时长不超过 time s，0 表示该项无效 
path: 文件存储路径
name: 文件名
notes: 文件备注

core.Clip(0, 481, new double[] { 100, 600 }, _path, _name);
```

## FileManager
提供记录文件读写以及跨文件 index 管理

## EDCoder
提供数据压缩、解压能力
