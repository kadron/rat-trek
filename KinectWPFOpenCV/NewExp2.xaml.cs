using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KinectWPFOpenCV
{
    /// <summary>
    /// Interaction logic for NewExp2.xaml
    /// </summary>
    public partial class NewExp2 : Window
    {
        int cageNum = -1;
        public NewExp2()
        {
            InitializeComponent();
            this.btn_toNew3.IsEnabled = false;
        }

        private void btn_toNew3_Click(object sender, RoutedEventArgs e)
        {
            //Distance of sensor gathered here
            //TODO Add to config
            double dist=this.sld_dist.Value;
            //Close this dialog
            //Open the next dialog
            Cage1 c1 = new Cage1();
            this.Close();
            c1.ShowDialog();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cageNum = this.cmb_cageNum.SelectedIndex + 1;
            this.btn_toNew3.IsEnabled = true;
        }

    }
}
