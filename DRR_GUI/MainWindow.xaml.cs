using DRRCommon.Network;
using DRRCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DRR_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        #region Recorder

        private void Recorder_Grid_Initialized(object sender, EventArgs e)
        {
            Recroder_Button_Add_Listener_Click(sender, new RoutedEventArgs());
        }

        private void Recroder_Button_Add_Listener_Click(object sender, RoutedEventArgs e)
        {
            Recorder_Listen_List.Items.Add(new Recorder_Listen_Item(Recorder_Listen_List.Items.Count,
                (object obj, EventArgs e) =>
                {
                    Recorder_Listen_List.Items.Remove(obj);

                    // keep the num equals order
                    int num = 0;
                    foreach (Recorder_Listen_Item item in Recorder_Listen_List.Items)
                    {
                        item.Num.Text = num.ToString();
                        num++;
                    }
                }
            ));
        }

        private void Recorder_Path_Button_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = Environment.CurrentDirectory };
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                MessageBox.Show("Wrong Path!");
            }
            else
            {
                _path = dlg.SelectedPath;

                Recorder_Button_Path.Content = "Path: " + _path;
            }
        }

        private string _path = Environment.CurrentDirectory;

        private Core.RecordCore _recorder = null;
        List<UDPReciverWithTime> _recivers = null;

        private void Recroder_Button_Start_Click(object sender, RoutedEventArgs e)
        {
            bool start_flag = _recorder == null ? true : false;
            // start
            if (start_flag)
            {
                List<IPEndPoint> points = new List<IPEndPoint>();
                _recivers = new List<UDPReciverWithTime>();
                Recorder_Listen_Item buff = null;
                // Check input
                try
                {
                    foreach (Recorder_Listen_Item item in Recorder_Listen_List.Items)
                    {
                        buff = item;

                        var reciver = new UDPReciverWithTime(item.Point.Get_IPPORT()).Start();

                        reciver.GetSocket().ReceiveBufferSize = 1024 * 1024;
                        reciver.QueueHeapCountMax = 1024;
                        reciver.QueueHeap_Event += (int heapCount) =>
                        {
                            return UDPReciverWithTime.QueueClearMode.Cancel;
                        };

                        reciver.DataRcv_Event += (byte[] rcvBytes, IPEndPoint point, DateTime time) =>
                        {
                            _recorder.Add(time.TotalSeconds(), point.Address.GetAddressBytes(), (ushort)point.Port, rcvBytes);
                        };

                        _recivers.Add(reciver);
                        points.Add(item.Point.Get_IPEND());
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Wrong Input!\nCheck the Listen List At " + buff?.Num.Text);
                    Recorder_Stop();
                    return;
                }

                double[] segPara = new double[] { 0, 0 };

                try
                {
                    double split_mb = double.Parse(Recorder_Seg_Size.Text);
                    if (split_mb < 0)
                    {
                        throw new Exception("Wrong Split Size Para!");
                    }

                    double split_time = double.Parse(Recorder_Seg_Time.Text);
                    if (split_time < 0)
                    {
                        throw new Exception("Wrong Split Time Para!");
                    }

                    segPara[0] = split_mb;
                    segPara[1] = split_time;
                }
                catch (Exception ee)
                {
                    MessageBox.Show(ee.Message);
                    Recorder_Stop();
                    return;
                }

                // Initial recorder core
                try
                {
                    _recorder = new Core.RecordCore(segPara, _path, Recorder_FileName.Text, Recorder_Notes.Text, points,
                        infoHandler: (Core.RecordCore.ReplayInfo info) =>
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                Recorder_Info.Text = "Pkg_Count: " + info.count
                                    + " Compress_Rate: " + (info.codedLength * 100.0 / (info.originLength == 0 ? -info.codedLength : info.originLength)).ToString("f2") + "%"
                                    + "\nPkg_Time: " + info.pkgTime;
                            });
                        });
                }
                catch (Exception ee)
                {
                    MessageBox.Show(ee.Message);
                    Recorder_Stop();
                    return;
                }

                Recorder_Button_Start.Content = "Stop";
            }
            // stop
            else
            {
                Recorder_Stop();

                Recorder_Button_Start.Content = "Start";
            }

            foreach (Recorder_Listen_Item item in Recorder_Listen_List.Items)
            {
                item.IsEnabled = !start_flag;
            }
            Recorder_Button_Add_Listener.IsEnabled = !start_flag;
            Recorder_Button_Path.IsEnabled = !start_flag;
            Recorder_FileName.IsEnabled = !start_flag;
            Recorder_Notes.IsEnabled = !start_flag;
            Recorder_Seg_Size.IsEnabled = !start_flag;
            Recorder_Seg_Time.IsEnabled = !start_flag;
        }

        private void Recorder_Stop()
        {
            if (_recivers != null)
            {
                foreach (var reciver in _recivers)
                {
                    reciver.Stop();
                }
            }

            _recorder?.WriteComplete();
            _recorder = null;
        }

        #endregion

        #region Replayer


        #endregion
        private Core.ReplayCore _replayer = null;
        private UDPSender _sender = null;
        private void Grid_Initialized(object sender, EventArgs e)
        {
            _sender = new UDPSender();
            _sender.GetSocket().SendBufferSize = 2 * 1024 * 1024;
        }

        private void Replayer_Path_Click(object sender, RoutedEventArgs e)
        {
            string[] paths = null;
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "*.lcl|*.lcl";
            if (openFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)//注意，此处一定要手动引入System.Window.Forms空间，否则你如果使用默认的DialogResult会发现没有OK属性
            {
                MessageBox.Show("Wrong Path!");
            }
            else
            {
                var p = openFileDialog.FileName;
                var pf = p.Remove(p.LastIndexOf('\\'));
                var name = openFileDialog.SafeFileName;
                name = name.Remove(name.LastIndexOf('_'));

                paths = System.IO.Directory.GetFiles(pf, name + "*.lcl");

                Replayer_Path.Content = "Path to File-0.LCL: " + pf;

                _replayer = new Core.ReplayCore(paths);

                var info = _replayer.FileInfo;

                foreach (var point in info.points)
                {
                    Replayer_Map.Items.Add(new Replayer_Map_Item(Replayer_Map.Items.Count, point));
                }

                Replayer_Slider.Maximum = info.totalIndex;
            }
        }

        private void Replayer_Button_Play_Click(object sender, RoutedEventArgs e)
        {
            if (_replayer == null)
            {
                MessageBox.Show("Please select file firstly!");
                return;
            }

            // Check map
            Replayer_Map_Item buff = null;
            Dictionary<IPEndPoint, IPEndPoint> map = new Dictionary<IPEndPoint, IPEndPoint>();
            try
            {
                foreach (Replayer_Map_Item item in Replayer_Map.Items)
                {
                    buff = item;

                    // ignore
                    if (!(bool)item.Valid.IsChecked)
                    {
                        continue;
                    }

                    var point = item.Point.Get_IPEND();

                    map.Add(item._point, point);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Wrong Input!\nCheck the Map List At " + buff?.Num.Text);
                Recorder_Stop();
                return;
            }

            _replayer.Initial(map,
            (Core.ReplayCore.SendInfo msg) =>
            {
                _sender.Send(msg.bytes.ToArray(), msg.point);
            },
            (Core.ReplayCore.ReplayInfo info) =>
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        Replayer_Slider.Value = info.index;
                    });
            });

            _replayer.P();
        }
    }
}
