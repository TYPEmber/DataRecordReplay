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
            Recorder_Listen_List.Items.Add(new Recorder_Listen_Item(Recorder_Listen_List.Items.Count));
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

        private double[] _segPara = new double[] { 0, 0 };
        private Core.RecordCore _recorder = null;
        List<UDPReciverWithTime> _recivers = null;

        private void Recroder_Button_Start_Click(object sender, RoutedEventArgs e)
        {
            bool start_flag = _recorder == null ? true : false;
            // not start
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
                }

                // Initial recorder core
                try
                {
                    _recorder = new Core.RecordCore(_segPara, _path, Recorder_FileName.Text, Recorder_Notes.Text, points);
                }
                catch (Exception ee)
                {

                    throw ee;
                }

                Recorder_Button_Start.Content = "Stop";
            }
            // start
            else
            {
                foreach (var reciver in _recivers)
                {
                    reciver.Stop();
                }
                _recorder.WriteComplete();
                _recorder = null;

                Recorder_Button_Start.Content = "Start";
            }


            Recorder_Listen_List.IsEnabled = !start_flag;
            Recorder_Button_Add_Listener.IsEnabled = !start_flag;
            Recorder_Button_Path.IsEnabled = !start_flag;
            Recorder_FileName.IsEnabled = !start_flag;
            Recorder_Notes.IsEnabled = !start_flag;
        }

        #endregion
    }
}
