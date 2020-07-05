using System;
using System.Collections.Generic;
using System.Configuration;
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

using DRRCommon.Network;

namespace DRR_GUI
{
    /// <summary>
    /// Input_IP_Port.xaml 的交互逻辑
    /// </summary>
    public partial class Input_IP_Port : UserControl
    {
        public Input_IP_Port()
        {
            InitializeComponent();
        }

        public IPEndPoint Get_IPEND()
        {
            var ret = new IPEndPoint(IPAddress.Parse(ip.Text), ushort.Parse(port.Text));

            //ip.IsReadOnly = true;
            //port.IsReadOnly = true;

            return ret;
        }

        public IPandPort Get_IPPORT()
        {
            return new IPandPort(ip.Text, ushort.Parse(port.Text));
        }

        public void Edit()
        {
            //ip.IsReadOnly = false;
            //port.IsReadOnly = false;
        }
    }
}
