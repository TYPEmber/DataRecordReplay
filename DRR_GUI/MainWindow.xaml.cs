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
                _record_path = dlg.SelectedPath;

                Recorder_Button_Path.Content = "Path: " + _record_path;
            }
        }

        private string _record_path = Environment.CurrentDirectory;

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

                        var reciver = new UDPReciverWithTime(item.Point.Get_IPPORT());

                        reciver.GetSocket().ReceiveBufferSize = 10 * 1024 * 1024;
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
                    _recorder = new Core.RecordCore(segPara, _record_path, Recorder_FileName.Text, Recorder_Notes.Text, points,
                        infoHandler: (Core.RecordCore.ReplayInfo info) =>
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                Recorder_Info.Text = "Pkg_Count: " + info.count
                                    + " Compress_Rate: " + (info.codedLength * 100.0 / (info.originLength == 0 ? -info.codedLength : info.originLength)).ToString("f2") + "%"
                                    + "\nPkg_Time: " + info.pkgTime;
                            });
                        });

                    // start at here to avoid the lamda func get a null recorder
                    foreach (var reciver in _recivers)
                    {
                        reciver.Start();
                    }
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
        private Core.ReplayCore _replayer = null;
        private UDPSender _sender = null;
        private void Replayer_Grid_Initialized(object sender, EventArgs e)
        {
            _sender = new UDPSender();
            _sender.GetSocket().SendBufferSize = 10 * 1024 * 1024;

            Replayer_Speed.Items.Add("0.01x");
            Replayer_Speed.Items.Add("0.1x");
            Replayer_Speed.Items.Add("0.25x");
            Replayer_Speed.Items.Add("0.5x");
            Replayer_Speed.Items.Add("0.75x");
            Replayer_Speed.Items.Add("1x");
            Replayer_Speed.Items.Add("1.5x");
            Replayer_Speed.Items.Add("2x");
            Replayer_Speed.Items.Add("4x");
            Replayer_Speed.Items.Add("8x");
            Replayer_Speed.Items.Add("16x");
            Replayer_Speed.Items.Add("32x");

            Replayer_Speed.SelectedItem = "1x";
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

                paths = paths.OrderBy(x =>
                {
                    var i = x.LastIndexOf("_") + 1;
                    var j = x.Length - ".lcl".Length;
                    var s = x.Substring(i, j - i);
                    return int.Parse(s);
                }).ToArray();

                Replayer_Path.Content = "Path to File_0.LCL: " + pf;

                _replayer = new Core.ReplayCore(paths);

                var info = _replayer.FileInfo;

                // open a new file
                Replayer_Map.Items.Clear();
                Replayer_Speed.SelectedItem = "1x";
                foreach (var point in info.points)
                {
                    Replayer_Map.Items.Add(new Replayer_Map_Item(Replayer_Map.Items.Count, point));
                }

                Replayer_Notes.Text = info.notes;
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

            // not playing
            // paused or stoped
            if (!_replayer.IsPlaying)
            {
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
                    if (msg.bytes.Length == 0)
                    {

                    }
                    _sender.Send(msg.bytes.ToArray(), msg.point);
                },
                (Core.ReplayCore.ReplayInfo info) =>
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            var finfo = _replayer.FileInfo;
                            if (info.index == finfo.totalIndex - 1)
                            {
                                if ((bool)Replayer_Flag_Loop.IsChecked)
                                {
                                    _replayer.JumpTo(0);
                                }
                            }
                            if (!Replayer_Flag_IsDraging)
                            {
                                Replayer_Slider.Value = info.index;
                            }
                            Replayer_Info.Text = info.time.AddHours(8) + " Progress Percentage: " + (100.0 * (double)info.index / ((double)finfo.totalIndex - 1)).ToString("f2") + "%"
                            + "\nProgress Index: " + info.index + " Total Index: " + finfo.totalIndex + " Cost Time:" + info.pkgCostTime;
                        });
                    });


                _replayer.P();
                Replayer_IsPlaying();
            }
            else
            {
                _replayer.P();
                Replayer_NotPlaying();
            }
        }

        private void Replayer_IsPlaying()
        {
            Replayer_Button_Play.Content = "Pause";
            Replayer_Path.IsEnabled = false;
            foreach (Replayer_Map_Item item in Replayer_Map.Items)
            {
                item.IsEnabled = false;
            }
        }

        private void Replayer_NotPlaying()
        {
            Replayer_Button_Play.Content = "Play";
            Replayer_Path.IsEnabled = true;
            foreach (Replayer_Map_Item item in Replayer_Map.Items)
            {
                item.IsEnabled = true;
            }
        }

        private void Replayer_Slider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_replayer != null)
            {
                _replayer.JumpTo((long)Replayer_Slider.Value);
                Replayer_Flag_IsDraging = false;
            }
            else
            {
                MessageBox.Show("Select File First!");
            }
        }

        private bool Replayer_Flag_IsDraging = false;
        private void Replayer_Slider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_replayer != null)
            {
                Replayer_Flag_IsDraging = true;
            }
            else
            {
                MessageBox.Show("Select File First!");
            }
        }

        private void Replayer_Speed_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_replayer != null)
            {
                var s = (string)Replayer_Speed.SelectedItem;
                var speed = double.Parse(s.Remove(s.Length - 1));
                _replayer.SpeedRate = speed;
            }
            else
            {
                // 1x is default set triggered by actor func
                if ((string)Replayer_Speed.SelectedItem != "1x")
                {
                    MessageBox.Show("Select File First!");
                }
            }
        }
        #endregion

        #region Editor
        private void Editor_Grid_Initialized(object sender, EventArgs e)
        {

        }

        private Core.EditCore _editor = null;
        private void Editor_Path_Click(object sender, RoutedEventArgs e)
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
                _editor_path = p.Remove(p.LastIndexOf('\\'));
                var name = openFileDialog.SafeFileName;
                name = name.Remove(name.LastIndexOf('_'));

                paths = System.IO.Directory.GetFiles(_editor_path, name + "*.lcl");

                paths = paths.OrderBy(x =>
                {
                    var i = x.LastIndexOf("_") + 1;
                    var j = x.Length - ".lcl".Length;
                    var s = x.Substring(i, j - i);
                    return int.Parse(s);
                }).ToArray();

                Editor_Path.Content = "Path to File-0.LCL: " + _editor_path;

                _editor = new Core.EditCore(paths);

                var info = _editor.FileInfo;

                Editor_Clip_To.Text = (info.totalIndex - 1).ToString();
                Editor_Notes.Text = info.notes;
                Editor_Button_Path.Content = "Path: " + _editor_path;
                Editor_FileName.Text = name;
            }
        }

        private string _editor_path = Environment.CurrentDirectory;

        private void Editor_Button_Path_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog { SelectedPath = _editor_path };
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                MessageBox.Show("Wrong Path!");
            }
            else
            {
                _editor_path = dlg.SelectedPath;

                Editor_Button_Path.Content = "Path: " + _editor_path;
            }
        }

        private void Editor_Button_Convert_Click(object sender, RoutedEventArgs e)
        {
            if (_editor == null)
            {
                MessageBox.Show("Please select file firstly!");
                return;
            }

            double[] segPara = new double[] { 0, 0 };
            long start = 0, end = 0;

            try
            {
                start = long.Parse(Editor_Clip_From.Text);
                if (start < 0)
                {
                    throw new Exception("Wrong Start Index Para!");
                }

                end = long.Parse(Editor_Clip_To.Text);
                if (end > _editor.FileInfo.totalIndex - 1 || start >= end)
                {
                    throw new Exception("Wrong End Index Para!");
                }

                double split_mb = double.Parse(Editor_Seg_Size.Text);
                if (split_mb < 0)
                {
                    throw new Exception("Wrong Split Size Para!");
                }

                double split_time = double.Parse(Editor_Seg_Time.Text);
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
                return;
            }

            try
            {
                Editor_Grid.IsEnabled = false;
                MessageBox.Show("Convert Start!");

                _editor.Clip(start, end, segPara, _editor_path, Editor_FileName.Text, Editor_Notes.Text);
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }


            MessageBox.Show("Success!");
            Editor_Grid.IsEnabled = true;
        }

        #endregion
    }
}
