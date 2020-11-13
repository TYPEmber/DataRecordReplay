using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DRRCommon.Network
{
    [Serializable]
    public class IPandPort
    {
        public IPandPort()
        {
            IP = "0.0.0.0";
            Port = 0;
        }

        public IPandPort(string ip, int port)
        {
            IP = ip;
            Port = port;
        }

        public IPandPort(IPEndPoint point)
        {
            IP = point.Address.ToString();
            Port = point.Port;
        }

        private string _ip;
        public string IP
        {
            set
            {
                var buffIP = value.Split('.');
                if (buffIP.Length != 4)
                {
                    throw new FormatException("Invalid IPv4 Format String!");
                }
                try
                {
                    for (int i = 0; i < buffIP.Length; i++)
                    {
                        ipBytes[i] = byte.Parse(buffIP[i]);
                    }
                    ipInt32 = BitConverter.ToInt32(ipBytes, 0);
                }
                catch (Exception e)
                {
                    throw new FormatException("Invalid IPv4 Format String!", e);
                }

                _ip = value;
            }
            get { return _ip; }
        }

        private byte[] ipBytes { set; get; } = new byte[4];
        public byte[] GetIPBytes()
        {
            if (ipBytes == null)
            {
                return null;
            }

            return new byte[] { ipBytes[0], ipBytes[1], ipBytes[2], ipBytes[3] };
        }

        private int ipInt32 { set; get; } = 0;

        public int Port { set; get; }

        public override string ToString()
        {
            return _ip + ":" + Port.ToString();
        }

        public override int GetHashCode()
        {
            return ipInt32 ^ Port;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            if (this.GetHashCode() != ((IPandPort)obj).GetHashCode())
            {
                return false;
            }

            if (Port != ((IPandPort)obj).Port || ipInt32 != ((IPandPort)obj).ipInt32)
            {
                return false;
            }

            return true;
        }

        public static bool operator ==(IPandPort a, IPandPort b)
        {
            if (a as object == null)
            {
                if (b as object == null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return a.Equals(b);
        }

        public static bool operator !=(IPandPort a, IPandPort b)
        {

            return !(a == b);
        }
    }

    public class UDPReciverWithTime : IDisposable
    {
        /// <summary>
        /// 注册收到 Byte[] 事件的方法列表
        /// </summary>
        public Delegate[] DataRcv_Event_InvocationList
        {
            get
            {
                if (DataRcv_Event != null)
                {
                    return DataRcv_Event.GetInvocationList();
                }
                else
                {
                    return new Delegate[] { };
                }
            }
        }

        private UdpClient _UDPClient;

        /// <summary>
        /// 获取内部所使用的 Socket
        /// 可以使用该方法设置发送、接收缓冲区大小
        /// </summary>
        /// <returns></returns>
        public Socket GetSocket()
        {
            return _UDPClient.Client;
        }

        private class RcvData
        {
            public byte[] Bytes { set; get; }
            public IPEndPoint Point { set; get; }
            public DateTime Time { set; get; }
        }
        private ConcurrentQueue<RcvData> _QueueData;

        IPEndPoint point;
        IPEndPoint pointListen;
        public IPandPort GetListenIPAndPort()
        {
            return new IPandPort()
            {
                IP = pointListen.Address.ToString(),
                Port = pointListen.Port
            };
        }
        /// <summary>
        /// 监听 0.0.0.0 的指定 Port
        /// </summary>
        /// <param name="Port"></param>
        public UDPReciverWithTime(int Port)
        {
            InitialUDPReciver(new IPandPort() { IP = "0.0.0.0", Port = Port });
        }
        /// <summary>
        /// 监听指定 IP 的指定 Port
        /// </summary>
        /// <param name="IPPort"></param>
        public UDPReciverWithTime(IPandPort IPPort)
        {
            InitialUDPReciver(IPPort);
        }

        private void InitialUDPReciver(IPandPort IPPort)
        {
            _QueueData = new ConcurrentQueue<RcvData>();
            pointListen = new IPEndPoint(IPAddress.Parse(IPPort.IP), IPPort.Port);

            point = new IPEndPoint(IPAddress.Any, 0);
            InitialUDPReciver();
        }

        private void InitialUDPReciver()
        {
            try
            {
                if (_UDPClient == null)
                {
                    _UDPClient = new UdpClient(pointListen);
                }
                else if (_UDPClient.Client == null)
                {
                    _UDPClient = new UdpClient(pointListen);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        private Task TaskListen;
        private Task TaskTrigger;
        private Task TaskCheck;
        /// <summary>
        /// 指示当前监听状态
        /// </summary>
        public bool IsListening { private set; get; } = false;
        /// <summary>
        /// 订阅收到 Byte[] 事件
        /// </summary>
        public event Events.Reciver_RcvData_Handler DataRcv_Event;
        /// <summary>
        /// 订阅 Queue 堆积事件
        /// 不订阅则默认完全清空队列
        /// </summary>
        public event Events.Reciver_QueueHeap_Handler QueueHeap_Event;
        /// <summary>
        /// 缓冲队列触发溢出事件数值
        /// </summary>
        public int QueueHeapCountMax { set; get; } = 1024;

        //Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);


        /// <summary>
        /// 开始监听
        /// </summary>
        public UDPReciverWithTime Start()
        {
            InitialUDPReciver();
            IsListening = true;


            TaskTrigger = Task.Factory.StartNew(() =>
            {
                RcvData buffData = new RcvData();

                while (IsListening)
                {
                    // 避免 CPU 占用过高
                    if (_QueueData.Count == 0)
                    {
                        System.Threading.Thread.Sleep(1);
                        //socket.Poll(1, SelectMode.SelectRead);
                        continue;
                    }

                    if (_QueueData.TryDequeue(out buffData))
                    {
                        DataRcv_Event?.Invoke(buffData.Bytes, buffData.Point, buffData.Time);
                    }
                }
            });

            TaskCheck = Task.Factory.StartNew(() =>
            {
                while (IsListening)
                {
                    if (_QueueData.Count > QueueHeapCountMax)
                    {
                        if (QueueHeap_Event != null)
                        {
                            QueueClearMode clearMode = QueueHeap_Event.Invoke(_QueueData.Count);
                            switch (clearMode)
                            {
                                case QueueClearMode.All:
                                    _QueueData = new ConcurrentQueue<RcvData>();
                                    break;
                                case QueueClearMode.Half:
                                    while (_QueueData.Count > QueueHeapCountMax / 2)
                                    {
                                        RcvData buffData = new RcvData();
                                        _QueueData.TryDequeue(out buffData);
                                    }
                                    break;
                                case QueueClearMode.Cancel:
                                    break;
                                default:
                                    _QueueData = new ConcurrentQueue<RcvData>();
                                    break;
                            }
                        }
                        else
                        {
                            _QueueData = new ConcurrentQueue<RcvData>();
                        }
                    }
                    System.Threading.Thread.Sleep(100);
                }
            });

            TaskListen = Task.Factory.StartNew(() =>
            {
                try
                {
                    while (IsListening)
                    {
                        _QueueData.Enqueue(new RcvData()
                        {
                            // 内部有阻塞无需 System.Threading.Thread.Sleep(1);
                            Bytes = _UDPClient.Receive(ref point),
                            Point = pointListen,
                            Time = DateTime.UtcNow
                        });
                    }
                }
                catch (SocketException e)
                {
                    // 由 Stop 函数触发
                    if (e.ErrorCode == 10004)
                    {
                        // Do Nothing
                    }
                    else
                    {
                        throw e;
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }

            });

            return this;
        }

        /// <summary>
        /// 停止监听
        /// </summary>
        public void Stop()
        {
            IsListening = false;

            try
            {
                _UDPClient.Close();

                TaskListen?.Wait();
                TaskListen?.Dispose();

                TaskTrigger?.Wait();
                TaskTrigger?.Dispose();

                TaskCheck?.Wait();
                TaskCheck?.Dispose();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// 缓冲队列触发溢出处理模式
        /// </summary>
        public enum QueueClearMode
        {
            /// <summary>
            /// 清除所有
            /// </summary>
            All,
            /// <summary>
            /// 清除一半
            /// </summary>
            Half,
            /// <summary>
            /// 不清除
            /// </summary>
            Cancel
        }

        public class Events
        {
            public delegate void Reciver_RcvData_Handler(byte[] rcvBytes, IPEndPoint point, DateTime time);

            public delegate QueueClearMode Reciver_QueueHeap_Handler(int heapCount);
        }

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)。
                    IsListening = false;
                    _UDPClient.Close();
                    _UDPClient = null;
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~UDPReciver() {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }

    public class UDPSender : IDisposable
    {
        private ConcurrentDictionary<IPandPort, IPEndPoint> _RemoteAddr = new ConcurrentDictionary<IPandPort, IPEndPoint>();

        private UdpClient _UDPClient = new UdpClient();

        private int _groupLength = 0;

        private int _groupIntervalMillSec = 0;

        /// <summary>
        /// 获取内部所使用的 Socket
        /// 可以使用该方法设置发送、接收缓冲区大小
        /// </summary>
        /// <returns></returns>
        public Socket GetSocket()
        {
            return _UDPClient.Client;
        }

        #region 构造函数：绑定 IP 与 Port
        /// <summary>
        /// 初始化 Sender，未指定 IP 与 Port
        /// </summary>
        public UDPSender()
        {
            InitialSendFuc();
        }
        /// <summary>
        /// 绑定单个接收端 IP 与 Port
        /// </summary>
        /// <param name="IPandPort"></param>
        public UDPSender(IPandPort IPandPort)
        {
            SetTargetIPandPort(IPandPort);
            InitialSendFuc();
        }

        /// <summary>
        /// 绑定多接收端 IP 与 Port
        /// </summary>
        /// <param name="IPandPortList"></param>
        public UDPSender(List<IPandPort> IPandPortList)
        {
            SetTargetIPandPort(IPandPortList);
            InitialSendFuc();
        }

        /// <summary>
        /// 批量绑定多接收端 IP 与相同 Port
        /// </summary>
        /// <param name="IPList"></param>
        /// <param name="Port"></param>
        public UDPSender(List<string> IPList, int Port)
        {
            SetTargetIPandPort(IPList, Port);
            InitialSendFuc();
        }
        #endregion
        /// <summary>
        /// 获取用于发送的 IP 与 Port
        /// </summary>
        /// <returns></returns>
        public IPandPort GetLocIPAndPort()
        {
            if (_UDPClient.Client.LocalEndPoint == null)
            {
                _UDPClient.Send(new byte[] { }, 0, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0));
            }
            var buff = _UDPClient.Client.LocalEndPoint.ToString();
            var buffList = buff.Split(':');
            return new IPandPort() { IP = buffList[0], Port = int.Parse(buffList[1]) };
        }
        /// <summary>
        /// 设置一次性发送 UDP 包个数，以及每一组 UDP 包发送的间隔毫秒数
        /// </summary>
        /// <param name="groupLength"></param>
        /// <param name="groupIntervalMillSec"></param>
        public void SetIntervalMillSec(uint groupLength, uint groupIntervalMillSec)
        {
            _groupLength = (int)groupLength;
            _groupIntervalMillSec = (int)groupIntervalMillSec;
            InitialSendFuc();
        }
        /// <summary>
        /// 设置发送的目的地 IP 与 Port
        /// </summary>
        /// <param name="IPandPort"></param>
        public void SetTargetIPandPort(IPandPort IPandPort)
        {
            _RemoteAddr.Clear();

            _RemoteAddr.TryAdd(IPandPort, new IPEndPoint(IPAddress.Parse(IPandPort.IP), IPandPort.Port));
        }
        /// <summary>
        /// 批量设置发送的目的地 IP 与 Port
        /// </summary>
        /// <param name="IPandPortList"></param>
        public void SetTargetIPandPort(List<IPandPort> IPandPortList)
        {
            _RemoteAddr.Clear();

            foreach (var IPandPort in IPandPortList)
            {
                _RemoteAddr.TryAdd(IPandPort, new IPEndPoint(IPAddress.Parse(IPandPort.IP), IPandPort.Port));
            }
        }
        /// <summary>
        /// 批量设置发送的目的地 IP 与 Port
        /// </summary>
        /// <param name="IPList"></param>
        /// <param name="Port"></param>
        public void SetTargetIPandPort(List<string> IPList, int Port)
        {
            _RemoteAddr.Clear();

            foreach (var ip in IPList)
            {
                _RemoteAddr.TryAdd(new IPandPort() { IP = ip, Port = Port }, new IPEndPoint(IPAddress.Parse(ip), Port));
            }
        }

        public bool AddTargetIPandPort(IPandPort ipAndPort)
        {
            return _RemoteAddr.TryAdd(ipAndPort, new IPEndPoint(IPAddress.Parse(ipAndPort.IP), ipAndPort.Port));
        }

        public bool RemoveTargetIPandPort(IPandPort ipAndPort)
        {
            IPEndPoint buff;
            return _RemoteAddr.TryRemove(ipAndPort, out buff);
        }

        /// <summary>
        /// 发送 Bytes
        /// </summary>
        /// <param name="msg"></param>
        public void Send(byte[] msg)
        {
            if (msg != null)
            {
                SendFuc(msg);
            }
        }

        //public void Send(byte[] msg, IPandPort ipAndPort)
        //{
        //    Send(msg, new IPEndPoint(IPAddress.Parse(ipAndPort.IP), ipAndPort.Port));
        //}

        public void Send(byte[] msg, IPEndPoint point)
        {
            while (true)
            {
                try
                {
                    _UDPClient.Send(msg, msg.Length, point);
                    break;
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.WouldBlock)
                    {
                        SleepHelper.Delay();
                    }
                    else
                    {
                        throw e;
                    }
                }
            }
        }

        /// <summary>
        /// 获取当前发送地址列表
        /// </summary>
        /// <returns></returns>
        public List<IPandPort> GetSendList()
        {
            var buffReturn = new List<IPandPort>();
            foreach (var point in _RemoteAddr.Keys)
            {
                buffReturn.Add(new IPandPort()
                {
                    IP = point.IP,
                    Port = point.Port
                });
            }
            return buffReturn;
        }

        private delegate void SendFuction(byte[] msg);
        private SendFuction SendFuc;
        private void InitialSendFuc()
        {
            _UDPClient.Client.SendBufferSize = 300000;

            if (_groupLength == 0)
            {
                SendFuc = SendFuc_Direct;
                group_Timer?.Dispose();
                _groupQueue?.Clear();
                group_Locker = null;
            }
            else
            {
                SendFuc = SendFuc_Group;

                _groupQueue = new Queue<byte[]>();

                group_Locker = new object();

                group_Timer = new Timer();
                group_Timer.Interval = _groupIntervalMillSec;
                group_Timer.Enabled = true;
                group_Timer.Elapsed += Group_Timer_Elapsed;
            }
        }

        private void SendFuc_Direct(byte[] msg)
        {
            foreach (var point in _RemoteAddr.Values)
            {
                while (true)
                {
                    try
                    {
                        _UDPClient.Send(msg, msg.Length, point);
                        break;
                    }
                    catch (SocketException e)
                    {
                        if (e.SocketErrorCode == SocketError.WouldBlock)
                        {
                            SleepHelper.Delay();
                        }
                        else
                        {
                            throw e;
                        }
                    }
                }
            }
        }

        private Timer group_Timer;
        private static object group_Locker;
        private Queue<byte[]> _groupQueue;
        private void SendFuc_Group(byte[] msg)
        {
            lock (group_Locker)
            {
                _groupQueue.Enqueue(msg);
            }
        }

        private void Group_Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (group_Locker)
            {
                int length = _groupQueue.Count > _groupLength ? _groupLength : _groupQueue.Count;
                for (int i = 0; i < length; i++)
                {
                    var msg = _groupQueue.Dequeue();
                    foreach (var point in _RemoteAddr.Values)
                    {
                        _UDPClient.Send(msg, msg.Length, point);
                    }
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)。
                    _UDPClient.Close();
                    _UDPClient = null;
                    _RemoteAddr.Clear();
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~UDPSender() {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
