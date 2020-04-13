# DataRecordReplay
A pure .net core UDP Record&amp;Replay tool.

本项目是一个完全基于 .Net Core 实现的 UDP 报文记录、回放、编辑的工具。

## Console Tools
### UDPRecorder
本模块用于记录 UDP 报文

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

### UDPReplayer
本模块用于回放记录的 UDP 报文



### UDPEditor
本模块用于编辑记录文件

## Core
提供核心功能的实现
### RecordCore
本模块提供数据记录的核心方法

使用流程如下：

实例化 RecordCore 对象
```
RecordCore(double[] segmentPara, string path, string name, string notes, List<IPEndPoint> points, double intervalTime = 1.0, DeleInfoHandler infoHandler = null)

segmentPara: 文件分段参数 [size time] 每段文件大小不超过 size MB，时长不超过 time s，0 表示该项无效 
path:文件存储路径
name:文件名
notes:文件备注
points:监听端口列表
intervalTime:打包压缩间隔时长，默认 1s
infoHandler:core 中运行信息委托

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

var core = ReplayCore(paths);
var fileInfo = core.FileInfo;
```

初始化 ReplayCore
```

```


### EditCore

## FileManager
提供记录文件读写以及跨文件 index 管理

## EDCoder
提供数据压缩、解压能力
