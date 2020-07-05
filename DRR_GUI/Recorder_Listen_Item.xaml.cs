using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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
    /// Recorder_Listen_Item.xaml 的交互逻辑
    /// </summary>
    public partial class Recorder_Listen_Item : UserControl
    {
        public Recorder_Listen_Item(int num)
        {
            InitializeComponent();

            Num.Text = num.ToString();
        }
    }
}
