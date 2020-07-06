using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
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
    /// Replayer_Map_Item.xaml 的交互逻辑
    /// </summary>
    public partial class Replayer_Map_Item : UserControl
    {
        public IPEndPoint _point;
        public Replayer_Map_Item(int num, IPEndPoint point)
        {
            InitializeComponent();

            Num.Text = num.ToString();
            IP_Port.Text = point.ToString();
            _point = point;
        }
    }
}
