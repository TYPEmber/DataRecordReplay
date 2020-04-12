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
-i 2.57    每隔 2.57s 打包压缩一次
```

指定记录文件备注，默认值为 ""
```
-n [interval]
-n This is a notes!    指定该记录文件的备注为 "This is a notes!"
```

### UDPReplayer
本模块用于回放记录的 UDP 报文



### UDPEditor
本模块用于编辑记录文件

## Core
提供核心功能的实现
### RecordCore
### ReplayCore
### EditCore
